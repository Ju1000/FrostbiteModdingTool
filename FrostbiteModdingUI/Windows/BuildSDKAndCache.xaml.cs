﻿using FrostyEditor.IO;
using FrostyEditor.Windows;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
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
using System.Windows.Shapes;
using v2k4FIFAModdingCL;
using SdkGenerator;
using Frosty.Hash;
using MahApps.Metro.Controls;

namespace FIFAModdingUI.Windows
{
    /// <summary>
    /// Interaction logic for BuildSDKAndCache.xaml
    /// </summary>
    public partial class BuildSDKAndCache : MetroWindow, ILogger, IDisposable
    {

        private BuildCache buildCache = new BuildCache();
        public bool DoNotAutoRebuild = false;

        public BuildSDKAndCache()
        {
            InitializeComponent();

            Loaded += BuildSDKAndCache_Loaded;
        }

        //protected override void OnContentRendered(EventArgs e)
        //{
        //    base.OnContentRendered(e);

        //    if(!DoNotAutoRebuild)
        //        Rebuild();
        //}

        private async void BuildSDKAndCache_Loaded(object sender, RoutedEventArgs e)
        {
            if(Visibility == Visibility.Visible)
            {
                if (!DoNotAutoRebuild)
                    Rebuild();
            }
        }


        public async Task Rebuild()
        {
            if (DoesCacheNeedsRebuilding())
            {
                lblCacheNeedsRebuilding.Visibility = Visibility.Visible;
                btnRunBuildCache.IsEnabled = false;
                btnRunBuild.IsEnabled = false;

                Dispatcher.Invoke(() => { txtOuputMessage.Content = "Building Cache. Please wait 3-15 minutes to complete!"; });

                if (FileSystem.Instance == null)
                    new FileSystem(GameInstanceSingleton.Instance.GAMERootPath);
                // -----------------------------------------
                //
                await buildCache.LoadDataAsync(GameInstanceSingleton.Instance.GAMEVERSION, FileSystem.Instance.BasePath, this, true, false);

                Dispatcher.Invoke(() => { txtOuputMessage.Content = "Building SDK. Please wait 1-2 minutes to complete!"; });

#if DEBUG 
                // ----------------------------------------------------------------------------------------------------------------------
                // Turned off until I get it past the issue with the assembly being loaded when it shouldnt have been before this process
                //
                //Dispatcher.Invoke(() => { txtOuputMessage.Text = "Cache Build Complete. Launching the Game."; });
                //if(GameInstanceSingleton.Instance.GAMEVERSION.Contains("FIFA", StringComparison.OrdinalIgnoreCase))
                //    Dispatcher.Invoke(() => { txtOutputSubMessage.Text = "FIFA will require you to start the game from launcher"; });

                //Process.Start(GameInstanceSingleton.GameEXE);
                //await Task.Delay(20000);

                //BuildSDK buildSDK = new BuildSDK(this);
                //buildSDK.WaitForMainMenu = true;
                //bool sdkBuildComplete = await buildSDK.Build();
                //if(!sdkBuildComplete)
                //{
                //    Dispatcher.Invoke(() => { txtOuputMessage.Text = "Unable to build SDK!"; });
                //}
                //Dispatcher.Invoke(() => { txtOuputMessage.Text = "SDK Build Complete! This dialog and game will now close."; });
                //var fbProcess = buildSDK.GetProcess();
                //if(fbProcess != null) 
                //    fbProcess.Close();
                //fbProcess = buildSDK.GetProcess();
                //if (fbProcess != null)
                //    fbProcess.CloseMainWindow();

#endif

                // -----------------------------------------
                // finish off with the SDK
                Dispatcher.Invoke(() => { txtOuputMessage.Content = "Building Cache EBX Indexing. Please wait 1-3 minutes to complete!"; });

                AssetManager.Instance.FullReset();
                AssetManager.Instance.Dispose();
                AssetManager.Instance = null;  
                
                await buildCache.LoadDataAsync(GameInstanceSingleton.Instance.GAMEVERSION, GameInstanceSingleton.Instance.GAMERootPath, this, false, true);


                await Task.Delay(2000);


            }

            DialogResult = true;
            this.Close();
        }

