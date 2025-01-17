using FMT.FileTools;
using Frostbite.FileManagers;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FrostbiteSdk.Frostbite.FileManagers
{
    public class ChunkFileManager : ICustomAssetManager, IChunkFileManager
    {
        public static IEnumerable<ChunkAssetEntry> LegacyChunks
        {
            get
            {
                return AssetManager.Instance.EnumerateChunks().Where(x => x.IsLegacy);
            }
        }

        public List<LegacyFileEntry> AddedFileEntries { get; set; }

        public static IChunkFileManager Instance = null;

        private Dictionary<string, LegacyFileEntry> legacyEntries = new Dictionary<string, LegacyFileEntry>();

        private Dictionary<Guid, byte[]> cachedChunks = new Dictionary<Guid, byte[]>();

        private bool cacheMode;
        public AssetManager AssetManager;

        public ChunkFileManager()
        {
            this.AssetManager = AssetManager.Instance;
            Instance = this;
        }

        public ChunkFileManager(AssetManager assetManager)
        {
            this.AssetManager = assetManager;
            Instance = this;
        }

        public virtual void Initialize(ILogger logger)
        {
            logger.Log("Loading legacy files");

            foreach (EbxAssetEntry item in AssetManager.EnumerateEbx("ChunkFileCollector"))
            {
                EbxAsset ebx = AssetManager.GetEbx(item);
                if (ebx != null)
                {
                    dynamic rootObject = ebx.RootObject;
                    dynamic val = rootObject.Manifest;
                    if (!Utilities.PropertyExists(val, "ChunkId"))
                        continue;

                    ChunkAssetEntry chunkAssetEntry = AssetManager.GetChunkEntry(val.ChunkId);
                    if (chunkAssetEntry != null)
                    {
                        Stream chunk = AssetManager.GetChunk(chunkAssetEntry);
                        if (chunk != null)
                        {
                            using (NativeReader nativeReader = new NativeReader(chunk))
                            {
                                uint num = nativeReader.ReadUInt();
                                long num3 = nativeReader.Position = nativeReader.ReadLong();
                                for (uint num4 = 0u; num4 < num; num4++)
                                {
                                    long position = nativeReader.ReadLong();
                                    long position2 = nativeReader.Position;
                                    nativeReader.Position = position;
                                    string text = nativeReader.ReadNullTerminatedString();
                                    nativeReader.Position = position2;
                                    LegacyFileEntry legacyFileEntry = null;
                                    if (!legacyEntries.ContainsKey(text))
                                    {
                                        legacyFileEntry = new LegacyFileEntry();
                                        legacyFileEntry.Name = text;
                                        legacyEntries.Add(text, legacyFileEntry);
                                    }
                                    else
                                    {
                                        legacyFileEntry = legacyEntries[text];
                                    }
                                    LegacyFileEntry.ChunkCollectorInstance chunkCollectorInstance = new LegacyFileEntry.ChunkCollectorInstance();
                                    chunkCollectorInstance.CompressedOffset = nativeReader.ReadLong();
                                    chunkCollectorInstance.CompressedSize = nativeReader.ReadLong();
                                    chunkCollectorInstance.Offset = nativeReader.ReadLong();
                                    chunkCollectorInstance.Size = nativeReader.ReadLong();
                                    chunkCollectorInstance.ChunkId = nativeReader.ReadGuid();
                                    chunkCollectorInstance.Entry = item;
                                    legacyFileEntry.CollectorInstances.Add(chunkCollectorInstance);
                                    legacyFileEntry.ChunkId = chunkCollectorInstance.ChunkId;

                                }
                            }
                        }
                    }
                }
            }
        }

        public void SetCacheModeEnabled(bool enabled)
        {
            cacheMode = enabled;
        }

        public void FlushCache()
        {
            cachedChunks.Clear();
        }

        public virtual IEnumerable<AssetEntry> EnumerateAssets(bool modifiedOnly)
        {
            if (legacyEntries.Count == 0)
                Initialize(new NullLogger());

            var lE = legacyEntries
                .Select(x => x.Value)
                .Where(x =>
                !modifiedOnly
                || x.HasModifiedData);
            return lE;
        }

        public AssetEntry GetAssetEntry(string key)
        {
            if (legacyEntries.ContainsKey(key))
            {
                return legacyEntries[key];
            }
            return null;
        }

        public LegacyFileEntry GetLFEntry(string key)
        {
            if (legacyEntries.ContainsKey(key))
            {
                return legacyEntries[key];
            }
            return null;
        }

        public Stream GetAsset(AssetEntry entry)
        {
            LegacyFileEntry legacyFileEntry = entry as LegacyFileEntry;
            Stream chunkStream = GetChunkStream(legacyFileEntry);
            if (chunkStream == null)
            {
                return null;
            }
            using (NativeReader nativeReader = new NativeReader(chunkStream))
            {
                LegacyFileEntry.ChunkCollectorInstance chunkCollectorInstance = legacyFileEntry.IsModified ? legacyFileEntry.CollectorInstances[0].ModifiedEntry : legacyFileEntry.CollectorInstances[0];
                nativeReader.Position = chunkCollectorInstance.Offset;
                return new MemoryStream(nativeReader.ReadBytes((int)chunkCollectorInstance.Size));
            }
        }

        public ReadOnlySpan<byte> GetAssetAsSpan(AssetEntry entry)
        {
            LegacyFileEntry legacyFileEntry = entry as LegacyFileEntry;
            Stream chunkStream = GetChunkStream(legacyFileEntry);
            if (chunkStream == null)
            {
                return null;
            }
            using (NativeReader nativeReader = new NativeReader(chunkStream))
            {
                LegacyFileEntry.ChunkCollectorInstance chunkCollectorInstance = legacyFileEntry.IsModified ? legacyFileEntry.CollectorInstances[0].ModifiedEntry : legacyFileEntry.CollectorInstances[0];
                nativeReader.Position = chunkCollectorInstance.Offset;
                return nativeReader.ReadBytes((int)chunkCollectorInstance.Size);
            }
        }

        public void ModifyAsset(string key, byte[] data)
        {
            if (legacyEntries.ContainsKey(key))
            {
                LegacyFileEntry legacyFileEntry = legacyEntries[key];
                MemoryStream memoryStream = new MemoryStream();
                using (NativeWriter nativeWriter = new NativeWriter(memoryStream, leaveOpen: true))
                {
                    nativeWriter.Write(data);
                }
                AssetManager.RevertAsset(legacyFileEntry);
                Guid guid = AssetManager.AddChunk(data, GenerateDeterministicGuid(legacyFileEntry), null);
                foreach (LegacyFileEntry.ChunkCollectorInstance collectorInstance in legacyFileEntry.CollectorInstances)
                {
                    collectorInstance.ModifiedEntry = new LegacyFileEntry.ChunkCollectorInstance();
                    ChunkAssetEntry chunkEntry = AssetManager.GetChunkEntry(guid);
                    collectorInstance.ModifiedEntry.ChunkId = guid;
                    collectorInstance.ModifiedEntry.Offset = 0L;
                    collectorInstance.ModifiedEntry.CompressedOffset = 0L;
                    collectorInstance.ModifiedEntry.Size = data.Length;
                    collectorInstance.ModifiedEntry.CompressedSize = chunkEntry.ModifiedEntry.Data.Length;
                    //chunkEntry.ModifiedEntry.AddToChunkBundle = true;
                    chunkEntry.ModifiedEntry.UserData = "legacy;" + legacyFileEntry.Name;
                    legacyFileEntry.LinkAsset(chunkEntry);
                    collectorInstance.Entry.LinkAsset(legacyFileEntry);
                }
                legacyFileEntry.IsDirty = true;
                memoryStream.Dispose();
            }
        }

        public void ModifyAsset(string key, byte[] data, bool rebuildChunk = false)
        {
            ModifyAsset(key, data);
        }
        private Stream GetChunkStream(LegacyFileEntry lfe)
        {
            if (cacheMode)
            {
                if (!cachedChunks.ContainsKey(lfe.ChunkId))
                {
                    using (Stream stream = AssetManager.GetChunk(AssetManager.GetChunkEntry(lfe.ChunkId)))
                    {
                        if (stream == null)
                        {
                            return null;
                        }
                        cachedChunks.Add(lfe.ChunkId, ((MemoryStream)stream).ToArray());
                    }
                }
                return new MemoryStream(cachedChunks[lfe.ChunkId]);
            }
            return AssetManager.GetChunk(AssetManager.GetChunkEntry(lfe.ChunkId));
        }

        public void OnCommand(string command, params object[] value)
        {
            if (!(command == "SetCacheModeEnabled"))
            {
                if (command == "FlushCache")
                {
                    FlushCache();
                }
            }
            else
            {
                SetCacheModeEnabled((bool)value[0]);
            }
        }

        public Guid GenerateDeterministicGuid(LegacyFileEntry lfe)
        {
            ulong num = Murmur2.HashString64(lfe.Filename, 18532uL);
            ulong value = Murmur2.HashString64(lfe.Path, 18532uL);
            int num2 = 1;
            Guid guid = Guid.Empty;
            do
            {
                using (NativeWriter nativeWriter = new NativeWriter(new MemoryStream()))
                {
                    nativeWriter.Write(value);
                    nativeWriter.Write((ulong)((long)num ^ num2));
                    byte[] array = ((MemoryStream)nativeWriter.BaseStream).ToArray();
                    array[15] = 1;
                    guid = new Guid(array);
                }
                num2++;
            }
            while (AssetManager.GetChunkEntry(guid) != null);
            return guid;
        }

        public void AddAsset(string key, LegacyFileEntry lfe)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
        }

        public void ResetAndDispose()
        {
        }

        public List<LegacyFileEntry> ModifyAssets(Dictionary<string, byte[]> data, bool rebuildChunk)
        {
            throw new NotImplementedException();
        }

        public void DuplicateAsset(string name, LegacyFileEntry originalAsset)
        {
            throw new NotImplementedException();
        }

        public void RevertAsset(AssetEntry assetEntry)
        {
            throw new NotImplementedException();
        }
    }
}
