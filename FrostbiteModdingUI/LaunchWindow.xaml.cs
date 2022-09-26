﻿using CareerExpansionMod.CEM;
using FIFAModdingUI;
using FIFAModdingUI.Windows;
using FIFAModdingUI.Windows.Profile;
using FMT.Mods;
using FrostbiteModdingUI.Models;
using FrostbiteModdingUI.Windows;
using FrostbiteSdk;
using FrostySdk;
using FrostySdk.Frosty;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using paulv2k4ModdingExecuter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using v2k4FIFAModding;
using v2k4FIFAModding.Frosty;
using v2k4FIFAModdingCL;

//namespace FIFAModdingUI
namespace FMT
{
    /// <summary>
    /// Interaction logic for LaunchWindow.xaml
    /// </summary>
    public partial class LaunchWindow : MetroWindow, ILogger
    {
        public string WindowTitle { get; set; }

        public ModListProfile Profile { get; set; }

        public LaunchWindow(Window owner)
        {
            Owner = owner;
            InitializeComponent();
            Loaded += LaunchWindow_Loaded;
        }

        private async void LaunchWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowTitle = "FMT Launcher - " + App.ProductVersion;
            DataContext = this;

            //App.AppInsightClient.TrackPageView("Launcher Window - " + WindowTitle);

            try
            {
                var bS = new FindGameEXEWindow().ShowDialog();
                if (bS.HasValue && bS.Value == true && !string.IsNullOrEmpty(AppSettings.Settings.GameInstallEXEPath))
                {
                    await InitialiseSelectedGame(AppSettings.Settings.GameInstallEXEPath);
                }
                else
                {
                    MessageBox.Show("You must select an EXE to launch a game. Close this screen and reopen to select one");
                    throw new Exception("Unable to start up Game");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                //this.Close();
                return;
            }

            BuildSDKAndCache buildSDKAndCacheWindow = new BuildSDKAndCache();
            if (buildSDKAndCacheWindow.DoesCacheNeedsRebuilding())
            {
                buildSDKAndCacheWindow.ShowDialog();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            ProjectManagement.Instance = null;
            if (AssetManager.Instance != null)
            {
                AssetManager.Instance.Dispose();
                AssetManager.Instance = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            AppSettings.Settings.GameInstallEXEPath = null;
            Owner.Visibility = Visibility.Visible;
        }

        private ObservableCollection<ModList.ModItem> ListOfMods = new ObservableCollection<ModList.ModItem>();

        public bool EditorModIncluded { get; internal set; }

        private void up_click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = this.listMods.SelectedIndex;

            if (selectedIndex > -1 && selectedIndex > 0)
            {
                var itemToMoveUp = this.ListOfMods[selectedIndex];
                this.ListOfMods.RemoveAt(selectedIndex);
                this.ListOfMods.Insert(selectedIndex - 1, itemToMoveUp);
                this.listMods.SelectedIndex = selectedIndex - 1;

                var mL = new Mods.ModList(Profile);
                mL.ModListItems.Swap(selectedIndex -1, selectedIndex);
                mL.Save();
            }
        }

        private void down_click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = this.listMods.SelectedIndex;

            if (selectedIndex > -1 && selectedIndex + 1 < this.ListOfMods.Count)
            {
                var itemToMoveDown = this.ListOfMods[selectedIndex];
                this.ListOfMods.RemoveAt(selectedIndex);
                this.ListOfMods.Insert(selectedIndex + 1, itemToMoveDown);
                this.listMods.SelectedIndex = selectedIndex + 1;


                var mL = new Mods.ModList(Profile);
                mL.ModListItems.Swap(selectedIndex, selectedIndex + 1);
                mL.Save();
            }
        }

