using FrostbiteSdk;
using FrostbiteSdk.SdkGenerator;
using System;

namespace SdkGenerator.Fifa21
{
	public class ClassInfo : IClassInfo
	{
		public ITypeInfo typeInfo { get; set; }

		public ushort id { get; set; }

		public ushort isDataContainer { get; set; }

		public byte[] padding { get; set; }

		public long parentClass { get; set; }

		public long nextOffset { get; set; }

		public void Read(MemoryReader reader)
		{
			long position = reader.Position;

			long typePosition = reader.ReadLong();
			var offset1 = reader.ReadLong();
            nextOffset = reader.ReadLong();

			id = reader.ReadUShort();
			//
			//reader.ReadUShort();
			isDataContainer = reader.ReadUShort();
			padding = new byte[4]
			{
				reader.ReadByte(),
				reader.ReadByte(),
				reader.ReadByte(),
				reader.ReadByte()
			};

			parentClass = reader.ReadLong();

			reader.Position = typePosition;
			typeInfo = new TypeInfo();
			typeInfo.Read(reader);
			if (typeInfo.parentClass != 0L)
			{
				parentClass = typeInfo.parentClass;
			}
			if (parentClass == position)
			{
				parentClass = 0L;
			}

			//ClassesSdkCreator.offset = nextOffset;
			//if(offset1 != position && offset1 > nextOffset && offset1 > reader.Position)
   //         {
			//	nextOffset = offset1;
   //         }


		}

		public override string ToString()
        {
			if(typeInfo!=null && !string.IsNullOrEmpty(typeInfo.name))
				return typeInfo.name;

            return base.ToString();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
