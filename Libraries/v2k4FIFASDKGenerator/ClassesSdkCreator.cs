﻿using FrostyEditor;
using FrostyEditor.IO;
using FrostyEditor.Windows;
using FrostySdk;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using v2k4FIFASDKGenerator.BaseInfo;
using static Frosty.OpenFrostyFiles;
using FieldInfo = v2k4FIFASDKGenerator.BaseInfo.FieldInfo;

namespace v2k4FIFASDKGenerator
{
    public class ClassesSdkCreator
    {
        public static AssetManager AssetManager;

        public static ResourceManager ResourceManager;

        public static FileSystem FileSystem;

        public static EbxAssetEntry SelectedAsset;

        public static string configFilename = "FrostyEditor.ini";


        

        public static long offset;

        private List<ClassInfo> classInfos = new List<ClassInfo>();

        private List<string> alreadyProcessedClasses = new List<string>();

        private Dictionary<long, ClassInfo> offsetClassInfoMapping = new Dictionary<long, ClassInfo>();

        private List<EbxClass> processed = new List<EbxClass>();

        private Dictionary<string, List<EbxField>> fieldMapping;

        private Dictionary<string, Tuple<EbxClass, DbObject>> mapping;

        private List<Tuple<EbxClass, DbObject>> values;

        private DbObject classList;

        private DbObject classMetaList;

        private SdkUpdateState state;

        public ClassesSdkCreator(SdkUpdateState inState)
        {
            state = inState;
        }

        public bool GatherTypeInfos(SdkUpdateTask task)
        {
            Trace.WriteLine("GatherTypeInfos");
            Debug.WriteLine("GatherTypeInfos");
            Console.WriteLine("GatherTypeInfos");

            var executingAssembly = Assembly.GetExecutingAssembly();
            var names = executingAssembly.GetManifestResourceNames();
            //using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FrostyEditor.Classes.txt"))
            //using (Stream stream = executingAssembly.GetManifestResourceStream("v2k4FIFASDKGenerator.Classes.txt"))
            using (FileStream stream = new FileStream("FIFA20.Classes.txt", FileMode.Open))
            {
                if (stream != null)
                {
                    classMetaList = TypeLibrary.LoadClassesSDK(stream);
                }
            }
            
            classList = DumpClasses(task);
            if (classList != null)
            {
                Trace.WriteLine("Classes Dumped");
                Debug.WriteLine("Classes Dumped");
                Console.WriteLine("Classes Dumped");

                return classList.Count > 0;
            }
            return false;
        }

        

        public bool CrossReferenceAssets(SdkUpdateTask task)
        {
            if(FileSystem == null)
            {
                FileSystem = AssetManager.Instance.fs;
            }
            // Data must be loaded and cached before SDK is built

            mapping = new Dictionary<string, Tuple<EbxClass, DbObject>>();
            fieldMapping = new Dictionary<string, List<EbxField>>();
            if (FileSystem.HasFileInMemoryFs("SharedTypeDescriptors.ebx"))
            {
                List<Guid> existingClasses = new List<Guid>();
                LoadSharedTypeDescriptors("SharedTypeDescriptors.ebx", mapping, existingClasses);
                LoadSharedTypeDescriptors("SharedTypeDescriptors_patch.ebx", mapping, existingClasses);
                LoadSharedTypeDescriptors("SharedTypeDescriptors_Patch.ebx", mapping, existingClasses);
            }
            else
            {
                throw new Exception("Havent found Shared Type Descriptors .EBX!");
                //uint ebxCount = AssetManager.GetEbxCount();
                //uint num = 0u;
                //foreach (EbxAssetEntry item3 in AssetManager.EnumerateEbx())
                //{
                //    Stream ebxStream = AssetManager.GetEbxStream(item3);
                //    if (ebxStream != null)
                //    {
                //        task.StatusMessage = $"{(float)(double)(++num) / (float)(double)ebxCount * 100f:0}%";
                //        using (EbxReader ebxReader = new EbxReader(ebxStream))
                //        {
                //            List<EbxClass> classTypes = ebxReader.ClassTypes;
                //            List<EbxField> fieldTypes = ebxReader.FieldTypes;
                //            foreach (EbxClass item4 in classTypes)
                //            {
                //                if (item4.Name != "array" && !mapping.ContainsKey(item4.Name))
                //                {
                //                    DbObject item = null;
                //                    int num2 = 0;
                //                    foreach (DbObject @class in classList)
                //                    {
                //                        if (@class.GetValue<string>("name") == item4.Name)
                //                        {
                //                            item = @class;
                //                            classList.RemoveAt(num2);
                //                            break;
                //                        }
                //                        num2++;
                //                    }
                //                    mapping.Add(item4.Name, new Tuple<EbxClass, DbObject>(item4, item));
                //                    fieldMapping.Add(item4.Name, new List<EbxField>());
                //                    for (int i = 0; i < item4.FieldCount; i++)
                //                    {
                //                        EbxField item2 = fieldTypes[item4.FieldIndex + i];
                //                        fieldMapping[item4.Name].Add(item2);
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}
            }
            return true;
        }

