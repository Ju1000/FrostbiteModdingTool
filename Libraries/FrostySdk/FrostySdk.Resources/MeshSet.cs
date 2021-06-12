using System;
using System.Collections.Generic;
using System.IO;
using Frosty.Hash;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;


public class MeshSet
{
	private TangentSpaceCompressionType tangentSpaceCompressionType;

	private AxisAlignedBox boundingBox;

	private string fullName;

	private uint nameHash;

	private readonly uint headerSize;

	private readonly List<uint> unknownUInts = new List<uint>();

	private readonly List<ushort> unknownUShorts = new List<ushort>();

	private readonly List<long> unknownOffsets = new List<long>();

	private readonly ushort boneCount;

	private readonly List<ushort> boneIndices = new List<ushort>();

	private readonly List<AxisAlignedBox> boneBoundingBoxes = new List<AxisAlignedBox>();

	private readonly List<AxisAlignedBox> partBoundingBoxes = new List<AxisAlignedBox>();

	private readonly List<LinearTransform> partTransforms = new List<LinearTransform>();

	public TangentSpaceCompressionType TangentSpaceCompressionType
	{
		get
		{
			return tangentSpaceCompressionType;
		}
		set
		{
			tangentSpaceCompressionType = value;
			foreach (MeshSetLod lod in Lods)
			{
				foreach (MeshSetSection section in lod.Sections)
				{
					section.TangentSpaceCompressionType = value;
				}
			}
		}
	}

	public AxisAlignedBox BoundingBox => boundingBox;

	public List<MeshSetLod> Lods { get; } = new List<MeshSetLod>();


	public MeshType Type { get; set; }

	public MeshLayoutFlags Flags { get; }

	public string FullName
	{
		get
		{
			return fullName;
		}
		set
		{
			fullName = value.ToLower();
			nameHash = (uint)Fnv1.HashString(fullName);
			int num = fullName.LastIndexOf('/');
			Name = ((num != -1) ? fullName.Substring(num + 1) : string.Empty);
		}
	}

	public string Name { get; private set; }

	public int HeaderSize => BitConverter.ToUInt16(Meta, 12);

	public int MaxLodCount
	{
		get
		{
			int dataVersion = ProfilesLibrary.DataVersion;
			if (dataVersion == 20131115 || dataVersion == 20140225 || (uint)(dataVersion - 20141117) <= 1u)
			{
				return 6;
			}
			return 7;
		}
	}

	public byte[] Meta { get; set; } = new byte[16];


	public MeshSet(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }
        NativeReader nativeReader = new NativeReader(stream);

        using (NativeWriter nw = new NativeWriter(new FileStream("MeshSet.dat", FileMode.Create)))
        {
            nw.Write(nativeReader.ReadToEnd());
        }
        nativeReader.Position = 0;