        private void GetListOfModsAndOrderThem()
        {
            // Load last profile


            // get profile list
            var items = ModListProfile.LoadAll().Select(x => x.ProfileName).ToList();
            foreach(var i in items)
            {
                var profButton = new Button() { Content = i };
                profButton.Click += (object sender, RoutedEventArgs e) => { };
                //cbProfiles.Items.Add(profButton);

            }
            var addnewprofilebutton = new Button() { Content = "Add new profile" };
            addnewprofilebutton.Click += Addnewprofilebutton_Click;
            //cbProfiles.Items.Add(addnewprofilebutton);

            // load list of mods
            var modItems = new ModList(Profile).ModListItems;
            ListOfMods = new ObservableCollection<ModList.ModItem>(modItems);
            listMods.ItemsSource = ListOfMods;
            
        }

        private void Addnewprofilebutton_Click(object sender, RoutedEventArgs e)
        {
            var newmodlistprofilewindow = new AddNewModListProfile();
            newmodlistprofilewindow.Show();
            newmodlistprofilewindow.Closed += Newmodlistprofilewindow_Closed;
        }

        private void Newmodlistprofilewindow_Closed(object sender, EventArgs e)
        {
            AddNewModListProfile newmodlistprofilewindow = sender as AddNewModListProfile;

        }

        public static void RecursiveDelete(DirectoryInfo baseDir)
        {
            try
            {
                if (!baseDir.Exists)
                    return;

                foreach (var dir in baseDir.EnumerateDirectories())
                {
                    RecursiveDelete(dir);
                }
                baseDir.Delete(true);
            }
            catch (Exception e)
            {

            }
        }

        bool stopLoggingUntilComplete = false;

        public async void LogAsync(string in_text)
        {
            if (stopLoggingUntilComplete)
                return;

            if(in_text.Contains("Read out of Cache"))
            {
                stopLoggingUntilComplete = true;

            }
            if (in_text.Contains("Loading complete "))
            {
                stopLoggingUntilComplete = false;
            }

            var txt = string.Empty;
            Dispatcher.Invoke(() => {
                txt = txtLog.Text;
            });

            var text = await Task.Run(() =>
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append(txt);
                stringBuilder.AppendLine(in_text);
                if(stopLoggingUntilComplete)
                {
                    stringBuilder.AppendLine("Please wait for compiler to finish first load. This may take 10-20 minutes");
                }

                return stringBuilder.ToString();
            });

            await Dispatcher.InvokeAsync(() =>
            {
                txtLog.Text = text;
                txtLog.ScrollToEnd();
            });

        }

        public void LogSync(string in_text)
        {
            Dispatcher.Invoke(() => {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append(txtLog.Text);
                stringBuilder.AppendLine(in_text);
                txtLog.Text = stringBuilder.ToString();
            });
        }