        public bool CreateSDK()
        {
            DbObject dbObject = new DbObject(bObject: false);
            values = mapping.Values.ToList();
            values.Sort((Tuple<EbxClass, DbObject> a, Tuple<EbxClass, DbObject> b) => a.Item1.Name.CompareTo(b.Item1.Name));

            Debug.WriteLine("Creating SDK");

            for (int i = 0; i < values.Count; i++)
            {
                Tuple<EbxClass, DbObject> tuple = values[i];
                EbxClass item = tuple.Item1;
                DbObject item2 = tuple.Item2;
                if (item2 != null)
                {
                    int num = (item.DebugType == EbxFieldType.Pointer) ? 8 : 0;
                    int fieldIndex = 0;
                    ProcessClass(item, item2, fieldMapping[item.Name], dbObject, ref num, ref fieldIndex);
                }
            }
            List<DbObject> list = new List<DbObject>();
            foreach (DbObject @class in classList)
            {
                var className = @class.GetValue<string>("name");
                if (className.Contains("positioning"))
                {

                }
                if(className.Contains("schema"))
                {

                }
                if (className.Contains("attrib"))
                {

                }
                if (className.Contains("gp_actor_movement"))
                {

                }
                if (!fieldMapping.ContainsKey(className))
                {
                    if ((byte)((@class.GetValue("flags", 0) >> 4) & 0x1F) == 3 && @class.GetValue("alignment", 0) == 0)
                    {
                        @class.SetValue("alignment", 4);
                    }
                    EbxClass ebxClass = default(EbxClass);
                    ebxClass.Name = @class.GetValue<string>("name");
                    ebxClass.Type = (ushort)@class.GetValue("flags", 0);
                    ebxClass.Alignment = (byte)@class.GetValue("alignment", 0);
                    ebxClass.FieldCount = (byte)@class.GetValue<DbObject>("fields").Count;
                    EbxClass classItem = ebxClass;
                    List<EbxField> fieldList = new List<EbxField>();
                    foreach (DbObject fieldInList in @class.GetValue<DbObject>("fields"))
                    {
                        EbxField ebxField = default(EbxField);
                        ebxField.Name = fieldInList.GetValue<string>("name");
                        ebxField.Type = (ushort)fieldInList.GetValue("flags", 0);
                        ebxField.NameHash = (uint)fieldInList.GetValue("nameHash",0);
                        fieldList.Add(ebxField);
                    }
                    if(ebxClass.Name.ToLower().Contains("attribschema"))
                    {

                    }
                    values.Add(new Tuple<EbxClass, DbObject>(classItem, @class));
                    fieldMapping.Add(classItem.Name, fieldList);
                    list.Add(@class);
                }
            }
            foreach (DbObject classObj in list)
            {
                if (classObj.HasValue("basic"))
                {
                    dbObject.Add(classObj);
                }
                else
                {
                    Tuple<EbxClass, DbObject> tuple2 = values.Find((Tuple<EbxClass, DbObject> a) => a.Item2 == classObj);
                    int num2 = 0;
                    int fieldIndex2 = 0;
                    ProcessClass(tuple2.Item1, tuple2.Item2, fieldMapping[tuple2.Item1.Name], dbObject, ref num2, ref fieldIndex2);
                }
            }
            using (ModuleWriter moduleWriter = new ModuleWriter("EbxClasses.dll", dbObject))
            {
                moduleWriter.Write(FileSystem.Head);
            }
            if (File.Exists("EbxClasses.dll"))
            {
                FileInfo fileInfo = new FileInfo(".\\TmpProfiles\\" + ProfilesLibrary.SDKFilename + ".dll");
                if (!fileInfo.Directory.Exists)
                {
                    Directory.CreateDirectory(fileInfo.Directory.FullName);
                }
                if (fileInfo.Exists)
                {
                    File.Delete(fileInfo.FullName);
                }
                File.Move("EbxClasses.dll", fileInfo.FullName);
                return true;
            }
            Console.WriteLine("Failed to produce SDK");
            return false;
        }

