﻿using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.FrostySdk.IO;
using FrostySdk.FrostySdk.IO._2022.Readers;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;


namespace FrostySdk.IO._2022.Readers
{
	public class EbxReader22A : EbxReader
	{
		private List<EbxClass> classTypes = new List<EbxClass>();

		internal byte[] boxedValueBuffer;

		//internal EbxVersion magic;

		public override string RootType => this.classTypes[this.instances[0].ClassRef].Name;

		internal EbxReader22A(Stream inStream, bool passthru)
			: base(inStream)
		{
			Position = 0;
			if (inStream == null)
			{
				throw new ArgumentNullException("inStream");
			}
		}

        public override EbxAsset ReadAsset(EbxAssetEntry entry = null)
        {
			EbxAsset ebxAsset = new EbxAsset();
			ebxAsset.ParentEntry = entry;
			this.InternalReadObjects();
			ebxAsset.fileGuid = fileGuid;
			ebxAsset.objects = objects;
			ebxAsset.dependencies = dependencies;
			ebxAsset.refCounts = refCounts;
			return ebxAsset;
		}

        public virtual T ReadAsset<T>() where T : EbxAsset, new()
		{
			T val = new T();
			this.InternalReadObjects();
			val.fileGuid = this.fileGuid;
			val.objects = this.objects;
			val.dependencies = this.dependencies;
			val.refCounts = this.refCounts;
			return val;
		}

		public new dynamic ReadObject()
		{
			this.InternalReadObjects();
			return this.objects[0];
		}

		public override List<object> ReadObjects()
		{
			this.InternalReadObjects();
			return this.objects;
		}

		public override List<object> GetUnreferencedObjects()
		{
			List<object> list = new List<object> { this.objects[0] };
			for (int i = 1; i < this.objects.Count; i++)
			{
				if (this.refCounts[i] == 0)
				{
					list.Add(this.objects[i]);
				}
			}
			return list;
		}

		public override void InternalReadObjects()
		{
			foreach (EbxInstance instance in this.instances)
			{
				EbxClass @class = this.GetClass(null, instance.ClassRef);
				for (int i = 0; i < instance.Count; i++)
				{
					Type inType = this.ParseClass(@class);
					this.objects.Add(TypeLibrary.CreateObject(inType));
					this.refCounts.Add(0);
				}
			}
			int num = 0;
			int num2 = 0;
			foreach (EbxInstance instance2 in this.instances)
			{
				EbxClass class2 = this.GetClass(null, instance2.ClassRef);
				for (int j = 0; j < instance2.Count; j++)
				{
					base.Pad(class2.Alignment);
					Guid inGuid = Guid.Empty;
					if (instance2.IsExported)
					{
						inGuid = base.ReadGuid();
					}
					if (class2.Alignment != 4)
					{
						base.Position += 8L;
					}
					dynamic val = this.objects[num++];
					val.SetInstanceGuid(new AssetClassGuid(inGuid, num2++));
					this.ReadClass(class2, val, base.Position - 8);
				}
			}
			if (this.boxedValuesCount != 0)
			{
				base.Position = this.boxedValuesOffset;
				this.boxedValueBuffer = base.ReadToEnd();
			}
		}

		private static IEnumerable<EbxField> _AllEbxFields;
		public static IEnumerable<EbxField> AllEbxFields 
		{ 
			get
			{
				if (_AllEbxFields == null)
					_AllEbxFields = EbxReader22B.patchStd.Fields.Union(EbxReader22B.std.Fields);

				return _AllEbxFields;
            }
		}

		private static Dictionary<uint, EbxField> NameHashToEbxField { get; } = new Dictionary<uint, EbxField>();

		public static EbxField GetEbxFieldByNameHash(uint nameHash)
		{
			if (NameHashToEbxField.ContainsKey(nameHash))
				return NameHashToEbxField[nameHash];

			var field = AllEbxFields.Single(x => x.NameHash == nameHash);
			NameHashToEbxField.Add(nameHash, field);
			return field;
        }

