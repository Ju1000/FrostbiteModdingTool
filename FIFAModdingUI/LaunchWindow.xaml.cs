﻿using FIFAModdingUI.Mods;
using FIFAModdingUI.Windows.Profile;
using FrostbiteModdingUI.Windows;
using FrostySdk;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
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

namespace FIFAModdingUI
{
    /// <summary>
    /// Interaction logic for LaunchWindow.xaml
    /// </summary>
    public partial class LaunchWindow : Window, ILogger
    {
        ModListProfile Profile = new ModListProfile(null);
        public LaunchWindow()
        {
            InitializeComponent();
            this.Closing += LaunchWindow_Closing;

            GetListOfModsAndOrderThem();

            try
            {
                if (!File.Exists(AppSettings.Settings.FIFAInstallEXEPath))
                {
                    AppSettings.Settings.FIFAInstallEXEPath = null;
                }

                if (!string.IsNullOrEmpty(AppSettings.Settings.FIFAInstallEXEPath))
                {
                    txtFIFADirectory.Text = AppSettings.Settings.FIFAInstallEXEPath;
                    InitializeOfSelectedGame(AppSettings.Settings.FIFAInstallEXEPath);
                }
                else
                {
                    var bS = new FindGameEXEWindow().ShowDialog();
                    if (bS.HasValue && !string.IsNullOrEmpty(AppSettings.Settings.FIFAInstallEXEPath))
                    {
                        InitializeOfSelectedGame(AppSettings.Settings.FIFAInstallEXEPath);
                    }
                    else
                    {
                        throw new Exception("Unable to start up Game");
                    }
                }
            }
            catch (Exception e)
            {
                txtFIFADirectory.Text = "";
                Trace.WriteLine(e.ToString());
            }
        }

        private void LaunchWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            new MainWindow().Show();
        }

        private ObservableCollection<string> ListOfMods = new ObservableCollection<string>();

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

                var mL = new Mods.ModList();
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


