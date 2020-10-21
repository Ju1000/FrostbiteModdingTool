﻿using FrostySdk;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using paulv2k4ModdingExecuter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FIFA21Plugin
{

    /// <summary>
    /// Currently. The Madden 21 Compiler does not work in game.
    /// </summary>
    public class FIFA21AssetCompiler : IAssetCompiler
    {
        public const string ModDirectory = "ModData";
        public const string PatchDirectory = "Patch";

        private static void DirectoryCopy(string sourceBasePath, string destinationBasePath, bool recursive = true)
        {
            if (!Directory.Exists(sourceBasePath))
                throw new DirectoryNotFoundException($"Directory '{sourceBasePath}' not found");

            var directoriesToProcess = new Queue<(string sourcePath, string destinationPath)>();
            directoriesToProcess.Enqueue((sourcePath: sourceBasePath, destinationPath: destinationBasePath));
            while (directoriesToProcess.Any())
            {
                (string sourcePath, string destinationPath) = directoriesToProcess.Dequeue();

                if (!Directory.Exists(destinationPath))
                    Directory.CreateDirectory(destinationPath);

                var sourceDirectoryInfo = new DirectoryInfo(sourcePath);
                    foreach (FileInfo sourceFileInfo in sourceDirectoryInfo.EnumerateFiles())
                        sourceFileInfo.CopyTo(Path.Combine(destinationPath, sourceFileInfo.Name), true);
                if (!recursive)
                    continue;

                foreach (DirectoryInfo sourceSubDirectoryInfo in sourceDirectoryInfo.EnumerateDirectories())
                    directoriesToProcess.Enqueue((
                        sourcePath: sourceSubDirectoryInfo.FullName,
                        destinationPath: Path.Combine(destinationPath, sourceSubDirectoryInfo.Name)));
            }
        }

        /// <summary>
        /// This is run AFTER the compilation of the fbmod into resource files ready for the Actions to TOC/SB/CAS to be taken
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="logger"></param>
        /// <param name="frostyModExecuter">Frosty Mod Executer object</param>
        /// <returns></returns>
        public bool Compile(FileSystem fs, ILogger logger, object frostyModExecuter)
        {

            // ------------------------------------------------------------------------------------------
            // You will need to change this to ProfilesLibrary.DataVersion if you change the Profile.json DataVersion field
            if (ProfilesLibrary.IsFIFA21DataVersion())
            {
                DbObject layoutToc = null;

                // Read the original Layout TOC into a DB Object
                //using (DbReader dbReaderOfLayoutTOC = new DbReader(new FileStream(fs.BasePath + PatchDirectory + "/layout.toc", FileMode.Open, FileAccess.Read), fs.CreateDeobfuscator()))
                //{
                //    layoutToc = dbReaderOfLayoutTOC.ReadDbObject();
                //}

                // Notify the Bundle Action of the Cas File Count
                FIFA21BundleAction.CasFileCount = fs.CasFileCount;
                List<FIFA21BundleAction> madden21BundleActions = new List<FIFA21BundleAction>();

                var numberOfCatalogs = fs.Catalogs.Count();
                var numberOfCatalogsCompleted = 0;

                if (!((FrostyModExecutor)frostyModExecuter).UseSymbolicLinks)
                {
                    logger.Log("No Symbolic Link - Copying files from Data to ModData");

                    Directory.CreateDirectory(fs.BasePath + ModDirectory + "//Data//");

                    var dataFiles = Directory.EnumerateFiles(fs.BasePath + "Data", "*.*", SearchOption.AllDirectories);
                    var dataFileCount = dataFiles.Count();
                    var indexOfDataFile = 0;
                    foreach (var f in dataFiles)
                    {
                        var finalDestination = f.Replace("Data", @"ModData\Data\");

                        var lastIndexOf = finalDestination.LastIndexOf("\\");
                        var newDirectory = finalDestination.Substring(0, lastIndexOf) + "\\";
                        if (!Directory.Exists(newDirectory))
                        {
                            Directory.CreateDirectory(newDirectory);
                        }
                        File.Copy(f, finalDestination, true);
                        indexOfDataFile++;
                        logger.Log($"Copied ({indexOfDataFile}/{dataFileCount}) - {f}");
                    }
                }

                logger.Log("Deleting CAS files from ModData");
                foreach (string casFileLocation in Directory.EnumerateFiles(fs.BasePath + ModDirectory + "//" + PatchDirectory, "*.cas", SearchOption.AllDirectories))
                {
                    File.Delete(casFileLocation);
                }
                logger.Log("Copying files from Patch to ModData");
                // Copied Patch CAS files from Patch to Mod Data Patch
                DirectoryCopy(fs.BasePath + PatchDirectory, fs.BasePath + ModDirectory + "//" + PatchDirectory, true);
                //foreach (CatalogInfo catalogItem in fs.EnumerateCatalogInfos())
                //{
                    FIFA21BundleAction fifaBundleAction = new FIFA21BundleAction((FrostyModExecutor)frostyModExecuter);
                    fifaBundleAction.Run();

                    //numberOfCatalogsCompleted++;
                    //logger.Log($"Compiling Mod Progress: { Math.Round((double)numberOfCatalogsCompleted / numberOfCatalogs, 2) * 100} %");
                //}
                // --------------------------------------------------------------------------------------


                // --------------------------------------------------------------------------------------
                // From the new bundles that have been created that has generated new CAS files, add these new CAS files to the Layout TOC
                //foreach (FIFA21BundleAction bundleAction in madden21BundleActions)
                //{
                //    if (bundleAction.HasErrored)
                //    {
                //        throw bundleAction.Exception;
                //    }
                //    if (bundleAction.CasFiles.Count > 0)
                //    {
                //        var installManifest = layoutToc.GetValue<DbObject>("installManifest");
                //        var installChunks = installManifest.GetValue<DbObject>("installChunks");
                //        foreach (DbObject installChunk in installChunks)
                //        {
                //            if (bundleAction.CatalogInfo.Name.Equals("win32/" + installChunk.GetValue<string>("name")))
                //            {
                //                foreach (int key in bundleAction.CasFiles.Keys)
                //                {
                //                    DbObject newFile = DbObject.CreateObject();
                //                    newFile.SetValue("id", key);
                //                    newFile.SetValue("path", bundleAction.CasFiles[key]);

                //                    var installChunkFiles = installChunk.GetValue<DbObject>("files");
                //                    installChunkFiles.Add(newFile);


                //                }
                //                break;
                //            }
                //        }
                //    }
                //}


                // --------------------------------------------------------------------------------------
                // Write a new Layout file
                //logger.Log("Writing new Layout file to Game");
                //using (DbWriter dbWriter = new DbWriter(new FileStream(ModDirectory + PatchDirectory + "/layout.toc", FileMode.Create), inWriteHeader: true))
                //{
                //    dbWriter.Write(layoutToc);
                //}
                // --------------------------------------------------------------------------------------

                return true;
            }
            return false;
        }


    }
}