        public static EbxField GetEbxFieldByProperty(EbxClass classType, PropertyInfo property)
        {
			var fieldIndex = property.GetCustomAttribute<FieldIndexAttribute>().Index;
			var nameHash = property.GetCustomAttribute<HashAttribute>().Hash;
			var ebxfieldmeta = property.GetCustomAttribute<EbxFieldMetaAttribute>();
            EbxFieldType fieldType = (EbxFieldType)((ebxfieldmeta.Flags >> 4) & 0x1Fu);

            // This needs fixing by looking at the movement GP
            // Need to include both patch and non patch in search, which highlights how shit the current system is!

            var allFields = EbxReader22B.std.Fields;
			List<EbxField> classFields = EbxReader22B.std.ClassFields.ContainsKey(classType) ? EbxReader22B.std.ClassFields[classType] : null;
            if (classType.SecondSize >= 1)
            {
                classFields = EbxReader22B.patchStd.ClassFields[classType];
				allFields = allFields.Union(EbxReader22B.patchStd.Fields).ToList();
            }

			EbxField field;
			var nameHashFields = classFields.Where(x => x.NameHash == nameHash);
			field = nameHashFields.FirstOrDefault(x => (x.DebugType == fieldType || (ebxfieldmeta.IsArray && x.DebugType == ebxfieldmeta.ArrayType)));
			//if (field.Equals(default(EbxField)) && fieldIndex > -1 && classFields.Count > fieldIndex)
			//	field = classFields[fieldIndex];
			//else 
			if (field.Equals(default(EbxField)))
                field = allFields.FirstOrDefault(x => x.NameHash == nameHash && (x.DebugType == fieldType || (ebxfieldmeta.IsArray && x.DebugType == ebxfieldmeta.ArrayType)));

			return field;
        }

        private static IEnumerable<EbxClass> _AllEbxClasses;
        public static IEnumerable<EbxClass> AllEbxClasses
        {
            get
            {
				if (_AllEbxClasses == null)
					_AllEbxClasses = EbxReader22B.patchStd.Classes.Union(EbxReader22B.std.Classes).Where(x => x.HasValue).Select(x => x.Value);

                return _AllEbxClasses;
            }
        }

        private static Dictionary<uint, EbxClass> NameHashToEbxClass { get; } = new Dictionary<uint, EbxClass>();

        public static EbxClass GetEbxClassByNameHash(uint nameHash)
        {
            if (NameHashToEbxClass.ContainsKey(nameHash))
                return NameHashToEbxClass[nameHash];

            var @class = AllEbxClasses.Single(x => x.NameHash == nameHash);
            NameHashToEbxClass.Add(nameHash, @class);
            return @class;
        }