        private void LoadSharedTypeDescriptors(string name, Dictionary<string, Tuple<EbxClass, DbObject>> mapping, List<Guid> existingClasses)
        {
            byte[] fileFromMemoryFs = FileSystem.GetFileFromMemoryFs(name);
            if (fileFromMemoryFs != null)
            {
                Dictionary<uint, DbObject> dictionary = new Dictionary<uint, DbObject>();
                Dictionary<uint, string> dictionary2 = new Dictionary<uint, string>();
                foreach (DbObject @class in classList)
                {
                    if (!@class.HasValue("basic"))
                    {
                        dictionary.Add((uint)@class.GetValue("nameHash", 0), @class);
                        foreach (DbObject item4 in @class.GetValue<DbObject>("fields"))
                        {
                            if (!dictionary2.ContainsKey((uint)item4.GetValue("nameHash", 0)))
                            {
                                dictionary2.Add((uint)item4.GetValue("nameHash", 0), item4.GetValue("name", ""));
                            }
                        }
                    }
                }
                using (NativeReader nativeReader = new NativeReader(new MemoryStream(fileFromMemoryFs)))
                {
                    nativeReader.ReadUInt();
                    ushort num = nativeReader.ReadUShort();
                    ushort num2 = nativeReader.ReadUShort();
                    List<EbxField> lstFields = new List<EbxField>();
                    for (int i = 0; i < num2; i++)
                    {
                        uint num3 = nativeReader.ReadUInt();
                        EbxField field = default(EbxField);
                        field.Name = (dictionary2.ContainsKey(num3) ? dictionary2[num3] : "");
                        field.NameHash = num3;
                        field.Type = (ushort)(nativeReader.ReadUShort() >> 1);
                        field.ClassRef = nativeReader.ReadUShort();
                        field.DataOffset = nativeReader.ReadUInt();
                        field.SecondOffset = nativeReader.ReadUInt();
                        lstFields.Add(field);
                    }
                    int num4 = 0;
                    List<EbxClass?> list2 = new List<EbxClass?>();
                    List<Guid> list3 = new List<Guid>();
                    for (int j = 0; j < num; j++)
                    {
                        long position = nativeReader.Position;
                        Guid guid2 = nativeReader.ReadGuid();
                        Guid b = nativeReader.ReadGuid();
                        if (existingClasses.Contains(guid2) && guid2 == b)
                        {
                            list3.Add(guid2);
                            list2.Add(null);
                        }
                        else
                        {
                            existingClasses.Add(guid2);
                            nativeReader.Position -= 16L;
                            uint nameHash = nativeReader.ReadUInt();
                            uint num5 = nativeReader.ReadUInt();
                            int num6 = nativeReader.ReadByte();
                            byte b2 = nativeReader.ReadByte();
                            ushort type = nativeReader.ReadUShort();
                            uint num7 = nativeReader.ReadUInt();
                            if ((b2 & 0x80) != 0)
                            {
                                num6 += 256;
                                b2 = (byte)(b2 & 0x7F);
                            }
                            EbxClass value = default(EbxClass);
                            value.NameHash = nameHash;
                            value.FieldCount = (byte)num6;
                            value.FieldIndex = (int)((position - (num5 - 8)) / 16);
                            value.Alignment = b2;
                            value.Type = type;
                            value.Size = (ushort)num7;
                            value.Index = j;
                            list2.Add(value);
                            list3.Add(guid2);
                        }
                    }
                    for (int k = 0; k < list2.Count; k++)
                    {
                        if (list2[k].HasValue)
                        {
                            EbxClass value2 = list2[k].Value;
                            Guid guid = list3[k];
                            if (dictionary.ContainsKey(value2.NameHash))
                            {
                                DbObject dbObject3 = dictionary[value2.NameHash];
                                if (mapping.ContainsKey(dbObject3.GetValue("name", "")))
                                {
                                    mapping.Remove(dbObject3.GetValue("name", ""));
                                    fieldMapping.Remove(dbObject3.GetValue("name", ""));
                                }
                                if (!dbObject3.HasValue("typeInfoGuid"))
                                {
                                    dbObject3.SetValue("typeInfoGuid", DbObject.CreateList());
                                }
                                if (dbObject3.GetValue<DbObject>("typeInfoGuid").FindIndex((object a) => (Guid)a == guid) == -1)
                                {
                                    dbObject3.GetValue<DbObject>("typeInfoGuid").Add(guid);
                                }
                                EbxClass item2 = default(EbxClass);
                                item2.Name = dbObject3.GetValue("name", "");
                                item2.FieldCount = value2.FieldCount;
                                item2.Alignment = value2.Alignment;
                                item2.Size = value2.Size;
                                item2.Type = (ushort)(value2.Type >> 1);
                                item2.SecondSize = (ushort)dbObject3.GetValue("size", 0);
                                mapping.Add(item2.Name, new Tuple<EbxClass, DbObject>(item2, dbObject3));
                                fieldMapping.Add(item2.Name, new List<EbxField>());
                                DbObject dbObjectFields = dbObject3.GetValue<DbObject>("fields");
                                DbObject dbObject4 = DbObject.CreateList();
                                dbObject3.RemoveValue("fields");
                                for (int l = 0; l < value2.FieldCount; l++)
                                {
                                    EbxField field = lstFields[value2.FieldIndex + l];
                                    bool flag = false;
                                    foreach (DbObject dbObjField in dbObjectFields)
                                    {
                                        var dbObjNameHash = dbObjField.GetValue("nameHash", 0);
                                        if (dbObjNameHash == (int)field.NameHash)
                                        {
                                            dbObjField.SetValue("type", field.Type);
                                            dbObjField.SetValue("offset", field.DataOffset);
                                            dbObjField.SetValue("value", (int)field.DataOffset);
                                            if (field.DebugType == EbxFieldType.Array)
                                            {
                                                Guid guid3 = list3[value2.Index + (short)field.ClassRef];
                                                dbObjField.SetValue("guid", guid3);
                                            }

                                            dbObject4.Add(dbObjField);
                                            flag = true;
                                            break;
                                        }
                                    }
                                    if (!flag)
                                    {
                                        uint num8 = (ProfilesLibrary.DataVersion == 20190905 || ProfilesLibrary.IsFIFA21DataVersion()) ? 3301947476u : 3109710567u;
                                        if (field.NameHash != num8)
                                        {
                                            field.Name = ((field.Name != "") ? field.Name : ("Unknown_" + field.NameHash.ToString("x8")));
                                            DbObject dbObject6 = DbObject.CreateObject();
                                            dbObject6.SetValue("name", field.Name);
                                            dbObject6.SetValue("nameHash", (int)field.NameHash);
                                            dbObject6.SetValue("type", field.Type);
                                            dbObject6.SetValue("flags", (ushort)0);
                                            dbObject6.SetValue("offset", field.DataOffset);
                                            dbObject6.SetValue("value", (int)field.DataOffset);
                                            dbObject4.Add(dbObject6);
                                        }
                                    }
                                    fieldMapping[item2.Name].Add(field);
                                    num4++;
                                }
                                dbObject3.SetValue("fields", dbObject4);
                            }
                            else
                            {
                                num4 += value2.FieldCount;
                            }
                        }
                    }
                }
            }
        }