        boundingBox = nativeReader.ReadAxisAlignedBox();
        long[] array = new long[MaxLodCount];
        for (int i2 = 0; i2 < MaxLodCount; i2++)
        {
            array[i2] = nativeReader.ReadLong();
        }
        long position = nativeReader.ReadLong();
        long position2 = nativeReader.ReadLong();
        nameHash = nativeReader.ReadUInt();
        Type = (MeshType)nativeReader.ReadUInt();
        Flags = (MeshLayoutFlags)nativeReader.ReadUInt();
        ReadUnknownUInts(nativeReader);
        ushort lodsCount = nativeReader.ReadUShort();
        nativeReader.ReadUShort();
        ushort num2 = 0;
        if (Type == MeshType.MeshType_Skinned)
        {
            boneCount = nativeReader.ReadUShort();
            num2 = nativeReader.ReadUShort();
            if (boneCount != 0)
            {
                nativeReader.ReadLong();
                nativeReader.ReadLong();
            }
        }
        else if (Type == MeshType.MeshType_Composite)
        {
            num2 = nativeReader.ReadUShort();
            boneCount = nativeReader.ReadUShort();
            long num3 = nativeReader.ReadLong();
            long num4 = nativeReader.ReadLong();
            long position3 = nativeReader.Position;
            if (num3 != 0L)
            {
                nativeReader.Position = num3;
                for (int n2 = 0; n2 < num2; n2++)
                {
                    partTransforms.Add(nativeReader.ReadLinearTransform());
                }
            }
            if (num4 != 0L)
            {
                nativeReader.Position = num4;
                for (int num5 = 0; num5 < num2; num5++)
                {
                    partBoundingBoxes.Add(nativeReader.ReadAxisAlignedBox());
                }
            }
            nativeReader.Position = position3;
        }
        nativeReader.Pad(16);
        headerSize = (uint)nativeReader.Position;
        for (int n = 0; n < lodsCount; n++)
        {
            Lods.Add(new MeshSetLod(nativeReader));
        }
        int sectionIndex = 0;
        foreach (MeshSetLod lod4 in Lods)
        {
            for (int m = 0; m < lod4.Sections.Count; m++)
            {
                lod4.Sections[m] = new MeshSetSection(nativeReader, sectionIndex++);
            }
        }
        nativeReader.Pad(16);
        nativeReader.Position = position;
        FullName = nativeReader.ReadNullTerminatedString();
        nativeReader.Position = position2;
        Name = nativeReader.ReadNullTerminatedString();
        nativeReader.Pad(16);
        foreach (MeshSetLod lod3 in Lods)
        {
            for (int l = 0; l < lod3.CategorySubsetIndices.Count; l++)
            {
                for (int j2 = 0; j2 < lod3.CategorySubsetIndices[l].Count; j2++)
                {
                    lod3.CategorySubsetIndices[l][j2] = nativeReader.ReadByte();
                }
            }
        }
        nativeReader.Pad(16);
        foreach (MeshSetLod lod2 in Lods)
        {
            nativeReader.Position += lod2.AdjacencyBufferSize;
        }
        nativeReader.Pad(16);
        foreach (MeshSetLod lod in Lods)
        {
            if (lod.Type == MeshType.MeshType_Skinned)
            {
                nativeReader.Position += lod.BoneCount * 4;
            }
            else if (lod.Type == MeshType.MeshType_Composite)
            {
                nativeReader.Position += lod.Sections.Count * 24;
            }
        }
        if (Type == MeshType.MeshType_Skinned)
        {
            nativeReader.Pad(16);
            for (int k = 0; k < num2; k++)
            {
                boneIndices.Add(nativeReader.ReadUShort());
            }
            nativeReader.Pad(16);
            for (int j = 0; j < num2; j++)
            {
                boneBoundingBoxes.Add(nativeReader.ReadAxisAlignedBox());
            }
        }
        else if (Type == MeshType.MeshType_Composite)
        {
            nativeReader.Pad(16);
            for (int i = 0; i < num2; i++)
            {
                partBoundingBoxes.Add(nativeReader.ReadAxisAlignedBox());
            }
        }
        nativeReader.Pad(16);
        foreach (MeshSetLod lod in Lods)
        {
            lod.ReadInlineData(nativeReader);
        }
    }

    private void ReadUnknownUInts(NativeReader nativeReader)
    {
        //for (int m2 = 0; m2 < 8; m2++)
        //{
        //    unknownUInts.Add(nativeReader.ReadUInt());
        //}
		switch (ProfilesLibrary.DataVersion)
		{
			case 20160927:
				unknownUInts.Add(nativeReader.ReadUInt());
				unknownUInts.Add(nativeReader.ReadUInt());
				break;
			case 20171210:
				unknownUInts.Add(nativeReader.ReadUShort());
				break;
			case (int)ProfilesLibrary.DataVersions.FIFA21:
			case 20170929:
			case 20180807:
			case 20180914:
			case (int)ProfilesLibrary.DataVersions.FIFA20:
				{
					for (int m = 0; m < 8; m++)
					{
						unknownUInts.Add(nativeReader.ReadUInt());
					}
					break;
				}
			case 20180628:
				{
					for (int k = 0; k < 6; k++)
					{
						unknownUInts.Add(nativeReader.ReadUInt());
					}
					break;
				}
			case 20181207:
			case 20190905:
			case 20191101:
				{
					for (int j = 0; j < 7; j++)
					{
						unknownUInts.Add(nativeReader.ReadUInt());
					}
					break;
				}
			case (int)ProfilesLibrary.DataVersions.MADDEN21:
			case 20190729:
				{
					for (int l = 0; l < 8; l++)
					{
						unknownUInts.Add(nativeReader.ReadUInt());
					}
					unknownUInts.Add(nativeReader.ReadUShort());
					break;
				}
			default:
				unknownUInts.Add(nativeReader.ReadUInt());
				if (ProfilesLibrary.DataVersion != 20170321)
				{
					unknownUInts.Add(nativeReader.ReadUInt());
					unknownUInts.Add(nativeReader.ReadUInt());
					unknownUInts.Add(nativeReader.ReadUInt());
					unknownUInts.Add(nativeReader.ReadUInt());
					unknownUInts.Add(nativeReader.ReadUInt());
					if (ProfilesLibrary.DataVersion == 20171117 || ProfilesLibrary.DataVersion == 20171110)
					{
						unknownUInts.Add(nativeReader.ReadUInt());
					}
				}
				break;
			case 20131115:
			case 20140225:
			case 20141117:
			case 20141118:
			case 20150223:
			case 20151103:
				break;
		}
	}

	public byte[] ToBytes()
	{
		MeshContainer meshContainer = new MeshContainer();
		PreProcess(meshContainer);
		using NativeWriter nativeWriter = new NativeWriter(new MemoryStream());
		Process(nativeWriter, meshContainer);
		uint num = (uint)nativeWriter.BaseStream.Position;
		uint num2 = 0u;
		uint num3 = 0u;
		foreach (MeshSetLod lod in Lods)
		{
			if (lod.ChunkId == Guid.Empty)
			{
				nativeWriter.WriteBytes(lod.InlineData);
				nativeWriter.WritePadding(16);
			}
		}
		num2 = (uint)(nativeWriter.BaseStream.Position - num);
		meshContainer.FixupRelocPtrs(nativeWriter);
		meshContainer.WriteRelocTable(nativeWriter);
		num3 = (uint)(nativeWriter.BaseStream.Position - num - num2);

        BitConverter.TryWriteBytes(Meta, num);
        BitConverter.TryWriteBytes(Meta.AsSpan(4), num2);
        BitConverter.TryWriteBytes(Meta.AsSpan(8), num3);
        BitConverter.TryWriteBytes(Meta.AsSpan(12), headerSize);

        //var msMeta = new MemoryStream();
        //using (var nwMeta = new NativeWriter(msMeta, true))
        //      {
        //	nwMeta.Write(num);
        //	nwMeta.Write(num2);
        //	nwMeta.Write(num3);
        //	nwMeta.Write(headerSize);
        //}
        //Meta = msMeta.ToArray();

        var bytes = ((MemoryStream)nativeWriter.BaseStream).ToArray();
		return bytes;
	}

	private void PreProcess(MeshContainer meshContainer)
	{
		uint inInlineDataOffset = 0u;
		foreach (MeshSetLod lod in Lods)
		{
			lod.PreProcess(meshContainer, ref inInlineDataOffset);
		}
		foreach (MeshSetLod lod2 in Lods)
		{
			meshContainer.AddRelocPtr("LOD", lod2);
		}
		meshContainer.AddString(fullName, FullName.Replace(Name, ""), ignoreNull: true);;
		meshContainer.AddString(Name, Name);
		if ((ProfilesLibrary.DataVersion == 20171117
			|| ProfilesLibrary.DataVersion == 20171110
			|| ProfilesLibrary.DataVersion == 20170321
			|| ProfilesLibrary.DataVersion == 20160927
			|| ProfilesLibrary.DataVersion == 20170929
			|| ProfilesLibrary.DataVersion == 20180807
			|| ProfilesLibrary.DataVersion == 20180914
			|| ProfilesLibrary.DataVersion == 20190911
			|| ProfilesLibrary.IsMadden21DataVersion()
			|| ProfilesLibrary.IsFIFA21DataVersion()
			|| ProfilesLibrary.DataVersion == 20190729) 
			&& Type == MeshType.MeshType_Skinned)
		{
			meshContainer.AddRelocPtr("BONEINDICES", boneIndices);
			meshContainer.AddRelocPtr("BONEBBOXES", boneBoundingBoxes);
		}
	}

	/*
	private void Process(NativeWriter writer, MeshContainer meshContainer)
	{
		writer.Write(boundingBox);
		for (int i = 0; i < MaxLodCount; i++)
		{
			if (i < Lods.Count)
			{
				meshContainer.WriteRelocPtr("LOD", Lods[i], writer);
			}
			else
			{
				writer.Write(0uL);
			}
		}
		meshContainer.WriteRelocPtr("STR", FullName, writer);
		meshContainer.WriteRelocPtr("STR", Name, writer);
		writer.Write(nameHash);
		writer.Write((uint)Type);
		writer.Write((uint)Flags);
		foreach (uint unknownUInt in unknownUInts)
		{
			writer.Write(unknownUInt);
		}
		writer.Write((ushort)Lods.Count);
		ushort num = 0;
		foreach (MeshSetLod lod in Lods)
		{
			num = (ushort)(num + (ushort)lod.Sections.Count);
		}
		writer.Write(num);
		if ((ProfilesLibrary.DataVersion == 20171117 
			|| ProfilesLibrary.DataVersion == 20171110 
			|| ProfilesLibrary.DataVersion == 20170321 
			|| ProfilesLibrary.DataVersion == 20160927 
			|| ProfilesLibrary.DataVersion == 20170929 
			|| ProfilesLibrary.DataVersion == 20180807
			|| ProfilesLibrary.DataVersion == 20180914
			|| ProfilesLibrary.DataVersion == 20190911
			|| ProfilesLibrary.DataVersion == 20190729
			|| ProfilesLibrary.IsFIFA21DataVersion()
			|| ProfilesLibrary.IsMadden21DataVersion()
			) 
			&& Type == MeshType.MeshType_Skinned)
		{
			writer.Write(boneCount);
			writer.Write((ushort)boneIndices.Count);
			meshContainer.WriteRelocPtr("BONEINDICES", boneIndices, writer);
			meshContainer.WriteRelocPtr("BONEBBOXES", boneBoundingBoxes, writer);
		}
		writer.WritePadding(16);
		foreach (MeshSetLod lod2 in Lods)
		{
			meshContainer.AddOffset("LOD", lod2, writer);
			lod2.Process(writer, meshContainer);
		}
		foreach (MeshSetLod lod3 in Lods)
		{
			meshContainer.AddOffset("SECTION", lod3.Sections, writer);
			foreach (MeshSetSection section in lod3.Sections)
			{
				section.Process(writer, meshContainer);
			}
		}
		writer.WritePadding(16);
		foreach (MeshSetLod lod4 in Lods)
		{
			foreach (MeshSetSection section2 in lod4.Sections)
			{
				if (section2.BoneList.Count <= 0)
				{
					continue;
				}
				meshContainer.AddOffset("BONELIST", section2.BoneList, writer);
				foreach (ushort bone in section2.BoneList)
				{
					writer.Write(bone);
				}
			}
		}
		writer.WritePadding(16);
		meshContainer.WriteStrings(writer);
		writer.WritePadding(16);
		foreach (MeshSetLod lod5 in Lods)
		{
			foreach (List<byte> categorySubsetIndex in lod5.CategorySubsetIndices)
			{
				meshContainer.AddOffset("SUBSET", categorySubsetIndex, writer);
				writer.Write(categorySubsetIndex.ToArray());
			}
		}
		writer.WritePadding(16);
		if (Type != MeshType.MeshType_Skinned)
		{
			return;
		}
		foreach (MeshSetLod lod6 in Lods)
		{
			meshContainer.AddOffset("BONES", lod6.BoneIndexArray, writer);
			foreach (uint item in lod6.BoneIndexArray)
			{
				writer.Write(item);
			}
			if (ProfilesLibrary.DataVersion == 20160927
				|| ProfilesLibrary.DataVersion == 20171117 
				|| ProfilesLibrary.DataVersion == 20170929 
				|| ProfilesLibrary.DataVersion == 20180807 
				|| ProfilesLibrary.DataVersion == 20180914 
				|| ProfilesLibrary.DataVersion == 20181207 
				|| ProfilesLibrary.DataVersion == 20190729 
				|| ProfilesLibrary.DataVersion == 20190911 
				|| ProfilesLibrary.IsFIFA21DataVersion()
				|| ProfilesLibrary.IsMadden21DataVersion()
				|| lod6.BoneShortNameArray.Count == 0)
			{
				continue;
			}
			meshContainer.AddOffset("BONESNAMES", lod6.BoneShortNameArray, writer);
			foreach (uint item2 in lod6.BoneShortNameArray)
			{
				writer.Write(item2);
			}
		}
		writer.WritePadding(16);
		//if (ProfilesLibrary.DataVersion != 20171117 
		//	&& ProfilesLibrary.DataVersion != 20171110
		//	&& ProfilesLibrary.DataVersion != 20170321 
		//	&& ProfilesLibrary.DataVersion != 20160927 
		//	&& ProfilesLibrary.DataVersion != 20170929 
		//	&& ProfilesLibrary.DataVersion != 20180807 
		//	&& ProfilesLibrary.DataVersion != 20180914 
		//	&& ProfilesLibrary.DataVersion != 20190911 
		//	&& ProfilesLibrary.DataVersion != 20190729
		//	&& !ProfilesLibrary.IsFIFA21DataVersion()
		//	&& !ProfilesLibrary.IsMadden21DataVersion()
		//	)
		//{
		//	return;
		//}
		meshContainer.AddOffset("BONEINDICES", boneIndices, writer);
		foreach (ushort boneIndex in boneIndices)
		{
			writer.Write(boneIndex);
		}
		writer.WritePadding(16);
		meshContainer.AddOffset("BONEBBOXES", boneBoundingBoxes, writer);
		foreach (AxisAlignedBox boneBoundingBox in boneBoundingBoxes)
		{
			writer.Write(boneBoundingBox);
		}
		writer.WritePadding(16);
	}
	*/

	private void Process(NativeWriter writer, MeshContainer meshContainer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (meshContainer == null)
		{
			throw new ArgumentNullException("meshContainer");
		}
		writer.WriteAxisAlignedBox(boundingBox);
		for (int i = 0; i < MaxLodCount; i++)
		{
			if (i < Lods.Count)
			{
				meshContainer.WriteRelocPtr("LOD", Lods[i], writer);
			}
			else
			{
				writer.WriteUInt64LittleEndian(0uL);
			}
		}
		meshContainer.WriteRelocPtr("STR", fullName, writer);
		meshContainer.WriteRelocPtr("STR", Name, writer);
		writer.WriteUInt32LittleEndian(nameHash);
		writer.WriteUInt32LittleEndian((uint)Type);
		writer.WriteUInt32LittleEndian((uint)Flags);
		foreach (uint unknownUInt in unknownUInts)
		{
			writer.WriteUInt32LittleEndian(unknownUInt);
		}
		writer.WriteUInt16LittleEndian((ushort)Lods.Count);
		ushort num = 0;
		foreach (MeshSetLod lod in Lods)
		{
			num = (ushort)(num + (ushort)lod.Sections.Count);
		}
		writer.WriteUInt16LittleEndian(num);
		if (Type == MeshType.MeshType_Skinned)
		{
			writer.WriteUInt16LittleEndian(boneCount);
			writer.WriteUInt16LittleEndian((ushort)boneIndices.Count);
			if (boneCount > 0)
			{
				meshContainer.WriteRelocPtr("BONEINDICES", boneIndices, writer);
				meshContainer.WriteRelocPtr("BONEBBOXES", boneBoundingBoxes, writer);
			}
		}
		else if (Type == MeshType.MeshType_Composite)
		{
			writer.WriteUInt16LittleEndian((ushort)boneIndices.Count);
			writer.WriteUInt16LittleEndian(boneCount);
		}
		writer.WritePadding(16);
		foreach (MeshSetLod lod2 in Lods)
		{
			meshContainer.AddOffset("LOD", lod2, writer);
			lod2.Process(writer, meshContainer);
		}
		foreach (MeshSetLod lod3 in Lods)
		{
			meshContainer.AddOffset("SECTION", lod3.Sections, writer);
			foreach (MeshSetSection section3 in lod3.Sections)
			{
				section3.Process(writer, meshContainer);
			}
		}
		writer.WritePadding(16);
		foreach (MeshSetLod lod5 in Lods)
		{
			foreach (MeshSetSection section2 in lod5.Sections)
			{
				if (section2.BoneList.Count == 0)
				{
					continue;
				}
				meshContainer.AddOffset("BONELIST", section2.BoneList, writer);
				foreach (ushort bone in section2.BoneList)
				{
					writer.WriteUInt16LittleEndian(bone);
				}
			}
		}
		writer.WritePadding(16);
		meshContainer.WriteStrings(writer);
		writer.WritePadding(16);
		foreach (MeshSetLod lod6 in Lods)
		{
			foreach (List<byte> categorySubsetIndex in lod6.CategorySubsetIndices)
			{
				meshContainer.AddOffset("SUBSET", categorySubsetIndex, writer);
				writer.WriteBytes(categorySubsetIndex.ToArray());
			}
		}
		writer.WritePadding(16);
		if (Type != MeshType.MeshType_Skinned)
		{
			return;
		}
		foreach (MeshSetLod lod4 in Lods)
		{
			meshContainer.AddOffset("BONES", lod4.BoneIndexArray, writer);
			foreach (uint item in lod4.BoneIndexArray)
			{
				writer.WriteUInt32LittleEndian(item);
			}
		}
		writer.WritePadding(16);
		meshContainer.AddOffset("BONEINDICES", boneIndices, writer);
		foreach (ushort boneIndex in boneIndices)
		{
			writer.WriteUInt16LittleEndian(boneIndex);
		}
		writer.WritePadding(16);
		meshContainer.AddOffset("BONEBBOXES", boneBoundingBoxes, writer);
		foreach (AxisAlignedBox boneBoundingBox in boneBoundingBoxes)
		{
			writer.WriteAxisAlignedBox(boneBoundingBox);
		}
		writer.WritePadding(16);
	}

}