        public override object ReadClass(EbxClass classType, object obj, long startOffset)
		{
			if (obj == null)
			{
				base.Position += classType.Size;
				base.Pad(classType.Alignment);
				return null;
			}
			Type type = obj.GetType();
            var ebxClassMeta = type.GetCustomAttribute<EbxClassMetaAttribute>();

#if DEBUG
			if (type.Name.Equals("SkinnedMeshAsset"))
			{

			}

			if (type.Name.Contains("MeshMaterial"))
			{

			}

			if (type.Name.Contains("SkeletonAsset"))
            {

            }

			if (type.Name.Contains("LinearTransform"))
			{

			}
#endif

			Dictionary<PropertyInfo, EbxFieldMetaAttribute> properties = new Dictionary<PropertyInfo, EbxFieldMetaAttribute>();
			foreach(var prp in obj.GetType().GetProperties())
            {
				var ebxfieldmeta = prp.GetCustomAttribute<EbxFieldMetaAttribute>();
				if(ebxfieldmeta != null)
                {
					properties.Add(prp, ebxfieldmeta);
				}
            }

			var orderedProps = properties
				.Where(x => x.Key.GetCustomAttribute<IsTransientAttribute>() == null && x.Key.GetCustomAttribute<FieldIndexAttribute>() != null)
				.OrderBy(x => x.Key.GetCustomAttribute<FieldIndexAttribute>().Index);

            foreach (var property in orderedProps)
            {
                var isTransient = property.Key.GetCustomAttribute<IsTransientAttribute>();
				if (isTransient != null)
					continue;

				IsReferenceAttribute referenceAttribute = property.Key.GetCustomAttribute<IsReferenceAttribute>();
				var isReferenceAttribute = referenceAttribute != null;

                var propFieldIndex = property.Key.GetCustomAttribute<FieldIndexAttribute>();
                var propNameHash = property.Key.GetCustomAttribute<HashAttribute>();
                if (propFieldIndex == null && propNameHash == null)
                    continue;

                EbxField field = default(EbxField);
                if (property.Key.Name.Contains("MaterialVariation")) 
				{ 
				}

                EbxFieldType debugType = (EbxFieldType)((property.Value.Flags >> 4) & 0x1Fu);


                if (propNameHash != null)
				{
					field = GetEbxFieldByProperty(classType, property.Key);
					//field = GetEbxFieldByNameHash(propNameHash.Hash);// AllEbxFields.FirstOrDefault(x => x.NameHash == propNameHash.Hash);
					//if (type.Name.Contains("LinearTransform") && property.Key.PropertyType.FullName.Equals("FrostySdk.Ebx.Vec3"))
					//{
     //                   var f2 = this.GetField(classType, classType.FieldIndex + propFieldIndex.Index);
     //                   field = f2;
     //               }
                }
				//           else 
				//if (propFieldIndex != null)
				//            {
				//field = this.GetField(classType, classType.FieldIndex + propFieldIndex.Index);
				//            }

				EbxTypeCategory debugTypeTC = field.TypeCategory;
                var classRef = field.ClassRef;

                // Variable from SDK is King here! Override DebugType.
                //if (field.DebugType != debugType) 
                //	field.Type = property.Value.Flags;

                if (debugType == EbxFieldType.Inherited)
                {
                    ReadClass(GetClass(classType, field.ClassRef), obj, startOffset);
                    continue;
                }
                //if (debugType == EbxFieldType.ResourceRef
                //    || debugType == EbxFieldType.TypeRef
                //    || debugType == EbxFieldType.FileRef
                //    || debugType == EbxFieldType.BoxedValueRef
                //    || debugType == EbxFieldType.UInt64
                //    || debugType == EbxFieldType.Int64
                //    || debugType == EbxFieldType.Float64)
                //{
                //    base.Pad(8);
                //}
                //else
                //{
                //    if (debugType == EbxFieldType.Array
                //        || debugType == EbxFieldType.Pointer
                //        )
                //    {
                //        base.Pad(4);
                //    }
                //}
                base.Position = property.Value.Offset + startOffset;
				//if(debugType == EbxFieldType.Guid)
				//{
				//	base.Pad(16);
				//}
                if (debugType == EbxFieldType.Array)
                {
     //               if (type.Name.Contains("SkeletonAsset"))
     //                   ReadArray(obj, property.Key, classType, isReferenceAttribute);
					//else
						ReadArray(obj, property.Key, classType, field, isReferenceAttribute);

                    continue;
                }
				object value = null;
                try
                {
                    value = ReadField(classType, debugType, field.ClassRef, false);
					//if(debugType == EbxFieldType.Guid)
					//{
					//	if (value.ToString().StartsWith("0000"))
					//	{

					//	}
					//}
                    property.Key.SetValue(obj, value);
                }
                catch (Exception ex)
                {
					Debug.WriteLine($"{ex.Message}");
					Debug.WriteLine($"Class:{type.Name}:Property:{property.Key.Name} failed to set with value {value}");
                }
				finally
				{

				}
            }

			if (ebxClassMeta != null)
			{
                base.Position = startOffset + ebxClassMeta.Size;
                base.Pad(ebxClassMeta.Alignment);
            }
			else
			{
				base.Position = startOffset + classType.Size;
				base.Pad(classType.Alignment);
			}
			return null;
		}

