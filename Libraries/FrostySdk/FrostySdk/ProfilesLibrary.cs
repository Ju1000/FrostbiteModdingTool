using Frosty.Hash;
using FrostySdk.Deobfuscators;
using FrostySdk.IO;
using FrostySdk.Managers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk
{
	public static class ProfilesLibrary
	{
		public struct Profile
		{
			public string Name { get; set; }

			public string EditorName => Name + " Editor";


			public string DisplayName { get; set; }

			public int DataVersion { get; set; }

			public string CacheName { get; set; }

			public string Deobfuscator { get; set; }

			public string AssetLoader { get; set; }

			public string AssetCompiler { get; set; }

			public List<FileSystemSource> Sources { get; set; }

			public string SDKFilename { get; set; }

			public byte[] Banner { get; set; }

			public int EbxVersion { get; set; }

			public bool RequiresKey { get; set; }

			public bool MustAddChunks { get; set; }

			public bool EnableExecution { get; set; }

			public string DefaultDiffuse { get; set; }

			public string DefaultNormals { get; set; }

			public string DefaultMask { get; set; }

			public string DefaultTint { get; set; }

			public Dictionary<int, string> SharedBundles { get; set; }

			public List<uint> IgnoredResTypes { get; set; }

			public string TextureImporter { get; set; }

			public string TextureExporter { get; set; }

			public string EditorScreen { get; set; }

			public string EBXReader { get; set; }

			public string EBXWriter { get; set; }

			/// <summary>
			/// 
			/// </summary>
			public string CacheWriter { get; set; }
			/// <summary>
			/// 
			/// </summary>
			public string CacheReader { get; set; }

			/// <summary>
			/// 
			/// </summary>
			public List<string> SupportedLauncherFileTypes { get; set; }

			/// <summary>
			/// 
			/// </summary>
			public string ModCompilerFileType { get; set; }

			/// <summary>
			/// 
			/// </summary>
			public bool CanEdit { get; set; }

			/// <summary>
			/// 
			/// </summary>
			public bool CanLaunchMods { get; set; }

            private string editorIcon;

            public string EditorIcon
			{
                get 
				{
					
					var fi = new FileInfo(AssetManager.ApplicationDirectory + "/Resources/images/" + Name + "Cover.jpg");
					if (fi.Exists)
						return fi.FullName;
					return editorIcon; 
				
				}
                set { editorIcon = value; }
            }

			private bool canImportMeshes;

            public bool CanImportMeshes
            {
                get { return canImportMeshes; }
                set { canImportMeshes = value; }
            }

			private bool canExportMeshes;

			public bool CanExportMeshes
			{
				get { return canExportMeshes; }
				set { canExportMeshes = value; }
			}

            private int? gddMaxStreams;

            public int? GDDMaxStreams
            {
                get { return gddMaxStreams; }
                set { gddMaxStreams = value; }
            }

            public string CompressedFormatEBX { get; set; }
            public string CompressedFormatRES { get; set; }
            public string CompressedFormatChunk { get; set; }
            public string CompressedFormatLegacy { get; set; }

			public string SDKFirstTypeInfo { get; set; }

			public string SDKAOBScan { get; set; }
			public string SDKClassesFile { get; set; }
			public string SDKGeneratorClassInfoType { get; set; }
			public string EBXTypeDescriptor { get; set; }

			public string LegacyFileManager { get; set; }

			public bool DoesNotUsePlugin { get; set; }

			public string EBXSharedTypeDescriptorNameInMemory { get; set; }

			public string ProjectEbxWriter { get; set; }
			public string ProjectEbxReader { get; set; }

		}
		public static string GetModProfileParentDirectoryPath()
		{
			var dir = AppContext.BaseDirectory + "\\Mods\\Profiles\\";
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			return dir;
		}


		public static string GetModProfileDirectoryPath()
        {
			var dir = AppContext.BaseDirectory + "/Mods/Profiles/" + ProfileName + "/";
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			return dir;
		}

		public static IEnumerable<Profile> EditorProfiles 
		{ 
			get 
			{
				var profiles = Directory.EnumerateFiles(
						ApplicationDirectory
						, "*profile.json").ToList();
				profiles.AddRange(Directory.EnumerateFiles(
						ApplicationDirectory + "FrostbiteProfiles\\"
						, "*profile.json").ToList());
				foreach (var p in profiles)
				{
					var prof = JsonConvert.DeserializeObject<Profile>(System.IO.File.ReadAllText(p));
					if (prof.CanEdit && prof.EditorScreen != null)
						yield return prof;
				}

				yield break;
			} 
		}

		public static string ApplicationDirectory
		{
			get
			{
				return System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\";
			}
		}

        [SupportedOSPlatform("windows")]
		public static Profile LoadedProfile;

		private static string DeobfuscatorNamespace = typeof(NullDeobfuscator).Namespace;

		private static string AssetLoaderNamespace = typeof(AssetManager).FullName;

		private static byte[][] obfuscationKey = new byte[6][]
		{
			new byte[6]
			{
				70,
				84,
				118,
				33,
				55,
				84
			},
			new byte[6]
			{
				72,
				82,
				50,
				69,
				86,
				41
			},
			new byte[6]
			{
				75,
				90,
				79,
				82,
				54,
				42
			},
			new byte[6]
			{
				77,
				86,
				67,
				83,
				58,
				82
			},
			new byte[6]
			{
				80,
				93,
				70,
				90,
				84,
				45
			},
			new byte[6]
			{
				86,
				80,
				74,
				37,
				67,
				89
			}
		};

        [SupportedOSPlatform("windows")]
		public static string ProfileName => LoadedProfile.Name;

		public static string DisplayName => LoadedProfile.DisplayName;

		public static string CacheName => LoadedProfile.CacheName;

		public static Type Deobfuscator => Type.GetType(DeobfuscatorNamespace + "." + LoadedProfile.Deobfuscator);

		public static Type AssetLoader => Type.GetType(AssetLoaderNamespace + "+" + LoadedProfile.AssetLoader);
		public static string AssetLoaderName => LoadedProfile.AssetLoader;
		public static string AssetCompilerName => LoadedProfile.AssetCompiler;

		public static string EditorScreen => LoadedProfile.EditorScreen;

		public static string EBXReader => LoadedProfile.EBXReader;

		public static string EBXWriter => LoadedProfile.EBXWriter;
		public static string CacheWriter => LoadedProfile.CacheWriter;
		public static string CacheReader => LoadedProfile.CacheReader;

		public static List<string> SupportedLauncherFileTypes => LoadedProfile.SupportedLauncherFileTypes;

		public static string ModCompilerFileType => LoadedProfile.ModCompilerFileType;

		public static bool CanImportMeshes => LoadedProfile.CanImportMeshes;
		public static bool CanExportMeshes => LoadedProfile.CanExportMeshes;

		public static string LegacyFileManager => LoadedProfile.LegacyFileManager;
		public static string SDKClassesFile => LoadedProfile.SDKClassesFile;
		public static string SDKGeneratorClassInfoType => LoadedProfile.SDKGeneratorClassInfoType;
		public static string EBXTypeDescriptor => LoadedProfile.EBXTypeDescriptor;
		public static bool DoesNotUsePlugin => LoadedProfile.DoesNotUsePlugin;
		public static string EBXSharedTypeDescriptorNameInMemory => LoadedProfile.EBXSharedTypeDescriptorNameInMemory;

		public enum CompTypeArea
        {
			EBX,
			RES,
			Chunks,
			Legacy
        }
		public static CompressionType GetCompressionType(CompTypeArea area)
        {
			var cf_text = "";
			switch(area)
            {
				case CompTypeArea.EBX:
					cf_text = (LoadedProfile.CompressedFormatEBX.ToUpper());
				break;
				case CompTypeArea.RES:
					cf_text = (LoadedProfile.CompressedFormatRES.ToUpper());
					break;
				case CompTypeArea.Chunks:
					cf_text = (LoadedProfile.CompressedFormatChunk.ToUpper());
					break;
				case CompTypeArea.Legacy:
					cf_text = (LoadedProfile.CompressedFormatLegacy.ToUpper());
					break;
			}

			switch(cf_text.ToUpper())
            {
				case "ZSTD":
					return CompressionType.ZStd;
				case "OODLE":
					return CompressionType.Oodle;
				case "LZ4":
					return CompressionType.LZ4;
				default:
					return CompressionType.None;
			}
        }


		/// <summary>
		/// 20190729 - Madden 20
		/// 20190911 - FIFA 20
		/// 
		/// </summary>
        [SupportedOSPlatform("windows")]
		public static int DataVersion => LoadedProfile.DataVersion;

		public enum DataVersions : int
        {
			FIFA17 = 20160927,
			FIFA18 = 20170929,
			FIFA19 = 20180914,
			FIFA20 = 20190911,
			FIFA21 = 20200929,
			FIFA22 = 20210922,

			MADDEN20 = 20190729,
			MADDEN21 = 20200831,
			MADDEN22 = 20210812
		}

		public static bool IsFIFADataVersion()
		{

			bool isFIFA = (LoadedProfile.DataVersion == (int)ProfilesLibrary.DataVersions.FIFA17
				|| LoadedProfile.DataVersion == (int)ProfilesLibrary.DataVersions.FIFA18
				|| LoadedProfile.DataVersion == (int)ProfilesLibrary.DataVersions.FIFA19
				|| LoadedProfile.DataVersion == (int)ProfilesLibrary.DataVersions.FIFA20
				)
				;

			return isFIFA;
		}

		public static bool IsFIFA19DataVersion()
		{

			bool isFIFA = false;

			isFIFA = (ProfilesLibrary.DataVersion == (int)ProfilesLibrary.DataVersions.FIFA19);

			return isFIFA;
		}


		public static bool IsFIFA20DataVersion()
		{

			bool isFIFA = false;

			isFIFA = (ProfilesLibrary.DataVersion == (int)ProfilesLibrary.DataVersions.FIFA20);

			return isFIFA;
		}

		[SupportedOSPlatform("windows")]
		public static bool IsFIFA21DataVersion()
		{

			bool isFIFA = false;

			isFIFA = (ProfilesLibrary.DataVersion == (int)ProfilesLibrary.DataVersions.FIFA21);

			return isFIFA;
		}

		public static bool IsFIFA22DataVersion()
		{

			bool isFIFA = false;

			isFIFA = (ProfilesLibrary.DataVersion == (int)ProfilesLibrary.DataVersions.FIFA22);

			return isFIFA;
		}

		public static bool IsBF4DataVersion()
		{

			bool isbf4 = false;

			isbf4 = (ProfilesLibrary.DataVersion == 20141117);

			return isbf4;
		}

		public static bool IsMadden21DataVersion()
		{

			bool isMadden = false;

			isMadden = (ProfilesLibrary.DataVersion == (int)ProfilesLibrary.DataVersions.MADDEN20
				|| ProfilesLibrary.DataVersion == (int)ProfilesLibrary.DataVersions.MADDEN21
				|| ProfilesLibrary.ProfileName.ToUpper().Contains("MADDEN")
				);

			return isMadden;
		}

		public static bool IsMadden22DataVersion()
		{

			bool isMadden = false;

			isMadden = ProfilesLibrary.DataVersion == (int)ProfilesLibrary.DataVersions.MADDEN22;

			return isMadden;
		}

		public static bool IsMadden20DataVersion()
		{

			bool isMadden = false;

			isMadden = (ProfilesLibrary.DataVersion == (int)ProfilesLibrary.DataVersions.MADDEN20);

			return isMadden;
		}

		public static List<FileSystemSource> Sources => LoadedProfile.Sources;

		public static string SDKFilename => LoadedProfile.SDKFilename;

		public static byte[] Banner => LoadedProfile.Banner;

		public static int EbxVersion => LoadedProfile.EbxVersion;

		public static bool RequiresKey => LoadedProfile.RequiresKey;

		public static bool MustAddChunks => LoadedProfile.MustAddChunks;

		public static bool EnableExecution => LoadedProfile.EnableExecution;

		public static string DefaultDiffuse => LoadedProfile.DefaultDiffuse;

		public static string DefaultNormals => LoadedProfile.DefaultNormals;

		public static string DefaultMask => LoadedProfile.DefaultMask;

		public static string DefaultTint => LoadedProfile.DefaultTint;

		public static Dictionary<int, string> SharedBundles => LoadedProfile.SharedBundles;

        public static string TextureImporter => LoadedProfile.TextureImporter;

		public static bool IsResTypeIgnored(ResourceType resType)
		{
			return LoadedProfile.IgnoredResTypes.Contains((uint)resType);
		}

		public static void DumpAllFrostyProfiles()
		{
			using (FileStream fileStream = new FileStream("FrostySdk.Profiles.bin", FileMode.Open))
			using (NativeReader nativeReader = new NativeReader(fileStream))
			{
				int num2 = nativeReader.ReadInt();

				string text = DecodeString(nativeReader);
				long num = nativeReader.ReadLong();

				if (num == -1)
				{
					return;
				}
				nativeReader.Position += num;
				Profile profile = default(Profile);
				profile.Name = text;
				profile.DisplayName = DecodeString(nativeReader);
				profile.DataVersion = nativeReader.ReadInt();
				profile.CacheName = DecodeString(nativeReader);
				profile.Deobfuscator = DecodeString(nativeReader);
				profile.AssetLoader = DecodeString(nativeReader);
				profile.Sources = new List<FileSystemSource>();
				profile.SharedBundles = new Dictionary<int, string>();
				profile.IgnoredResTypes = new List<uint>();
				int num4 = nativeReader.ReadInt();
				for (int j = 0; j < num4; j++)
				{
					FileSystemSource item = default(FileSystemSource);
					item.Path = DecodeString(nativeReader);
					item.SubDirs = (nativeReader.ReadByte() == 1);
					profile.Sources.Add(item);
				}
				profile.SDKFilename = DecodeString(nativeReader);
				profile.Banner = nativeReader.ReadBytes(nativeReader.ReadInt());
				profile.DefaultDiffuse = DecodeString(nativeReader);
				profile.DefaultNormals = DecodeString(nativeReader);
				profile.DefaultMask = DecodeString(nativeReader);
				profile.DefaultTint = DecodeString(nativeReader);
				int num5 = nativeReader.ReadInt();
				for (int k = 0; k < num5; k++)
				{
					string text2 = DecodeString(nativeReader);
					profile.SharedBundles.Add(Fnv1.HashString(text2.ToLower()), text2);
				}
				int num6 = nativeReader.ReadInt();
				for (int l = 0; l < num6; l++)
				{
					profile.IgnoredResTypes.Add(nativeReader.ReadUInt());
				}
				profile.MustAddChunks = (nativeReader.ReadByte() == 1);
				profile.EbxVersion = nativeReader.ReadByte();
				profile.RequiresKey = (nativeReader.ReadByte() == 1);
				profile.EnableExecution = (nativeReader.ReadByte() != 1);

				System.IO.File.WriteAllText(text + "Profile.json", JsonConvert.SerializeObject(profile));
			}
		}

		public static void DumpFrostyProfile(string profileKey)
		{
			using (FileStream fileStream = new FileStream("FrostySdk.Profiles.bin", FileMode.Open))
			using (NativeReader nativeReader = new NativeReader(fileStream))
			{
				long num = -1L;
				int num2 = nativeReader.ReadInt();
				for (int i = 0; i < num2; i++)
				{
					var text = DecodeString(nativeReader);
					long num3 = nativeReader.ReadLong();
					//if (text.Equals("FIFA18", StringComparison.OrdinalIgnoreCase))
					if (text.Equals(profileKey, StringComparison.OrdinalIgnoreCase))
					{
						num = num3;
						profileKey = text;
					}
					
				}
				if (num == -1)
				{
					return;
				}
				nativeReader.Position += num;
				Profile profile = default(Profile);
				profile.Name = profileKey;
				profile.DisplayName = DecodeString(nativeReader);
				profile.DataVersion = nativeReader.ReadInt();
				profile.CacheName = DecodeString(nativeReader);
				profile.Deobfuscator = DecodeString(nativeReader);
				profile.AssetLoader = DecodeString(nativeReader);
				profile.Sources = new List<FileSystemSource>();
				profile.SharedBundles = new Dictionary<int, string>();
				profile.IgnoredResTypes = new List<uint>();
				int num4 = nativeReader.ReadInt();
				for (int j = 0; j < num4; j++)
				{
					FileSystemSource item = default(FileSystemSource);
					item.Path = DecodeString(nativeReader);
					item.SubDirs = (nativeReader.ReadByte() == 1);
					profile.Sources.Add(item);
				}
				profile.SDKFilename = DecodeString(nativeReader);
				profile.Banner = nativeReader.ReadBytes(nativeReader.ReadInt());
				profile.DefaultDiffuse = DecodeString(nativeReader);
				profile.DefaultNormals = DecodeString(nativeReader);
				profile.DefaultMask = DecodeString(nativeReader);
				profile.DefaultTint = DecodeString(nativeReader);
				int num5 = nativeReader.ReadInt();
				for (int k = 0; k < num5; k++)
				{
					string text2 = DecodeString(nativeReader);
					profile.SharedBundles.Add(Fnv1.HashString(text2.ToLower()), text2);
				}
				int num6 = nativeReader.ReadInt();
				for (int l = 0; l < num6; l++)
				{
					profile.IgnoredResTypes.Add(nativeReader.ReadUInt());
				}
				profile.MustAddChunks = (nativeReader.ReadByte() == 1);
				profile.EbxVersion = nativeReader.ReadByte();
				profile.RequiresKey = (nativeReader.ReadByte() == 1);
				profile.EnableExecution = (nativeReader.ReadByte() != 1);

				System.IO.File.WriteAllText(profileKey + "Profile.json", JsonConvert.SerializeObject(profile));
			}
		}

        [SupportedOSPlatform("windows")]
		public static bool Initialize(string profileKey)
		{
			if (!Directory.Exists("Debugging"))
				Directory.CreateDirectory("Debugging");
			if (!Directory.Exists("Debugging/EBX"))
				Directory.CreateDirectory("Debugging/EBX");
			if (!Directory.Exists("Debugging/RES"))
				Directory.CreateDirectory("Debugging/RES");
			if (!Directory.Exists("Debugging/Chunk"))
				Directory.CreateDirectory("Debugging/Chunk");
			if (!Directory.Exists("Debugging/Other"))
				Directory.CreateDirectory("Debugging/Other");


			
			
			if (File.Exists("FrostbiteProfiles/" + profileKey + "Profile.json"))
			{
				Profile profile = default(Profile);
				profile = JsonConvert.DeserializeObject<Profile>(System.IO.File.ReadAllText("FrostbiteProfiles/" + profileKey + "Profile.json"));
				LoadedProfile = profile;
				return true;
			}
			else if (File.Exists(profileKey + "Profile.json"))
			{
				Profile profile = default(Profile);
				profile = JsonConvert.DeserializeObject<Profile>(System.IO.File.ReadAllText(profileKey + "Profile.json"));
				LoadedProfile = profile;
				return true;
			}
			else
			{
				throw new Exception($"Cannot find {profileKey}Profile.json. You have either not installed FMT correctly or a Profile for this game doesn't exist!");
				//long num = -1L;
				//using (FileStream fileStream = new FileStream("FrostySdk.Profiles.bin", FileMode.Open))
				//using (NativeReader nativeReader = new NativeReader(fileStream))
				//{
				//	int num2 = nativeReader.ReadInt();
				//	for (int i = 0; i < num2; i++)
				//	{
				//		string text = DecodeString(nativeReader);
				//		long num3 = nativeReader.ReadLong();
				//		if (text.Equals(profileKey, StringComparison.OrdinalIgnoreCase))
				//		{
				//			num = num3;
				//			profileKey = text;
				//		}
				//	}
				//	if (num == -1)
				//	{
				//		return false;
				//	}
				//	nativeReader.Position += num;
				//	Profile profile = default(Profile);
				//	profile.Name = profileKey;
				//	profile.DisplayName = DecodeString(nativeReader);
				//	profile.DataVersion = nativeReader.ReadInt();
				//	profile.CacheName = DecodeString(nativeReader);
				//	profile.Deobfuscator = DecodeString(nativeReader);
				//	profile.AssetLoader = DecodeString(nativeReader);
				//	profile.Sources = new List<FileSystemSource>();
				//	profile.SharedBundles = new Dictionary<int, string>();
				//	profile.IgnoredResTypes = new List<uint>();
				//	int num4 = nativeReader.ReadInt();
				//	for (int j = 0; j < num4; j++)
				//	{
				//		FileSystemSource item = default(FileSystemSource);
				//		item.Path = DecodeString(nativeReader);
				//		item.SubDirs = (nativeReader.ReadByte() == 1);
				//		profile.Sources.Add(item);
				//	}
				//	profile.SDKFilename = DecodeString(nativeReader);
				//	profile.Banner = nativeReader.ReadBytes(nativeReader.ReadInt());
				//	profile.DefaultDiffuse = DecodeString(nativeReader);
				//	profile.DefaultNormals = DecodeString(nativeReader);
				//	profile.DefaultMask = DecodeString(nativeReader);
				//	profile.DefaultTint = DecodeString(nativeReader);
				//	int num5 = nativeReader.ReadInt();
				//	for (int k = 0; k < num5; k++)
				//	{
				//		string text2 = DecodeString(nativeReader);
				//		profile.SharedBundles.Add(Fnv1.HashString(text2.ToLower()), text2);
				//	}
				//	int num6 = nativeReader.ReadInt();
				//	for (int l = 0; l < num6; l++)
				//	{
				//		profile.IgnoredResTypes.Add(nativeReader.ReadUInt());
				//	}
				//	profile.MustAddChunks = (nativeReader.ReadByte() == 1);
				//	profile.EbxVersion = nativeReader.ReadByte();
				//	profile.RequiresKey = (nativeReader.ReadByte() == 1);
				//	profile.EnableExecution = (nativeReader.ReadByte() != 1);

				//	System.IO.File.WriteAllText(profileKey + "Profile.json", JsonConvert.SerializeObject(profile));
				//	// Write out to json and reinit
				//	return Initialize(profileKey);
				//}

			}

		}

		public static async Task<bool> InitialiseAsync(string profileKey)
        {
			return await Task.Run(() => Initialize(profileKey));
        }
		private static string DecodeString(NativeReader reader)
		{
			int num = reader.Read7BitEncodedInt();
			byte[] array = reader.ReadBytes(num);
			for (int i = 0; i < num; i++)
			{
				array[i] = (byte)(array[i] ^ obfuscationKey[i % obfuscationKey.Length][(i + obfuscationKey.Length * (0x1000 | i)) % obfuscationKey.Length]);
			}
			return Encoding.UTF8.GetString(array);
		}
	}
}