        private int ProcessClass(EbxClass pclass, DbObject pobj, List<EbxField> fields, DbObject outList, ref int offset, ref int fieldIndex)
        {
            string parent = pobj.GetValue<string>("parent");
            if (parent != "")
            {
                Tuple<EbxClass, DbObject> tuple2 = values.Find((Tuple<EbxClass, DbObject> a) => a.Item1.Name == parent);
                offset = ProcessClass(tuple2.Item1, tuple2.Item2, fieldMapping[tuple2.Item1.Name], outList, ref offset, ref fieldIndex);
                if (tuple2.Item1.Name == "DataContainer" && pclass.Name != "Asset")
                {
                    pobj.SetValue("isData", true);
                }
            }
            if (processed.Contains(pclass))
            {
                foreach (DbObject dbObject in outList.List)
                {
                    if (dbObject.GetValue<string>("name") == pclass.Name)
                    {
                        fieldIndex += dbObject.GetValue<DbObject>("fields").Count;
                        return dbObject.GetValue<int>("size");
                    }
                }
                return 0;
            }
            processed.Add(pclass);
            DbObject dbObject2 = classMetaList.List.FirstOrDefault((object o) => ((DbObject)o).GetValue<string>("name") == pclass.Name) as DbObject;
            DbObject dbObject3 = pobj.GetValue<DbObject>("fields");
            DbObject dbObject4 = DbObject.CreateList();
            if (pclass.DebugType == EbxFieldType.Enum)
            {
                foreach (DbObject dbObject8 in dbObject3.List)
                {
                    DbObject dbObject12 = DbObject.CreateObject();
                    dbObject12.AddValue("name", dbObject8.GetValue<string>("name"));
                    dbObject12.AddValue("value", dbObject8.GetValue<int>("value"));
                    dbObject4.List.Add(dbObject12);
                }
            }
            else
            {
                List<EbxField> ebxFieldList = new List<EbxField>();
                foreach (DbObject dbObject7 in dbObject3.List)
                {
                    ebxFieldList.Add(new EbxField
                    {
                        Name = dbObject7.GetValue<string>("name"),
                        Type = (ushort)dbObject7.GetValue<int>("flags"),
                        DataOffset = (uint)dbObject7.GetValue<int>("offset"),
                        NameHash = (uint)dbObject7.GetValue<int>("nameHash")
                    });
                }
                ebxFieldList.Sort((EbxField a, EbxField b) => a.DataOffset.CompareTo(b.DataOffset));
                foreach (EbxField ebxField in ebxFieldList)
                {
                    EbxField field = ebxField;
                    if (field.DebugType == EbxFieldType.Inherited)
                    {
                        continue;
                    }
                    DbObject dbObject6 = null;
                    foreach (DbObject dbObject11 in dbObject3.List)
                    {
                        if (dbObject11.GetValue<string>("name") == field.Name)
                        {
                            dbObject6 = dbObject11;
                            break;
                        }
                    }
                    if (dbObject6 == null)
                    {
                        Console.WriteLine(pclass.Name + "." + field.Name + " missing from executable definition");
                        continue;
                    }
                    DbObject fieldObj = DbObject.CreateObject();
                    if (dbObject2 != null)
                    {
                        DbObject dbObject10 = dbObject2.GetValue<DbObject>("fields");
            DbObject dbObject13 = classMetaList.List.FirstOrDefault((object o) => ((DbObject)o).GetValue<string>("name") == pclass.Name) as DbObject;
                        if (dbObject13 !=  null)
                        {
                            fieldObj.AddValue("meta", dbObject13);
                        }
                    }
                    fieldObj.AddValue("name", field.Name);
                    fieldObj.AddValue("type", (int)field.DebugType);
                    fieldObj.AddValue("flags", (int)field.Type);
                    fieldObj.AddValue("offset", (int)field.DataOffset);
                    fieldObj.AddValue("nameHash", field.NameHash);
                    if (field.DebugType == EbxFieldType.Pointer || field.DebugType == EbxFieldType.Struct || field.DebugType == EbxFieldType.Enum)
                    {
                        string baseTypeName2 = dbObject6.GetValue<string>("baseType");
                        int index2 = values.FindIndex((Tuple<EbxClass, DbObject> a) => a.Item1.Name == baseTypeName2 && !a.Item2.HasValue("basic"));
                        if (index2 != -1)
                        {
                            fieldObj.AddValue("baseType", values[index2].Item1.Name);
                        }
                        else if (field.DebugType == EbxFieldType.Enum)
                        {
                            throw new InvalidDataException();
                        }
                        if (field.DebugType == EbxFieldType.Struct)
                        {
                            foreach (EbxField field2 in fields)
                            {
                                if (field2.Name.Equals(field.Name))
                                {
                                    if (field.Type != field2.Type)
                                    {
                                        fieldObj.SetValue("flags", (int)field2.Type);
                                    }
                                    break;
                                }
                            }
                            while (offset % (int)values[index2].Item1.Alignment != 0)
                            {
                                offset++;
                            }
                        }
                    }
                    else if (field.DebugType == EbxFieldType.Array)
                    {
                        string baseTypeName = dbObject6.GetValue<string>("baseType");
                        int index3 = values.FindIndex((Tuple<EbxClass, DbObject> a) => a.Item1.Name == baseTypeName && !a.Item2.HasValue("basic"));
                        if (index3 != -1)
                        {
                            fieldObj.AddValue("baseType", values[index3].Item1.Name);
                            fieldObj.AddValue("arrayFlags", (int)values[index3].Item1.Type);
                        }
                        else
                        {
                            EbxFieldType ebxFieldType = (EbxFieldType)((uint)(dbObject6.GetValue<int>("arrayFlags") >> 4) & 0x1Fu);
                            if (ebxFieldType - 2 <= EbxFieldType.DbObject || ebxFieldType == EbxFieldType.Enum)
                            {
                                fieldObj.AddValue("baseType", baseTypeName);
                            }
                            fieldObj.AddValue("arrayFlags", dbObject6.GetValue<int>("arrayFlags"));
                        }
                        if (dbObject6.HasValue("guid"))
                        {
                            fieldObj.SetValue("guid", dbObject6.GetValue<Guid>("guid"));
                        }
                    }
                    if (field.DebugType == EbxFieldType.ResourceRef || field.DebugType == EbxFieldType.TypeRef || field.DebugType == EbxFieldType.FileRef || field.DebugType == EbxFieldType.BoxedValueRef)
                    {
                        while (offset % 8 != 0)
                        {
                            offset++;
                        }
                    }
                    else if (field.DebugType == EbxFieldType.Array || field.DebugType == EbxFieldType.Pointer)
                    {
                        while (offset % 4 != 0)
                        {
                            offset++;
                        }
                    }
                    fieldObj.AddValue("offset", offset);
                    fieldObj.SetValue("index", dbObject6.GetValue<int>("index") + fieldIndex);
                    dbObject4.List.Add(fieldObj);
                    switch (field.DebugType)
                    {
                        case EbxFieldType.Struct:
                            {
                                Tuple<EbxClass, DbObject> tuple = values.Find((Tuple<EbxClass, DbObject> a) => a.Item1.Name == fieldObj.GetValue<string>("baseType"));
                                int offset2 = 0;
                                int fieldIndex2 = 0;
                                offset += ProcessClass(tuple.Item1, tuple.Item2, fieldMapping[tuple.Item1.Name], outList, ref offset2, ref fieldIndex2);
                                break;
                            }
                        case EbxFieldType.Pointer:
                            offset += 4;
                            break;
                        case EbxFieldType.Array:
                            offset += 4;
                            break;
                        case EbxFieldType.String:
                            offset += 32;
                            break;
                        case EbxFieldType.CString:
                            offset += 4;
                            break;
                        case EbxFieldType.Enum:
                            offset += 4;
                            break;
                        case EbxFieldType.FileRef:
                            offset += 8;
                            break;
                        case EbxFieldType.Boolean:
                            offset++;
                            break;
                        case EbxFieldType.Int8:
                            offset++;
                            break;
                        case EbxFieldType.UInt8:
                            offset++;
                            break;
                        case EbxFieldType.Int16:
                            offset += 2;
                            break;
                        case EbxFieldType.UInt16:
                            offset += 2;
                            break;
                        case EbxFieldType.Int32:
                            offset += 4;
                            break;
                        case EbxFieldType.UInt32:
                            offset += 4;
                            break;
                        case EbxFieldType.UInt64:
                            offset += 8;
                            break;
                        case EbxFieldType.Int64:
                            offset += 8;
                            break;
                        case EbxFieldType.Float32:
                            offset += 4;
                            break;
                        case EbxFieldType.Float64:
                            offset += 8;
                            break;
                        case EbxFieldType.Guid:
                            offset += 16;
                            break;
                        case EbxFieldType.Sha1:
                            offset += 20;
                            break;
                        case EbxFieldType.ResourceRef:
                            offset += 8;
                            break;
                        case EbxFieldType.TypeRef:
                            offset += 8;
                            break;
                        case EbxFieldType.BoxedValueRef:
                            offset += 16;
                            break;
                    }
                }
            }
            while (offset % (int)pclass.Alignment != 0)
            {
                offset++;
            }
            pobj.SetValue("flags", (int)pclass.Type);
            pobj.SetValue("size", offset);
            if (pclass.DebugType == EbxFieldType.Enum)
            {
                pobj.SetValue("size", 4);
            }
            pobj.SetValue("alignment", (int)pclass.Alignment);
            pobj.SetValue("fields", dbObject4);
            fieldIndex += dbObject4.Count;
            if (dbObject2 != null)
            {
                pobj.AddValue("meta", dbObject2);
                foreach (DbObject dbObject5 in dbObject2.GetValue<DbObject>("fields").List)
                {
                    if (dbObject5.GetValue<bool>("added"))
                    {
                        DbObject dbObject9 = DbObject.CreateObject();
                        dbObject9.AddValue("name", dbObject5.GetValue<string>("name"));
                        dbObject9.AddValue("type", 15);
                        dbObject9.AddValue("meta", dbObject5);
                        pobj.GetValue<DbObject>("fields").List.Add(dbObject9);
                    }
                }
            }
            outList.List.Add(pobj);
            return offset;
        }


