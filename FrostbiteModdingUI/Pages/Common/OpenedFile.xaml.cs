﻿using AvalonDock.Layout;
using CSharpImageLibrary;
using FMT.FileTools;
using Frostbite.Textures;
using FrostbiteModdingUI.Models;
using FrostbiteModdingUI.Windows;
using FrostySdk;
using FrostySdk.Frostbite.IO;
using FrostySdk.Frostbite.IO.Input;
using FrostySdk.Frostbite.IO.Output;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using v2k4FIFAModding.Frosty;

namespace FMT.Pages.Common
{
    /// <summary>
    /// Interaction logic for OpenedFile.xaml
    /// </summary>
    public partial class OpenedFile : UserControl
    {

        MainViewModel ModelViewerModel;
        public HelixToolkit.Wpf.SharpDX.Camera Camera { get; set; }

        #region Entry Properties

        private AssetEntry assetEntry1;

        public AssetEntry SelectedEntry
        {
            get
            {
                if (assetEntry1 == null && SelectedLegacyEntry != null)
                    return SelectedLegacyEntry;

                return assetEntry1;
            }
            set { assetEntry1 = value; }
        }

        public LegacyFileEntry SelectedLegacyEntry { get; set; }

        private EbxAsset ebxAsset;

        public EbxAsset SelectedEbxAsset
        {
            get { return ebxAsset; }
            set { ebxAsset = value; }
        }


        #endregion

        private IEditorWindow MainEditorWindow
        {
            get
            {
                return App.MainEditorWindow as IEditorWindow;
            }
        }

        public OpenedFile(IAssetEntry entry)
        {
            InitializeComponent();

            SelectedLegacyEntry = null;
            if (entry is LegacyFileEntry legacyFileEntry)
            {
                SelectedEntry = legacyFileEntry;
                SelectedLegacyEntry = legacyFileEntry;
            }
            else
            {
                SelectedEntry = (AssetEntry)entry;
            }

            Loaded += OpenedFile_Loaded;
        }

        private async void OpenedFile_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdateLoadingVisibility(true);
            await Task.Delay(200);
            await Dispatcher.InvokeAsync(async () =>
            {
                await OpenAsset(SelectedEntry);
            });
            await Task.Delay(200);
            await UpdateLoadingVisibility(false);
        }