/*
public class MeshSet
{
	private TangentSpaceCompressionType tangentSpaceCompressionType;

	private AxisAlignedBox boundingBox;

	private string fullName;

	private uint nameHash;

	private readonly uint headerSize;

	private readonly List<uint> unknownUInts = new List<uint>();

	private readonly List<ushort> unknownUShorts = new List<ushort>();

	private readonly List<long> unknownOffsets = new List<long>();

	private readonly ushort boneCount;

	private readonly List<ushort> boneIndices = new List<ushort>();

	private readonly List<AxisAlignedBox> boneBoundingBoxes = new List<AxisAlignedBox>();

	private readonly List<AxisAlignedBox> partBoundingBoxes = new List<AxisAlignedBox>();

	private readonly List<LinearTransform> partTransforms = new List<LinearTransform>();

	public TangentSpaceCompressionType TangentSpaceCompressionType
	{
		get
		{
			return tangentSpaceCompressionType;
		}
		set
		{
			tangentSpaceCompressionType = value;
			foreach (MeshSetLod lod in Lods)
			{
				foreach (MeshSetSection section in lod.Sections)
				{
					section.TangentSpaceCompressionType = value;
				}
			}
		}
	}

	public AxisAlignedBox BoundingBox => boundingBox;

	public List<MeshSetLod> Lods { get; } = new List<MeshSetLod>();


	public MeshType Type { get; set; }

	public MeshLayoutFlags Flags { get; }

	public string FullName
	{
		get
		{
			return fullName;
		}
		set
		{
			fullName = value.ToLower();
			nameHash = (uint)Fnv1.HashString(fullName);
			int num = fullName.LastIndexOf('/');
			Name = ((num != -1) ? fullName.Substring(num + 1) : string.Empty);
		}
	}

	public string Name { get; private set; }

	public int HeaderSize => BitConverter.ToUInt16(Meta, 12);

	public int MaxLodCount => 7;

	public byte[] Meta { get; } = new byte[16];


	public MeshSet(Stream stream)
	{
		NativeReader nativeReader = new NativeReader(stream);
		var posBeforeRead = nativeReader.Position;
		var allBytesOfStream = nativeReader.ReadToEnd();
		using (var fsOut = new FileStream("MeshSet.dat", FileMode.OpenOrCreate))
			new NativeWriter(fsOut).Write(allBytesOfStream);
		nativeReader.Position = posBeforeRead;

		boundingBox = nativeReader.ReadAxisAlignedBox();
		long[] array = new long[MaxLodCount];
		for (int i2 = 0; i2 < MaxLodCount; i2++)
		{
			array[i2] = nativeReader.ReadLong();
		}
		long position = nativeReader.ReadLong();
		long position2 = nativeReader.ReadLong();
		nameHash = nativeReader.ReadUInt();
		Type = (MeshType)nativeReader.ReadUInt();
		Flags = (MeshLayoutFlags)nativeReader.ReadUInt();
		for (int m2 = 0; m2 < 8; m2++)
		{
			unknownUInts.Add(nativeReader.ReadUInt());
		}
		ushort lodsCount = nativeReader.ReadUShort();
		ushort unknownCount2 = nativeReader.ReadUShort();
		ushort num2 = 0;
		if (Type == MeshType.MeshType_Skinned)
		{
			boneCount = nativeReader.ReadUShort();
			num2 = nativeReader.ReadUShort();
			if (boneCount != 0)
			{
				var unk1 = (MeshLayoutFlags)nativeReader.ReadLong();
				var unk2 = nativeReader.ReadLong();
			}
		}
		else if (Type == MeshType.MeshType_Composite)
		{
			num2 = nativeReader.ReadUShort();
			boneCount = nativeReader.ReadUShort();
			long num3 = nativeReader.ReadLong();
			long num4 = nativeReader.ReadLong();
			long position3 = nativeReader.Position;
			if (num3 != 0L)
			{
				nativeReader.Position = num3;
				for (int n2 = 0; n2 < num2; n2++)
				{
					partTransforms.Add(nativeReader.ReadLinearTransform());
				}
			}
			if (num4 != 0L)
			{
				nativeReader.Position = num4;
				for (int num5 = 0; num5 < num2; num5++)
				{
					partBoundingBoxes.Add(nativeReader.ReadAxisAlignedBox());
				}
			}
			nativeReader.Position = position3;
		}
		nativeReader.Pad(16);
		headerSize = (uint)nativeReader.Position;
		for (int n = 0; n < lodsCount; n++)
		{
			Lods.Add(new MeshSetLod(nativeReader));
		}
		int sectionIndex = 0;
		foreach (MeshSetLod lod4 in Lods)
		{
			for (int m = 0; m < lod4.Sections.Count; m++)
			{
				lod4.Sections[m] = new MeshSetSection(nativeReader,  sectionIndex++);
			}
		}
		nativeReader.Pad(16);
		nativeReader.Position = position;
		FullName = nativeReader.ReadNullTerminatedString();
		nativeReader.Position = position2;
		Name = nativeReader.ReadNullTerminatedString();
		nativeReader.Pad(16);
		foreach (MeshSetLod lod3 in Lods)
		{
			for (int l = 0; l < lod3.CategorySubsetIndices.Count; l++)
			{
				for (int j2 = 0; j2 < lod3.CategorySubsetIndices[l].Count; j2++)
				{
					lod3.CategorySubsetIndices[l][j2] = nativeReader.ReadByte();
				}
			}
		}
		nativeReader.Pad(16);
		foreach (MeshSetLod lod2 in Lods)
		{
			nativeReader.Position += lod2.AdjacencyBufferSize;
		}
		nativeReader.Pad(16);
		foreach (MeshSetLod lod in Lods)
		{
			if (lod.Type == MeshType.MeshType_Skinned)
			{
				nativeReader.Position += lod.BoneCount * 4;
			}
			else if (lod.Type == MeshType.MeshType_Composite)
			{
				nativeReader.Position += lod.Sections.Count * 24;
			}
		}
		if (Type == MeshType.MeshType_Skinned)
		{
			nativeReader.Pad(16);
			for (int k = 0; k < num2; k++)
			{
				boneIndices.Add(nativeReader.ReadUShort());
			}
			nativeReader.Pad(16);
			for (int j = 0; j < num2; j++)
			{
				boneBoundingBoxes.Add(nativeReader.ReadAxisAlignedBox());
			}
		}
		else if (Type == MeshType.MeshType_Composite)
		{
			nativeReader.Pad(16);
			for (int i = 0; i < num2; i++)
			{
				partBoundingBoxes.Add(nativeReader.ReadAxisAlignedBox());
			}
		}
		nativeReader.Pad(16);
		foreach (MeshSetLod lod5 in Lods)
		{
			lod5.ReadInlineData(nativeReader);
		}
	}

	private void PreProcess(MeshContainer meshContainer)
	{
		if (meshContainer == null)
		{
			throw new ArgumentNullException("meshContainer");
		}
		uint inInlineDataOffset = 0u;
		foreach (MeshSetLod lod3 in Lods)
		{
			lod3.PreProcess(meshContainer, ref inInlineDataOffset);
		}
		foreach (MeshSetLod lod2 in Lods)
		{
			meshContainer.AddRelocPtr("LOD", lod2);
		}
		meshContainer.AddString(fullName, fullName.Replace(Name, ""), ignoreNull: true);
		meshContainer.AddString(Name, Name);
		if (Type == MeshType.MeshType_Skinned)
		{
			meshContainer.AddRelocPtr("BONEINDICES", boneIndices);
			meshContainer.AddRelocPtr("BONEBBOXES", boneBoundingBoxes);
		}
	}

	public byte[] ToBytes()
	{
		MeshContainer meshContainer = new MeshContainer();
		PreProcess(meshContainer);
		using NativeWriter nativeWriter = new NativeWriter(new MemoryStream());
		Process(nativeWriter, meshContainer);
		uint num = (uint)nativeWriter.BaseStream.Position;
		uint num2 = 0u;
		uint num3 = 0u;
		foreach (MeshSetLod lod in Lods)
		{
			if (lod.ChunkId == Guid.Empty)
			{
				nativeWriter.WriteBytes(lod.InlineData);
				nativeWriter.WritePadding(16);
			}
		}
		num2 = (uint)(nativeWriter.BaseStream.Position - num);
		meshContainer.FixupRelocPtrs(nativeWriter);
		meshContainer.WriteRelocTable(nativeWriter);
		num3 = (uint)(nativeWriter.BaseStream.Position - num - num2);
		BitConverter.TryWriteBytes(Meta, num);
		BitConverter.TryWriteBytes(Meta.AsSpan(4), num2);
		BitConverter.TryWriteBytes(Meta.AsSpan(8), num3);
		BitConverter.TryWriteBytes(Meta.AsSpan(12), headerSize);
		return ((MemoryStream)nativeWriter.BaseStream).ToArray();
	}

	private void Process(NativeWriter writer, MeshContainer meshContainer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (meshContainer == null)
		{
			throw new ArgumentNullException("meshContainer");
		}
		writer.WriteAxisAlignedBox(boundingBox);
		for (int i = 0; i < MaxLodCount; i++)
		{
			if (i < Lods.Count)
			{
				meshContainer.WriteRelocPtr("LOD", Lods[i], writer);
			}
			else
			{
				writer.Write(0uL);
			}
		}
		meshContainer.WriteRelocPtr("STR", fullName, writer);
		meshContainer.WriteRelocPtr("STR", Name, writer);
		writer.WriteUInt32LittleEndian(nameHash);
		writer.WriteUInt32LittleEndian((uint)Type);
		writer.WriteUInt32LittleEndian((uint)Flags);
		foreach (uint unknownUInt in unknownUInts)
		{
			writer.WriteUInt32LittleEndian(unknownUInt);
		}
		writer.Write((ushort)Lods.Count);
		ushort num = 0;
		foreach (MeshSetLod lod in Lods)
		{
			num = (ushort)(num + (ushort)lod.Sections.Count);
		}
		writer.Write((ushort)num);
		if (Type == MeshType.MeshType_Skinned)
		{
			writer.Write(boneCount);
			writer.Write((ushort)boneIndices.Count);
			if (boneCount > 0)
			{
				meshContainer.WriteRelocPtr("BONEINDICES", boneIndices, writer);
				meshContainer.WriteRelocPtr("BONEBBOXES", boneBoundingBoxes, writer);
			}
		}
		else if (Type == MeshType.MeshType_Composite)
		{
			writer.Write((ushort)boneIndices.Count);
			writer.Write(boneCount);
		}
		writer.WritePadding(16);
		foreach (MeshSetLod lod2 in Lods)
		{
			meshContainer.AddOffset("LOD", lod2, writer);
			lod2.Process(writer, meshContainer);
		}
		foreach (MeshSetLod lod3 in Lods)
		{
			meshContainer.AddOffset("SECTION", lod3.Sections, writer);
			foreach (MeshSetSection section3 in lod3.Sections)
			{
				section3.Process(writer, meshContainer);
			}
		}
		writer.WritePadding(16);
		foreach (MeshSetLod lod5 in Lods)
		{
			foreach (MeshSetSection section2 in lod5.Sections)
			{
				if (section2.BoneList.Count == 0)
				{
					continue;
				}
				meshContainer.AddOffset("BONELIST", section2.BoneList, writer);
				foreach (ushort bone in section2.BoneList)
				{
					writer.Write(bone);
				}
			}
		}
		writer.WritePadding(16);
		meshContainer.WriteStrings(writer);
		writer.WritePadding(16);
		foreach (MeshSetLod lod6 in Lods)
		{
			foreach (List<byte> categorySubsetIndex in lod6.CategorySubsetIndices)
			{
				meshContainer.AddOffset("SUBSET", categorySubsetIndex, writer);
				writer.WriteBytes(categorySubsetIndex.ToArray());
			}
		}
		writer.WritePadding(16);
		if (Type != MeshType.MeshType_Skinned)
		{
			return;
		}
		foreach (MeshSetLod lod4 in Lods)
		{
			meshContainer.AddOffset("BONES", lod4.BoneIndexArray, writer);
			foreach (uint item in lod4.BoneIndexArray)
			{
				writer.WriteUInt32LittleEndian(item);
			}
		}
		writer.WritePadding(16);
		meshContainer.AddOffset("BONEINDICES", boneIndices, writer);
		foreach (ushort boneIndex in boneIndices)
		{ 		
			writer.Write(boneIndex);
		}
		writer.WritePadding(16);
		meshContainer.AddOffset("BONEBBOXES", boneBoundingBoxes, writer);
		foreach (AxisAlignedBox boneBoundingBox in boneBoundingBoxes)
		{
			writer.WriteAxisAlignedBox(boneBoundingBox);
		}
		writer.WritePadding(16);
	}
}

*/