        /// <summary>
        /// Setup and Extract lmods to the LegacyMods folder
        /// </summary>
        private void DoLegacyModSetup()
        {
            if (switchCleanLegacyModDirectory.IsOn)
            {
                RecursiveDelete(new DirectoryInfo(GameInstanceSingleton.Instance.LegacyModsPath));
            }

            if (!Directory.Exists(GameInstanceSingleton.Instance.GAMERootPath + "\\LegacyMods\\"))
                Directory.CreateDirectory(GameInstanceSingleton.Instance.GAMERootPath + "\\LegacyMods\\");
            if (!Directory.Exists(GameInstanceSingleton.Instance.LegacyModsPath))
                Directory.CreateDirectory(GameInstanceSingleton.Instance.LegacyModsPath);

            if (switchUseLegacyModSupport.IsOn)
            {
                foreach (var lmodZipped in ListOfMods.Select(x=>x.Path).Where(x => x.Contains(".zip")))
                {
                    using (FileStream fsZip = new FileStream(lmodZipped, FileMode.Open))
                    {
                        ZipArchive zipA = new ZipArchive(fsZip);
                        foreach (var zipEntry in zipA.Entries.Where(x => x.FullName.Contains(".lmod")))
                        {
                            ZipArchive zipLMod = new ZipArchive(zipEntry.Open());
                            foreach (var zipEntLMOD in zipA.Entries)
                            {
                                if (File.Exists(GameInstanceSingleton.Instance.GAMERootPath + "\\LegacyMods\\Legacy\\" + zipEntLMOD.FullName))
                                {
                                    File.Delete(GameInstanceSingleton.Instance.GAMERootPath + "\\LegacyMods\\Legacy\\" + zipEntLMOD.FullName);
                                }
                            }
                            zipLMod.ExtractToDirectory(GameInstanceSingleton.Instance.GAMERootPath + "\\LegacyMods\\Legacy\\");
                        }
                    }
                }
                foreach (var lmod in ListOfMods.Select(x => x.Path).Where(x => x.Contains(".lmod")))
                {
                    using (FileStream fs = new FileStream(lmod, FileMode.Open))
                    {
                        ZipArchive zipA = new ZipArchive(fs);
                        foreach (var ent in zipA.Entries)
                        {
                            if (File.Exists(GameInstanceSingleton.Instance.GAMERootPath + "\\LegacyMods\\Legacy\\" + ent.FullName))
                            {
                                File.Delete(GameInstanceSingleton.Instance.GAMERootPath + "\\LegacyMods\\Legacy\\" + ent.FullName);
                            }
                        }
                        zipA.ExtractToDirectory(GameInstanceSingleton.Instance.GAMERootPath + "\\LegacyMods\\Legacy\\");
                    }
                }
            }

        }