        public async Task UpdateLoadingVisibility(bool show)
        {
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    this.borderLoading.Visibility = show ? Visibility.Visible : Visibility.Hidden;
                });

                //await Dispatcher.InvokeAsync(() =>
                //{
                //    this.DataContext = null;
                //    this.DataContext = this;
                //    this.UpdateLayout();
                //});
            });
        }

        private async Task OpenAsset(IAssetEntry entry)
        {
            if (entry is EbxAssetEntry ebxEntry)
            {
                await OpenEbxAsset(ebxEntry);
                return;
            }

            if (entry is LiveTuningUpdate.LiveTuningUpdateEntry ltuEntry)
            {
                OpenLTUAsset(ltuEntry);
                return;
            }

            try
            {



                LegacyFileEntry legacyFileEntry = entry as LegacyFileEntry;
                if (legacyFileEntry != null)
                {
                    SelectedLegacyEntry = legacyFileEntry;
                    btnImport.IsEnabled = true;

                    List<string> textViewers = new List<string>()
                        {
                            "LUA",
                            "XML",
                            "INI",
                            "NAV",
                            "JSON",
                            "TXT",
                            "CSV",
                            "TG", // some custom XML / JS / LUA file that is used in FIFA
							"JLT", // some custom XML / LUA file that is used in FIFA
							"PLS" // some custom XML / LUA file that is used in FIFA
						};

                    List<string> imageViewers = new List<string>()
                        {
                            "PNG",
                            "DDS"
                        };

                    List<string> bigViewers = new List<string>()
                        {
                            "BIG",
                            "AST"
                        };

                    if (textViewers.Contains(legacyFileEntry.Type))
                    {
                        MainEditorWindow.Log("Loading Legacy File " + SelectedLegacyEntry.Filename);

                        btnImport.IsEnabled = true;
                        btnExport.IsEnabled = true;
                        btnRevert.IsEnabled = true;

                        TextViewer.Visibility = Visibility.Visible;
                        using (var nr = new NativeReader(AssetManager.Instance.GetCustomAsset("legacy", legacyFileEntry)))
                        {
                            //TextViewer.Text = ASCIIEncoding.ASCII.GetString(nr.ReadToEnd());
                            TextViewer.Text = UTF8Encoding.UTF8.GetString(nr.ReadToEnd());
                        }
                    }
                    else if (imageViewers.Contains(legacyFileEntry.Type))
                    {
                        MainEditorWindow.Log("Loading Legacy File " + SelectedLegacyEntry.Filename);
                        btnImport.IsEnabled = true;
                        btnExport.IsEnabled = true;
                        ImageViewerScreen.Visibility = Visibility.Visible;

                        BuildTextureViewerFromStream((MemoryStream)ProjectManagement.Instance.Project.AssetManager.GetCustomAsset("legacy", legacyFileEntry));


                    }
                    else if (bigViewers.Contains(legacyFileEntry.Type))
                    {
                        BIGViewer.Visibility = Visibility.Visible;
                        BIGViewer.AssetEntry = legacyFileEntry;
                        //BIGViewer.ParentBrowser = this;
                        switch (legacyFileEntry.Type)
                        {
                            //case "BIG":
                            //	BIGViewer.LoadBig();
                            //	break;
                            default:
                                BIGViewer.LoadBig();
                                break;

                        }

                        btnImport.IsEnabled = true;
                        btnExport.IsEnabled = true;
                        btnRevert.IsEnabled = true;
                    }
                    else
                    {
                        MainEditorWindow.Log("Loading Unknown Legacy File " + SelectedLegacyEntry.Filename);
                        btnExport.IsEnabled = true;
                        btnImport.IsEnabled = true;
                        btnRevert.IsEnabled = true;

                        unknownFileDocumentsPane.Children.Clear();
                        var newLayoutDoc = new LayoutDocument();
                        newLayoutDoc.Title = SelectedEntry.DisplayName;
                        WpfHexaEditor.HexEditor hexEditor = new WpfHexaEditor.HexEditor();
                        using (var nr = new NativeReader(ProjectManagement.Instance.Project.AssetManager.GetCustomAsset("legacy", legacyFileEntry)))
                        {
                            hexEditor.Stream = new MemoryStream(nr.ReadToEnd());
                        }
                        newLayoutDoc.Content = hexEditor;
                        //hexEditor.BytesModified += HexEditor_BytesModified;
                        unknownFileDocumentsPane.Children.Insert(0, newLayoutDoc);
                        unknownFileDocumentsPane.SelectedContentIndex = 0;


                        UnknownFileViewer.Visibility = Visibility.Visible;
                    }

                }

            }
            catch (Exception e)
            {
                MainEditorWindow.Log($"Failed to load file with message {e.Message}");
                Debug.WriteLine(e.ToString());

                //DisplayUnknownFileViewer(AssetManager.Instance.GetEbxStream(ebxEntry));

            }

            DataContext = null;
            DataContext = this;
        }

        private async Task OpenEbxAsset(EbxAssetEntry ebxEntry, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                SelectedEntry = ebxEntry;
                SelectedEbxAsset = await AssetManager.Instance.GetEbxAsync(ebxEntry, cancellationToken: cancellationToken);
                if (ebxEntry.Type == "TextureAsset")
                {
                    try
                    {
                        MainEditorWindow.Log("Loading Texture " + ebxEntry.Filename);
                        var res = AssetManager.Instance.GetResEntry(ebxEntry.Name);
                        if (res != null)
                        {
                            MainEditorWindow.Log("Loading RES " + ebxEntry.Filename);

                            BuildTextureViewerFromAssetEntry(res);
                        }
                        else
                        {
                            throw new Exception("Unable to find RES Entry for " + ebxEntry.Name);
                        }
                    }
                    catch (Exception e)
                    {
                        MainEditorWindow.Log($"Failed to load texture with the message :: {e.Message}");
                    }



                }
                else if (ebxEntry.Type == "SkinnedMeshAsset")
                {
                    if (ebxEntry == null || ebxEntry.Type == "EncryptedAsset")
                    {
                        return;
                    }

                    MainEditorWindow.Log("Loading 3D Model " + ebxEntry.Filename);
                    var exporter = new MeshSetToFbxExport();
                    MeshSet meshSet = exporter.LoadMeshSet(ebxEntry);
                    exporter.Export(AssetManager.Instance, SelectedEbxAsset.RootObject, "test_noSkel.obj", "2012", "Meters", true, null, "*.obj", meshSet);
                    Thread.Sleep(150);

                    if (ModelViewerModel != null)
                        ModelViewerModel.Dispose();

                    ModelViewerModel = new MainViewModel(skinnedMeshAsset: SelectedEbxAsset, meshSet: meshSet);
                    this.ModelViewer.DataContext = ModelViewerModel;
                    this.ModelDockingManager.Visibility = Visibility.Visible;

                    await ModelViewerEBX.LoadEbx(ebxEntry, SelectedEbxAsset, MainEditorWindow);

                    Dispatcher.Invoke(() =>
                    {
                        this.btnExport.IsEnabled = ProfileManager.CanExportMeshes;
                        this.btnImport.IsEnabled = ProfileManager.CanImportMeshes;
                        this.btnRevert.IsEnabled = SelectedEntry.HasModifiedData;
                    });

                }
                else if (string.IsNullOrEmpty(ebxEntry.Type) || ebxEntry.Type == "UnknownType")
                {
                    //DisplayUnknownFileViewer(AssetManager.Instance.GetEbxStream(ebxEntry));
                }
                else
                {
                    if (ebxEntry == null || ebxEntry.Type == "EncryptedAsset")
                    {
                        return;
                    }
                    MainEditorWindow.Log("Loading EBX " + ebxEntry.Filename);

                    if (SelectedEbxAsset == null)
                    {
                        MainEditorWindow.Log($"Failed to load Selected Ebx Asset for {ebxEntry.Filename}");
                        return;
                    }

                    var successful = await EBXViewer.LoadEbx(ebxEntry, SelectedEbxAsset, MainEditorWindow);
                    Dispatcher.Invoke(() =>
                    {
                        EBXViewer.Visibility = Visibility.Visible;
                    });
                    Dispatcher.Invoke(() =>
                    {
                        btnRevert.IsEnabled = true;
                        btnImport.IsEnabled = true;
                        btnExport.IsEnabled = true;
                    });
                }
            }
            catch (Exception e)
            {
                MainEditorWindow.Log($"Failed to load file with message {e.Message}");
                Debug.WriteLine(e.ToString());

                //DisplayUnknownFileViewer(AssetManager.Instance.GetEbxStream(ebxEntry));

            }
        }

        private void OpenLTUAsset(LiveTuningUpdate.LiveTuningUpdateEntry entry)
        {
            MainEditorWindow.Log("Loading EBX " + entry.Filename);
            var ebx = entry.GetAsset();
            var successful = EBXViewer.LoadEbx(entry, ebx, MainEditorWindow);
            EBXViewer.Visibility = Visibility.Visible;
        }

        private async void btnImport_Click(object sender, RoutedEventArgs e)
        {
            var importStartTime = DateTime.Now;

            try
            {
                AssetEntryImporter assetEntryImporter = new AssetEntryImporter(SelectedEntry);
                var openFileDialog = assetEntryImporter.GetOpenDialogWithFilter();
                var dialogResult = openFileDialog.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value == true)
                {
                    _ = UpdateLoadingVisibility(true);

                    MainEditorWindow.Log("Importing " + openFileDialog.FileName);

                    if (SelectedEntry.Type == "SkinnedMeshAsset")
                    {
                        var skeletonEntryText = "content/character/rig/skeleton/player/skeleton_player";
                        MeshSkeletonSelector meshSkeletonSelector = new MeshSkeletonSelector();
                        var meshSelectorResult = meshSkeletonSelector.ShowDialog();
                        if (meshSelectorResult.HasValue && meshSelectorResult.Value)
                        {
                            if (!meshSelectorResult.Value)
                            {
                                MessageBox.Show("Cannot import without a Skeleton");
                                return;
                            }

                            skeletonEntryText = meshSkeletonSelector.AssetEntry.Name;
                            assetEntryImporter.ImportEbxSkinnedMesh(openFileDialog.FileName, skeletonEntryText);

                        }
                        else
                        {
                            MessageBox.Show("Cannot import without a Skeleton");
                            return;
                        }
                    }
                    else
                    {
                        var importResult = await assetEntryImporter.ImportAsync(openFileDialog.FileName);
                        if (!importResult)
                        {
                            MainEditorWindow.LogError("Failed to import file to " + SelectedEntry.Name);
                            return;
                        }

                        if (SelectedEntry.Type == "TextureAsset")
                        {
                            BuildTextureViewerFromAssetEntry(AssetManager.Instance.GetResEntry(SelectedEntry.Name));
                        }
                        else
                        {
                            await OpenAsset(SelectedEntry);
                        }
                        MainEditorWindow.Log($"Imported file {openFileDialog.FileName} successfully.");

                    }
                }

            }
            catch (Exception ex)
            {
                MainEditorWindow.LogError(ex.Message + Environment.NewLine);
            }

            _ = UpdateLoadingVisibility(false);

        }

        private async void btnExport_Click(object sender, RoutedEventArgs e)
        {
            _ = UpdateLoadingVisibility(true);
            try
            {
                if (SelectedLegacyEntry != null)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    var filt = "*." + SelectedLegacyEntry.Type;
                    if (SelectedLegacyEntry.Type == "DDS")
                        saveFileDialog.Filter = "Image files (*.png,*.dds)|*.png;*.dds;";
                    else
                        saveFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;

                    saveFileDialog.FileName = SelectedLegacyEntry.Filename;

                    if (saveFileDialog.ShowDialog().Value)
                    {
                        await ExportAsset(SelectedLegacyEntry, saveFileDialog.FileName);
                    }
                }
                else if (SelectedEntry != null)
                {
                    if (SelectedEntry.Type == "TextureAsset")
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog();
                        //var imageFilter = "Image files (*.DDS, *.PNG)|*.DDS;*.PNG";
                        var imageFilter = "Image files (*.PNG)|*.PNG";
                        saveFileDialog.Filter = imageFilter;
                        saveFileDialog.FileName = SelectedEntry.Filename;
                        saveFileDialog.AddExtension = true;
                        if (saveFileDialog.ShowDialog().Value)
                        {
                            await ExportAsset(SelectedEntry, saveFileDialog.FileName);

                            //var resEntry = ProjectManagement.Instance.Project.AssetManager.GetResEntry(SelectedEntry.Name);
                            //if (resEntry != null)
                            //{

                            //	using (var resStream = ProjectManagement.Instance.Project.AssetManager.GetRes(resEntry))
                            //	{
                            //		Texture texture = new Texture(resStream, ProjectManagement.Instance.Project.AssetManager);
                            //		var extractedExt = saveFileDialog.FileName.Substring(saveFileDialog.FileName.Length - 3, 3);
                            //		TextureExporter textureExporter = new TextureExporter();
                            //		textureExporter.Export(texture, saveFileDialog.FileName, "*." + extractedExt);
                            //		MainEditorWindow.Log($"Exported {SelectedEntry.Filename} to {saveFileDialog.FileName}");
                            //	}


                            //}
                        }
                    }

                    else if (SelectedEntry.Type == "HotspotDataAsset")
                    {
                        var ebx = AssetManager.Instance.GetEbx((EbxAssetEntry)SelectedEntry);
                        if (ebx != null)
                        {
                            SaveFileDialog saveFileDialog = new SaveFileDialog();
                            var filt = "*.json";
                            saveFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
                            saveFileDialog.FileName = SelectedEntry.Filename;
                            var dialogAnswer = saveFileDialog.ShowDialog();
                            if (dialogAnswer.HasValue && dialogAnswer.Value)
                            {
                                var json = JsonConvert.SerializeObject(ebx.RootObject, Formatting.Indented);
                                File.WriteAllText(saveFileDialog.FileName, json);
                                MainEditorWindow.Log($"Exported {SelectedEntry.Filename} to {saveFileDialog.FileName}");
                            }
                        }
                        else
                        {
                            MainEditorWindow.Log("Failed to export file");
                        }
                    }

                    else if (SelectedEntry.Type == "SkinnedMeshAsset")
                    {
                        var skinnedMeshEntry = (EbxAssetEntry)SelectedEntry;
                        if (skinnedMeshEntry != null)
                        {

                            var skinnedMeshEbx = AssetManager.Instance.GetEbx(skinnedMeshEntry);
                            if (skinnedMeshEbx != null)
                            {
                                //var resentry = AssetManager.Instance.GetResEntry(skinnedMeshEntry.Name);
                                //var res = AssetManager.Instance.GetRes(resentry);
                                //MeshSet meshSet = new MeshSet(res);
                                var skeletonEntryText = "content/character/rig/skeleton/player/skeleton_player";
                                var fifaMasterSkeleton = AssetManager.Instance.EBX.ContainsKey(skeletonEntryText);
                                if (!fifaMasterSkeleton)
                                {
                                    MeshSkeletonSelector meshSkeletonSelector = new MeshSkeletonSelector();
                                    var meshSelectorResult = meshSkeletonSelector.ShowDialog();
                                    if (meshSelectorResult.HasValue && meshSelectorResult.Value)
                                    {
                                        if (!meshSelectorResult.Value)
                                        {
                                            MessageBox.Show("Cannot export without a Skeleton");
                                            return;
                                        }

                                        skeletonEntryText = meshSkeletonSelector.AssetEntry.Name;

                                    }
                                    else
                                    {
                                        MessageBox.Show("Cannot export without a Skeleton");
                                        return;
                                    }
                                }

                                SaveFileDialog saveFileDialog = new SaveFileDialog();
                                var filt = "*.fbx";
                                saveFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
                                saveFileDialog.FileName = SelectedEntry.Filename;
                                var dialogAnswer = saveFileDialog.ShowDialog();
                                if (dialogAnswer.HasValue && dialogAnswer.Value)
                                {
                                    var exporter = new MeshSetToFbxExport();
                                    MeshSet meshSet = exporter.LoadMeshSet(skinnedMeshEntry);
                                    exporter.Export(AssetManager.Instance
                                        , skinnedMeshEbx.RootObject
                                        , saveFileDialog.FileName, "FBX_2012", "Meters", true, skeletonEntryText, "*.fbx", meshSet);


                                    MainEditorWindow.Log($"Exported {SelectedEntry.Name} to {saveFileDialog.FileName}");


                                }
                            }
                        }
                    }

                    else
                    {
                        var ebx = AssetManager.Instance.GetEbxStream((EbxAssetEntry)SelectedEntry);
                        if (ebx != null)
                        {
                            MessageBoxResult useJsonResult = MessageBox.Show(
                                                                    "Would you like to Export as JSON?"
                                                                    , "Export as JSON?"
                                                                    , MessageBoxButton.YesNoCancel);
                            if (useJsonResult == MessageBoxResult.Yes)
                            {
                                SaveFileDialog saveFileDialog = new SaveFileDialog();
                                var filt = "*.json";
                                saveFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
                                saveFileDialog.FileName = SelectedEntry.Filename;
                                var dialogAnswer = saveFileDialog.ShowDialog();
                                if (dialogAnswer.HasValue && dialogAnswer.Value)
                                {

                                    var obj = await Task.Run(() =>
                                    {
                                        return AssetManager.Instance.GetEbx((EbxAssetEntry)SelectedEntry).RootObject;
                                    });
                                    var serialisedObj = await Task.Run(() =>
                                    {
                                        return JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings()
                                        {
                                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                                            MaxDepth = 4,
                                        });
                                    });
                                    await File.WriteAllTextAsync(saveFileDialog.FileName, serialisedObj);
                                    MainEditorWindow.Log($"Exported {SelectedEntry.Filename} to {saveFileDialog.FileName}");

                                }
                            }
                            else if (useJsonResult == MessageBoxResult.No)
                            {
                                SaveFileDialog saveFileDialog = new SaveFileDialog();
                                var filt = "*.bin";
                                saveFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
                                saveFileDialog.FileName = SelectedEntry.Filename;
                                var dialogAnswer = saveFileDialog.ShowDialog();
                                if (dialogAnswer.HasValue && dialogAnswer.Value)
                                {
                                    File.WriteAllBytes(saveFileDialog.FileName, ((MemoryStream)ebx).ToArray());
                                    MainEditorWindow.Log($"Exported {SelectedEntry.Filename} to {saveFileDialog.FileName}");
                                }
                            }
                            else
                            {

                            }



                        }
                        else
                        {
                            MainEditorWindow.Log("Failed to export file");
                        }
                    }
                }
            }
            finally
            {
                UpdateLoadingVisibility(false);
            }
        }

        private async void btnRevert_Click(object sender, RoutedEventArgs e)
        {
            UpdateLoadingVisibility(true);
            try
            {
                if (SelectedEntry != null)
                {
                    if (EBXViewer != null && EBXViewer.Visibility == Visibility.Visible)
                    {
                        EBXViewer.RevertAsset();
                    }
                    else
                    {
                        AssetManager.Instance.RevertAsset(SelectedEntry);
                    }
                }

                if (SelectedLegacyEntry != null)
                {
                    AssetManager.Instance.RevertAsset(SelectedLegacyEntry);
                    if (SelectedLegacyEntry.Type == "DDS")
                    {
                        //BuildTextureViewerFromStream(AssetManager.Instance.GetCustomAsset("legacy", SelectedLegacyEntry), SelectedLegacyEntry);
                        BuildTextureViewerFromStream((MemoryStream)AssetManager.Instance.GetCustomAsset("legacy", SelectedLegacyEntry));
                    }
                }

                //if (MainEditorWindow != null)
                //    MainEditorWindow.UpdateAllBrowsers();

                await OpenAsset(SelectedEntry);

                //UpdateAssetListView();
            }
            finally
            {
                UpdateLoadingVisibility(false);
            }

        }

        private void TextViewer_LostFocus(object sender, RoutedEventArgs e)
        {
            var bytes = ASCIIEncoding.UTF8.GetBytes(TextViewer.Text);

            //if (SelectedLegacyEntry != null)
            //{
            //    AssetManager.Instance.ModifyLegacyAsset(SelectedLegacyEntry.Name
            //                , bytes
            //                , false);
            //    UpdateAssetListView();
            //}
        }

        private void BuildTextureViewerFromAssetEntry(ResAssetEntry res)
        {
            ImageViewer.Source = null;

            using (Texture textureAsset = new Texture(res))
            {
                //MemoryStream memoryStream = new MemoryStream();
                Stream expToStream = null;

                try
                {
                    CurrentDDSImageFormat = textureAsset.PixelFormat;

                    var bPath = Directory.GetCurrentDirectory() + @"\temp.png";

                    TextureExporter textureExporter = new TextureExporter();

                    try
                    {
                        expToStream = textureExporter.ExportToStream(textureAsset, TextureUtils.ImageFormat.PNG);
                        expToStream.Position = 0;
                        //var ddsData = textureExporter.WriteToDDS(textureAsset);
                        //BuildTextureViewerFromStream(new MemoryStream(ddsData));

                    }
                    catch (Exception exception_ToStream)
                    {
                        MainEditorWindow.LogError($"Error loading texture with message :: {exception_ToStream.Message}");
                        MainEditorWindow.LogError(exception_ToStream.ToString());
                        ImageViewer.Source = null; ImageViewerScreen.Visibility = Visibility.Collapsed;

                        textureExporter.Export(textureAsset, res.Filename + ".DDS", "*.DDS");
                        MainEditorWindow.LogError($"As the viewer failed. The image has been exported to {res.Filename}.dds instead.");
                        return;
                    }

                    //using var nr = new NativeReader(expToStream);
                    //nr.Position = 0;
                    //var textureBytes = nr.ReadToEnd();

                    //ImageViewer.Source = LoadImage(textureBytes);
                    ImageViewer.Source = LoadImage(((MemoryStream)expToStream).ToArray());




                    ImageViewerScreen.Visibility = Visibility.Visible;
                    ImageViewer.MaxHeight = textureAsset.Height;
                    ImageViewer.MaxWidth = textureAsset.Width;

                    btnExport.IsEnabled = true;
                    btnImport.IsEnabled = true;
                    btnRevert.IsEnabled = true;

                }
                catch (Exception e)
                {
                    MainEditorWindow.LogError($"Error loading texture with message :: {e.Message}");
                    MainEditorWindow.LogError(e.ToString());
                    ImageViewer.Source = null; ImageViewerScreen.Visibility = Visibility.Collapsed;
                }
                finally
                {
                    // clean up resources
                    if (expToStream != null)
                    {
                        expToStream.Close();
                        expToStream.Dispose();
                        expToStream = null;
                    }
                }
            }
        }

        public string CurrentDDSImageFormat { get; set; }

        //private void BuildTextureViewerFromStream(Stream stream, AssetEntry assetEntry = null)
        private void BuildTextureViewerFromStream(MemoryStream stream)
        {

            try
            {
                ImageViewer.Source = null;

                var bPath = Directory.GetCurrentDirectory() + @"\temp.png";

                ImageEngineImage imageEngineImage = new ImageEngineImage(stream.ToArray());
                var iData = imageEngineImage.Save(new ImageFormats.ImageEngineFormatDetails(ImageEngineFormat.BMP), MipHandling.KeepTopOnly, removeAlpha: false);

                //var CurrentDDSImage = new DDSImage(stream);
                //stream.Position = 0;
                //var dds2 = new DDSImage2(((MemoryStream)stream).ToArray());
                //FourCC fourCC = dds2.GetPixelFormatFourCC();

                //CurrentDDSImageFormat = fourCC.ToString() + " - " + CurrentDDSImage._image.ToString() + " - " + CurrentDDSImage._image.Format.ToString();
                //var textureBytes = new NativeReader(CurrentDDSImage.SaveToStream()).ReadToEnd();
                ////var textureBytes = new NativeReader(textureExporter.ExportToStream(texture)).ReadToEnd();

                //CurrentDDSImageFormat = imageEngineImage.Format.ToString() + " - " + imageEngineImage.FormatDetails.DX10Format;

                //ImageViewer.Source = LoadImage(textureBytes);
                ImageViewer.Source = LoadImage(iData);
                ImageViewerScreen.Visibility = Visibility.Visible;

                btnExport.IsEnabled = true;
                btnImport.IsEnabled = true;
                btnRevert.IsEnabled = true;

            }
            catch (Exception e)
            {
                MainEditorWindow.LogError(e.Message);
                ImageViewer.Source = null; ImageViewerScreen.Visibility = Visibility.Collapsed;
            }

        }

        System.Windows.Media.Imaging.BitmapImage bitmapImage = new System.Windows.Media.Imaging.BitmapImage();

        private System.Windows.Media.Imaging.BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            if (bitmapImage != null)
            {
                bitmapImage = null;
                bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
            }

            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.UriSource = null;
                bitmapImage.StreamSource = mem;
                bitmapImage.EndInit();
            }
            bitmapImage.Freeze();

            imageData = null;

            return bitmapImage;
        }

        private async Task ExportAsset(AssetEntry assetEntry, string saveLocation)
        {
            bool isFolder = false;
            if (new DirectoryInfo(saveLocation).Exists)
            {
                saveLocation += "\\" + assetEntry.Filename;
                isFolder = true;
            }

            if (assetEntry is LegacyFileEntry)
            {
                var legacyData = ((MemoryStream)AssetManager.Instance.GetCustomAsset("legacy", assetEntry)).ToArray();
                if (assetEntry.Type == "DDS"
                    &&
                    (saveLocation.Contains("PNG", StringComparison.OrdinalIgnoreCase)
                     || isFolder)
                    )
                {
                    if (isFolder)
                    {
                        saveLocation += ".png";
                    }

                    ImageEngineImage originalImage = new ImageEngineImage(legacyData);

                    var imageBytes = originalImage.Save(
                        new ImageFormats.ImageEngineFormatDetails(
                            ImageEngineFormat.PNG)
                        , MipHandling.KeepTopOnly
                        , removeAlpha: false);

                    await File.WriteAllBytesAsync(saveLocation, imageBytes);
                }
                else if (assetEntry.Type == "DDS" && saveLocation.Contains("DDS", StringComparison.OrdinalIgnoreCase))
                {
                    ImageEngineImage originalImage = new ImageEngineImage(legacyData);

                    await originalImage.Save(saveLocation
                        , new ImageFormats.ImageEngineFormatDetails(ImageEngineFormat.DDS_DXT5)
                        , GenerateMips: MipHandling.KeepExisting
                        , removeAlpha: false);
                }
                else
                {
                    if (isFolder)
                        saveLocation += "." + assetEntry.Type;

                    await File.WriteAllBytesAsync(saveLocation, legacyData);
                }
                MainEditorWindow.Log($"Exported {assetEntry.Filename} to {saveLocation}");
            }
            else if (assetEntry is EbxAssetEntry)
            {
                if (assetEntry.Type == "TextureAsset")
                {
                    var resEntry = AssetManager.Instance.GetResEntry(assetEntry.Name);
                    if (resEntry != null)
                    {
                        using (var resStream = AssetManager.Instance.GetRes(resEntry))
                        {
                            Texture texture = new Texture(resStream, ProjectManagement.Instance.Project.AssetManager);
                            TextureExporter textureExporter = new TextureExporter();
                            if (isFolder)
                                saveLocation += ".png";
                            textureExporter.Export(texture, saveLocation, "*.png");
                            MainEditorWindow.Log($"Exported {assetEntry.Filename} to {saveLocation}");
                        }
                    }
                }
                else
                {
                    var ebx = AssetManager.Instance.GetEbxStream((EbxAssetEntry)assetEntry);
                    if (ebx != null)
                    {
                        if (isFolder)
                            saveLocation += ".bin";
                        File.WriteAllBytes(saveLocation, ((MemoryStream)ebx).ToArray());
                        MainEditorWindow.Log($"Exported {assetEntry.Filename} to {saveLocation}");

                    }
                    else
                    {
                        MainEditorWindow.Log("Failed to export file");
                    }
                }
            }
        }
    }
}
