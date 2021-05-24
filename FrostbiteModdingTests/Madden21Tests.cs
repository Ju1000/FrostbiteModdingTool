﻿using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SdkGenerator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using v2k4FIFAModding.Frosty;
using v2k4FIFAModdingCL;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class Madden21Tests : ILogger
    {

        public const string GamePath = @"F:\Origin Games\Madden NFL 21\";
        public const string GamePathExe = GamePath + "\\Madden21.exe";

        public void Log(string text, params object[] vars)
        {
            Debug.WriteLine(text);
        }

        public void LogError(string text, params object[] vars)
        {
            Debug.WriteLine(text);
        }

        public void LogWarning(string text, params object[] vars)
        {
            Debug.WriteLine(text);
        }

        [TestMethod]
        public void TestBuildCache()
        {
            var buildCache = new BuildCache();
            buildCache.LoadData("Madden21", GamePath, this, true);
        }

        [TestMethod]
        public void TestBuildSDK()
        {
            var buildCache = new BuildCache();
            buildCache.LoadData("Madden21", GamePath, this, false);

            var buildSDK = new BuildSDK();
            buildSDK.Build().Wait();
        }

        [TestMethod]
        public void TestSplashScreenMod()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePath + "\\Madden21.exe");
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(@"G:\\MaddenSplashProject.fbproject");

            var oldFiles = Directory.GetFiles(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "*.fbmod");
            foreach (var oFile in oldFiles) File.Delete(oFile);
            var testfbmodname = "test-" + new Random().Next().ToString() + ".fbmod";

            projectManagement.Project.WriteToMod(testfbmodname, new FrostySdk.ModSettings());

            

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    testfbmodname
                }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestMainMenuSplashScreenMod()
        {
            //ProjectManagement projectManagement = new ProjectManagement(GamePath + "\\Madden21.exe");
            GameInstanceSingleton.InitializeSingleton(GamePathExe);


            var oldFiles = Directory.GetFiles(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "*.fbmod");
            foreach (var oFile in oldFiles) File.Delete(oFile);
            var testfbmodname = @"G:\Work\MADDEN Modding\Paulv2k4 Main Menu splash mod.fbmod";

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    testfbmodname
                }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestTeamWipeMod()
        {
            //ProjectManagement projectManagement = new ProjectManagement(GamePath + "\\Madden21.exe");
            GameInstanceSingleton.InitializeSingleton(GamePathExe);


            var oldFiles = Directory.GetFiles(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "*.fbmod");
            foreach (var oFile in oldFiles) File.Delete(oFile);
            var testfbmodname = @"G:\Work\MADDEN Modding\Paulv2k4 Team Wipe Mod.fbmod";

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    testfbmodname
                }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestColtKitMod()
        {
            //ProjectManagement projectManagement = new ProjectManagement(GamePath + "\\Madden21.exe");
            GameInstanceSingleton.InitializeSingleton(GamePathExe);

            var oldFiles = Directory.GetFiles(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "*.fbmod");
            foreach (var oFile in oldFiles) File.Delete(oFile);
            var testfbmodname = @"G:\Work\MADDEN Modding\Paulv2k4 Colt kit mod.fbmod";

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GamePath, "",
                new System.Collections.Generic.List<string>() {
                    testfbmodname
                }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestGPMod()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePath + "\\Madden21.exe");
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(@"G:\\MaddenGPProject2.fbproject");

            var oldFiles = Directory.GetFiles(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "*.fbmod");
            foreach (var oFile in oldFiles) File.Delete(oFile);
            var testfbmodname = "test-" + new Random().Next().ToString() + ".fbmod";

            projectManagement.Project.WriteToMod(testfbmodname, new FrostySdk.ModSettings());

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    testfbmodname
                }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestLegacyMod()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathExe);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(@"G:\\MaddenLegacyProject.fbproject");

            var oldFiles = Directory.GetFiles(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "*.fbmod");
            foreach (var oFile in oldFiles) File.Delete(oFile);
            var testfbmodname = "test-" + new Random().Next().ToString() + ".fbmod";

            projectManagement.Project.WriteToMod(testfbmodname, new FrostySdk.ModSettings());

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    testfbmodname
                }.ToArray()).Wait();

        }

    }
}