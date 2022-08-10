using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FrostySdk.IO
{
	public class EbxReaderV2 : EbxReader
	{
		public static IEbxSharedTypeDescriptor std;

		public static IEbxSharedTypeDescriptor patchStd;

		public List<Guid> classGuids = new List<Guid>();

		public bool patched;

		public override string RootType => TypeLibrary.GetType(classGuids[instances[0].ClassRef])?.Name ?? string.Empty;

		public static void InitialiseStd()
		{
			if (!string.IsNullOrEmpty(ProfilesLibrary.EBXTypeDescriptor))
			{
				if (std == null)
				{
					std = (IEbxSharedTypeDescriptor)AssetManager.Instance.LoadTypeByName(ProfilesLibrary.EBXTypeDescriptor
						, AssetManager.Instance.fs, "SharedTypeDescriptors.ebx", false);
					//std = new EbxSharedTypeDescriptorV2(AssetManager.Instance.fs, "SharedTypeDescriptors.ebx", patch: false);
				}
				if (patchStd == null)
				{
					if (AssetManager.Instance.fs.HasFileInMemoryFs("SharedTypeDescriptors_patch.ebx"))
					{
						patchStd = (IEbxSharedTypeDescriptor)AssetManager.Instance.LoadTypeByName(ProfilesLibrary.EBXTypeDescriptor
						, AssetManager.Instance.fs, "SharedTypeDescriptors_patch.ebx", true);
					}
				}
				if (patchStd == null)
				{
					if (AssetManager.Instance.fs.HasFileInMemoryFs("SharedTypeDescriptors_Patch.ebx"))
					{
						patchStd = (IEbxSharedTypeDescriptor)AssetManager.Instance.LoadTypeByName(ProfilesLibrary.EBXTypeDescriptor
						, AssetManager.Instance.fs, "SharedTypeDescriptors_Patch.ebx", true);
					}
				}

			}
			else
			{

				if (std == null)
				{
					if (ProfilesLibrary.IsFIFA19DataVersion())
						std = new EbxSharedTypeDescriptors(AssetManager.Instance.fs, "Dictionaries/ebx.dict", patch: false);
					else
						std = new EbxSharedTypeDescriptors(AssetManager.Instance.fs, "SharedTypeDescriptors.ebx", patch: false);
				}

				if (patchStd == null)
				{
					var allSTDs = AssetManager.Instance.fs.memoryFs.Where(x => x.Key.Contains("SharedTypeDescriptors", StringComparison.OrdinalIgnoreCase)).ToList();
					if (AssetManager.Instance.fs.HasFileInMemoryFs("SharedTypeDescriptors_patch.ebx"))
					{
						patchStd = new EbxSharedTypeDescriptors(AssetManager.Instance.fs, "SharedTypeDescriptors_patch.ebx", patch: true);
					}
					if (AssetManager.Instance.fs.HasFileInMemoryFs("SharedTypeDescriptors_Patch.ebx"))
					{
						patchStd = new EbxSharedTypeDescriptors(AssetManager.Instance.fs, "SharedTypeDescriptors_Patch.ebx", patch: true);
					}
				}
			}
		}

		public EbxReaderV2(Stream InStream, bool inPatched)
			: base(InStream, passthru: true)
		{
			InitialiseStd();
			//if (std == null)
			//{
			//	std = new EbxSharedTypeDescriptors(FileSystem.Instance, "SharedTypeDescriptors.ebx", patch: false);
			//	if (FileSystem.Instance.HasFileInMemoryFs("SharedTypeDescriptors_patch.ebx"))
			//	{
			//		patchStd = new EbxSharedTypeDescriptors(FileSystem.Instance, "SharedTypeDescriptors_patch.ebx", patch: true);
			//	}
			//	if (FileSystem.Instance.HasFileInMemoryFs("SharedTypeDescriptors_Patch.ebx"))
			//	{
			//		patchStd = new EbxSharedTypeDescriptors(FileSystem.Instance, "SharedTypeDescriptors_Patch.ebx", patch: true);
			//	}
			//}




			patched = inPatched;
			ebxVersion = (EbxVersion)ReadUInt32LittleEndian();
			if (ebxVersion != EbxVersion.Version2 && ebxVersion != EbxVersion.Version4)
			{
				return;
			}
			stringsOffset = ReadUInt32LittleEndian();
			stringsAndDataLen = ReadUInt32LittleEndian();
			guidCount = ReadUInt32LittleEndian();
			instanceCount = ReadUInt16LittleEndian();
			exportedCount = ReadUInt16LittleEndian();
			uniqueClassCount = ReadUInt16LittleEndian();
			classTypeCount = ReadUInt16LittleEndian();
			fieldTypeCount = ReadUInt16LittleEndian();
			typeNamesLen = ReadUInt16LittleEndian();
			stringsLen = ReadUInt32LittleEndian();
			arrayCount = ReadUInt32LittleEndian();
			dataLen = ReadUInt32LittleEndian();
			arraysOffset = stringsOffset + stringsLen + dataLen;
			fileGuid = ReadGuid();
			boxedValuesCount = ReadUInt32LittleEndian();
			boxedValuesOffset = ReadUInt32LittleEndian();
			boxedValuesOffset += stringsOffset + stringsLen;
			for (int num = 0; num < guidCount; num++)
			{
				EbxImportReference ebxImportReference = new EbxImportReference
				{
					FileGuid = ReadGuid(),
					ClassGuid = ReadGuid()
				};
				imports.Add(ebxImportReference);
				if (!dependencies.Contains(ebxImportReference.FileGuid))
				{
					dependencies.Add(ebxImportReference.FileGuid);
				}
			}
			Dictionary<int, string> dictionary = new Dictionary<int, string>();
			long position = base.Position;
			while (base.Position - position < typeNamesLen)
			{
				string text = ReadNullTerminatedString();
				int key = HashString(text);
				dictionary.TryAdd(key, text);
			}
			for (int i = 0; i < fieldTypeCount; i++)
			{
				EbxField item = default(EbxField);
				int key2 = ReadInt32LittleEndian();
				item.Type = ((ebxVersion == EbxVersion.Version2) ? ReadUInt16LittleEndian() : ((ushort)(ReadUInt16LittleEndian() >> 1)));
				item.ClassRef = ReadUInt16LittleEndian();
				item.DataOffset = ReadUInt32LittleEndian();
				item.SecondOffset = ReadUInt32LittleEndian();
				item.Name = dictionary[key2];
				fieldTypes.Add(item);
			}
			for (int j = 0; j < classTypeCount; j++)
			{
				Guid item2 = ReadGuid();
				classGuids.Add(item2);
			}
			ushort num2 = exportedCount;
			for (int k = 0; k < instanceCount; k++)
			{
				EbxInstance item3 = new EbxInstance
				{
					ClassRef = ReadUInt16LittleEndian(),
					Count = ReadUInt16LittleEndian()
				};
				if (num2 != 0)
				{
					item3.IsExported = true;
					num2 = (ushort)(num2 - 1);
				}
				instances.Add(item3);
			}
			while (base.Position % 16 != 0L)
			{
				base.Position++;
			}
			for (int num3 = 0; num3 < arrayCount; num3++)
			{
				EbxArray item4 = new EbxArray
				{
					Offset = ReadUInt32LittleEndian(),
					Count = ReadUInt32LittleEndian(),
					ClassRef = ReadInt32LittleEndian()
				};
				arrays.Add(item4);
			}
			Pad(16);
			for (int l = 0; l < boxedValuesCount; l++)
			{
				EbxBoxedValue item5 = new EbxBoxedValue
				{
					Offset = ReadUInt32LittleEndian(),
					ClassRef = ReadUInt16LittleEndian(),
					Type = ReadUInt16LittleEndian()
				};
				boxedValues.Add(item5);
			}
			base.Position = stringsOffset + stringsLen;
			isValid = true;


            if (RootType.Contains("gp_"))
            {
                InStream.Position = 0;
                var fsDump = new FileStream("ebxV2_" + RootType + ".dat", FileMode.OpenOrCreate);
                InStream.CopyTo(fsDump);
                fsDump.Close();
                fsDump.Dispose();
                InStream.Position = stringsOffset + stringsLen;
            }


        }

		public override void InternalReadObjects()
		{
			foreach (EbxInstance ebxInstance in instances)
			{
				Type type = TypeLibrary.GetType(classGuids[ebxInstance.ClassRef]);
				for (int i = 0; i < ebxInstance.Count; i++)
				{
					objects.Add(TypeLibrary.CreateObject(type));
					refCounts.Add(0);
				}
			}
			int num = 0;
			int num2 = 0;
			foreach (EbxInstance ebxInstance2 in instances)
			{
				for (int j = 0; j < ebxInstance2.Count; j++)
				{
					dynamic obj = objects[num++];
					Type objType = obj.GetType();
					EbxClass @class = GetClass(objType);
					while (base.Position % (long)@class.Alignment != 0L)
					{
						base.Position++;
					}
					Guid inGuid = Guid.Empty;
					if (ebxInstance2.IsExported)
					{
						inGuid = ReadGuid();
					}
					if (@class.Alignment != 4)
					{
						base.Position += 8L;
					}
					obj.SetInstanceGuid(new AssetClassGuid(inGuid, num2++));
					this.ReadClass(@class, obj, base.Position - 8);
				}
			}
		}

		public EbxClass GetClass(Type objType)
		{
			EbxClass? ebxClass = null;
			foreach (TypeInfoGuidAttribute typeInfoGuidAttribute in objType.GetCustomAttributes(typeof(TypeInfoGuidAttribute), inherit: true).Cast<TypeInfoGuidAttribute>())
			{
				if (classGuids.Contains(typeInfoGuidAttribute.Guid))
				{
					if (patched && patchStd != null)
					{
						ebxClass = patchStd.GetClass(typeInfoGuidAttribute.Guid);
					}
					if (!ebxClass.HasValue)
					{
						ebxClass = std.GetClass(typeInfoGuidAttribute.Guid);
					}
					break;
				}
			}
			return ebxClass.Value;
		}

		public override PropertyInfo GetProperty(Type objType, EbxField field)
		{
			if (field.NameHash == 3109710567u)
			{
				return null;
			}
			PropertyInfo[] properties = objType.GetProperties();
			foreach (PropertyInfo propertyInfo in properties)
			{
				HashAttribute customAttribute = propertyInfo.GetCustomAttribute<HashAttribute>();
				if (customAttribute != null && customAttribute.Hash == (int)field.NameHash)
				{
					return propertyInfo;
				}
			}
			return null;
		}

		public override EbxClass GetClass(EbxClass? classType, int index)
		{
			EbxClass? ebxClass = null;
			Guid? guid = null;
			int index2 = (short)index + (classType.HasValue ? classType.Value.Index : 0);
			guid = std.GetGuid(index2);
			if (classType.HasValue && classType.Value.SecondSize == 1)
			{
				guid = patchStd.GetGuid(index2);
				ebxClass = patchStd.GetClass(index2);
				if (!ebxClass.HasValue)
				{
					ebxClass = std.GetClass(guid.Value);
				}
			}
			else
			{
				ebxClass = std.GetClass(index2);
			}
			if (ebxClass.HasValue)
			{
				TypeLibrary.AddType(ebxClass.Value.Name, guid);
			}
			return ebxClass.Value;
		}

		public override EbxField GetField(EbxClass classType, int index)
		{
			if (classType.SecondSize == 1)
			{
				return patchStd.GetField(index).Value;
			}
			return std.GetField(index).Value;
		}

		public override object CreateObject(EbxClass classType)
		{
			if (classType.SecondSize == 1)
			{
				return TypeLibrary.CreateObject(patchStd.GetGuid(classType).Value);
			}
			return TypeLibrary.CreateObject(std.GetGuid(classType).Value);
		}

		public object ReadClass(EbxClassMetaAttribute classMeta, object obj, Type objType, long startOffset)
		{
			if (obj == null)
			{
				base.Position += classMeta.Size;
				while (base.Position % (long)classMeta.Alignment != 0L)
				{
					base.Position++;
				}
				return null;
			}
			if (objType.BaseType != typeof(object))
			{
				ReadClass(objType.BaseType.GetCustomAttribute<EbxClassMetaAttribute>(), obj, objType.BaseType, startOffset);
			}
			PropertyInfo[] properties = objType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
			foreach (PropertyInfo propertyInfo in properties)
			{
				if (propertyInfo.GetCustomAttribute<IsTransientAttribute>() != null)
				{
					continue;
				}
				IsReferenceAttribute customAttribute = propertyInfo.GetCustomAttribute<IsReferenceAttribute>();
				EbxFieldMetaAttribute customAttribute2 = propertyInfo.GetCustomAttribute<EbxFieldMetaAttribute>();
				base.Position = startOffset + customAttribute2.Offset;
				if (customAttribute2.Type == EbxFieldType.Array)
				{
					int index = ReadInt32LittleEndian();
					EbxArray ebxArray = arrays[index];
					long position2 = base.Position;
					base.Position = arraysOffset + ebxArray.Offset;
					propertyInfo?.GetValue(obj).GetType().GetMethod("Clear")
						.Invoke(propertyInfo.GetValue(obj), new object[0]);
					for (int i = 0; i < ebxArray.Count; i++)
					{
						object obj2 = ReadField(customAttribute2.ArrayType, customAttribute2.BaseType, customAttribute != null);
						propertyInfo?.GetValue(obj).GetType().GetMethod("Add")
							.Invoke(propertyInfo.GetValue(obj), new object[1] { obj2 });
					}
					base.Position = position2;
				}
				else
				{
					object value = ReadField(customAttribute2.Type, propertyInfo.PropertyType, customAttribute != null);
					propertyInfo?.SetValue(obj, value);
				}
			}
			while (base.Position - startOffset != classMeta.Size)
			{
				base.Position++;
			}
			return null;
		}

		public object ReadField(EbxFieldType type, Type baseType, bool dontRefCount = false)
		{
			switch (type)
			{
				case EbxFieldType.DbObject:
					throw new InvalidDataException("DbObject");
				case EbxFieldType.Struct:
					{
						object obj = TypeLibrary.CreateObject(baseType);
						EbxClassMetaAttribute customAttribute = obj.GetType().GetCustomAttribute<EbxClassMetaAttribute>();
						while (base.Position % (long)customAttribute.Alignment != 0L)
						{
							base.Position++;
						}
						ReadClass(customAttribute, obj, obj.GetType(), base.Position);
						return obj;
					}
				case EbxFieldType.Pointer:
					{
						uint num = ReadUInt32LittleEndian();
						if (num >> 31 == 1)
						{
							EbxImportReference ebxImportReference = imports[(int)(num & 0x7FFFFFFF)];
							if (dontRefCount && !dependencies.Contains(ebxImportReference.FileGuid))
							{
								dependencies.Add(ebxImportReference.FileGuid);
							}
							return new PointerRef(ebxImportReference);
						}
						if (num == 0)
						{
							return default(PointerRef);
						}
						if (!dontRefCount)
						{
							refCounts[(int)(num - 1)]++;
						}
						return new PointerRef(objects[(int)(num - 1)]);
					}
				case EbxFieldType.String:
					return ReadSizedString(32);
				case EbxFieldType.CString:
					return ReadCString(ReadUInt32LittleEndian());
				case EbxFieldType.Enum:
					return ReadInt32LittleEndian();
				case EbxFieldType.FileRef:
					return ReadFileRef();
				case EbxFieldType.Boolean:
					return ReadByte() > 0;
				case EbxFieldType.Int8:
					return (sbyte)ReadByte();
				case EbxFieldType.UInt8:
					return ReadByte();
				case EbxFieldType.Int16:
					return ReadInt16LittleEndian();
				case EbxFieldType.UInt16:
					return ReadUInt16LittleEndian();
				case EbxFieldType.Int32:
					return ReadInt32LittleEndian();
				case EbxFieldType.UInt32:
					return ReadUInt32LittleEndian();
				case EbxFieldType.UInt64:
					return ReadUInt64LittleEndian();
				case EbxFieldType.Int64:
					return ReadInt64LittleEndian();
				case EbxFieldType.Float32:
					return ReadSingleLittleEndian();
				case EbxFieldType.Float64:
					return ReadDoubleLittleEndian();
				case EbxFieldType.Guid:
					return ReadGuid();
				case EbxFieldType.Sha1:
					return ReadSha1();
				case EbxFieldType.ResourceRef:
					return ReadResourceRef();
				case EbxFieldType.TypeRef:
					return ReadTypeRef();
				case EbxFieldType.BoxedValueRef:
					return ReadBoxedValueRef();
				default:
					throw new InvalidDataException("Unknown");
			}
		}
	}

}