		protected virtual void ReadArray(object obj, PropertyInfo property, EbxClass classType, bool isReference)
		{
			if(obj.GetType().Name == "FloatCurve")
            {

            }
			long position = base.Position;
			int arrayOffset = base.ReadInt32LittleEndian();
			base.Position += arrayOffset - 4;
			base.Position -= 4L;
			uint arrayCount = base.ReadUInt32LittleEndian();
			var genericType = property.PropertyType.GetGenericArguments()[0];
            EbxField field = default(EbxField);

			var nameHashAttribute = genericType.GetCustomAttribute<HashAttribute>();
			var ebxfieldmeta = genericType.GetCustomAttribute<EbxFieldMetaAttribute>();
			var ebxclassmeta = genericType.GetCustomAttribute<EbxClassMetaAttribute>();
			if (nameHashAttribute != null && ebxfieldmeta != null)
				field = EbxReader22A.GetEbxFieldByNameHash(nameHashAttribute.Hash);
			else if (nameHashAttribute != null && ebxclassmeta != null)
				classType = GetEbxClassByNameHash(nameHashAttribute.Hash);
                //classType = EbxReader22B.patchStd.Classes
                //		  .Union(EbxReader22B.std.Classes).FirstOrDefault(x => x.HasValue && x.Value.NameHash == nameHashAttribute.Hash).Value;

                //EbxClass @class = GetClass(classType, 65535);

            for (int i = 0; i < arrayCount; i++)
			{
				EbxFieldType ebxFieldType = EbxFieldType.Struct;
				if (!Enum.TryParse<EbxFieldType>(genericType.Name, out ebxFieldType)) 
				{
					ebxFieldType = EbxFieldType.Struct;
                }
				field.DebugTypeOverride = genericType.Name == "Single" ? EbxFieldType.Float32 : ebxFieldType;
				classType.DebugTypeOverride = genericType.Name == "Single" ? EbxFieldType.Float32 : ebxFieldType;
				field.ClassRef = 65535;
				try
				{
					object obj2 = null;
					if (field.DebugType == EbxFieldType.Struct)
					{
						obj2 = CreateObject(classType);
						this.ReadClass(classType, obj2, base.Position);
					}
					else
						obj2 = this.ReadField(classType, field.DebugType, field.ClassRef, isReference);
					if (property != null)
					{
                    
							property.GetValue(obj).GetType().GetMethod("Add")
								.Invoke(property.GetValue(obj), new object[1] { obj2 });
                    
					}
				}
				catch (Exception)
				{
				}
				//EbxFieldType debugType = field.DebugType;
				if (ebxFieldType == EbxFieldType.Pointer || ebxFieldType == EbxFieldType.CString)
				{
					base.Pad(8);
				}
			}
			base.Position = position;
		}

		protected virtual void ReadArray(object obj, PropertyInfo property, EbxClass classType, EbxField field, bool isReference)
		{
			EbxClass @class = this.GetClass(classType, field.ClassRef);
			int index = base.ReadInt32LittleEndian();
			EbxArray ebxArray = this.arrays[index];
			long position = base.Position;
			base.Position = this.arraysOffset + ebxArray.Offset;
			for (int i = 0; i < ebxArray.Count; i++)
			{
				EbxField field2 = this.GetField(@class, @class.FieldIndex);
				object obj2 = this.ReadField(@class, field2.DebugType, field2.ClassRef, isReference);
				if (property != null)
				{
					try
					{
						property.GetValue(obj).GetType().GetMethod("Add")
							.Invoke(property.GetValue(obj), new object[1] { obj2 });
					}
					catch (Exception)
					{
					}
				}
			}
			base.Position = position;
		}

		protected bool IsFieldInClassAnArray(EbxClass @class, EbxField field)
		{
			return field.TypeCategory == EbxTypeCategory.ArrayType
				|| field.DebugType == EbxFieldType.Array;
				//|| field.DebugType22 == EbxFieldType22.ArrayOfStructs;
		}

		public override PropertyInfo GetProperty(Type objType, EbxField field)
		{
			return objType.GetProperty(field.Name);
		}

		public override EbxClass GetClass(EbxClass? parentClass, int index)
		{
			return this.classTypes[index];
		}

		public override EbxField GetField(EbxClass classType, int index)
		{
			return this.fieldTypes[index];
		}

		public override object CreateObject(EbxClass classType)
		{
			return TypeLibrary.CreateObject(classType.Name);
		}

		public virtual Type GetType(EbxClass classType)
		{
			return TypeLibrary.GetType(classType.Name);
		}

