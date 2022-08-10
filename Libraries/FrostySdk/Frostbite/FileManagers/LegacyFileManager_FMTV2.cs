﻿using FrostbiteSdk.Frostbite.FileManagers;
using Frosty.Hash;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Frostbite.FileManagers
{
	public class LegacyFileManager_FMTV2 : ILegacyFileManager, ICustomAssetManager
	{

		public List<LegacyFileEntry> AddedFileEntries { get; set; }

		public List<LegacyFileEntry> OriginalLegacyEntries = new List<LegacyFileEntry>();

		public Dictionary<string, LegacyFileEntry> LegacyEntries = new Dictionary<string, LegacyFileEntry>();

		public List<ChunkAssetEntry> ModifiedChunks = new List<ChunkAssetEntry>();

		Dictionary<Guid, List<LegacyFileEntry>> LegacyChunksToParent = new Dictionary<Guid, List<LegacyFileEntry>>();

		public AssetManager AssetManager => AssetManager.Instance;

		public class ChunkBatch
		{
			public uint UnkCount1 { get; set; }
			public long Offset1_Unk { get; set; }
			public int NumberOfFiles { get; set; }

			public long Offset2_Files { get; set; }

			public uint BootableItemCount { get; set; }


			public byte[] Initial64Data { get; set; }

			public EbxAssetEntry EbxAssetEntry { get; set; }

			public ChunkAssetEntry ChunkAssetEntry { get; set; }

			public IEnumerable<LegacyFileEntry> BatchLegacyFiles
			{
				get
				{
					var lam = (LegacyFileManager_FMTV2)AssetManager.Instance.GetLegacyAssetManager();
					if(lam != null)
                    {
						return lam.LegacyEntries.Values.Where(x => x.ParentGuid == ChunkAssetEntry.Id);
					}
					return null;
				}
			}

			public ChunkBatch()
			{
			}

			public override string ToString()
			{
				return ChunkAssetEntry != null ? ChunkAssetEntry.Id.ToString() : base.ToString();
			}

			public override bool Equals(object obj)
			{
				return base.Equals(obj);
			}

			public override int GetHashCode()
			{
				return base.GetHashCode();
			}

			public byte[] WriteChunkBatch()
			{
				return null;
			}

			public Dictionary<Guid, List<LegacyFileEntry>> ChunkGroupsInBatch
			{
				get
				{
					return BatchLegacyFiles.GroupBy(x => x.ChunkId).ToDictionary(x => x.Key, x => x.ToList());
				}
			}

			public Dictionary<Guid, List<LegacyFileEntry>> ChunkGroupsInBatchModified
			{
				get
				{
					return BatchLegacyFiles
						.Where(x => x.ModifiedEntry != null && x.ModifiedEntry.ChunkId.HasValue)
						.GroupBy(x => x.ModifiedEntry.ChunkId.Value)
						.ToDictionary(x => x.Key, x => x.ToList());
				}
			}

			public Dictionary<Guid, ChunkAssetEntry> ChunkAssetEntriesInBatch
			{
				get
				{
					return BatchLegacyFiles.GroupBy(x => x.ChunkId).ToDictionary(x => x.Key, x => AssetManager.Instance.GetChunkEntry(x.Key));
				}
			}

			public LegacyFileEntry GetLegacyFileByName(string name)
			{
				return BatchLegacyFiles.FirstOrDefault(x => x.Name == name);
			}

			public int EndOfStrings { get; set; }
			public byte[] EndData { get; set; }
			public long BootableItemOffset { get; set; }
			public uint LinkedChunkCount { get; set; }
			public long LinkedChunkOffset { get; set; }
			public EbxAsset EbxAsset { get; set; }

			public List<ChunkAssetEntry> CompressedItemChunks = new List<ChunkAssetEntry>();

			public List<BootableItem> BootableItems = new List<BootableItem>();

			public List<ChunkAssetEntry> LinkedChunks = new List<ChunkAssetEntry>();

			public List<UnknownItem> UnknownItems = new List<UnknownItem>();

			public List<int> BottomUnknownOffsets = new List<int>();

			public class BootableItem
			{
				public long NameOffset;
				public string Name;
				public uint Index;
				public uint Active;
			}

			public class UnknownItem
            {
				public long Offset { get; set; }
                public string Name { get; set; }
            }
		}

		/// <summary>
		/// Chunk Batches are the entire batch of chunks and locations of the chunks. Each is an EBX
		/// </summary>
		public List<ChunkBatch> ChunkBatches = new List<ChunkBatch>();

		public LegacyFileManager_FMTV2()
		{
		}

		public virtual void Initialize(ILogger logger)
		{
			logger.Log("Loading legacy files");
            AddedFileEntries = new List<LegacyFileEntry>();

			//ChunkBatches = new List<ChunkBatch>();
			//LegacyEntries = new Dictionary<string, LegacyFileEntry>();

			var chAttempt1 = AssetManager.Instance.GetChunkEntry(Guid.Parse("FA40D5B2-358C-B8BF-DACA-B7EA34EF1F45"));
			var chAttempt2 = AssetManager.Instance.GetChunkEntry(Guid.Parse("FA40D5B2-358C-B8BF-DACA-B7EA34EF1F45"));

            foreach (EbxAssetEntry item in AssetManager.EnumerateEbx("ChunkFileCollector"))
			{
				GetChunkAssetForEbx(item, out ChunkAssetEntry chunkAssetEntry, out EbxAsset ebxAsset);
				if (chunkAssetEntry == null)
					continue;

				chunkAssetEntry.IsLegacy = true;

				MemoryStream chunk = new MemoryStream();
				GetChunkStreamForEbx(item).CopyTo(chunk);
				if (chunk != null)
				{
					using (NativeReader nativeReader = new NativeReader(chunk))
					{
						File.WriteAllBytes("_debug_legacy_" + item.Name.Replace(@"/","_") + ".dat", chunk.ToArray());
						nativeReader.Position = 0;

						var chunkBatch = new ChunkBatch()
						{
							EbxAssetEntry = item,
							EbxAsset = ebxAsset,
							ChunkAssetEntry = chunkAssetEntry,
							UnkCount1 = nativeReader.ReadUInt(), // 0
							Offset1_Unk = nativeReader.ReadLong(), // 4
							NumberOfFiles = nativeReader.ReadInt(), // 12
							Offset2_Files = nativeReader.ReadLong(), // 16
							BootableItemCount = nativeReader.ReadUInt(), // 24 
							BootableItemOffset = nativeReader.ReadLong(), // 28
							LinkedChunkCount = nativeReader.ReadUInt(), // 36
							LinkedChunkOffset = nativeReader.ReadLong() // 40
						};
						//nativeReader.Position = 0;
						//chunkBatch.Initial64Data = nativeReader.ReadBytes((int)chunkBatch.FileFirstPosition);
						nativeReader.Position = chunkBatch.Offset1_Unk;
						for (uint index = 0u; index < chunkBatch.UnkCount1; index++)
						{
							var unkLong = nativeReader.ReadLong();
							var name = string.Empty; 
							chunkBatch.UnknownItems.Add(new ChunkBatch.UnknownItem() { Offset = unkLong, Name = name });
						}

						chunkBatch.EndOfStrings = 0;
						nativeReader.Position = chunkBatch.Offset2_Files;
						for (uint index = 0u; index < chunkBatch.NumberOfFiles; index++)
						{
							LegacyFileEntry legacyFileEntry = new LegacyFileEntry();
							legacyFileEntry.FileNameInBatchOffset = nativeReader.ReadLong(); // 0 - 8
							
							//legacyFileEntry.BatchOffset = positionOfItem;
							legacyFileEntry.ParentGuid = chunkAssetEntry.Id;

							legacyFileEntry.CompressedOffsetPosition = nativeReader.Position;
							legacyFileEntry.CompressedOffset = nativeReader.ReadLong();
							legacyFileEntry.CompressedOffsetStart = legacyFileEntry.CompressedOffset;
							legacyFileEntry.CompressedSizePosition = nativeReader.Position;
							legacyFileEntry.CompressedOffsetEnd = nativeReader.ReadLong();
							legacyFileEntry.CompressedSize = legacyFileEntry.CompressedOffsetEnd - legacyFileEntry.CompressedOffset;

							legacyFileEntry.ActualOffsetPosition = (int)nativeReader.Position;
							legacyFileEntry.ExtraData = new AssetExtraData() { DataOffset = (uint)nativeReader.ReadLong() };
							legacyFileEntry.ActualSizePosition = (int)nativeReader.Position;
							legacyFileEntry.Size = nativeReader.ReadLong();

							legacyFileEntry.ChunkIdPosition = nativeReader.Position;
							var chunkId = nativeReader.ReadGuid();
							if (chunkId != Guid.Empty)
							{
								var cha = AssetManager.Instance.GetChunkEntry(chunkId);
								cha.IsLegacy = true;

								legacyFileEntry.ChunkId = chunkId;

								nativeReader.Position = legacyFileEntry.FileNameInBatchOffset;
								string name = nativeReader.ReadNullTerminatedString();
								//if (nativeReader.Position > chunkBatch.EndOfStrings)
								//	chunkBatch.EndOfStrings = (int)nativeReader.Position;

								if (!LegacyEntries.ContainsKey(name))
								{
									legacyFileEntry.Name = name;
									LegacyEntries.Add(name, legacyFileEntry);
								}
								else
								{
									legacyFileEntry = LegacyEntries[name];
								}
							}

							//if (name.Contains("playervalues.ini", StringComparison.OrdinalIgnoreCase))
							//{
							//	var chunkStreamTestPV = GetChunkStream(legacyFileEntry);
							//	var assetStreamTestPV = GetAsset(legacyFileEntry);

							//}

							nativeReader.Position = chunkBatch.Offset2_Files + ((8 + 8 + 8 + 8 + 8 + 16) * (index + 1));



						}

						if (ProfilesLibrary.IsFIFA21DataVersion())
						{
							nativeReader.Position = chunkBatch.BootableItemOffset;
							for (uint j = 0u; j < chunkBatch.BootableItemCount; j++)
							{
								long nameOffset3 = nativeReader.ReadLong();
								var unknumber1 = nativeReader.ReadUInt();
								var unknumber2 = nativeReader.ReadUInt();
								nativeReader.Position = nameOffset3;
								var unkName2 = nativeReader.ReadNullTerminatedString();
								if (nativeReader.Position > chunkBatch.EndOfStrings)
									chunkBatch.EndOfStrings = (int)nativeReader.Position;

								nativeReader.Position = chunkBatch.BootableItemOffset + (16 * (j + 1));


								ChunkBatch.BootableItem bootableItem = new ChunkBatch.BootableItem()
								{
									NameOffset = nameOffset3,
									Name = unkName2,
									Index = unknumber1,
									Active = unknumber2
								};
								chunkBatch.BootableItems.Add(bootableItem);
							}

							nativeReader.Position = chunkBatch.LinkedChunkOffset;
							for (uint i = 0u; i < chunkBatch.LinkedChunkCount; i++)
							{
								var chunkOriginalSize = nativeReader.ReadLong();
								var chunkGuid = nativeReader.ReadGuid();
								var otherChunk = AssetManager.Instance.GetChunkEntry(chunkGuid);
								otherChunk.OriginalSize = chunkOriginalSize;
								chunkBatch.LinkedChunks.Add(otherChunk);
							}

							nativeReader.Position = chunkBatch.EndOfStrings;
                            nativeReader.Pad(16);
                            double numberOfEndUnkItems = ((double)nativeReader.Length - (double)nativeReader.Position) / 4;
							for (int index = 0; index < numberOfEndUnkItems; index++) 
							{
								chunkBatch.BottomUnknownOffsets.Add(nativeReader.ReadInt());
							}
						}

						ChunkBatches.Add(chunkBatch);


					}
				}
			}

			logger.Log($"Loaded {LegacyEntries.Count} legacy files");
			LegacyFileManager.Instance = this;
		}

		public void WriteAllLegacy(bool assertEquivalent = false)
		{
			// A chunk batch corresponds to each Ebx in the CFC
			foreach (ChunkBatch chunkBatch in ChunkBatches)
			{
				var originalData = (AssetManager.Instance.GetChunk(chunkBatch.ChunkAssetEntry) as MemoryStream).ToArray();

				MemoryStream ms_newChunkBatch = new MemoryStream();
				using (NativeWriter nw = new NativeWriter(ms_newChunkBatch, true))
				{
					Debug.WriteLine("Processing Legacy Item: " + chunkBatch.EbxAssetEntry.Name);
					nw.Write(chunkBatch.UnkCount1);
					nw.Write(chunkBatch.Offset1_Unk);
					var oldNumberOfFiles = chunkBatch.NumberOfFiles;
					var newNumberOfFiles = (uint)chunkBatch.BatchLegacyFiles.Count();
					nw.Write(newNumberOfFiles);
					nw.Write(chunkBatch.Offset2_Files);
					nw.Write(chunkBatch.BootableItemCount);
					nw.Write(chunkBatch.BootableItemOffset);
					nw.Write(chunkBatch.LinkedChunkCount);
					nw.Write(chunkBatch.LinkedChunkOffset);

					foreach (var item in chunkBatch.UnknownItems)
					{
						nw.Write(item.Offset);
					}

					chunkBatch.Offset2_Files = nw.Position;
					// Quick write before Strings
					foreach (var item in chunkBatch.BatchLegacyFiles)
					{
						if (item.ModifiedEntry != null)
						{

						}
						if (item.Name.Contains("p258487"))
						{

						}
						nw.Write((ulong)item.FileNameInBatchOffset);
						// compressed
						nw.Write(item.ModifiedEntry != null && item.ModifiedEntry.CompressedOffset.HasValue ? (ulong)item.ModifiedEntry.CompressedOffset : (ulong)item.CompressedOffset);
						// compressed end
						nw.Write(item.ModifiedEntry != null && item.ModifiedEntry.CompressedOffsetEnd.HasValue ? (ulong)item.ModifiedEntry.CompressedOffsetEnd : (ulong)item.CompressedOffsetEnd);
						// data offset
						nw.Write(item.ModifiedEntry != null && item.ModifiedEntry.NewOffset.HasValue ? (ulong)item.ModifiedEntry.NewOffset : (ulong)item.ExtraData.DataOffset);
						// data size
						nw.Write(item.ModifiedEntry != null && item.ModifiedEntry.Data != null ? (ulong)item.ModifiedEntry.Data.Length : (ulong)item.Size);
						nw.Write(item.ModifiedEntry != null && item.ModifiedEntry.ChunkId.HasValue ? item.ModifiedEntry.ChunkId.Value : item.ChunkId);
					}

					nw.WritePadding(16);

					chunkBatch.BootableItemOffset = nw.Position;
					if (chunkBatch.BootableItems.Any())
					{
						foreach (var item in chunkBatch.BootableItems)
						{
							nw.Write(item.NameOffset);
							nw.Write(item.Index);
							nw.Write(item.Active);
						}


					}

					chunkBatch.LinkedChunkOffset = nw.Position;
					if (chunkBatch.LinkedChunks.Any())
					{
						foreach (var item in chunkBatch.LinkedChunks)
						{
							nw.Write((long)item.OriginalSize);
							nw.Write(item.Id);
						}
					}

					nw.WritePadding(16);
					nw.WriteNullTerminatedString("Source/runtime/fifa/pc64");
					nw.WriteNullTerminatedString("Source/runtime/fifa/cmn");

					// Strings
					foreach (var item in chunkBatch.BatchLegacyFiles)
					{
						item.FileNameInBatchOffset = nw.Position;
						nw.WriteNullTerminatedString(item.Name);
					}
					foreach (var item in chunkBatch.BootableItems)
					{
						item.NameOffset = nw.Position;
						nw.WriteNullTerminatedString(item.Name);
					}

					nw.WritePadding(16);
					var endOfStrings = nw.Position;

					nw.Position = chunkBatch.BootableItemOffset;
					foreach (var item in chunkBatch.BootableItems)
					{
						nw.Write(item.NameOffset);
						nw.Write(item.Index);
						nw.Write(item.Active);
					}


					nw.Position = 0;
					nw.Write(chunkBatch.UnkCount1);
					nw.Write(chunkBatch.Offset1_Unk);
					nw.Write(newNumberOfFiles);
					nw.Write(chunkBatch.Offset2_Files);
					nw.Write(chunkBatch.BootableItemCount);
					nw.Write(chunkBatch.BootableItemOffset);
					nw.Write(chunkBatch.LinkedChunkCount);
					nw.Write(chunkBatch.LinkedChunkOffset);

					nw.Position = nw.Length;
					foreach (var item in chunkBatch.BottomUnknownOffsets)
					{
						nw.Write(item);
					}

					nw.Position = chunkBatch.Offset2_Files;
					foreach (var item in chunkBatch.BatchLegacyFiles)
					{
						if (item.ModifiedEntry != null)
						{

						}
						if (item.Name.Contains("p258487"))
						{

						}
						nw.Write((ulong)item.FileNameInBatchOffset);
						nw.Position += 8;
						nw.Position += 8;
						nw.Position += 8;
						nw.Position += 8;
						nw.Position += 16;
						
					}
				}
				
				
				File.WriteAllBytes("_debug_legacy_old_" + chunkBatch.EbxAssetEntry.Name.Replace(@"/", "_") + ".dat", originalData);
				File.WriteAllBytes("_debug_legacy_new_" + chunkBatch.EbxAssetEntry.Name.Replace(@"/", "_") + ".dat", ms_newChunkBatch.ToArray());


				AssetManager.Instance.ModifyChunk(chunkBatch.ChunkAssetEntry.Id, ms_newChunkBatch.ToArray(), null, ProfilesLibrary.GetCompressionType(ProfilesLibrary.CompTypeArea.Legacy));
				//AssetManager.Instance.ModifyEbx(chunkBatch.EbxAssetEntry.Name, chunkBatch.EbxAsset);
			}
		}

		public static EbxAsset GetEbxAssetForEbx(EbxAssetEntry ebxAssetEntry)
		{
			EbxAsset ebx = AssetManager.Instance.GetEbx(ebxAssetEntry);
			ebx.ParentEntry = ebxAssetEntry;
			return ebx;
		}

		public static void GetChunkAssetForEbx(EbxAssetEntry ebxAssetEntry, out ChunkAssetEntry chunkAssetEntry, out EbxAsset ebxAsset)
		{
			chunkAssetEntry = null;
			ebxAsset = AssetManager.Instance.GetEbx(ebxAssetEntry);
			if (ebxAsset != null)
			{
				dynamic rootObject = ebxAsset.RootObject;
				if (rootObject != null)
				{
					dynamic val = rootObject.Manifest;
					chunkAssetEntry = AssetManager.Instance.GetChunkEntry(val.ChunkId);
				}
			}
		}

		public Stream GetChunkStreamForEbx(EbxAssetEntry ebxAssetEntry)
		{
			GetChunkAssetForEbx(ebxAssetEntry, out ChunkAssetEntry chunkAssetEntry, out EbxAsset asset);
			if (chunkAssetEntry != null)
				return AssetManager.GetChunk(chunkAssetEntry);

			return null;
		}

		public void SetCacheModeEnabled(bool enabled)
		{
			//cacheMode = enabled;
		}

		public void FlushCache()
		{
			//cachedChunks.Clear();
		}

		public IEnumerable<AssetEntry> EnumerateAssets(bool modifiedOnly)
		{
			var lE = LegacyEntries.Where(x =>
				!modifiedOnly
				|| x.Value.HasModifiedData);
			return lE.Select(x => x.Value);
		}

		public AssetEntry GetAssetEntry(string key)
		{
			LegacyFileEntry legacyFileEntry = null;
			if (LegacyEntries.ContainsKey(key))
			{
				legacyFileEntry = LegacyEntries[key];
			}
			else if (LegacyEntries.ContainsKey(key.ToLower()))
			{
				legacyFileEntry = LegacyEntries[key];
			}
			else if (Guid.TryParse(key, out Guid keyGuid) && LegacyEntries.Any(x=> x.Value.ChunkId == Guid.Parse(key)))
			{
				legacyFileEntry = LegacyEntries.First(x => x.Value.ChunkId == keyGuid).Value;
			}
			return legacyFileEntry;
		}

		public LegacyFileEntry GetLFEntry(string key)
		{
			return GetAssetEntry(key) as LegacyFileEntry;
		}

		long? testDO;

		public Stream GetAsset(AssetEntry entry)
		{
			LegacyFileEntry legacyFileEntry = entry as LegacyFileEntry;
			if (legacyFileEntry == null)
				return null;

			if (legacyFileEntry.ModifiedEntry != null && legacyFileEntry.ModifiedEntry.Data != null)
			{
				return new MemoryStream(legacyFileEntry.ModifiedEntry.Data);
			}

			Stream chunkStream = GetChunkStream(legacyFileEntry);
			if (chunkStream == null)
			{
				return null;
			}
			using (NativeReader nativeReader = new NativeReader(chunkStream))
			{
				// 
				if (legacyFileEntry.Name.Contains("voicecommand"))
				{
					if (!testDO.HasValue)
						testDO = legacyFileEntry.ExtraData.DataOffset;
					else if (testDO.Value != legacyFileEntry.ExtraData.DataOffset)
						throw new Exception("these should be the same");

				}
				nativeReader.Position = legacyFileEntry.ExtraData.DataOffset;
				return new MemoryStream(nativeReader.ReadBytes((int)legacyFileEntry.Size));
			}
		}

		//public void RebuildOneFile(LegacyFileEntry legacyFileEntry)
		//{
		//	AssetManager.ModifyChunk(legacyFileEntry.ChunkId, legacyFileEntry.ModifiedEntry.Data);
		//}


		public virtual List<LegacyFileEntry> RebuildEntireChunk(Guid chunkId, List<LegacyFileEntry> replaceFileEntries, List<LegacyFileEntry> newFileEntries = null)
		{

			replaceFileEntries.ForEach(x => { if(x.ModifiedEntry != null && x.ModifiedEntry.ChunkId.HasValue) x.ModifiedEntry.ChunkId = null; });

            CompressionType compressionType = ProfilesLibrary.GetCompressionType(ProfilesLibrary.CompTypeArea.Legacy);

            // get the chunk batch (the main batch with offsets etc)
            ChunkBatch chunkBatch = ChunkBatches.FirstOrDefault(x => x.ChunkAssetEntry.Id == chunkId);
			if (chunkBatch != null)
			{

				var edited = replaceFileEntries.GroupBy(x => x.ChunkId).ToDictionary(x => x.Key, x => x.ToList());

				var edited2 = chunkBatch.BatchLegacyFiles.Where(x => x.ModifiedEntry != null).GroupBy(x => x.ChunkId).ToDictionary(x => x.Key, x => x.ToList());
				foreach (var gItem in edited)
				{
					//BuildNewChunkForLegacyItem(gItem);


					// Easily handle Singular Chunk
					if (gItem.Value.Count == 1 && chunkBatch.ChunkGroupsInBatch[gItem.Value.First().ChunkId].Count == 1)
					{
						var chunkEntry = AssetManager.Instance.GetChunkEntry(gItem.Key);
						var legacyItem = gItem.Value.First();
						legacyItem.ModifiedEntry.NewOffset = 0;
						legacyItem.ModifiedEntry.Size = legacyItem.ModifiedEntry.Data.Length;
						//legacyItem.ModifiedEntry.CompressedOffset = 0;
						//legacyItem.ModifiedEntry.CompressedOffsetEnd = 0;


						ModifiedChunks.Add(chunkEntry);

						AssetManager.Instance.ModifyChunk(gItem.Key, legacyItem.ModifiedEntry.Data, null, compressionType);
						chunkEntry.ModifiedEntry.AddToChunkBundle = true;
						chunkEntry.ModifiedEntry.AddToTOCChunks = true;
					}
					// Otherwise handle Chunk Batch
					else
					{
						var batchGuid = gItem.Key;
						foreach (var gItem2 in gItem.Value)
						{

							var groupOfLegacyFilesWithOnlyOne = chunkBatch.ChunkGroupsInBatch
								.Where(x => !chunkBatch.ChunkGroupsInBatchModified.ContainsKey(x.Key))
								.Where(x => x.Value.Count == 1)
								.First();

							// other way of doing it (add to another file)
							gItem2.ModifiedEntry.ChunkId = groupOfLegacyFilesWithOnlyOne.Key;
							var groupOfLegacyFiles = chunkBatch.ChunkGroupsInBatch.First(x => x.Key == groupOfLegacyFilesWithOnlyOne.Key).Value;
							groupOfLegacyFiles.Add(gItem2);
							batchGuid = groupOfLegacyFilesWithOnlyOne.Key;

							// standard way of doing it
							//var groupOfLegacyFiles = chunkBatch.ChunkGroupsInBatch.First(x => x.Key == batchGuid).Value;
							var groupChunkEntry = AssetManager.Instance.GetChunkEntry(batchGuid);
							var groupChunk = AssetManager.Instance.GetChunk(groupChunkEntry);

							var ms_newChunkGroup = new MemoryStream();
							using (var nw_newChunkGroup = new NativeWriter(ms_newChunkGroup, leaveOpen: true))
							{
								using (var nr_GroupChunk = new NativeReader(groupChunk))
								{
									long lastOffset = 0;
									foreach (var itemInChunkGroup in groupOfLegacyFiles)
									{
										byte[] d = null;

										if (itemInChunkGroup.HasModifiedData)
										{
											itemInChunkGroup.ModifiedEntry.Size = itemInChunkGroup.ModifiedEntry.Data.Length;
											d = itemInChunkGroup.ModifiedEntry.Data;
										}
										else
										{
											nr_GroupChunk.Position = itemInChunkGroup.ExtraData.DataOffset;
											d = nr_GroupChunk.ReadBytes((int)itemInChunkGroup.Size);
											itemInChunkGroup.ModifiedEntry = new ModifiedAssetEntry();
										}
										itemInChunkGroup.ModifiedEntry.NewOffset = lastOffset;
										itemInChunkGroup.ModifiedEntry.Size = d.Length;
										lastOffset += d.Length;

										//var compressedData = Utils.CompressFile(d, compressionOverride: compressionType);
										itemInChunkGroup.ModifiedEntry.CompressedOffset = 0;
										nw_newChunkGroup.Write(d);
										itemInChunkGroup.ModifiedEntry.CompressedOffsetEnd = d.Length;


									}

								}
							}
							// Modify the Chunk
							ms_newChunkGroup.Position = 0;
							byte[] newChunkGroupData = new NativeReader(ms_newChunkGroup).ReadToEnd();


							var oldEntry = AssetManager.Instance.GetChunkEntry(batchGuid);
							ModifiedChunks.Add(oldEntry);

							//AssetManager.Instance.ModifyChunk(batchGuid, newChunkGroupData, compressionOverride: compressionType);
							//oldEntry.ModifiedEntry.AddToChunkBundle = true;
							//oldEntry.ModifiedEntry.AddToTOCChunks = true;

							var newChunkAlreadyCompressed = CompressChunkGroup(ms_newChunkGroup, groupOfLegacyFiles, compressionType);
							groupChunkEntry.ModifiedEntry = new ModifiedAssetEntry()
							{
								Data = newChunkAlreadyCompressed.newChunk,
								Size = newChunkAlreadyCompressed.newChunk.Length,
								LogicalSize = (uint)newChunkAlreadyCompressed.newChunk.Length,
								OriginalSize = newChunkAlreadyCompressed.newChunk.Length,
								//Sha1 = AssetManager.Instance.GenerateSha1(newChunkAlreadyCompressed.newChunk),
								Sha1 = oldEntry.Sha1,
								AddToChunkBundle = true,
								AddToTOCChunks = true
							};
						}


						//                  var unmodifiedfiles = groupOfLegacyFiles.Where(x => x.ModifiedEntry == null);
						//var zeroCompLegacyFiles = groupOfLegacyFiles.Where(x => x.ModifiedEntry != null && x.ModifiedEntry.CompressedOffset == 0);

						//groupChunkEntry.IsDirty = true;
						//groupChunkEntry.ModifiedEntry.AddToChunkBundle = true;

						//AssetManager.Instance.ModifyChunk(gItem.Key, newChunkGroupData, compressionOverride: compressionType);


					}

				}




                byte[] oldBatchData;

                using (var nrOldBatch = new NativeReader(AssetManager.Instance.GetChunk(chunkBatch.ChunkAssetEntry)))
                {
                    oldBatchData = nrOldBatch.ReadToEnd();
                }

                var msNewBatch = new MemoryStream();
                using (var nwNewBatch = new NativeWriter(msNewBatch, leaveOpen: true))
                {
                    nwNewBatch.Write(oldBatchData);
                    foreach (var lfe in chunkBatch.BatchLegacyFiles)
                    {
                        nwNewBatch.Position = lfe.ActualOffsetPosition;
                        if (lfe.ModifiedEntry != null && lfe.ModifiedEntry.NewOffset.HasValue)
                            nwNewBatch.Write((long)lfe.ModifiedEntry.NewOffset.Value);
                        else
                            nwNewBatch.Write((long)lfe.ExtraData.DataOffset);

                        nwNewBatch.Position = lfe.ActualSizePosition;
                        if (lfe.ModifiedEntry != null && lfe.ModifiedEntry.Data != null)
                            nwNewBatch.Write((long)lfe.ModifiedEntry.Data.Length);
                        else
                            nwNewBatch.Write((long)lfe.Size);

                        // Compressed Offset
                        nwNewBatch.Position = lfe.CompressedOffsetPosition;
                        if (lfe.ModifiedEntry != null && lfe.ModifiedEntry.CompressedOffset != null)
                            nwNewBatch.Write((long)lfe.ModifiedEntry.CompressedOffset);
                        else
                            nwNewBatch.Write((long)lfe.CompressedOffset);

                        // Compressed Size
                        nwNewBatch.Position = lfe.CompressedSizePosition;
                        if (lfe.ModifiedEntry != null && lfe.ModifiedEntry.CompressedOffsetEnd != null)
                            nwNewBatch.Write((long)lfe.ModifiedEntry.CompressedOffsetEnd);
                        else
                            nwNewBatch.Write((long)lfe.CompressedOffsetEnd);

                        nwNewBatch.Position = lfe.ChunkIdPosition;
                        if (lfe.ModifiedEntry != null && lfe.ModifiedEntry.ChunkId.HasValue)
                            nwNewBatch.Write(lfe.ModifiedEntry.ChunkId.Value);
                        else
                            nwNewBatch.Write(lfe.ChunkId);

                    }

                }
                msNewBatch.Position = 0;

                var newBatchData = new NativeReader(msNewBatch).ReadToEnd();

                ModifiedChunks.Add(AssetManager.Instance.GetChunkEntry(chunkBatch.ChunkAssetEntry.Id));

                AssetManager.Instance.ModifyChunk(chunkBatch.ChunkAssetEntry.Id, newBatchData, compressionOverride: compressionType);
                var cE = AssetManager.Instance.GetChunkEntry(chunkBatch.ChunkAssetEntry.Id);
                cE.ModifiedEntry.OriginalSize = msNewBatch.Length;
                cE.ModifiedEntry.LogicalSize = Convert.ToUInt32(Utils.CompressFile(newBatchData, null, ResourceType.Invalid, compressionType).Length);
                cE.ModifiedEntry.Size = cE.ModifiedEntry.LogicalSize;
                cE.ModifiedEntry.AddToChunkBundle = true;
                cE.ModifiedEntry.AddToTOCChunks = true;

                msNewBatch.Close();
                msNewBatch.Dispose();

                var allFiles = new List<LegacyFileEntry>();
				if (replaceFileEntries != null)
					allFiles.AddRange(replaceFileEntries);
				if (newFileEntries != null)
					allFiles.AddRange(newFileEntries);
				return allFiles;
			}

			return null;
		}


		private static void BuildNewChunkForLegacyItem(KeyValuePair<Guid, List<LegacyFileEntry>> gItem)
        {
            foreach (var i in gItem.Value)
            {
                var newGuid = GenerateDeterministicGuid(i);
                i.ChunkId = newGuid;
                i.ModifiedEntry.NewOffset = 0;
                i.ModifiedEntry.CompressedOffset = 0;
                i.ModifiedEntry.CompressedOffsetEnd = 0;
                i.ModifiedEntry.AddToChunkBundle = true;
                i.ModifiedEntry.AddToTOCChunks = true;
            }
        }

        public void ModifyAsset(string key, byte[] data)
		{
			ModifyAsset(key, data, true);
		}

		public void ModifyAsset(string key, byte[] data, bool rebuildChunk = true)
		{
			if (LegacyEntries.ContainsKey(key))
			{
				LegacyFileEntry legacyFileEntry = LegacyEntries[key];

				if (!OriginalLegacyEntries.Contains(legacyFileEntry))
					OriginalLegacyEntries.Add(legacyFileEntry);

				legacyFileEntry.ModifiedEntry = new ModifiedAssetEntry()
				{
					Data = data,
					AddToTOCChunks = true,
					AddToChunkBundle = true,
				};

				legacyFileEntry.IsDirty = true;

				//if (rebuildChunk)
				//{
				//	ChunkBatches.ForEach(x => x.ChunkGroupsInBatchModified.Clear());
				//	RebuildEntireChunk(legacyFileEntry.ParentGuid, new List<LegacyFileEntry>() { legacyFileEntry }, null);
				//}
			}
		}

		public List<LegacyFileEntry> ModifyAssets(Dictionary<string, byte[]> data, bool rebuildChunk = true)
		{
			List<LegacyFileEntry> filesEdited = new List<LegacyFileEntry>();

			foreach (var dpi in data.Where(x => LegacyEntries.ContainsKey(x.Key)))
			{
				LegacyFileEntry legacyFileEntry = LegacyEntries[dpi.Key];
				legacyFileEntry.ModifiedEntry = new ModifiedAssetEntry()
				{
					Data = dpi.Value,
					AddToTOCChunks = true,
					AddToChunkBundle = true,
				};

				legacyFileEntry.IsDirty = true;
				filesEdited.Add(legacyFileEntry);
			}

			if (rebuildChunk)
			{
				ChunkBatches.ForEach(x => x.ChunkGroupsInBatchModified.Clear());

				var groupedFiles = filesEdited.GroupBy(x => x.ParentGuid).ToDictionary(x => x.Key, x => x.ToList());
				foreach (var grpFile in groupedFiles)
				{
					filesEdited.AddRange(RebuildEntireChunk(grpFile.Key, grpFile.Value));
				}

				//WriteAllLegacy();
			}
			return filesEdited;
		}

		///// <summary>
		///// Modifies multiple assets and expects the entries to already has a ModifiedEntry object instance
		///// </summary>
		///// <param name="entries"></param>
		//public IEnumerable<ChunkAssetEntry> ModifyAssets(List<LegacyFileEntry> entries)
		//{
		//	var groupedAssets = entries.GroupBy(x => x.ParentGuid).ToDictionary(x => x.Key, x => x.ToList());
		//	foreach (var group in groupedAssets)
		//	{
		//		foreach (var lfe in group.Value)
		//		{
		//			ModifyAsset(lfe.Name, lfe.ModifiedEntry.Data);
		//		}
		//		yield return RebuildEntireChunk(group.Key, group.Value);
		//	}
		//}

		/// <summary>
		/// Modifies multiple assets and expects the entries to already has a ModifiedEntry object instance
		/// </summary>
		/// <param name="entries"></param>
		public void LoadEntriesModifiedFromProject(List<LegacyFileEntry> entries)
		{
			var groupedAssets = entries.GroupBy(x => x.ParentGuid).ToDictionary(x => x.Key, x => x.ToList());
			foreach (var group in groupedAssets)
			{
				foreach (var lfe in group.Value)
				{
					if (LegacyEntries.ContainsKey(lfe.Name))
					{
                        LegacyEntries[lfe.Name].ModifiedEntry = new ModifiedAssetEntry()
                        {
                            Data = lfe.ModifiedEntry.Data
                        };
                    };
				}
			}

			// Reset Chunks broken by patches
			foreach (var chunk in LegacyFileManager.LegacyChunks)
			{
				AssetManager.Instance.RevertAsset(chunk);
			}
		}

		/// <summary>
		/// Adds multiple assets and expects the entries to already have a ModifiedEntry object instance
		/// </summary>
		/// <param name="entries"></param>
		public void LoadEntriesAddedFromProject(List<LegacyFileEntry> entries)
		{
			var groupedAssets = entries.GroupBy(x => x.ParentGuid).ToDictionary(x => x.Key, x => x.ToList());
			foreach (var group in groupedAssets)
			{
				foreach (var lfe in group.Value)
				{
					LegacyEntries[lfe.Name].ModifiedEntry = new ModifiedAssetEntry()
					{
						Data = lfe.ModifiedEntry.Data
					};

				}
			}
			AddedFileEntries = entries;
		}

		private Stream GetChunkStream(LegacyFileEntry lfe)
		{
			var chunkEntry = AssetManager.GetChunkEntry(lfe.ChunkId);
			if(chunkEntry.ModifiedEntry != null && chunkEntry.ModifiedEntry.Data != null)
            {
				return new MemoryStream(chunkEntry.ModifiedEntry.Data);
			}

			var chunkStream = AssetManager.GetChunk(chunkEntry);
			return chunkStream;
		}

		/// <summary>
		/// Cleans up asset and associated chunks (chunks should not be modified but this checks them over)
		/// </summary>
		/// <param name="entry"></param>
		public void RevertAsset(AssetEntry entry)
		{
			LegacyFileEntry legacyFileEntry = entry as LegacyFileEntry;
			if (legacyFileEntry != null && AssetManager.Instance != null)
			{
				legacyFileEntry.ModifiedEntry = null;
				legacyFileEntry.IsDirty = false;
				var chunkEntry = AssetManager.Instance.GetChunkEntry(legacyFileEntry.ParentGuid);
				AssetManager.Instance.RevertAsset(chunkEntry);
				foreach (EbxAssetEntry item in AssetManager.EnumerateEbx("ChunkFileCollector"))
				{
					GetChunkAssetForEbx(item, out ChunkAssetEntry chunkAssetEntry, out EbxAsset ebxAsset);
					if (chunkAssetEntry == null || (chunkAssetEntry != null && !chunkAssetEntry.HasModifiedData))
						continue;
					AssetManager.Instance.RevertAsset(chunkAssetEntry);
				}
			}

			ChunkAssetEntry chunkAssetEntry1 = entry as ChunkAssetEntry;
			if (chunkAssetEntry1 != null)
			{
				AssetManager.Instance.RevertAsset(chunkAssetEntry1);
			}

		}

		public static Task ResetAsync(bool fullReset = false)
		{
			return Task.Run(() => { CleanUpChunks(fullReset); });
		}

		public static void CleanUpChunks(bool fullReset = false)
		{
			var lam = (LegacyFileManager_FMTV2)AssetManager.Instance.GetLegacyAssetManager();
			var movedEntries = lam.LegacyEntries.Where(x => x.Value.ModifiedEntry != null
					&& x.Value.ModifiedEntry.Data == null
					&& x.Value.ModifiedEntry.NewOffset.HasValue
					);

			// Revert all the moved files
			foreach (var lfe in movedEntries)
			{
				lfe.Value.ModifiedEntry = null;
			}

			// Revert all the chunks
			foreach (EbxAssetEntry item in AssetManager.Instance.EnumerateEbx("ChunkFileCollector"))
			{
				GetChunkAssetForEbx(item, out ChunkAssetEntry chunkAssetEntry, out EbxAsset ebxAsset);
				if (chunkAssetEntry != null)
				{
					AssetManager.Instance.RevertAsset(chunkAssetEntry);
				}
				AssetManager.Instance.RevertAsset(item);
			}

			foreach (ChunkAssetEntry assetEntry in lam.ModifiedChunks)
			{
				assetEntry.ModifiedEntry = null;
			}
			lam.ModifiedChunks.Clear();

			// Revert all legacy files
			if (fullReset)
			{
				var resetEntryCount = 0;
				var modifiedEntries = lam.LegacyEntries.Where(x => x.Value.ModifiedEntry != null).Select(x => x.Value);
				foreach (var lfe in modifiedEntries)
				{
					lfe.ModifiedEntry = null;
					resetEntryCount++;
				}
				AssetManager.Instance.logger.Log($"Reset {resetEntryCount} legacy files");
			}
		}

		public void AddAsset(string key, LegacyFileEntry lfe)
		{
			if (!LegacyEntries.ContainsKey(key))
			{
				lfe.IsAdded = true;
				lfe.ModifiedEntry = new ModifiedAssetEntry() { Data = ((MemoryStream)GetChunkStream(lfe)).ToArray() };
				LegacyEntries.Add(key, lfe);
			}
		}

		public static (byte[] newChunk, long uncompressedSize) CompressChunkGroup(
			Stream uncompressedChunkStream
			, List<LegacyFileEntry> filesInChunk
			, CompressionType compressionType)
		{
			MemoryStream compressedStream = new MemoryStream();
			var modDCS = new NativeReader(uncompressedChunkStream);
			modDCS.Position = 0L;
			int progress = 0;

			//filesInChunk = filesInChunk.OrderBy(x => x.ModifiedEntry.NewOffset).ToList();

			long lastOffsetEnd = 0;
			while (modDCS.Position < modDCS.Length)
			{
				bool isFirstSet = modDCS.Position == 0;

				int iP = Convert.ToInt32(Math.Round((modDCS.Position / (double)modDCS.Length) * 100));
				if (iP != progress)
                {
					progress = iP;
					Debug.WriteLine("Progress of Recompress: " + progress.ToString());
				}

				long remainingInStream = modDCS.Length - modDCS.Position;
				int amountToRead = (int)Math.Min(Utils.MaxBufferSize, remainingInStream);
				List<LegacyFileEntry> list = (from f in filesInChunk
												where f.ModifiedEntry.NewOffset.Value 
												>= modDCS.Position 
												&& f.ModifiedEntry.NewOffset.Value
												<= modDCS.Position + amountToRead
											  select f).ToList();
				List<LegacyFileEntry> filesEndingInBlock = (from f in filesInChunk
															where
                                                            (
															f.ModifiedEntry.NewOffset.Value + f.ModifiedEntry.Size
                                                            >= modDCS.Position
															&& f.ModifiedEntry.NewOffset.Value + f.ModifiedEntry.Size
															<= modDCS.Position + amountToRead)
															select f).ToList();
				foreach (LegacyFileEntry item in list)
				{
					item.ModifiedEntry.CompressedOffset = compressedStream.Position;
				}
				byte[] compressedBlock = Utils.CompressFile(modDCS.ReadBytes(amountToRead), null, ResourceType.Invalid, compressionType);
				compressedStream.Write(compressedBlock);
				lastOffsetEnd = compressedStream.Position;
				foreach (LegacyFileEntry item in filesEndingInBlock)
                {
					item.ModifiedEntry.CompressedOffsetEnd = lastOffsetEnd;
                }
            }
			var messedupfiles = filesInChunk.Where(x => !x.ModifiedEntry.CompressedOffset.HasValue || !x.ModifiedEntry.CompressedOffsetEnd.HasValue);
			if (messedupfiles.Any())
            {

            }
			return (compressedStream.ToArray(), modDCS.Length);
		}


		public static Guid GenerateDeterministicGuid(LegacyFileEntry lfe)
		{
			ulong filenameHash = Murmur2.HashString64(lfe.Filename, 18532uL);
			ulong pathHash = Murmur2.HashString64(lfe.Path, 18532uL);
			int counter = 1;
			Guid guid = Guid.Empty;
			Span<byte> guidSpan = stackalloc byte[16];
			do
			{
				BinaryPrimitives.WriteUInt64LittleEndian(guidSpan, pathHash);
				Span<byte> span = guidSpan;
				BinaryPrimitives.WriteUInt64LittleEndian(span[8..], filenameHash ^ (ulong)counter);
				guidSpan[15] = 1;
				guid = new Guid(guidSpan);
				counter++;
			}
			while (AssetManager.Instance.GetChunkEntry(guid) != null);
			return guid;
		}

        public void OnCommand(string command, params object[] value)
        {
            //throw new NotImplementedException();
        }

		public void Reset()
		{
			//if (LegacyEntries != null)
			//{
			//	ResetAsync(true).Wait();
			//}
		}

		public void ResetAndDispose()
        {
			//Reset();

			//if (LegacyEntries != null)
			//{
			//	LegacyEntries.Clear();
			//	LegacyEntries = null;
			//}
        }
	}


}