        private async void btnLaunch_Click(object sender, RoutedEventArgs e)
        {
            var launchStartTime = DateTimeOffset.Now;
            if (!string.IsNullOrEmpty(GameInstanceSingleton.Instance.GAMEVERSION) && !string.IsNullOrEmpty(GameInstanceSingleton.Instance.GAMERootPath))
            {
                // -------------------------------------
                // Ensure this is the logger
                //
                GameInstanceSingleton.Logger = this;

                if (launcherOptions != null)
                {
                    launcherOptions.UseModData = switchUseModData.IsOn;
                    launcherOptions.UseLegacyModSupport = switchUseLegacyModSupport.IsOn;
                    launcherOptions.UseLiveEditor = switchUseLiveEditor.IsOn;
                    launcherOptions.InstallEmbeddedFiles = switchInstallEmbeddedFiles.IsOn;
                    launcherOptions.Save();
                }

                TabCont.SelectedIndex = 0;
                DoLegacyModSetup();

                // -------------------------------------------------------------------------
                // Ensure the latest locale.ini is installing into the ModData
                if (ProfilesLibrary.IsFIFA21DataVersion() || ProfilesLibrary.IsFIFA22DataVersion())
                {
                    FileInfo localeIni = new FileInfo(GameInstanceSingleton.Instance.FIFALocaleINIPath);
                    if (localeIni.Exists)
                    {
                        if (Directory.Exists(GameInstanceSingleton.Instance.ModDataPath))
                        {
                            FileInfo localeIniModData = new FileInfo(GameInstanceSingleton.Instance.FIFALocaleINIModDataPath);
                            if(localeIniModData.Exists)
                                File.Copy(localeIni.FullName, localeIniModData.FullName, true);
                        }
                    }
                }

                var useLegacyMods = switchUseLegacyModSupport.IsOn;
                var useLiveEditor = switchUseLiveEditor.IsOn;
                var useSymbolicLink = switchUseSymbolicLink.IsOn;
                var forceReinstallOfMods = switchForceReinstallMods.IsOn;
                var useModData = switchUseModData.IsOn;
                if(!useModData)
                {
                    MessageBoxResult modDataResult = MessageBox.Show("You are NOT using a ModData folder. " + Environment.NewLine +
                        "This is very risky and shouldn't be used unless all other options don't work! " + Environment.NewLine + 
                        "Do NOT try and play this ONLINE! You WILL be BANNED! " + Environment.NewLine +
                        "If your game breaks, it's NOT my fault. " + Environment.NewLine +
                        "EA Desktop does NOT have a REPAIR option. You will need to REINSTALL if the game breaks."
                        , "WARNING about not using the ModData folder", MessageBoxButton.OKCancel);
                    if(modDataResult == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                // Start the game with mods
                await new TaskFactory().StartNew(async () =>
                {

                Dispatcher.Invoke(() =>
                {
                    btnLaunch.IsEnabled = false;
                    //btnLaunchOtherTool.IsEnabled = false;
                });
                await Task.Delay(500);


                    Log("Mod Compiler Started for " + GameInstanceSingleton.Instance.GAMEVERSION);

                    Dispatcher.Invoke(() => {

                        DiscordInterop.DiscordRpcClient.UpdateDetails("Launching " + GameInstanceSingleton.Instance.GAMEVERSION + " with " + ListOfMods.Count + " mods");
                        DiscordInterop.DiscordRpcClient.UpdateState("V." + App.ProductVersion);

                        //var presence = new DiscordRPC.RichPresence();
                        //presence.Details = ;
                        //presence.State = "V." + App.ProductVersion;
                        //App.DiscordRpcClient.SetPresence(presence);
                        //App.DiscordRpcClient.Invoke();
                    });

                    var launchSuccess = false;
                    try
                    {
                        var launchTask = LaunchFIFA.LaunchAsync(
                            GameInstanceSingleton.Instance.GAMERootPath
                            , ""
                            , new Mods.ModList(Profile).ModListItems.Select(x=>x.Path).ToList()
                            , this
                            , GameInstanceSingleton.Instance.GAMEVERSION
                            , forceReinstallOfMods
                            , useSymbolicLink
                            , useModData);
                        launchSuccess = await launchTask;

                        //App.AppInsightClient.TrackRequest("Launcher Window - " + WindowTitle + " - Game Launched", launchStartTime,
                        //    TimeSpan.FromMilliseconds((DateTime.Now - launchStartTime).Milliseconds), "200", true);
                    }
                    catch(Exception launchException)
                    {
                        Log("[ERROR] Error caught in Launch Task. You must fix the error before using this Launcher.");
                        LogError(launchException.ToString());

                        //App.AppInsightClient.TrackRequest("Launcher Window - " + WindowTitle + " - Launch Error", launchStartTime,
                        //    TimeSpan.FromMilliseconds((DateTime.Now - launchStartTime).Milliseconds), "200", true);

                        //App.AppInsightClient.TrackException(launchException);

                    }
                    if (launchSuccess)
                    {
                        var ApplicationRunningLocation = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

                        if (useLegacyMods)
                        {
                            //LogSync("Legacy Injection - Tool running location is " + runningLocation);

                            string legacyModSupportFile = null;
                            if (GameInstanceSingleton.Instance.GAMEVERSION == "FIFA20")
                            {
                                LogSync("Legacy Injection - FIFA 20 found. Using FIFA20Legacy.DLL.");

                                legacyModSupportFile = ApplicationRunningLocation + @"\FIFA20Legacy.dll";
                            }
                            else if (ProfilesLibrary.IsFIFA21DataVersion())// GameInstanceSingleton.Instance.GAMEVERSION == "FIFA21")
                            {
                                LogSync("Legacy Injection - FIFA 21 found. Using FIFA.DLL.");
                                legacyModSupportFile = ApplicationRunningLocation + @"\FIFA.dll";
                            }

                            if (!File.Exists(legacyModSupportFile))
                            {
                                LogError($"Legacy Injection - Unable to find Legacy Injection DLL {legacyModSupportFile}");
                            }
                            else
                            {
                                var legmodsupportdllpath = @GameInstanceSingleton.Instance.GAMERootPath + @"v2k4LegacyModSupport.dll";

                                LogSync("Copying " + legacyModSupportFile + " to " + legmodsupportdllpath);
                                await Task.Delay(500);
                                File.Copy(legacyModSupportFile, legmodsupportdllpath, true);
                                await Task.Delay(500);

                                try
                                {
                                    LogSync("Live Legacy Mod Support - Injecting");
                                    await Task.Delay(500);
                                    bool InjectionSuccess = await GameInstanceSingleton.InjectDLLAsync(legmodsupportdllpath);
                                    if (InjectionSuccess)
                                        LogSync("Live Legacy Mod Support - Injected");
                                    else
                                        LogError("Launcher could not inject Live Legacy File Support");

                                }
                                catch (Exception InjectDLLException)
                                {
                                    LogError("Launcher could not inject Live Legacy File Support");
                                    LogError(InjectDLLException.ToString());

                                    //App.AppInsightClient.TrackException(InjectDLLException);
                                    //App.AppInsightClient.TrackException(new Exception(JsonConvert.SerializeObject(Process.GetProcesses().Select(x => x.ProcessName))));
                                }
                            }
                        }

                        if (useLiveEditor)
                        {
                            Log("Live Editor - Injecting");
                            var liveEditorResult = false;
                            if (File.Exists(@GameInstanceSingleton.Instance.GAMERootPath + @"FIFALiveEditor.DLL"))
                            {
                                Log("Live Editor - Using " + @GameInstanceSingleton.Instance.GAMERootPath + @"FIFALiveEditor.DLL");
                                liveEditorResult = await GameInstanceSingleton.InjectDLLAsync(@GameInstanceSingleton.Instance.GAMERootPath + @"FIFALiveEditor.DLL");
                            }
                            else if (File.Exists(@GameInstanceSingleton.Instance.GAMERootPath + @"LiveEditor\FIFALiveEditor.DLL"))
                            {
                                Log("Live Editor - Using " + @GameInstanceSingleton.Instance.GAMERootPath + @"LiveEditor\FIFALiveEditor.DLL");
                                liveEditorResult = await GameInstanceSingleton.InjectDLLAsync(@GameInstanceSingleton.Instance.GAMERootPath + @"LiveEditor\FIFALiveEditor.DLL");
                            }

                            if (liveEditorResult)
                            {
                                Log("Live Editor - Injected");
                                if(File.Exists(App.ApplicationDirectory + "\\CEM\\Data\\fifa_ng_db-meta.XML"))
                                {
                                    if (!Directory.Exists(GameInstanceSingleton.Instance.GAMERootPath + @"LiveEditorMods\root\Legacy\data\db"))
                                        Directory.CreateDirectory(GameInstanceSingleton.Instance.GAMERootPath + @"LiveEditorMods\root\Legacy\data\db");

                                    File.Copy(App.ApplicationDirectory + "\\CEM\\Data\\fifa_ng_db-meta.XML", GameInstanceSingleton.Instance.GAMERootPath + @"LiveEditorMods\root\Legacy\data\db" + "\\fifa_ng_db-meta.XML", true);
                                }
                            }
                            else
                            {
                                LogError("Live Editor - Unable to initialise");
                            }
                        }

                        Dispatcher.Invoke(() =>
                        {
                            switchForceReinstallMods.IsOn = false;
                            switchForceReinstallMods.IsEnabled = true;
                        });
                        Dispatcher.Invoke(() => {
                            //var presence = new DiscordRPC.RichPresence();
                            //presence.Details = "Playing " + GameInstanceSingleton.Instance.GAMEVERSION + " with " + ListOfMods.Count + " mods";
                            //presence.State = "V." + App.ProductVersion;

                            //App.DiscordRpcClient.SetPresence(presence);
                            //App.DiscordRpcClient.Invoke();

                            DiscordInterop.DiscordRpcClient.UpdateDetails("Playing " + GameInstanceSingleton.Instance.GAMEVERSION + " with " + ListOfMods.Count + " mods");

                        });

                        if (AssetManager.Instance != null)
                        {
                            AssetManager.Instance.Reset();
                            // Do Cleanup of Resources - Saving Memory
                            AssetManager.Instance.Dispose();
                            AssetManager.Instance = null;
                        }
                        ProjectManagement.Instance = null;

                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                    }
                    else
                    {
                        Dispatcher.Invoke(() => {
                            DiscordInterop.DiscordRpcClient.ClearPresence();
                        });
                    }

                    //});
                    await Task.Delay(1000);


                    Dispatcher.Invoke(() =>
                    {
                        btnLaunch.IsEnabled = true;
                        //btnLaunchOtherTool.IsEnabled = true;
                    });



                });
            }
        }

        private CEMWindow CEMWindow;

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = this.listMods.SelectedIndex;
            if (selectedIndex > -1)
            {
                var mL = new Mods.ModList(Profile);
                mL.ModListItems.Remove(this.ListOfMods[selectedIndex]);
                mL.Save();
                this.ListOfMods.RemoveAt(selectedIndex);

                GetListOfModsAndOrderThem();

                switchForceReinstallMods.IsEnabled = false;
                switchForceReinstallMods.IsOn = true;
            }

        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Browse for your mod";
            dialog.Multiselect = false;
            // remove zip as not compatible with *.fifamod / FIFA 21
            //dialog.Filter = "Mod files (*.zip, *.lmod, *.fbmod, *.fifamod)|*.zip;*.lmod;*.fbmod;*.fifamod";
            var supportedFiles = ProfilesLibrary.SupportedLauncherFileTypes;
            var filter = "Mod files (*.fbmod)|*.fbmod";
            if (supportedFiles != null && supportedFiles.Count > 0)
            {
                filter = "Mod files (";
                for (var i = 0; i < supportedFiles.Count; i++)
                {
                    filter += "*." + supportedFiles[i];
                    if (i < supportedFiles.Count - 1)
                        filter += ",";
                }
                filter += ")|";
                for (var i = 0; i < supportedFiles.Count; i++)
                {
                    filter += "*." + supportedFiles[i];
                    if (i < supportedFiles.Count - 1)
                        filter += ";";
                }
            }
            dialog.Filter = filter;//"Mod files (*.lmod, *.fbmod, *.fifamod)|*.lmod;*.fbmod;*.fifamod";
            dialog.FilterIndex = 0;
            dialog.ShowDialog(this);
            var filePath = dialog.FileName;
            if (!string.IsNullOrEmpty(filePath))
            {
                var mL = new Mods.ModList(Profile);
                mL.ModListItems.Add(new ModList.ModItem(filePath));
                mL.Save();
                GetListOfModsAndOrderThem();

                switchForceReinstallMods.IsEnabled = false;
                switchForceReinstallMods.IsOn = true;
            }

        }

        private void btnCloseModdingTool_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //private void btnBrowseFIFADirectory_Click(object sender, RoutedEventArgs e)
        //{
        //    var dialog = new OpenFileDialog();
        //    dialog.Title = "Find your FIFA exe";
        //    dialog.Multiselect = false;
        //    dialog.Filter = "exe files (*.exe)|*.exe";
        //    dialog.FilterIndex = 0;
        //    dialog.ShowDialog(this);
        //    var filePath = dialog.FileName;
        //    InitializeOfSelectedGame(filePath);

        //}

        public string LastGamePathLocation => App.ApplicationDirectory + "\\" + GameInstanceSingleton.Instance.GAMEVERSION + "LastLocation.json";


        private async Task InitialiseSelectedGame(string filePath)
        {
            if (!File.Exists(filePath))
            {
                LogError($"Game EXE Path {filePath} doesn't exist!");
                return;
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                this.DataContext = this;
                AppSettings.Settings.GameInstallEXEPath = filePath;

                //await File.WriteAllTextAsync(LastGamePathLocation, AppSettings.Settings.GameInstallEXEPath);

                if (GameInstanceSingleton.InitializeSingleton(filePath))
                {
                    if (!ProfilesLibrary.Initialize(GameInstanceSingleton.Instance.GAMEVERSION))
                    {
                        throw new Exception("Unable to Initialize Profile");
                    }
                    btnLaunch.IsEnabled = true;
                    GameInstanceSingleton.Logger = this;

                }
                else
                {
                    throw new Exception("Unsupported Game EXE Selected");
                }

                DiscordInterop.DiscordRpcClient.UpdateDetails("In Launcher - " + GameInstanceSingleton.Instance.GAMEVERSION);

                // -------------------------------------
                // V11.9 - Temporarily disable CEM due to changes for DB/Squad files
                //btnOpenCEMWindow.IsEnabled = false;
                // -------------------------------------

                if (ProfilesLibrary.IsFIFA21DataVersion())
                {
                    switchUseSymbolicLink.Visibility = Visibility.Collapsed;
                    switchUseSymbolicLink.IsOn = false;
                    //btnLaunchOtherTool.Visibility = Visibility.Visible;

                    //btnOpenCEMWindow.Visibility = Visibility.Visible;
                }

                if (ProfilesLibrary.IsMadden21DataVersion())
                {
                    switchUseSymbolicLink.Visibility = Visibility.Collapsed;
                    switchUseSymbolicLink.IsOn = false;
                }

                if (ProfilesLibrary.IsFIFA22DataVersion())
                {
                    switchUseSymbolicLink.Visibility = Visibility.Collapsed;
                    switchUseSymbolicLink.IsOn = false;
                    //switchUseModData.Visibility = Visibility.Collapsed;
                    //switchUseModData.IsOn = false;
                }

                switchCleanLegacyModDirectory.IsOn = false;
                switchCleanLegacyModDirectory.IsEnabled = GameInstanceSingleton.IsCompatibleWithLegacyMod();

                switchUseLegacyModSupport.IsEnabled = GameInstanceSingleton.IsCompatibleWithLegacyMod();
                switchUseLegacyModSupport.IsOn = false;

                switchInstallEmbeddedFiles.IsEnabled = ProfilesLibrary.IsFIFA21DataVersion();
                switchUseLiveEditor.IsEnabled = ProfilesLibrary.IsFIFA21DataVersion();

                if (GameInstanceSingleton.IsCompatibleWithFbMod() || GameInstanceSingleton.IsCompatibleWithLegacyMod())
                {
                    listMods.IsEnabled = true;

                    Profile = new ModListProfile(GameInstanceSingleton.Instance.GAMEVERSION);
                    GetListOfModsAndOrderThem();
                }
                else
                {
                    btnAdd.IsEnabled = false;
                    btnRemove.IsEnabled = false;
                    btnUp.IsEnabled = false;
                    btnDown.IsEnabled = false;
                    listMods.IsEnabled = false;
                    //listMods.Items.Clear();
                    new Mods.ModList();
                }

                launcherOptions = await LauncherOptions.LoadAsync();
                switchUseModData.IsOn = launcherOptions.UseModData.HasValue 
                                                ? launcherOptions.UseModData.Value : ProfilesLibrary.IsFIFA21DataVersion() || ProfilesLibrary.IsFIFA22DataVersion();
                switchUseLegacyModSupport.IsOn = launcherOptions.UseLegacyModSupport.HasValue && GameInstanceSingleton.IsCompatibleWithLegacyMod()
                                                ? launcherOptions.UseLegacyModSupport.Value : GameInstanceSingleton.IsCompatibleWithLegacyMod();
                switchInstallEmbeddedFiles.IsOn = launcherOptions.InstallEmbeddedFiles.HasValue ? launcherOptions.InstallEmbeddedFiles.Value : false;
                switchUseLiveEditor.IsOn = launcherOptions.UseLiveEditor.HasValue ? launcherOptions.UseLiveEditor.Value : false;

            }

            DataContext = null;
            DataContext = this;
        }

        public LauncherOptions launcherOptions { get; set; }

        public void Log(string text, params object[] vars)
        {
            LogAsync(text);
        }

        public void LogWarning(string text, params object[] vars)
        {
            LogAsync("[WARNING] " + text);
        }

        public void LogError(string text, params object[] vars)
        {
            LogSync("[ERROR] " + text);

            File.AppendAllText($"ErrorLog-{DateTime.Now.ToString("yyyy-MM-dd")}.txt", DateTime.Now.ToString() + " \n" + text);
        }

        public ModList.ModItem SelectedModListItem { get; set; }

        public IFrostbiteMod SelectedFrostbiteMod { get; set; }

        private void listMods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.listMods != null && this.listMods.SelectedIndex != -1 && this.listMods.SelectedItem != null) {
                var selectedIndex = this.listMods.SelectedIndex;
                SelectedModListItem = this.listMods.SelectedItem as ModList.ModItem;
                if (SelectedModListItem == null)
                    return;

                var selectedMod = SelectedModListItem.Path;

                if (selectedMod.Contains(".zip"))
                {
                    txtModDescription.Text = "Includes the following mods: \n";
                    using (FileStream fsModZipped = new FileStream(selectedMod, FileMode.Open))
                    {
                        ZipArchive zipArchive = new ZipArchive(fsModZipped);
                        foreach (var zaentr in zipArchive.Entries.Where(x => x.FullName.Contains(".fbmod")))
                        {
                            txtModDescription.Text += zaentr.Name + "\n";
                        }
                    }

                    txtModAuthor.Text = "Multiple";
                    txtModTitle.Text = selectedMod;
                    FileInfo fiZip = new FileInfo(selectedMod);
                    txtModVersion.Text = fiZip.CreationTime.ToString();
                }
                else
                {
                    SelectedFrostbiteMod = SelectedModListItem.GetFrostbiteMod();
                }
            }


