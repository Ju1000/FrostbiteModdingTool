﻿using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Managers;
using FrostySdk.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using SdkGenerator;
using System;
using System.Diagnostics;
using System.Linq;
using v2k4FIFAModdingCL;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class DeadSpaceTests : IFMTTest
    {
        private string prevText = string.Empty;

        public string GamePath
        {
            get
            {

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey($"Software\\EA Games\\{ProfileManager.DisplayName}"))
                {
                    if (key != null)
                    {
                        string installDir = key.GetValue("Install Dir").ToString();
                        return installDir;
                    }
                }
                return string.Empty;
            }
        }
        public string GamePathEXE
        {
            get
            {

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey($"Software\\EA Games\\{ProfileManager.DisplayName}"))
                {
                    if (key != null)
                    {
                        string installDir = key.GetValue("Install Dir").ToString();
                        return installDir + "Dead Space.exe";
                    }
                }
                return string.Empty;
            }
        }

        public string TestMeshesPath
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
        }

        public string GameName => throw new NotImplementedException();

        public string GameEXE => throw new NotImplementedException();

        public void Log(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine($"[LOGGER][DEBUG][{DateTime.Now.ToShortTimeString()}] {text}");
                prevText = text;
            }
        }

        public void LogError(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine($"[LOGGER][ERROR][{DateTime.Now.ToShortTimeString()}] {text}");
                prevText = text;
            }
        }

        public void LogWarning(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine($"[LOGGER][WARN][{DateTime.Now.ToShortTimeString()}] {text}");
                prevText = text;
            }
        }

        [TestMethod]
        public void BuildCache()
        {
            ProfileManager.Initialize("DeadSpace");
            var buildCache = new CacheManager();
            buildCache.LoadData("DeadSpace", GamePath, this, true, true);

            var ebxItems = AssetManager.Instance.EnumerateEbx().ToList();
            var resItems = AssetManager.Instance.EnumerateRes().ToList();
            var chunkItems = AssetManager.Instance.EnumerateChunks().ToList();
            //var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }

        [TestMethod]
        public void BuildSDK()
        {
            ProfileManager.Initialize("DeadSpace");
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, false);
            var buildSDK = new BuildSDK();
            buildSDK.Build().Wait();

            //var ebxItems = AssetManager.Instance.EnumerateEbx().ToList();
            //var resItems = AssetManager.Instance.EnumerateRes().ToList();
            //var chunkItems = AssetManager.Instance.EnumerateChunks().ToList();
            //var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }


        [TestMethod]
        public void LoadLegacy()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("NeedForSpeedUnbound", GamePath, this, false, true);

            var ebxFCC = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("legacy", StringComparison.OrdinalIgnoreCase));
            var ebxFile = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("file", StringComparison.OrdinalIgnoreCase));
            var ebxCollector = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("collector", StringComparison.OrdinalIgnoreCase));
            var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }

        [TestMethod]
        public void LoadSplashscreenTexture()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("NeedForSpeedUnbound", GamePath, this, false, true);
            var entryName = "ui/textures/bootflow/excaliburloadingscreen";
            var ebxEntrySplash = AssetManager.Instance.GetEbxEntry(entryName);
            Assert.IsNotNull(ebxEntrySplash);
            var ebxSplash = AssetManager.Instance.GetEbx(ebxEntrySplash);
            var resEntrySplash = AssetManager.Instance.GetResEntry(entryName);
            Assert.IsNotNull(resEntrySplash);
            //var resSplash = AssetManager.Instance.GetRes(resEntrySplash);
            using (Texture textureAsset = new Texture(resEntrySplash))
            {

            }
        }

        [TestMethod]
        public void LoadEbxGameplayGameModes()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("NeedForSpeedUnbound", GamePath, this, false, true);
            var entryName = "gameplay/gamemodes/races/defaultracegamemodesettings";
            var ebxEntry = AssetManager.Instance.GetEbxEntry(entryName);
            var ebx = AssetManager.Instance.GetEbx(ebxEntry);
        }

    }
}
