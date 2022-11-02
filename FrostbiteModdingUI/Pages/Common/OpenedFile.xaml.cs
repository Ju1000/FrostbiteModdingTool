﻿using AvalonDock.Layout;
using CSharpImageLibrary;
using Frostbite.Textures;
using FrostbiteModdingUI.Models;
using FrostbiteModdingUI.Windows;
using FrostySdk;
using FrostySdk.Frostbite.IO;
using FrostySdk.Frostbite.IO.Output;
using FrostySdk.FrostySdk.Managers;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using v2k4FIFAModding.Frosty;

namespace FMT.Pages.Common
{
    /// <summary>
    /// Interaction logic for OpenedFile.xaml
    /// </summary>
    public partial class OpenedFile : UserControl
    {

        public bool IsLoading { get; set; }
        public Visibility LoadingAssetVisibility
        {
            get
            {
                return IsLoading ? Visibility.Visible : Visibility.Collapsed;
            }
        }


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

        private void OpenedFile_Loaded(object sender, RoutedEventArgs e)
        {
            //_ = Task.Run(async () =>
            //{
                Dispatcher.Invoke(() =>
                {
                    IsLoading = true;
                });
            Dispatcher.InvokeAsync(async() =>
            {
                await OpenAsset(SelectedEntry);
            });
                Dispatcher.Invoke(() =>
                {
                    IsLoading = false;
                });
            //});

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

        private async Task OpenEbxAsset(EbxAssetEntry ebxEntry)
        {
            try
            {
                SelectedEntry = ebxEntry;
                SelectedEbxAsset = AssetManager.Instance.GetEbx(ebxEntry);
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

                    var resentry = AssetManager.Instance.GetResEntry(ebxEntry.Name);
                    var res = AssetManager.Instance.GetRes(resentry);
                    MeshSet meshSet = new MeshSet(res);

                    var exporter = new MeshSetToFbxExport();
                    exporter.Export(AssetManager.Instance, SelectedEbxAsset.RootObject, "test_noSkel.obj", "2012", "Meters", true, null, "*.obj", meshSet);
                    Thread.Sleep(150);

                    if (ModelViewerModel != null)
                        ModelViewerModel.Dispose();

                    ModelViewerModel = new MainViewModel(skinnedMeshAsset: SelectedEbxAsset, meshSet: meshSet);
                    this.ModelViewer.DataContext = ModelViewerModel;
                    this.ModelDockingManager.Visibility = Visibility.Visible;

                    await ModelViewerEBX.LoadEbx(ebxEntry, SelectedEbxAsset, ProjectManagement.Instance.Project, MainEditorWindow);

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
                    var ebx = ProjectManagement.Instance.Project.AssetManager.GetEbx(ebxEntry);
                    if (ebx != null)
                    {
                        MainEditorWindow.Log("Loading EBX " + ebxEntry.Filename);

                        //EBXViewer = new Editor(ebxEntry, ebx, ProjectManagement.Instance.Project, MainEditorWindow);
                            var successful = await EBXViewer.LoadEbx(ebxEntry, ebx, ProjectManagement.Instance.Project, MainEditorWindow);
                        Dispatcher.Invoke(() =>
                        {
                            EBXViewer.Visibility = Visibility.Visible;
                        });
                        //EBXViewerPG.SetClass(ebx.RootObject);
                        //                  EBXViewerPG.Recreate();
                        //EBXViewerPG.Visibility = Visibility.Visible;


                        //EBXViewer.Visibility = successful.Result ? Visibility.Visible : Visibility.Collapsed;
                        //BackupEBXViewer.Visibility = !successful.Result ? Visibility.Visible : Visibility.Collapsed;
                        //BackupEBXViewer.SelectedObject = ebx.RootObject;
                        //BackupEBXViewer.SelectedPropertyItemChanged += BackupEBXViewer_SelectedPropertyItemChanged;
                        //BackupEBXViewer.Asset = ebx;
                        //BackupEBXViewer.SetClass(ebx.RootObject);
                        //BackupEBXViewer.Recreate();
                        Dispatcher.Invoke(() =>
                        {

                            btnRevert.IsEnabled = true;
                            //if (ebxEntry.Type == "HotspotDataAsset")
                            //{
                            btnImport.IsEnabled = true;
                            btnExport.IsEnabled = true;
                            //}
                        });
                    }
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
            var successful = EBXViewer.LoadEbx(entry, ebx, ProjectManagement.Instance.Project, MainEditorWindow);
            EBXViewer.Visibility = Visibility.Visible;
        }

        private async void btnImport_Click(object sender, RoutedEventArgs e)
        {
            var importStartTime = DateTime.Now;

            LoadingDialog loadingDialog = new LoadingDialog();
            loadingDialog.Show();
            try
            {

                var imageFilter = "Image files (*.dds, *.png)|*.dds;*.png";
                if (SelectedLegacyEntry != null)
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Filter = $"Files (*.{SelectedLegacyEntry.Type})|*.{SelectedLegacyEntry.Type}";
                    openFileDialog.FileName = SelectedLegacyEntry.Filename;

                    bool isImage = false;
                    if (SelectedLegacyEntry.Type == "DDS")
                    {
                        openFileDialog.Filter = imageFilter;
                        isImage = true;
                    }

                    var result = openFileDialog.ShowDialog();
                    if (result.HasValue && result.Value == true)
                    {
                        byte[] bytes = File.ReadAllBytes(openFileDialog.FileName);

                        if (isImage)
                        {
                            if (AssetManager.Instance.DoLegacyImageImport(openFileDialog.FileName, SelectedLegacyEntry))
                            {
                                BuildTextureViewerFromStream((MemoryStream)AssetManager.Instance.GetCustomAsset("legacy", SelectedLegacyEntry));
                            }
                            else
                            {
                                if (loadingDialog != null && loadingDialog.Visibility == Visibility.Visible)
                                {
                                    loadingDialog.Close();
                                }
                                return;
                            }
                        }
                        else
                        {
                            if (SelectedLegacyEntry.Type.ToUpper() != "DB" && SelectedLegacyEntry.Type.ToUpper() != "LOC")
                                TextViewer.Text = ASCIIEncoding.UTF8.GetString(bytes);

                            AssetManager.Instance.ModifyLegacyAsset(
                                SelectedLegacyEntry.Name
                                , bytes
                                , false);
                        }

                        MainEditorWindow.Log($"Imported {openFileDialog.FileName} to {SelectedLegacyEntry.Filename}");
                    }
                }
                else if (SelectedEntry != null)
                {
                    if (SelectedEntry.Type == "TextureAsset" || SelectedEntry.Type == "Texture")
                    {
                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        openFileDialog.Filter = imageFilter;
                        openFileDialog.FileName = SelectedEntry.Filename;
                        if (openFileDialog.ShowDialog().Value)
                        {
                            var resEntry = ProjectManagement.Instance.Project.AssetManager.GetResEntry(SelectedEntry.Name);
                            if (resEntry != null)
                            {
                                Texture texture = new Texture(resEntry);
                                TextureImporter textureImporter = new TextureImporter();
                                EbxAssetEntry ebxAssetEntry = SelectedEntry as EbxAssetEntry;

                                if (ebxAssetEntry != null)
                                {
                                    if (!textureImporter.Import(openFileDialog.FileName, ebxAssetEntry, ref texture))
                                    {
                                        MainEditorWindow.LogError("Unable to import");
                                    }
                                }

                                BuildTextureViewerFromAssetEntry(resEntry);

                                MainEditorWindow.Log($"Imported {openFileDialog.FileName} to {SelectedEntry.Filename}");

                            }
                        }
                    }

                    else if (SelectedEntry.Type == "HotspotDataAsset")
                    {
                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        var filt = "*.json";
                        openFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
                        openFileDialog.FileName = SelectedEntry.Filename;
                        var dialogResult = openFileDialog.ShowDialog();
                        if (dialogResult.HasValue && dialogResult.Value)
                        {
                            var ebx = AssetManager.Instance.GetEbx((EbxAssetEntry)SelectedEntry);
                            if (ebx != null)
                            {
                                var robj = (dynamic)ebx.RootObject;
                                var fileHS = (Newtonsoft.Json.Linq.JArray)JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(openFileDialog.FileName)).Hotspots;
                                var fhs2 = fileHS.ToObject<List<dynamic>>();
                                for (var i = 0; i < robj.Hotspots.Count; i++)
                                {
                                    robj.Hotspots[i].Bounds.x = (float)fhs2[i].Bounds.x;
                                    robj.Hotspots[i].Bounds.y = (float)fhs2[i].Bounds.y;
                                    robj.Hotspots[i].Bounds.z = (float)fhs2[i].Bounds.z;
                                    robj.Hotspots[i].Bounds.w = (float)fhs2[i].Bounds.w;
                                    robj.Hotspots[i].Rotation = (float)fhs2[i].Rotation;
                                }
                                AssetManager.Instance.ModifyEbx(SelectedEntry.Name, ebx);

                                // Update the Viewers
                                //EBXViewer = new Editor(SelectedEntry, ebx, ProjectManagement.Instance.Project, MainEditorWindow);
                                await EBXViewer.LoadEbx(SelectedEntry, ebx, ProjectManagement.Instance.Project, MainEditorWindow);

                            }
                        }
                    }


                    else if (SelectedEntry.Type == "SkinnedMeshAsset")
                    {
                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        openFileDialog.Filter = "Fbx files (*.fbx)|*.fbx";
                        openFileDialog.FileName = SelectedEntry.Filename;

                        var fbximport_dialogresult = openFileDialog.ShowDialog();
                        if (fbximport_dialogresult.HasValue && fbximport_dialogresult.Value)
                        {
                            var skinnedMeshEbx = await AssetManager.Instance.GetEbxAsync((EbxAssetEntry)SelectedEntry);
                            if (skinnedMeshEbx != null)
                            {
                                var resentry = AssetManager.Instance.GetResEntry(SelectedEntry.Name);
                                var res = await AssetManager.Instance.GetResAsync(resentry);
                                MeshSet meshSet = new MeshSet(res);

                                var skeletonEntryText = "content/character/rig/skeleton/player/skeleton_player";
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

                                try
                                {
                                    await loadingDialog.UpdateAsync("Importing Mesh", "");
                                    FrostySdk.Frostbite.IO.Input.FBXImporter importer = new FrostySdk.Frostbite.IO.Input.FBXImporter();
                                    importer.ImportFBX(openFileDialog.FileName, meshSet, skinnedMeshEbx, (EbxAssetEntry)SelectedEntry
                                        , new FrostySdk.Frostbite.IO.Input.MeshImportSettings()
                                        {
                                            SkeletonAsset = skeletonEntryText
                                        });
                                    MainEditorWindow.Log($"Imported {openFileDialog.FileName} to {SelectedEntry.Name}");

                                    OpenAsset(SelectedEntry);
                                }
                                catch (Exception ImportException)
                                {
                                    MainEditorWindow.LogError(ImportException.Message);

                                }

                            }
                        }
                    }

                    else // Raw data import
                    {
                        MessageBoxResult useJsonResult = MessageBox.Show(
                                                                "Would you like to Import as JSON?"
                                                                , "Import as JSON?"
                                                                , MessageBoxButton.YesNoCancel);
                        if (useJsonResult == MessageBoxResult.Yes)
                        {
                            OpenFileDialog openFileDialog = new OpenFileDialog();
                            var filt = "*.json";
                            openFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
                            openFileDialog.FileName = SelectedEntry.Filename;
                            var dialogResult = openFileDialog.ShowDialog();
                            if (dialogResult.HasValue && dialogResult.Value)
                            {
                                var binaryText = File.ReadAllText(openFileDialog.FileName);
                                AssetManager.Instance.ModifyEbxJson(SelectedEntry.Name, binaryText);

                                OpenAsset(SelectedEntry);
                            }
                        }
                        else if (useJsonResult == MessageBoxResult.No)
                        {
                            OpenFileDialog openFileDialog = new OpenFileDialog();
                            var filt = "*.bin";
                            openFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
                            openFileDialog.FileName = SelectedEntry.Filename;
                            var dialogResult = openFileDialog.ShowDialog();
                            if (dialogResult.HasValue && dialogResult.Value)
                            {
                                var binaryData = File.ReadAllBytes(openFileDialog.FileName);
                                AssetManager.Instance.ModifyEbxBinary(SelectedEntry.Name, binaryData);

                                OpenAsset(SelectedEntry);
                            }
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                MainEditorWindow.LogError(ex.Message);
            }

            if (MainEditorWindow != null)
            {
                MainEditorWindow.UpdateAllBrowsers();
            }

            if (loadingDialog != null && loadingDialog.Visibility == Visibility.Visible)
            {
                loadingDialog.Close();
            }
        }

        private async void btnExport_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void btnRevert_Click(object sender, RoutedEventArgs e)
        {
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

            using (Texture textureAsset = new Texture(res))
            {
                try
                {
                    ImageViewer.Source = null;
                    CurrentDDSImageFormat = textureAsset.PixelFormat;


                    var bPath = Directory.GetCurrentDirectory() + @"\temp.png";

                    TextureExporter textureExporter = new TextureExporter();
                    MemoryStream memoryStream = new MemoryStream();

                    Stream expToStream = null;
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

                ImageEngineImage imageEngineImage = new ImageEngineImage(((MemoryStream)stream).ToArray());
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

        private static System.Windows.Media.Imaging.BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new System.Windows.Media.Imaging.BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }
    }
}