            this.DataContext = null;
            this.DataContext = this;


        }

        private void cbProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnLegacyModSupportSettings_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnLaunchOtherTool_Click(object sender, RoutedEventArgs e)
        {
            DoLegacyModSetup();

            FindOtherLauncherEXEWindow findOtherLauncherEXEWindow = new FindOtherLauncherEXEWindow();
            findOtherLauncherEXEWindow.InjectLegacyModSupport = switchUseLegacyModSupport.IsOn;
            findOtherLauncherEXEWindow.InjectLiveEditorSupport = switchUseLiveEditor.IsOn;

            //btnLaunchOtherTool.IsEnabled = false;
            findOtherLauncherEXEWindow.ShowDialog();
            //btnLaunchOtherTool.IsEnabled = true;

        }

        private void btnOpenCEMWindow_Click(object sender, RoutedEventArgs e)
        {
            if(CEMWindow != null)
            {
                CEMWindow.Close();
                CEMWindow = null;
            }
            CEMWindow = new CEMWindow();
            CEMWindow.ShowDialog();
        }

        private void switchUseModData_Toggled(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => { 
                switchUseSymbolicLink.IsEnabled = switchUseModData.IsOn;

                // Cannot use symbolic link if there is no Mod Data folder
                if (!switchUseModData.IsOn)
                    switchUseSymbolicLink.IsOn = false;
            });
        }

        private void switchUseSymbolicLink_Toggled(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => { switchUseModData.IsOn = switchUseSymbolicLink.IsOn; switchUseModData.IsEnabled = !switchUseSymbolicLink.IsOn; });
        }
    }

    static class IListExtensions
    {
        public static void Swap<T>(
            this IList<T> list,
            int firstIndex,
            int secondIndex
        )
        {
            Contract.Requires(list != null);
            Contract.Requires(firstIndex >= 0 && firstIndex < list.Count);
            Contract.Requires(secondIndex >= 0 && secondIndex < list.Count);
            if (firstIndex == secondIndex)
            {
                return;
            }
            T temp = list[firstIndex];
            list[firstIndex] = list[secondIndex];
            list[secondIndex] = temp;
        }
    }
}