        private DbObject DumpClasses(SdkUpdateTask task)
        {
            MemoryReader memoryReader = null;
            //string typeStr = "v2k4FIFASDKGenerator.ClassesSdkCreator+ClassInfo";
            string typeStr = "v2k4FIFASDKGenerator.Madden21.ClassInfo";

            //if (ProfilesLibrary.DataVersion == 20181207)
            //{
            //    str = "FrostyEditor.Anthem.";
            //}
            //else if (ProfilesLibrary.DataVersion == 20190729)
            //{
            //    str = "FrostyEditor.Madden20.";
            //}
            //else if (ProfilesLibrary.DataVersion == 20190905)
            //{
            //    str = "FrostyEditor.Madden20.";
            //}
            //else 
            if (ProfilesLibrary.DataVersion == 20190911)
            {
                typeStr = "v2k4FIFASDKGenerator.Madden20.ClassInfo";
            }
            else if (ProfilesLibrary.DisplayName.Contains("18"))
            {
                typeStr = "v2k4FIFASDKGenerator.FIFA18.ClassInfo";
            }
            else if (ProfilesLibrary.IsFIFA21DataVersion())
            {
                typeStr = "v2k4FIFASDKGenerator.FIFA21.ClassInfo";
            }
            //else if (ProfilesLibrary.DataVersion == 20191101)
            //{
            //    str = "FrostyEditor.Madden20.";
            //}

            // Find types to find out all is good
            //Assembly thisLibClasses = typeof(v2k4FIFASDKGenerator.ClassesSdkCreator).Assembly;
            //var types = thisLibClasses.GetTypes();
            //foreach (Type type in types)
            //{
            //    Debug.WriteLine(type.FullName);
            //}


            long typeInfoOffset = state.TypeInfoOffset;
            memoryReader = new MemoryReader(state.Process, typeInfoOffset);
            offsetClassInfoMapping.Clear();
            classInfos.Clear();
            alreadyProcessedClasses.Clear();
            processed.Clear();
            if (fieldMapping != null)
            {
                fieldMapping.Clear();
            }
            offset = typeInfoOffset;
            int num = 0;
            while (offset != 0L)
            {
                task.StatusMessage = $"Found {++num} type(s)";
                memoryReader.Position = offset;
                var t = Type.GetType(typeStr);
                ClassInfo classInfo = (ClassInfo)Activator.CreateInstance(t);
                classInfo.Read(memoryReader);
                //Debug.WriteLine(classInfo.typeInfo.name);
                classInfos.Add(classInfo);
                offsetClassInfoMapping.Add(typeInfoOffset, classInfo);
                if (offset != 0L)
                {
                    typeInfoOffset = offset;
                }
            }
            Debug.WriteLine(task.StatusMessage);
            memoryReader.Dispose();
            DbObject result = new DbObject(bObject: false);
            classInfos.Sort((ClassInfo a, ClassInfo b) => a.typeInfo.name.CompareTo(b.typeInfo.name));

            var findSomeStuffTest = classInfos.Where(x => x.typeInfo.name.ToLower().Contains("lua")).ToList();

            foreach (ClassInfo classInfo2 in classInfos)
            {
                if (classInfo2.typeInfo.Type == 2
                    || classInfo2.typeInfo.Type == 3
                    || classInfo2.typeInfo.Type == 8
                    || classInfo2.typeInfo.Type == 27)
                {
                    if (classInfo2.typeInfo.Type == 27)
                    {
                        classInfo2.typeInfo.flags = 48;
                    }
                    CreateClassObject(classInfo2, ref result);
                }
                else if (classInfo2.typeInfo.Type != 4)
                {
                    CreateBasicClassObject(classInfo2, ref result);
                }
            }
            return result;
        }