		public override object ReadField(EbxClass? parentClass, EbxFieldType fieldType, ushort fieldClassRef, bool dontRefCount = false)
		{
			if (buffer == null || BaseStream == null)
				return null;

			switch (fieldType)
			{
				case EbxFieldType.Boolean:
					return base.ReadByte() > 0;
				case EbxFieldType.Int8:
					return (sbyte)base.ReadByte();
				case EbxFieldType.UInt8:
					return base.ReadByte();
				case EbxFieldType.Int16:
					return base.ReadInt16LittleEndian();
				case EbxFieldType.UInt16:
					return base.ReadUInt16LittleEndian();
				case EbxFieldType.Int32:
					return base.ReadInt32LittleEndian();
				case EbxFieldType.UInt32:
					return base.ReadUInt32LittleEndian();
				case EbxFieldType.Int64:
					return base.ReadInt64LittleEndian();
				case EbxFieldType.UInt64:
					return base.ReadUInt64LittleEndian();
				case EbxFieldType.Float32:
					return base.ReadSingleLittleEndian();
				case EbxFieldType.Float64:
					return base.ReadDoubleLittleEndian();
				case EbxFieldType.Guid:
					return base.ReadGuid();
				case EbxFieldType.ResourceRef:
					return this.ReadResourceRef();
				case EbxFieldType.Sha1:
					return base.ReadSha1();
				case EbxFieldType.String:
					return base.ReadSizedString(32);
				case EbxFieldType.CString:
					return this.ReadCString(base.ReadUInt32LittleEndian());
				case EbxFieldType.FileRef:
					return this.ReadFileRef();
				case EbxFieldType.TypeRef:
					return this.ReadTypeRef();
				case EbxFieldType.BoxedValueRef:
					return this.ReadBoxedValueRef();
				case EbxFieldType.Struct:
					{
						var positionBeforeRead = base.Position;
                        EbxClass @class = GetClass(parentClass, fieldClassRef);
                        base.Pad(@class.Alignment);
                        object obj = CreateObject(@class);
                        this.ReadClass(@class, obj, base.Position);
						base.Position = positionBeforeRead + @class.Size;
                        return obj;
                    }
				case EbxFieldType.Enum:
					return base.ReadInt32LittleEndian();
				case EbxFieldType.Pointer:
					{
						uint num = base.ReadUInt32LittleEndian();
						if (num >> 31 == 1)
						{
							return new PointerRef(this.imports[(int)(num & 0x7FFFFFFF)]);
						}
						if (num == 0)
						{
							return default(PointerRef);
						}
						if (!dontRefCount)
						{
							this.refCounts[(int)(num - 1)]++;
						}
						return new PointerRef(this.objects[(int)(num - 1)]);
					}
				case EbxFieldType.DbObject:
					throw new InvalidDataException("DbObject");
				case EbxFieldType.Inherited:
					{
						return null;
                    }
                default:
					{
                        DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Unknown field type ");
						defaultInterpolatedStringHandler.AppendFormatted(fieldType);
						throw new InvalidDataException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
			}
		}

		internal Type ParseClass(EbxClass classType)
		{
			Type type = TypeLibrary.AddType(classType.Name);
			if (type != null)
			{
				return type;
			}
			List<FieldType> list = new List<FieldType>();
			Type parentType = null;
			for (int i = 0; i < classType.FieldCount; i++)
			{
				EbxField ebxField = this.fieldTypes[classType.FieldIndex + i];
				if (ebxField.DebugType == EbxFieldType.Inherited)
				{
					parentType = this.ParseClass(this.classTypes[ebxField.ClassRef]);
					continue;
				}
				Type typeFromEbxField = this.GetTypeFromEbxField(ebxField);
				list.Add(new FieldType(ebxField.Name, typeFromEbxField, null, ebxField, (ebxField.DebugType == EbxFieldType.Array) ? new EbxField?(this.fieldTypes[this.classTypes[ebxField.ClassRef].FieldIndex]) : null));
			}
			if (classType.DebugType == EbxFieldType.Struct)
			{
				return TypeLibrary.FinalizeStruct(classType.Name, list, classType);
			}
			return TypeLibrary.FinalizeClass(classType.Name, list, parentType, classType);
		}

		internal Type GetTypeFromEbxField(EbxField fieldType)
		{
			switch (fieldType.DebugType)
			{
				case EbxFieldType.DbObject:
					return null;
				case EbxFieldType.Struct:
					return this.ParseClass(this.classTypes[fieldType.ClassRef]);
				case EbxFieldType.Pointer:
					return typeof(PointerRef);
				case EbxFieldType.Array:
					{
						EbxClass ebxClass = this.classTypes[fieldType.ClassRef];
						return typeof(List<>).MakeGenericType(this.GetTypeFromEbxField(this.fieldTypes[ebxClass.FieldIndex]));
					}
				case EbxFieldType.String:
					return typeof(string);
				case EbxFieldType.CString:
					return typeof(CString);
				case EbxFieldType.Enum:
					{
						EbxClass classInfo = this.classTypes[fieldType.ClassRef];
						List<Tuple<string, int>> list = new List<Tuple<string, int>>();
						for (int i = 0; i < classInfo.FieldCount; i++)
						{
							list.Add(new Tuple<string, int>(this.fieldTypes[classInfo.FieldIndex + i].Name, (int)this.fieldTypes[classInfo.FieldIndex + i].DataOffset));
						}
						return TypeLibrary.AddEnum(classInfo.Name, list, classInfo);
					}
				case EbxFieldType.FileRef:
					return typeof(FileRef);
				case EbxFieldType.Boolean:
					return typeof(bool);
				case EbxFieldType.Int8:
					return typeof(sbyte);
				case EbxFieldType.UInt8:
					return typeof(byte);
				case EbxFieldType.Int16:
					return typeof(short);
				case EbxFieldType.UInt16:
					return typeof(ushort);
				case EbxFieldType.Int32:
					return typeof(int);
				case EbxFieldType.UInt32:
					return typeof(uint);
				case EbxFieldType.UInt64:
					return typeof(ulong);
				case EbxFieldType.Int64:
					return typeof(long);
				case EbxFieldType.Float32:
					return typeof(float);
				case EbxFieldType.Float64:
					return typeof(double);
				case EbxFieldType.Guid:
					return typeof(Guid);
				case EbxFieldType.Sha1:
					return typeof(Sha1);
				case EbxFieldType.ResourceRef:
					return typeof(ResourceRef);
				case EbxFieldType.TypeRef:
					return typeof(TypeRef);
				case EbxFieldType.BoxedValueRef:
					return typeof(ulong);
				default:
					return null;
			}
		}

		internal string ReadString(uint offset)
		{
			if (offset == uint.MaxValue)
			{
				return string.Empty;
			}
			long position = base.Position;
			if (this.magic == EbxVersion.Riff)
			{
				base.Position = position + offset - 4;
			}
			else
			{
				base.Position = this.stringsOffset + offset;
			}
			string result = base.ReadNullTerminatedString();
			base.Position = position;
			return result;
		}

		internal CString ReadCString(uint offset)
		{
			return new CString(this.ReadString(offset));
		}

		internal ResourceRef ReadResourceRef()
		{
			return new ResourceRef(base.ReadUInt64LittleEndian());
		}

		internal FileRef ReadFileRef()
		{
			uint offset = base.ReadUInt32LittleEndian();
			base.Position += 4L;
			return new FileRef(this.ReadString(offset));
		}

		internal TypeRef ReadTypeRef()
		{
			if (this.magic == EbxVersion.Riff)
			{
				return new TypeRef(base.ReadUInt32LittleEndian().ToString(CultureInfo.InvariantCulture));
			}
			string text = this.ReadString(base.ReadUInt32LittleEndian());
			base.Position += 4L;
			if (text == "")
			{
				return new TypeRef();
			}
			if (Guid.TryParse(text, out var result) && result != Guid.Empty)
			{
				return new TypeRef(result);
			}
			return new TypeRef(text);
		}

		internal new BoxedValueRef ReadBoxedValueRef()
		{
			uint value = base.ReadUInt32LittleEndian();
			int unk = base.ReadInt32LittleEndian();
			long offset = base.ReadInt64LittleEndian();
			long restorePosition = base.Position;
			try
			{
				_ = -1;
				if ((value & 0x80000000u) == 2147483648u)
				{
					value &= 0x7FFFFFFFu;
					EbxFieldType typeCode = (EbxFieldType)((value >> 5) & 0x1Fu);
					base.Position += offset - 8;
					return new BoxedValueRef(this.ReadField(null, typeCode, ushort.MaxValue), typeCode);
				}
				return new BoxedValueRef();
			}
			finally
			{
				base.Position = restorePosition;
			}
		}

		internal new int HashString(string strToHash)
		{
			int num = 5381;
			for (int i = 0; i < strToHash.Length; i++)
			{
				byte b = (byte)strToHash[i];
				num = (num * 33) ^ b;
			}
			return num;
		}
	}
}