        public void Dispose()
        {
        }

        public bool DoesCacheNeedsRebuilding()
        {
            if (!GameInstanceSingleton.Instance.INITIALIZED)
                throw new Exception("Game has not been selected");

            if (ProfileManager.Initialize(GameInstanceSingleton.Instance.GAMEVERSION))
            {
                if (!File.Exists(ProfileManager.CacheName + ".cache"))
                {
                    return true;
                }

                if (ProfileManager.RequiresKey)
                {
                    byte[] array;

                    //array = NativeReader.ReadInStream(new FileStream(ProfilesLibrary.CacheName + ".key", FileMode.Open, FileAccess.Read));
                    // change this so it reads the easy version of the key
                    // 0B0E04030409080C010708010E0B0B02﻿
                    Debug.WriteLine($"[DEBUG] LoadDataAsync::Reading the Key");

                    array = NativeReader.ReadInStream(new FileStream(System.IO.Path.Combine(AppContext.BaseDirectory, "FrostbiteKeys", "fifa20.key"), FileMode.Open, FileAccess.Read));
                    byte[] array2 = new byte[16];
                    Array.Copy(array, array2, 16);
                    KeyManager.Instance.AddKey("Key1", array2);
                    if (array.Length > 16)
                    {
                        array2 = new byte[16];
                        Array.Copy(array, 16, array2, 0, 16);
                        KeyManager.Instance.AddKey("Key2", array2);
                        array2 = new byte[16384];
                        Array.Copy(array, 32, array2, 0, 16384);
                        KeyManager.Instance.AddKey("Key3", array2);
                    }

                    var FileSystem = new FileSystem(GameInstanceSingleton.Instance.GAMERootPath);

                    bool patched = false;

                    foreach (FileSystemSource source in ProfileManager.Sources)
                    {
                        FileSystem.AddSource(source.Path, source.SubDirs);
                        if (source.Path.ToLower().Contains("patch"))
                            patched = true;
                    }
                    byte[] key = KeyManager.Instance.GetKey("Key1");
                    FileSystem.Initialize(key, patched);

                    //using (NativeReader nativeReader = new NativeReader(new FileStream(ProfilesLibrary.CacheName + ".cache", FileMode.Open, FileAccess.Read)))
                    using (NativeReader nativeReader = new NativeReader(AssetManager.CacheDecompress()))
                    {
                        if (nativeReader.ReadLengthPrefixedString() != ProfileManager.ProfileName)
                        {
                            return true;
                        }
                        var cacheHead = nativeReader.ReadULong();
                        if (cacheHead != FileSystem.SystemIteration)
                        {
                            return true;
                        }
                    }
                }

            }

            return false;
        }

        public string LogText = string.Empty;

        public void Log(string text, params object[] vars)
        {
            LogAsync(text);
        }

        private string LastMessage = string.Empty;

        public async void LogAsync(string in_text)
        {
            if (LastMessage == in_text)
                return;

            LastMessage = in_text;

            await Task.Run(() =>
            {
                Dispatcher.Invoke(() => { txtOutputSubMessage.Content = in_text; });
            });

        }

        public void LogWarning(string text, params object[] vars)
        {
            Debug.WriteLine("[WARNING] " + text);
            LogAsync("[WARNING] " + text);
        }

        public void LogError(string text, params object[] vars)
        {
            Debug.WriteLine("[ERROR] " + text);
            LogAsync("[ERROR] " + text);
        }


        private async void btnRunBuildCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await buildCache.LoadDataAsync(GameInstanceSingleton.Instance.GAMEVERSION, GameInstanceSingleton.Instance.GAMERootPath, this, true);
            }
            catch(Exception ex)
            {
                txtOuputMessage.Content = "There was an error during the cache building process";
                File.WriteAllText("log_cache_error.txt", ex.ToString());
            }
        }

        private async void btnRunBuild_Click(object sender, RoutedEventArgs e)
        {
			Dispatcher.Invoke(() => btnRunBuild.IsEnabled = false);

			var buildSDK = new BuildSDK();
            await buildSDK.Build();
			
			Dispatcher.Invoke(() => btnRunBuild.IsEnabled = true);
		}

     
    }
}
