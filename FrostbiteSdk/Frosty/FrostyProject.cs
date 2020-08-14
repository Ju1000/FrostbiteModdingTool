using Frosty.Hash;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace FrostySdk
{
    
    public class FrostyProject
	{
		private const uint FormatVersion = 12u;

		private const ulong Magic = 98218709832262uL;

		private string filename;

		private DateTime creationDate;

		private DateTime modifiedDate;

		private uint gameVersion;

		private ModSettings modSettings;

		public string DisplayName
		{
			get
			{
				if (filename == "")
				{
					return "New Project.fbproject";
				}
				return new FileInfo(filename).Name;
			}
		}

		public string Filename
		{
			get
			{
				return filename;
			}
			set
			{
				filename = value;
			}
		}

		public bool IsDirty
		{
			get
			{
				if (AssetManager.GetDirtyCount() == 0)
				{
					return modSettings.IsDirty;
				}
				return true;
			}
		}

		public ModSettings ModSettings => modSettings;

        public AssetManager AssetManager;
        public FileSystem FileSystem;

		public FrostyProject(AssetManager assetManager, FileSystem fileSystem)
		{
            AssetManager = assetManager;
            FileSystem = fileSystem;

			filename = "";
			creationDate = DateTime.Now;
			modifiedDate = DateTime.Now;
			gameVersion = 0u;
			modSettings = new ModSettings();
            modSettings.Author = "";
			modSettings.ClearDirtyFlag();
		}

		public bool Load(string inFilename)
		{
			filename = inFilename;
			using (NativeReader nativeReader = new NativeReader(new FileStream(inFilename, FileMode.Open, FileAccess.Read)))
			{
				if (nativeReader.ReadULong() == 98218709832262L)
				{
					return InternalLoad(nativeReader);
				}
			}
			return LegacyLoad(inFilename);
		}

		public void Save(string overrideFilename = "", bool updateDirtyState = true)
		{
			string fileName = filename;
			if (!string.IsNullOrEmpty(overrideFilename))
			{
				fileName = overrideFilename;
			}
			modifiedDate = DateTime.Now;
			gameVersion = FileSystem.Head;
			FileInfo fileInfo = new FileInfo(fileName);
			if (!fileInfo.Directory.Exists)
			{
				Directory.CreateDirectory(fileInfo.DirectoryName);
			}
			string text = fileInfo.FullName + ".tmp";
			using (NativeWriter nativeWriter = new NativeWriter(new FileStream(text, FileMode.Create)))
			{
				nativeWriter.Write(98218709832262uL);
				nativeWriter.Write(12u);
				nativeWriter.WriteNullTerminatedString(ProfilesLibrary.ProfileName);
				nativeWriter.Write(creationDate.Ticks);
				nativeWriter.Write(modifiedDate.Ticks);
				nativeWriter.Write(gameVersion);
				nativeWriter.WriteNullTerminatedString(modSettings.Title);
				nativeWriter.WriteNullTerminatedString(modSettings.Author);
				nativeWriter.WriteNullTerminatedString(modSettings.Category);
				nativeWriter.WriteNullTerminatedString(modSettings.Version);
				nativeWriter.WriteNullTerminatedString(modSettings.Description);
				if (modSettings.Icon != null && modSettings.Icon.Length != 0)
				{
					nativeWriter.Write(modSettings.Icon.Length);
					nativeWriter.Write(modSettings.Icon);
				}
				else
				{
					nativeWriter.Write(0);
				}
				for (int i = 0; i < 4; i++)
				{
					byte[] screenshot = modSettings.GetScreenshot(i);
					if (screenshot != null && screenshot.Length != 0)
					{
						nativeWriter.Write(screenshot.Length);
						nativeWriter.Write(screenshot);
					}
					else
					{
						nativeWriter.Write(0);
					}
				}
				nativeWriter.Write(0);
				long position = nativeWriter.BaseStream.Position;
				nativeWriter.Write(3735928559u);
				int num = 0;
				foreach (BundleEntry item in AssetManager.EnumerateBundles(BundleType.None, modifiedOnly: true))
				{
					if (item.Added)
					{
						nativeWriter.WriteNullTerminatedString(item.Name);
						nativeWriter.WriteNullTerminatedString(AssetManager.GetSuperBundle(item.SuperBundleId).Name);
						nativeWriter.Write((int)item.Type);
						num++;
					}
				}
				nativeWriter.BaseStream.Position = position;
				nativeWriter.Write(num);
				nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
				position = nativeWriter.BaseStream.Position;
				nativeWriter.Write(3735928559u);
				num = 0;
				foreach (EbxAssetEntry item2 in AssetManager.EnumerateEbx("", modifiedOnly: true))
				{
					if (item2.IsAdded)
					{
						nativeWriter.WriteNullTerminatedString(item2.Name);
						nativeWriter.Write(item2.Guid);
						num++;
					}
				}
				nativeWriter.BaseStream.Position = position;
				nativeWriter.Write(num);
				nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
				position = nativeWriter.BaseStream.Position;
				nativeWriter.Write(3735928559u);
				num = 0;
				foreach (ResAssetEntry item3 in AssetManager.EnumerateRes(0u, modifiedOnly: true))
				{
					if (item3.IsAdded)
					{
						nativeWriter.WriteNullTerminatedString(item3.Name);
						nativeWriter.Write(item3.ResRid);
						nativeWriter.Write(item3.ResType);
						nativeWriter.Write(item3.ResMeta);
						num++;
					}
				}
				nativeWriter.BaseStream.Position = position;
				nativeWriter.Write(num);
				nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
				position = nativeWriter.BaseStream.Position;
				nativeWriter.Write(3735928559u);
				num = 0;
				foreach (ChunkAssetEntry item4 in AssetManager.EnumerateChunks(modifiedOnly: true))
				{
					if (item4.IsAdded)
					{
						nativeWriter.Write(item4.Id);
						nativeWriter.Write(item4.H32);
						num++;
					}
				}
				nativeWriter.BaseStream.Position = position;
				nativeWriter.Write(num);
				nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
				position = nativeWriter.BaseStream.Position;
				nativeWriter.Write(3735928559u);
				num = 0;
				foreach (EbxAssetEntry item5 in AssetManager.EnumerateEbx("", modifiedOnly: true, includeLinked: true))
				{
					nativeWriter.WriteNullTerminatedString(item5.Name);
					SaveLinkedAssets(item5, nativeWriter);
					nativeWriter.Write(item5.IsDirectlyModified);
					if (item5.IsDirectlyModified)
					{
						nativeWriter.Write(item5.ModifiedEntry.IsTransientModified);
						nativeWriter.WriteNullTerminatedString(item5.ModifiedEntry.UserData);
						nativeWriter.Write(item5.AddBundles.Count);
						foreach (int addBundle in item5.AddBundles)
						{
							nativeWriter.WriteNullTerminatedString(AssetManager.GetBundleEntry(addBundle).Name);
						}
						EbxAsset asset = item5.ModifiedEntry.DataObject as EbxAsset;
						using (EbxWriter ebxWriter = new EbxWriter(new MemoryStream(), EbxWriteFlags.IncludeTransient))
						{
							ebxWriter.WriteAsset(asset);
							byte[] array = ((MemoryStream)ebxWriter.BaseStream).ToArray();
							nativeWriter.Write(array.Length);
							nativeWriter.Write(array);
						}
						if (updateDirtyState)
						{
							item5.IsDirty = false;
						}
					}
					num++;
				}
				nativeWriter.BaseStream.Position = position;
				nativeWriter.Write(num);
				nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
				position = nativeWriter.BaseStream.Position;
				nativeWriter.Write(3735928559u);
				num = 0;
				foreach (ResAssetEntry item6 in AssetManager.EnumerateRes(0u, modifiedOnly: true))
				{
					nativeWriter.WriteNullTerminatedString(item6.Name);
					SaveLinkedAssets(item6, nativeWriter);
					nativeWriter.Write(item6.IsDirectlyModified);
					if (item6.IsDirectlyModified)
					{
						nativeWriter.Write(item6.ModifiedEntry.Sha1);
						nativeWriter.Write(item6.ModifiedEntry.OriginalSize);
						if (item6.ModifiedEntry.ResMeta != null)
						{
							nativeWriter.Write(item6.ModifiedEntry.ResMeta.Length);
							nativeWriter.Write(item6.ModifiedEntry.ResMeta);
						}
						else
						{
							nativeWriter.Write(0);
						}
						nativeWriter.WriteNullTerminatedString(item6.ModifiedEntry.UserData);
						nativeWriter.Write(item6.AddBundles.Count);
						foreach (int addBundle2 in item6.AddBundles)
						{
							nativeWriter.WriteNullTerminatedString(AssetManager.GetBundleEntry(addBundle2).Name);
						}
						byte[] array2 = item6.ModifiedEntry.Data;
						if (item6.ModifiedEntry.DataObject != null)
						{
							array2 = (item6.ModifiedEntry.DataObject as ModifiedResource).Save();
						}
						nativeWriter.Write(array2.Length);
						nativeWriter.Write(array2);
						if (updateDirtyState)
						{
							item6.IsDirty = false;
						}
					}
					num++;
				}
				nativeWriter.BaseStream.Position = position;
				nativeWriter.Write(num);
				nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
				position = nativeWriter.BaseStream.Position;
				nativeWriter.Write(3735928559u);
				num = 0;
				foreach (ChunkAssetEntry item7 in AssetManager.EnumerateChunks(modifiedOnly: true))
				{
					nativeWriter.Write(item7.Id);
					nativeWriter.Write(item7.ModifiedEntry.Sha1);
					nativeWriter.Write(item7.ModifiedEntry.LogicalOffset);
					nativeWriter.Write(item7.ModifiedEntry.LogicalSize);
					nativeWriter.Write(item7.ModifiedEntry.RangeStart);
					nativeWriter.Write(item7.ModifiedEntry.RangeEnd);
					nativeWriter.Write(item7.ModifiedEntry.FirstMip);
					nativeWriter.Write(item7.ModifiedEntry.H32);
					nativeWriter.Write(item7.ModifiedEntry.AddToChunkBundle);
					nativeWriter.WriteNullTerminatedString(item7.ModifiedEntry.UserData);
					nativeWriter.Write(item7.AddBundles.Count);
					foreach (int addBundle3 in item7.AddBundles)
					{
						nativeWriter.WriteNullTerminatedString(AssetManager.GetBundleEntry(addBundle3).Name);
					}
					nativeWriter.Write(item7.ModifiedEntry.Data.Length);
					nativeWriter.Write(item7.ModifiedEntry.Data);
					if (updateDirtyState)
					{
						item7.IsDirty = false;
					}
					num++;
				}
				nativeWriter.BaseStream.Position = position;
				nativeWriter.Write(num);
				nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
				position = nativeWriter.BaseStream.Position;
				nativeWriter.Write(3735928559u);
				num = 0;
				Type[] types = Assembly.GetExecutingAssembly().GetTypes();
				foreach (Type type in types)
				{
					if (type.GetInterface(typeof(ICustomActionHandler).Name) != null)
					{
						ICustomActionHandler customActionHandler = (ICustomActionHandler)Activator.CreateInstance(type);
						if (customActionHandler != null && customActionHandler.SaveToProject(nativeWriter))
						{
							num++;
						}
					}
				}
				nativeWriter.BaseStream.Position = position;
				nativeWriter.Write(num);
				nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
				if (updateDirtyState)
				{
					modSettings.ClearDirtyFlag();
				}
			}
			if (File.Exists(text))
			{
				bool flag = false;
				using (FileStream fileStream = new FileStream(text, FileMode.Open, FileAccess.Read))
				{
					if (fileStream.Length > 0)
					{
						flag = true;
					}
				}
				if (flag)
				{
					File.Delete(fileInfo.FullName);
					File.Move(text, fileInfo.FullName);
				}
			}
		}

		public ModSettings GetModSettings()
		{
			return modSettings;
		}

		public void WriteToMod(string filename, ModSettings overrideSettings)
		{
			using (FrostyModWriter frostyModWriter = new FrostyModWriter(new FileStream(filename, FileMode.Create), overrideSettings))
			{
				frostyModWriter.WriteProject(this);
			}
		}

		public static void SaveLinkedAssets(AssetEntry entry, NativeWriter writer)
		{
			writer.Write(entry.LinkedAssets.Count);
			foreach (AssetEntry linkedAsset in entry.LinkedAssets)
			{
				writer.WriteNullTerminatedString(linkedAsset.AssetType);
				if (linkedAsset is ChunkAssetEntry)
				{
					writer.Write(((ChunkAssetEntry)linkedAsset).Id);
				}
				else
				{
					writer.WriteNullTerminatedString(linkedAsset.Name);
				}
			}
		}

		public List<AssetEntry> LoadLinkedAssets(NativeReader reader)
		{
			int num = reader.ReadInt();
			List<AssetEntry> list = new List<AssetEntry>();
			for (int i = 0; i < num; i++)
			{
				string text = reader.ReadNullTerminatedString();
				if (text == "ebx")
				{
					string name = reader.ReadNullTerminatedString();
					EbxAssetEntry ebxEntry = AssetManager.GetEbxEntry(name);
					if (ebxEntry != null)
					{
						list.Add(ebxEntry);
					}
				}
				else if (text == "res")
				{
					string name2 = reader.ReadNullTerminatedString();
					ResAssetEntry resEntry = AssetManager.GetResEntry(name2);
					if (resEntry != null)
					{
						list.Add(resEntry);
					}
				}
				else if (text == "chunk")
				{
					Guid id = reader.ReadGuid();
					ChunkAssetEntry chunkEntry = AssetManager.GetChunkEntry(id);
					if (chunkEntry != null)
					{
						list.Add(chunkEntry);
					}
				}
				else
				{
					string key = reader.ReadNullTerminatedString();
					AssetEntry customAssetEntry = AssetManager.GetCustomAssetEntry(text, key);
					if (customAssetEntry != null)
					{
						list.Add(customAssetEntry);
					}
				}
			}
			return list;
		}

		public void LoadLinkedAssets(DbObject asset, AssetEntry entry, uint version)
		{
			if (version == 2)
			{
				string value = asset.GetValue<string>("linkedAssetType");
				if (value == "res")
				{
					string value2 = asset.GetValue<string>("linkedAssetId");
					entry.LinkedAssets.Add(AssetManager.GetResEntry(value2));
				}
				else if (value == "chunk")
				{
					Guid value3 = asset.GetValue<Guid>("linkedAssetId");
					entry.LinkedAssets.Add(AssetManager.GetChunkEntry(value3));
				}
			}
			else
			{
				foreach (DbObject item in asset.GetValue<DbObject>("linkedAssets"))
				{
					string value4 = item.GetValue<string>("type");
					if (value4 == "ebx")
					{
						string value5 = item.GetValue<string>("id");
						EbxAssetEntry ebxEntry = AssetManager.GetEbxEntry(value5);
						if (ebxEntry != null)
						{
							entry.LinkedAssets.Add(ebxEntry);
						}
					}
					else if (value4 == "res")
					{
						string value6 = item.GetValue<string>("id");
						ResAssetEntry resEntry = AssetManager.GetResEntry(value6);
						if (resEntry != null)
						{
							entry.LinkedAssets.Add(resEntry);
						}
					}
					else if (value4 == "chunk")
					{
						Guid value7 = item.GetValue<Guid>("id");
						ChunkAssetEntry chunkEntry = AssetManager.GetChunkEntry(value7);
						if (chunkEntry != null)
						{
							entry.LinkedAssets.Add(chunkEntry);
						}
					}
					else
					{
						string value8 = item.GetValue<string>("id");
						AssetEntry customAssetEntry = AssetManager.GetCustomAssetEntry(value4, value8);
						if (customAssetEntry != null)
						{
							entry.LinkedAssets.Add(customAssetEntry);
						}
					}
				}
			}
		}

		private bool InternalLoad(NativeReader reader)
		{
			uint num = reader.ReadUInt();
			switch (num)
			{
			default:
				return false;
			case 0u:
			case 1u:
			case 2u:
			case 3u:
			case 4u:
			case 5u:
			case 6u:
			case 7u:
			case 8u:
				return false;
			case 9u:
			case 10u:
			case 11u:
			case 12u:
			{
				if (reader.ReadNullTerminatedString() != ProfilesLibrary.ProfileName)
				{
					return false;
				}
				Dictionary<int, AssetEntry> dictionary = new Dictionary<int, AssetEntry>();
				creationDate = new DateTime(reader.ReadLong());
				modifiedDate = new DateTime(reader.ReadLong());
				gameVersion = reader.ReadUInt();
				modSettings.Title = reader.ReadNullTerminatedString();
				modSettings.Author = reader.ReadNullTerminatedString();
				modSettings.Category = reader.ReadNullTerminatedString();
				modSettings.Version = reader.ReadNullTerminatedString();
				modSettings.Description = reader.ReadNullTerminatedString();
				int num2 = reader.ReadInt();
				if (num2 > 0)
				{
					modSettings.Icon = reader.ReadBytes(num2);
				}
				for (int i = 0; i < 4; i++)
				{
					num2 = reader.ReadInt();
					if (num2 > 0)
					{
						modSettings.SetScreenshot(i, reader.ReadBytes(num2));
					}
				}
				modSettings.ClearDirtyFlag();
				int num3 = reader.ReadInt();
				num3 = reader.ReadInt();
				for (int j = 0; j < num3; j++)
				{
					string name = reader.ReadNullTerminatedString();
					string sbname = reader.ReadNullTerminatedString();
					BundleType type = (BundleType)reader.ReadInt();
					AssetManager.AddBundle(name, type, AssetManager.GetSuperBundleId(sbname));
				}
				num3 = reader.ReadInt();
				for (int k = 0; k < num3; k++)
				{
					EbxAssetEntry ebxAssetEntry = new EbxAssetEntry();
					ebxAssetEntry.Name = reader.ReadNullTerminatedString();
					ebxAssetEntry.Guid = reader.ReadGuid();
					AssetManager.AddEbx(ebxAssetEntry);
				}
				num3 = reader.ReadInt();
				for (int l = 0; l < num3; l++)
				{
					ResAssetEntry resAssetEntry = new ResAssetEntry();
					resAssetEntry.Name = reader.ReadNullTerminatedString();
					resAssetEntry.ResRid = reader.ReadULong();
					resAssetEntry.ResType = reader.ReadUInt();
					resAssetEntry.ResMeta = reader.ReadBytes(16);
					AssetManager.AddRes(resAssetEntry);
				}
				num3 = reader.ReadInt();
				for (int m = 0; m < num3; m++)
				{
					ChunkAssetEntry chunkAssetEntry = new ChunkAssetEntry();
					chunkAssetEntry.Id = reader.ReadGuid();
					chunkAssetEntry.H32 = reader.ReadInt();
					AssetManager.AddChunk(chunkAssetEntry);
				}
				num3 = reader.ReadInt();
				for (int n = 0; n < num3; n++)
				{
					string name2 = reader.ReadNullTerminatedString();
					List<AssetEntry> collection = LoadLinkedAssets(reader);
					bool flag = reader.ReadBoolean();
					bool isTransientModified = false;
					string userData = "";
					List<int> list = new List<int>();
					byte[] buffer = null;
					if (flag)
					{
						isTransientModified = reader.ReadBoolean();
						if (num >= 12)
						{
							userData = reader.ReadNullTerminatedString();
						}
						int num4 = reader.ReadInt();
						for (int num5 = 0; num5 < num4; num5++)
						{
							string name3 = reader.ReadNullTerminatedString();
							int bundleId = AssetManager.GetBundleId(name3);
							if (bundleId != -1)
							{
								list.Add(bundleId);
							}
						}
						buffer = reader.ReadBytes(reader.ReadInt());
					}
					EbxAssetEntry ebxEntry = AssetManager.GetEbxEntry(name2);
					if (ebxEntry != null)
					{
						ebxEntry.LinkedAssets.AddRange(collection);
						ebxEntry.AddBundles.AddRange(list);
						if (flag)
						{
							ebxEntry.ModifiedEntry = new ModifiedAssetEntry();
							ebxEntry.ModifiedEntry.IsTransientModified = isTransientModified;
							ebxEntry.ModifiedEntry.UserData = userData;
							using (EbxReader ebxReader = (ProfilesLibrary.DataVersion == 20190911 && num == 9) ? new EbxReaderV2(new MemoryStream(buffer), FileSystem, inPatched: false) : new EbxReader(new MemoryStream(buffer)))
							{
								EbxAsset ebxAsset = ebxReader.ReadAsset();
								ebxEntry.ModifiedEntry.DataObject = ebxAsset;
								if (ebxEntry.IsAdded)
								{
									ebxEntry.Type = ebxAsset.RootObject.GetType().Name;
								}
								ebxEntry.ModifiedEntry.DependentAssets.AddRange(ebxAsset.Dependencies);
							}
						}
						int key = Fnv1.HashString(ebxEntry.Name);
						if (!dictionary.ContainsKey(key))
						{
							dictionary.Add(key, ebxEntry);
						}
					}
				}
				num3 = reader.ReadInt();
				for (int num6 = 0; num6 < num3; num6++)
				{
					string text = reader.ReadNullTerminatedString();
					List<AssetEntry> collection2 = LoadLinkedAssets(reader);
					bool flag2 = reader.ReadBoolean();
					Sha1 sha = Sha1.Zero;
					long originalSize = 0L;
					List<int> list2 = new List<int>();
					byte[] array = null;
					byte[] array2 = null;
					string userData2 = "";
					if (flag2)
					{
						sha = reader.ReadSha1();
						originalSize = reader.ReadLong();
						int num7 = reader.ReadInt();
						if (num7 > 0)
						{
							array = reader.ReadBytes(num7);
						}
						if (num >= 12)
						{
							userData2 = reader.ReadNullTerminatedString();
						}
						num7 = reader.ReadInt();
						for (int num8 = 0; num8 < num7; num8++)
						{
							string name4 = reader.ReadNullTerminatedString();
							int bundleId2 = AssetManager.GetBundleId(name4);
							if (bundleId2 != -1)
							{
								list2.Add(bundleId2);
							}
						}
						array2 = reader.ReadBytes(reader.ReadInt());
					}
					ResAssetEntry resEntry = AssetManager.GetResEntry(text);
					if (num < 11)
					{
						if (resEntry == null)
						{
							string name5 = text;
							int num9 = text.LastIndexOf("shaderblocks");
							if (num9 != -1)
							{
								name5 = text.Remove(num9);
								name5 += "shaderblocks_variation/blocks";
							}
							else
							{
								num9 = text.LastIndexOf("_mesh_");
								if (num9 != -1)
								{
									name5 = text.Remove(num9 + 5);
									name5 += "_mesh/blocks";
								}
							}
							bool flag3 = text.Contains("persistentblock");
							ResAssetEntry resEntry2 = AssetManager.GetResEntry(name5);
							if (resEntry2 != null)
							{
								ShaderBlockDepot resAs = AssetManager.GetResAs<ShaderBlockDepot>(resEntry2);
								Resources.Old.ShaderBlockResource shaderBlockResource = null;
								using (CasReader casReader = new CasReader(new MemoryStream(array2)))
								{
									using (NativeReader reader2 = new NativeReader(new MemoryStream(casReader.Read())))
									{
										shaderBlockResource = ((!flag3) ? ((Resources.Old.ShaderBlockResource)new Resources.Old.MeshParamDbBlock(reader2)) : ((Resources.Old.ShaderBlockResource)new Resources.Old.ShaderPersistentParamDbBlock(reader2)));
									}
								}
								Resources.ShaderBlockResource newResource = shaderBlockResource.Convert();
								if (!resAs.ReplaceResource(newResource))
								{
									Console.WriteLine(text);
								}
								AssetManager.ModifyRes(resEntry2.Name, resAs);
								resEntry2.IsDirty = false;
							}
						}
						else if (resEntry != null && resEntry.ResType == 3639990959u)
						{
							ShaderBlockDepot shaderBlockDepot = new ShaderBlockDepot();
							using (CasReader casReader2 = new CasReader(new MemoryStream(array2)))
							{
								using (NativeReader reader3 = new NativeReader(new MemoryStream(casReader2.Read())))
								{
									shaderBlockDepot.Read(reader3, AssetManager, resEntry, null);
								}
							}
							for (int num10 = 0; num10 < shaderBlockDepot.ResourceCount; num10++)
							{
								Resources.ShaderBlockResource resource = shaderBlockDepot.GetResource(num10);
								if (resource is Resources.ShaderPersistentParamDbBlock || resource is Resources.MeshParamDbBlock)
								{
									resource.IsModified = true;
								}
							}
							AssetManager.ModifyRes(resEntry.Name, shaderBlockDepot, array);
							resEntry.IsDirty = false;
							flag2 = false;
						}
					}
					if (resEntry == null)
					{
						continue;
					}
					resEntry.LinkedAssets.AddRange(collection2);
					resEntry.AddBundles.AddRange(list2);
					if (flag2)
					{
						resEntry.ModifiedEntry = new ModifiedAssetEntry();
						resEntry.ModifiedEntry.Sha1 = sha;
						resEntry.ModifiedEntry.OriginalSize = originalSize;
						resEntry.ModifiedEntry.ResMeta = array;
						resEntry.ModifiedEntry.UserData = userData2;
						if (sha == Sha1.Zero)
						{
							resEntry.ModifiedEntry.DataObject = ModifiedResource.Read(array2);
						}
						else
						{
							resEntry.ModifiedEntry.Data = array2;
						}
					}
					int key2 = Fnv1.HashString(resEntry.Name);
					if (!dictionary.ContainsKey(key2))
					{
						dictionary.Add(key2, resEntry);
					}
				}
				num3 = reader.ReadInt();
				for (int num11 = 0; num11 < num3; num11++)
				{
					Guid id = reader.ReadGuid();
					Sha1 sha2 = reader.ReadSha1();
					uint logicalOffset = reader.ReadUInt();
					uint logicalSize = reader.ReadUInt();
					uint rangeStart = reader.ReadUInt();
					uint rangeEnd = reader.ReadUInt();
					int firstMip = reader.ReadInt();
					int h = reader.ReadInt();
					bool addToChunkBundle = reader.ReadBoolean();
					string userData3 = "";
					if (num >= 12)
					{
						userData3 = reader.ReadNullTerminatedString();
					}
					List<int> list3 = new List<int>();
					int num12 = reader.ReadInt();
					for (int num13 = 0; num13 < num12; num13++)
					{
						string name6 = reader.ReadNullTerminatedString();
						int bundleId3 = AssetManager.GetBundleId(name6);
						if (bundleId3 != -1)
						{
							list3.Add(bundleId3);
						}
					}
					byte[] data = reader.ReadBytes(reader.ReadInt());
					ChunkAssetEntry chunkAssetEntry2 = AssetManager.GetChunkEntry(id);
					if (chunkAssetEntry2 == null)
					{
						ChunkAssetEntry chunkAssetEntry3 = new ChunkAssetEntry();
						chunkAssetEntry3.Id = id;
						chunkAssetEntry3.H32 = h;
						AssetManager.AddChunk(chunkAssetEntry3);
						if (dictionary.ContainsKey(chunkAssetEntry3.H32))
						{
							foreach (int bundle in dictionary[chunkAssetEntry3.H32].Bundles)
							{
								chunkAssetEntry3.AddToBundle(bundle);
							}
						}
						chunkAssetEntry2 = chunkAssetEntry3;
					}
					chunkAssetEntry2.AddBundles.AddRange(list3);
					chunkAssetEntry2.ModifiedEntry = new ModifiedAssetEntry();
					chunkAssetEntry2.ModifiedEntry.Sha1 = sha2;
					chunkAssetEntry2.ModifiedEntry.LogicalOffset = logicalOffset;
					chunkAssetEntry2.ModifiedEntry.LogicalSize = logicalSize;
					chunkAssetEntry2.ModifiedEntry.RangeStart = rangeStart;
					chunkAssetEntry2.ModifiedEntry.RangeEnd = rangeEnd;
					chunkAssetEntry2.ModifiedEntry.FirstMip = firstMip;
					chunkAssetEntry2.ModifiedEntry.H32 = h;
					chunkAssetEntry2.ModifiedEntry.AddToChunkBundle = addToChunkBundle;
					chunkAssetEntry2.ModifiedEntry.UserData = userData3;
					chunkAssetEntry2.ModifiedEntry.Data = data;
				}
				num3 = reader.ReadInt();
				for (int num14 = 0; num14 < num3; num14++)
				{
					string type2 = reader.ReadNullTerminatedString();
					Type[] types = Assembly.GetExecutingAssembly().GetTypes();
					foreach (Type type3 in types)
					{
						if (type3.GetInterface(typeof(ICustomActionHandler).Name) != null)
						{
							((ICustomActionHandler)Activator.CreateInstance(type3))?.LoadFromProject(num, reader, type2);
						}
					}
					if (num < 12)
					{
						break;
					}
				}
				return true;
			}
			}
		}

		private bool LegacyLoad(string inFilename)
		{
			Dictionary<int, AssetEntry> dictionary = new Dictionary<int, AssetEntry>();
			DbObject dbObject = null;
			using (DbReader dbReader = new DbReader(new FileStream(inFilename, FileMode.Open, FileAccess.Read), null))
			{
				dbObject = dbReader.ReadDbObject();
			}
			uint value = dbObject.GetValue("version", 0u);
			if (value > 12)
			{
				return false;
			}
			if (dbObject.GetValue("gameProfile", ProfilesLibrary.ProfileName) != ProfilesLibrary.ProfileName)
			{
				return false;
			}
			creationDate = new DateTime(dbObject.GetValue("creationDate", 0L));
			modifiedDate = new DateTime(dbObject.GetValue("modifiedDate", 0L));
			gameVersion = dbObject.GetValue("gameVersion", 0u);
			DbObject value2 = dbObject.GetValue<DbObject>("modSettings");
			if (value2 != null)
			{
				modSettings.Title = value2.GetValue("title", "");
				modSettings.Author = value2.GetValue("author", "");
				modSettings.Category = value2.GetValue("category", "");
				modSettings.Version = value2.GetValue("version", "");
				modSettings.Description = value2.GetValue("description", "");
				modSettings.Icon = value2.GetValue<byte[]>("icon");
				modSettings.SetScreenshot(0, value2.GetValue<byte[]>("screenshot1"));
				modSettings.SetScreenshot(1, value2.GetValue<byte[]>("screenshot2"));
				modSettings.SetScreenshot(2, value2.GetValue<byte[]>("screenshot3"));
				modSettings.SetScreenshot(3, value2.GetValue<byte[]>("screenshot4"));
				modSettings.ClearDirtyFlag();
			}
			DbObject value3 = dbObject.GetValue<DbObject>("added");
			if (value3 != null)
			{
				foreach (DbObject item in value3.GetValue<DbObject>("superbundles"))
				{
					AssetManager.AddSuperBundle(item.GetValue<string>("name"));
				}
				foreach (DbObject item2 in value3.GetValue<DbObject>("bundles"))
				{
					AssetManager.AddBundle(item2.GetValue<string>("name"), (BundleType)item2.GetValue("type", 0), AssetManager.GetSuperBundleId(item2.GetValue<string>("superbundle")));
				}
				foreach (DbObject item3 in value3.GetValue<DbObject>("ebx"))
				{
					EbxAssetEntry ebxAssetEntry = new EbxAssetEntry();
					ebxAssetEntry.Name = item3.GetValue<string>("name");
					ebxAssetEntry.Guid = item3.GetValue<Guid>("guid");
					ebxAssetEntry.Type = item3.GetValue("type", "UnknownAsset");
					AssetManager.AddEbx(ebxAssetEntry);
				}
				foreach (DbObject item4 in value3.GetValue<DbObject>("res"))
				{
					ResAssetEntry resAssetEntry = new ResAssetEntry();
					resAssetEntry.Name = item4.GetValue<string>("name");
					resAssetEntry.ResRid = (ulong)item4.GetValue("resRid", 0L);
					resAssetEntry.ResType = (uint)item4.GetValue("resType", 0);
					AssetManager.AddRes(resAssetEntry);
				}
				foreach (DbObject item5 in value3.GetValue<DbObject>("chunks"))
				{
					ChunkAssetEntry chunkAssetEntry = new ChunkAssetEntry();
					chunkAssetEntry.Id = item5.GetValue<Guid>("id");
					chunkAssetEntry.H32 = item5.GetValue("H32", 0);
					AssetManager.AddChunk(chunkAssetEntry);
				}
			}
			if (value < 6)
			{
				foreach (DbObject item6 in dbObject.GetValue<DbObject>("chunks"))
				{
					if (item6.GetValue("added", defaultValue: false))
					{
						ChunkAssetEntry chunkAssetEntry2 = new ChunkAssetEntry();
						chunkAssetEntry2.Id = item6.GetValue<Guid>("id");
						AssetManager.AddChunk(chunkAssetEntry2);
					}
				}
			}
			DbObject dbObject8 = dbObject.GetValue<DbObject>("modified");
			if (dbObject8 == null)
			{
				dbObject8 = dbObject;
			}
			foreach (DbObject item7 in dbObject8.GetValue<DbObject>("res"))
			{
				ResAssetEntry resEntry = AssetManager.GetResEntry(item7.GetValue<string>("name"));
				if (resEntry != null)
				{
					LoadLinkedAssets(item7, resEntry, value);
					if (item7.HasValue("data"))
					{
						resEntry.ModifiedEntry = new ModifiedAssetEntry();
						resEntry.ModifiedEntry.Sha1 = item7.GetValue<Sha1>("sha1");
						resEntry.ModifiedEntry.OriginalSize = item7.GetValue("originalSize", 0L);
						resEntry.ModifiedEntry.Data = item7.GetValue<byte[]>("data");
						resEntry.ModifiedEntry.ResMeta = item7.GetValue<byte[]>("meta");
					}
					if (item7.HasValue("bundles"))
					{
						foreach (string item8 in item7.GetValue<DbObject>("bundles"))
						{
							resEntry.AddBundles.Add(AssetManager.GetBundleId(item8));
						}
					}
					int key = Fnv1.HashString(resEntry.Name);
					if (!dictionary.ContainsKey(key))
					{
						dictionary.Add(key, resEntry);
					}
				}
			}
			foreach (DbObject item9 in dbObject8.GetValue<DbObject>("ebx"))
			{
				EbxAssetEntry ebxEntry = AssetManager.GetEbxEntry(item9.GetValue<string>("name"));
				if (ebxEntry != null)
				{
					LoadLinkedAssets(item9, ebxEntry, value);
					if (item9.HasValue("data"))
					{
						ebxEntry.ModifiedEntry = new ModifiedAssetEntry();
						byte[] buffer = item9.GetValue<byte[]>("data");
						if (value < 7)
						{
							using (CasReader casReader = new CasReader(new MemoryStream(buffer)))
							{
								buffer = casReader.Read();
							}
						}
						using (EbxReader ebxReader = new EbxReader(new MemoryStream(buffer)))
						{
							EbxAsset dataObject = ebxReader.ReadAsset();
							ebxEntry.ModifiedEntry.DataObject = dataObject;
						}
						if (item9.HasValue("transient"))
						{
							ebxEntry.ModifiedEntry.IsTransientModified = true;
						}
					}
					if (item9.HasValue("bundles"))
					{
						foreach (string item10 in item9.GetValue<DbObject>("bundles"))
						{
							ebxEntry.AddBundles.Add(AssetManager.GetBundleId(item10));
						}
					}
					int key2 = Fnv1.HashString(ebxEntry.Name);
					if (!dictionary.ContainsKey(key2))
					{
						dictionary.Add(key2, ebxEntry);
					}
				}
			}
			foreach (DbObject item11 in dbObject8.GetValue<DbObject>("chunks"))
			{
				Guid value4 = item11.GetValue<Guid>("id");
				ChunkAssetEntry chunkAssetEntry3 = AssetManager.GetChunkEntry(value4);
				if (chunkAssetEntry3 == null)
				{
					ChunkAssetEntry chunkAssetEntry4 = new ChunkAssetEntry();
					chunkAssetEntry4.Id = item11.GetValue<Guid>("id");
					chunkAssetEntry4.H32 = item11.GetValue("H32", 0);
					AssetManager.AddChunk(chunkAssetEntry4);
					if (dictionary.ContainsKey(chunkAssetEntry4.H32))
					{
						foreach (int bundle in dictionary[chunkAssetEntry4.H32].Bundles)
						{
							chunkAssetEntry4.AddToBundle(bundle);
						}
					}
					chunkAssetEntry3 = chunkAssetEntry4;
				}
				if (item11.HasValue("data"))
				{
					chunkAssetEntry3.ModifiedEntry = new ModifiedAssetEntry();
					chunkAssetEntry3.ModifiedEntry.Sha1 = item11.GetValue<Sha1>("sha1");
					chunkAssetEntry3.ModifiedEntry.Data = item11.GetValue<byte[]>("data");
					chunkAssetEntry3.ModifiedEntry.LogicalOffset = item11.GetValue("logicalOffset", 0u);
					chunkAssetEntry3.ModifiedEntry.LogicalSize = item11.GetValue("logicalSize", 0u);
					chunkAssetEntry3.ModifiedEntry.RangeStart = item11.GetValue("rangeStart", 0u);
					chunkAssetEntry3.ModifiedEntry.RangeEnd = item11.GetValue("rangeEnd", 0u);
					chunkAssetEntry3.ModifiedEntry.FirstMip = item11.GetValue("firstMip", -1);
					chunkAssetEntry3.ModifiedEntry.H32 = item11.GetValue("h32", 0);
					chunkAssetEntry3.ModifiedEntry.AddToChunkBundle = item11.GetValue("addToChunkBundle", defaultValue: true);
				}
				else
				{
					chunkAssetEntry3.FirstMip = item11.GetValue("firstMip", -1);
					chunkAssetEntry3.H32 = item11.GetValue("h32", 0);
				}
				if (item11.HasValue("bundles"))
				{
					foreach (string item12 in item11.GetValue<DbObject>("bundles"))
					{
						chunkAssetEntry3.AddBundles.Add(AssetManager.GetBundleId(item12));
					}
				}
			}
			if (value < 6)
			{
				//FrostyTask.Update("Retroactively fixing textures");
				foreach (ResAssetEntry item13 in AssetManager.EnumerateRes(0u, modifiedOnly: true))
				{
					ResourceType resType = (ResourceType)item13.ResType;
					if (resType == ResourceType.Texture || resType == ResourceType.DxTexture)
					{
						Texture texture = new Texture(AssetManager.GetRes(item13), AssetManager);
						if (texture.MipCount > 1)
						{
							byte[] buffer2 = new NativeReader(texture.Data).ReadToEnd();
							AssetManager.RevertAsset(item13);
							Texture texture2 = new Texture(AssetManager.GetRes(item13), AssetManager);
							texture.FirstMip = texture2.FirstMip;
							texture.LogicalOffset = 0u;
							for (int i = 0; i < texture.MipCount - texture.FirstMip; i++)
							{
								texture.LogicalSize |= (uint)(3 << i * 2);
							}
							texture.LogicalOffset = (texture.ChunkSize & ~texture.LogicalSize);
							texture.LogicalSize = (texture.ChunkSize & texture.LogicalSize);
							AssetManager.ModifyChunk(texture.ChunkId, buffer2, texture);
							AssetManager.ModifyRes(item13.Name, texture.ToBytes());
						}
					}
				}
			}
			if (value < 8)
			{
				foreach (EbxAssetEntry item14 in AssetManager.EnumerateEbx("", modifiedOnly: true))
				{
					foreach (AssetEntry linkedAsset in item14.LinkedAssets)
					{
						if (linkedAsset is ChunkAssetEntry)
						{
							linkedAsset.ModifiedEntry.H32 = Fnv1.HashString(item14.Name.ToLower());
						}
					}
				}
				foreach (ResAssetEntry item15 in AssetManager.EnumerateRes(0u, modifiedOnly: true))
				{
					foreach (AssetEntry linkedAsset2 in item15.LinkedAssets)
					{
						if (linkedAsset2 is ChunkAssetEntry)
						{
							linkedAsset2.ModifiedEntry.H32 = Fnv1.HashString(item15.Name.ToLower());
						}
					}
				}
			}
			return true;
		}
	}

        public class ModSettings
        {
            private string title = "";

            private string author = "";

            private string category = "";

            private string version = "";

            private string description = "";

            private byte[] iconData;

            private byte[][] screenshotData;

            private bool isDirty;

            public bool IsDirty => isDirty;

            public string Title
            {
                get
                {
                    return title;
                }
                set
                {
                    if (!title.Equals(value))
                    {
                        title = value;
                        isDirty = true;
                    }
                }
            }

            public string Author
            {
                get
                {
                    return author;
                }
                set
                {
                    if (!author.Equals(value))
                    {
                        author = value;
                        isDirty = true;
                    }
                }
            }

            public string Category
            {
                get
                {
                    return category;
                }
                set
                {
                    if (!category.Equals(value))
                    {
                        category = value;
                        isDirty = true;
                    }
                }
            }

            public string Version
            {
                get
                {
                    return version;
                }
                set
                {
                    if (!version.Equals(value))
                    {
                        version = value;
                        isDirty = true;
                    }
                }
            }

            public string Description
            {
                get
                {
                    return description;
                }
                set
                {
                    if (!description.Equals(value))
                    {
                        description = value;
                        isDirty = true;
                    }
                }
            }

            public byte[] Icon
            {
                get
                {
                    return iconData;
                }
                set
                {
                    iconData = value;
                    isDirty = true;
                }
            }

            public ModSettings()
            {
                screenshotData = new byte[4][];
            }

            public void SetScreenshot(int index, byte[] buffer)
            {
                screenshotData[index] = buffer;
                isDirty = true;
            }

            public byte[] GetScreenshot(int index)
            {
                return screenshotData[index];
            }

            public void ClearDirtyFlag()
            {
                isDirty = false;
            }
        }
    

}