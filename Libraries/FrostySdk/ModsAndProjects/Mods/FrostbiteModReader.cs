using FMT.FileTools;
using FMT.FileTools.Modding;
using FrostbiteSdk.Frosty.Abstract;
using FrostySdk;
using FrostySdk.Managers;
using System.IO;

namespace FrostbiteSdk
{
    public class FrostbiteModReader : BaseModReader
    {
        public class BundleResource : BaseModResource
        {
            private int superBundleName;

            public override ModResourceType Type => ModResourceType.Bundle;

            public override void Read(NativeReader reader, uint modVersion = 6u)
            {
                base.Read(reader);
                name = reader.ReadNullTerminatedString();
                superBundleName = reader.ReadInt32LittleEndian();
            }

            public override void FillAssetEntry(IAssetEntry entry)
            {
                BundleEntry obj = (BundleEntry)entry;
                obj.Name = name;
                obj.SuperBundleId = superBundleName;
            }
        }


        public long dataOffset;

        public int dataCount;

        public FrostbiteModReader(Stream inStream)
            : base(inStream)
        {
            //var viewableBytes = new NativeReader(inStream).ReadToEnd();
            //inStream.Position = 0;
            var magic = ReadULong();
            if (magic != FrostbiteMod.MagicFrosty && magic != FrostbiteMod.Magic2)
            {
                IsValid = false;
                return;
            }
            Version = ReadUInt();
            //if (version <= FrostbiteMod.Version)
            {
                dataOffset = ReadLong();
                dataCount = ReadInt();
                //var pn = ReadSizedString(ReadByte());
                GameName = ReadLengthPrefixedString();

                //Debug.WriteLine("FrostyModReader::Mod ProfileName::" + pn);
                if (GameName == ProfileManager.ProfileName)
                {
                    GameVersion = ReadInt();
                    IsValid = true;
                    //Debug.WriteLine("FrostyModReader::Mod Game Version::" + gameVersion);
                }
                else
                {
                    IsValid = false;
                    //throw new Exception("FrostyModReader::Cannot match profile " + pn + " to " + ProfilesLibrary.ProfileName);
                }
            }
        }

        public FrostbiteModDetails ReadModDetails()
        {
            if (Version == FrostbiteMod.CurrentVersion
                || Version == FrostbiteMod.HashVersions[7]
                )
            {
                return new FrostbiteModDetails(ReadLengthPrefixedString(), ReadLengthPrefixedString(), ReadLengthPrefixedString(), ReadLengthPrefixedString(), ReadLengthPrefixedString(), ReadInt());
            }
            else if (Version >= 5)
            {
                return new FrostbiteModDetails(ReadNullTerminatedString(), ReadNullTerminatedString(), ReadNullTerminatedString(), ReadNullTerminatedString(), ReadNullTerminatedString(), ReadInt());
            }
            else
            {
                return new FrostbiteModDetails(ReadNullTerminatedString(), ReadNullTerminatedString(), ReadNullTerminatedString(), ReadNullTerminatedString(), ReadNullTerminatedString());
            }
        }

        public BaseModResource[] ReadResources()
        {
            int num = ReadInt();
            BaseModResource[] array = new BaseModResource[num];
            for (int i = 0; i < num; i++)
            {
                switch ((ModResourceType)ReadByte())
                {
                    case ModResourceType.Embedded:
                        array[i] = new EmbeddedResource();
                        break;
                    case ModResourceType.Ebx:
                        array[i] = new EbxResource();
                        break;
                    case ModResourceType.Res:
                        array[i] = new ResResource();
                        break;
                    case ModResourceType.Chunk:
                        array[i] = new ChunkResource();
                        break;
                    case ModResourceType.Legacy:
                        array[i] = new LegacyFileResource();
                        break;
                    case ModResourceType.EmbeddedFile:
                        array[i] = new EmbeddedFileResource();
                        break;
                }
                if (array[i] != null)
                    array[i].Read(this, Version);
            }
            return array;
        }

        public byte[] GetResourceData(BaseModResource resource)
        {
            if (resource.ResourceIndex == -1)
            {
                return null;
            }

            Position = dataOffset + resource.ResourceIndex * 16;
            long offset = ReadLong();
            long size = ReadLong();

            Position = dataOffset + dataCount * 16 + offset;
            var data = ReadBytes((int)size);

            //if(resource is ResResource)
            //         {
            //	using (MemoryStream memoryStream = new MemoryStream(data))
            //	{
            //		ResAssetEntry resAssetEntry = new ResAssetEntry();
            //		resource.FillAssetEntry(resAssetEntry);
            //	}
            //         }

            return data;
        }
    }
}