        private void CreateBasicClassObject(ClassInfo classInfo, ref DbObject classList)
        {
            int alignment = classInfo.typeInfo.alignment;
            int size = (int)classInfo.typeInfo.size;
            DbObject dbObject = DbObject.CreateObject();
            dbObject.SetValue("name", classInfo.typeInfo.name);
            dbObject.SetValue("type", classInfo.typeInfo.Type);
            dbObject.SetValue("flags", (int)classInfo.typeInfo.flags);
            dbObject.SetValue("alignment", alignment);
            dbObject.SetValue("size", size);
            dbObject.SetValue("runtimeSize", size);
            if (classInfo.typeInfo.guid != Guid.Empty)
            {
                dbObject.SetValue("guid", classInfo.typeInfo.guid);
            }
            dbObject.SetValue("namespace", classInfo.typeInfo.nameSpace);
            dbObject.SetValue("fields", DbObject.CreateList());
            dbObject.SetValue("parent", "");
            dbObject.SetValue("basic", true);
            classInfo.typeInfo.Modify(dbObject);
            classList.Add(dbObject);
        }

        private void CreateClassObject(ClassInfo classInfo, ref DbObject classList)
        {
            if (!alreadyProcessedClasses.Contains(classInfo.typeInfo.name))
            {
                ClassInfo classInfo2 = offsetClassInfoMapping.ContainsKey(classInfo.parentClass) ? offsetClassInfoMapping[classInfo.parentClass] : null;
                if (classInfo2 != null)
                {
                    CreateClassObject(classInfo2, ref classList);
                }
                int alignment = classInfo.typeInfo.alignment;
                int size = (int)classInfo.typeInfo.size;
                DbObject dbObject = new DbObject();
                dbObject.AddValue("name", classInfo.typeInfo.name);
                dbObject.AddValue("parent", (classInfo2 != null) ? classInfo2.typeInfo.name : "");
                dbObject.AddValue("type", classInfo.typeInfo.Type);
                dbObject.AddValue("flags", (int)classInfo.typeInfo.flags);
                dbObject.AddValue("alignment", alignment);
                dbObject.AddValue("size", size);
                dbObject.AddValue("runtimeSize", size);
                dbObject.AddValue("additional", (int)classInfo.isDataContainer);
                dbObject.AddValue("namespace", classInfo.typeInfo.nameSpace);
                if (classInfo.typeInfo.guid != Guid.Empty)
                {
                    dbObject.AddValue("guid", classInfo.typeInfo.guid);
                }
                classInfo.typeInfo.Modify(dbObject);
                DbObject dbObject2 = new DbObject(bObject: false);
                foreach (FieldInfo field in classInfo.typeInfo.fields)
                {
                    DbObject dbObject3 = new DbObject();
                    if (classInfo.typeInfo.Type == 8)
                    {
                        dbObject3.AddValue("name", field.name);
                        dbObject3.AddValue("value", (int)field.typeOffset);
                    }
                    else
                    {
                        ClassInfo classInfo3 = offsetClassInfoMapping[field.typeOffset];
                        dbObject3.AddValue("name", field.name);
                        dbObject3.AddValue("type", classInfo3.typeInfo.Type);
                        dbObject3.AddValue("flags", (int)classInfo3.typeInfo.flags);
                        dbObject3.AddValue("offset", (int)field.offset);
                        dbObject3.AddValue("index", field.index);
                        if (classInfo3.typeInfo.Type == 3
                            || classInfo3.typeInfo.Type == 2 
                            || classInfo3.typeInfo.Type == 8)
                        {
                            dbObject3.AddValue("baseType", classInfo3.typeInfo.name);
                        }
                        else if (classInfo3.typeInfo.Type == 4)
                        {
                            classInfo3 = offsetClassInfoMapping[classInfo3.parentClass];
                            dbObject3.AddValue("baseType", classInfo3.typeInfo.name);
                            dbObject3.AddValue("arrayFlags", (int)classInfo3.typeInfo.flags);
                        }
                    }
                    field.Modify(dbObject3);
                    dbObject2.Add(dbObject3);
                }
                dbObject.AddValue("fields", dbObject2);
                classList.Add(dbObject);
                alreadyProcessedClasses.Add(classInfo.typeInfo.name);
            }
        }
    }
}