                var mL = new Mods.ModList();
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
                cbProfiles.Items.Add(profButton);

            }
            var addnewprofilebutton = new Button() { Content = "Add new profile" };
            addnewprofilebutton.Click += Addnewprofilebutton_Click;
            cbProfiles.Items.Add(addnewprofilebutton);

            // load list of mods
            ListOfMods = new ObservableCollection<string>(new ModList(Profile).ModListItems);
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

        /// <summary>
        /// Setup and Extract lmods to the LegacyMods folder
        /// </summary>
        private void DoLegacyModSetup()
        {
            if (chkCleanLegacyModDirectory.IsChecked.HasValue && chkCleanLegacyModDirectory.IsChecked.Value)
            {
                RecursiveDelete(new DirectoryInfo(GameInstanceSingleton.LegacyModsPath));
            }

            if (!Directory.Exists(GameInstanceSingleton.GAMERootPath + "\\LegacyMods\\"))
                Directory.CreateDirectory(GameInstanceSingleton.GAMERootPath + "\\LegacyMods\\");
            if (!Directory.Exists(GameInstanceSingleton.LegacyModsPath))
                Directory.CreateDirectory(GameInstanceSingleton.LegacyModsPath);

            if (chkUseLegacyModSupport.IsChecked.HasValue && chkUseLegacyModSupport.IsChecked.Value)
            {
                foreach (var lmodZipped in ListOfMods.Where(x => x.Contains(".zip")))
                {
                    using (FileStream fsZip = new FileStream(lmodZipped, FileMode.Open))
                    {
                        ZipArchive zipA = new ZipArchive(fsZip);
                        foreach (var zipEntry in zipA.Entries.Where(x => x.FullName.Contains(".lmod")))
                        {
                            ZipArchive zipLMod = new ZipArchive(zipEntry.Open());
                            foreach (var zipEntLMOD in zipA.Entries)
                            {
                                if (File.Exists(txtFIFADirectory.Text + "\\LegacyMods\\Legacy\\" + zipEntLMOD.FullName))
                                {
                                    File.Delete(txtFIFADirectory.Text + "\\LegacyMods\\Legacy\\" + zipEntLMOD.FullName);
                                }
                            }
                            zipLMod.ExtractToDirectory(txtFIFADirectory.Text + "\\LegacyMods\\Legacy\\");
                        }
                    }
                }
                foreach (var lmod in ListOfMods.Where(x => x.Contains(".lmod")))
                {
                    using (FileStream fs = new FileStream(lmod, FileMode.Open))
                    {
                        ZipArchive zipA = new ZipArchive(fs);
                        foreach (var ent in zipA.Entries)
                        {
                            if (File.Exists(txtFIFADirectory.Text + "\\LegacyMods\\Legacy\\" + ent.FullName))
                            {
                                File.Delete(txtFIFADirectory.Text + "\\LegacyMods\\Legacy\\" + ent.FullName);
                            }
                        }
                        zipA.ExtractToDirectory(txtFIFADirectory.Text + "\\LegacyMods\\Legacy\\");
                    }
                }
            }

        }

        private async void btnLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(GameInstanceSingleton.GAMEVERSION) && !string.IsNullOrEmpty(GameInstanceSingleton.GAMERootPath))
            {
                DoLegacyModSetup();

                // Copy the Locale.ini if checked
                if (chkInstallLocale.IsChecked.Value)
                {
                    foreach (var z in ListOfMods.Where(x => x.Contains(".zip")))
                    {
                        using (FileStream fs = new FileStream(z, FileMode.Open))
                        {
                            ZipArchive zipA = new ZipArchive(fs);
                            foreach (var ent in zipA.Entries)
                            {
                                if (ent.Name.Contains("locale.ini"))
                                {
                                    ent.ExtractToFile(GameInstanceSingleton.FIFALocaleINIPath);
                                }
                            }
                        }
                    }
                }

                var k = chkUseFileSystem.IsChecked.Value;
                var useLegacyMods = chkUseLegacyModSupport.IsChecked.Value;
                var useLiveEditor = chkUseLiveEditor.IsChecked.Value;
                var useSymbolicLink = chkUseSymbolicLink.IsChecked.Value;
                // Start the game with mods
                await new TaskFactory().StartNew(async () =>
                {

                Dispatcher.Invoke(() =>
                {
                    btnLaunch.IsEnabled = false;
                });
                await Task.Delay(1000);

                    try
                    {
                        if (AssetManager.Instance == null)
                        {
                            if (string.IsNullOrEmpty(GameInstanceSingleton.GAMERootPath))
                                throw new Exception("Game path has not been selected or initialized");

                            if (string.IsNullOrEmpty(GameInstanceSingleton.GAMEVERSION))
                                throw new Exception("Game EXE has not been selected or initialized");

                            Log("Asset Manager is not initialised - Starting");
                            ProjectManagement projectManagement = new ProjectManagement(
                                GameInstanceSingleton.GAMERootPath + "\\" + GameInstanceSingleton.GAMEVERSION + ".exe"
                                , this);

                            if(AssetManager.Instance == null)
                            {
                                throw new Exception("Asset Manager has not been loaded against " + GameInstanceSingleton.GAMERootPath + "\\" + GameInstanceSingleton.GAMEVERSION + ".exe");
                            }
                            Log("Asset Manager loading complete");
                        }
                    }
                    catch(Exception AssetManagerException)
                    {
                        LogError(AssetManagerException.ToString());
                        return;
                    }
                    //Dispatcher.Invoke(() =>
                    //{

                    Log("Mod Compiler Started for " + GameInstanceSingleton.GAMEVERSION);

                    var launchSuccess = false;
                    try
                    {
                        var launchTask = LaunchFIFA.LaunchAsync(GameInstanceSingleton.GAMERootPath, "", new Mods.ModList().ModListItems, this, GameInstanceSingleton.GAMEVERSION, true, useSymbolicLink);
                        launchSuccess = await launchTask;
                    }
                    catch(Exception launchException)
                    {
                        Log("[ERROR] Error caught in Launch Task. You must fix the error before using this Launcher.");
                        LogError(launchException.ToString());
                    }
                    if (launchSuccess)
                    {

                        if (useLegacyMods)
                        {
                            string legacyModSupportFile = null;
                            if (GameInstanceSingleton.GAMEVERSION == "FIFA20")
                            {
                                legacyModSupportFile = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + @"\FIFA20Legacy.dll";
                            }
                            else if (GameInstanceSingleton.GAMEVERSION == "FIFA21")
                            {
                                legacyModSupportFile = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + @"\FIFA.dll";
                            }

                            if (!string.IsNullOrEmpty(legacyModSupportFile))
                            {

                                if (File.Exists(legacyModSupportFile))
                                {
                                    File.Copy(legacyModSupportFile, @GameInstanceSingleton.GAMERootPath + "v2k4LegacyModSupport.dll", true);
                                }

                                var legmodsupportdllpath = @GameInstanceSingleton.GAMERootPath + @"v2k4LegacyModSupport.dll";
                                //var actualsupportdllpath = @"E:\Origin Games\FIFA 20\v2k4LegacyModSupport.dll";
                                //Debug.WriteLine(legmodsupportdllpath);
                                //Debug.WriteLine(actualsupportdllpath);
                                try
                                {
                                    Log("Injecting Live Legacy Mod Support");
                                    GameInstanceSingleton.InjectDLLAsync(legmodsupportdllpath);
                                    Log("Injected Live Legacy Mod Support");

                                }
                                catch (Exception InjectDLLException)
                                {
                                    LogError("Launcher could not inject Live Legacy File Support");
                                    LogError(InjectDLLException.ToString());
                                }
                            }
                        }

                        if (useLiveEditor)
                        {
                            if (File.Exists(@GameInstanceSingleton.GAMERootPath + @"FIFALiveEditor.DLL"))
                                GameInstanceSingleton.InjectDLLAsync(@GameInstanceSingleton.GAMERootPath + @"FIFALiveEditor.DLL");
                        }
                    }
                    //});
                    await Task.Delay(1000);
                    Dispatcher.Invoke(() =>
                    {
                        btnLaunch.IsEnabled = true;
                    });

                });
            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = this.listMods.SelectedIndex;
            if (selectedIndex > -1)
            {
                var mL = new Mods.ModList();
                mL.ModListItems.Remove(this.ListOfMods[selectedIndex]);
                mL.Save();
                this.ListOfMods.RemoveAt(selectedIndex);

                GetListOfModsAndOrderThem();
            }

        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            dialog.Title = "Browse for your mod";
            dialog.Multiselect = false;
            dialog.Filter = "Mod files (*.zip, *.lmod, *.fbmod)|*.zip;*.lmod;*.fbmod";
            dialog.FilterIndex = 0;
            dialog.ShowDialog(this);
            var filePath = dialog.FileName;
            //var filePathSplit = filePath.Split('\\');
            //var filename = filePathSplit[filePathSplit.Length-1];
            //File.Copy(filePath, "Mods\\" + filename);
            if (!string.IsNullOrEmpty(filePath))
            {
                var mL = new Mods.ModList();
                mL.ModListItems.Add(filePath);
                mL.Save();
                GetListOfModsAndOrderThem();
            }

        }

        private void btnCloseModdingTool_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnBrowseFIFADirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            dialog.Title = "Find your FIFA exe";
            dialog.Multiselect = false;
            dialog.Filter = "exe files (*.exe)|*.exe";
            dialog.FilterIndex = 0;
            dialog.ShowDialog(this);
            var filePath = dialog.FileName;
            InitializeOfSelectedGame(filePath);

        }

        private void InitializeOfSelectedGame(string filePath)
        {
            if(!File.Exists(filePath))
            {
                LogError($"Game EXE Path {filePath} doesn't exist!");
                return;
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                AppSettings.Settings.FIFAInstallEXEPath = filePath;
                AppSettings.Settings.Save();

                if(GameInstanceSingleton.InitializeSingleton(filePath))
                {
                    if (!ProfilesLibrary.Initialize(GameInstanceSingleton.GAMEVERSION))
                    {
                        throw new Exception("Unable to Initialize Profile");
                    }
                    txtFIFADirectory.Text = GameInstanceSingleton.GAMERootPath;
                    btnLaunch.IsEnabled = true;
                }
                else
                {
                    throw new Exception("Unsupported Game EXE Selected");
                }

                if(GameInstanceSingleton.GAMEVERSION == "FIFA21")
                {
                    txtWarningAboutPersonalSettings.Visibility = Visibility.Visible;
                    chkUseSymbolicLink.Visibility = Visibility.Collapsed;
                    chkUseSymbolicLink.IsChecked = false;
                    btnLaunchOtherTool.Visibility = Visibility.Visible;
                    btnLaunchOtherTool.IsEnabled = true;
                }

                if(GameInstanceSingleton.IsCompatibleWithFbMod() || GameInstanceSingleton.IsCompatibleWithLegacyMod())
                {
                    listMods.IsEnabled = true;
                    
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
            }
        }

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
            LogAsync("[ERROR] " + text);

            if (File.Exists("ErrorLog.txt"))
                File.Delete("ErrorLog.txt");

            File.WriteAllText("ErrorLog.txt", DateTime.Now.ToString() + " \n" + text);
        }

        private void listMods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.listMods != null && this.listMods.SelectedIndex != -1 && this.listMods.SelectedItem != null) {
                var selectedIndex = this.listMods.SelectedIndex;
                var selectedMod = this.listMods.SelectedItem as string;
                if (selectedMod.Contains(".fbmod")) {
                        var fm = new FrostbiteMod(selectedMod);
                        if (fm.ModDetails != null)
                        {
                            txtModAuthor.Text = fm.ModDetails.Author;
                            txtModDescription.Text = fm.ModDetails.Description;
                            txtModTitle.Text = fm.ModDetails.Title;
                            txtModVersion.Text = fm.ModDetails.Version;
                        }
                    }
                    else if (selectedMod.Contains(".zip"))
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
            }
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
            findOtherLauncherEXEWindow.InjectLegacyModSupport = chkUseLegacyModSupport.IsChecked.Value;
            findOtherLauncherEXEWindow.InjectLiveEditorSupport = chkUseLiveEditor.IsChecked.Value;
            findOtherLauncherEXEWindow.ShowDialog();
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
