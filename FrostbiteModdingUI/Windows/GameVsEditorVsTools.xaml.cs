﻿using FIFAModdingUI.Windows.Profile;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
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
using static FrostySdk.ProfileManager;

namespace FMT.Windows
{
    /// <summary>
    /// Interaction logic for GameVsEditorVsTools.xaml
    /// </summary>
    public partial class GameVsEditorVsTools : MetroWindow
    {
        Profile SelectedProfile { get; set; }

        //public GameVsEditorVsTools()
        //{
        //    InitializeComponent();
        //}

        public GameVsEditorVsTools(Window owner, Profile selectedProfile)
        {
            InitializeComponent();
            Owner = owner;
            SelectedProfile = selectedProfile;
            //Loaded += GameVsEditorVsTools_Loaded;
            //Closing += GameVsEditorVsTools_Closing;
        }

        private void GameVsEditorVsTools_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void GameVsEditorVsTools_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //if(Owner.Visibility != Visibility.Visible)
            //{

            //}
            //Owner.Show();
        }

        private void btnLoadGameModLauncher_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            LaunchWindow launchWindow = new LaunchWindow(Owner);
            launchWindow.Show();
            this.Close();

        }

        private void btnLoadGameEditor_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = a.GetTypes().FirstOrDefault(x => x.Name.Contains(SelectedProfile.EditorScreen, StringComparison.OrdinalIgnoreCase));
                if (t != null)
                {
                    App.MainEditorWindow = (Window)Activator.CreateInstance(t, Owner);
                    App.MainEditorWindow.Show();
                    this.Close();
                    return;
                }
            }
        }

        private void btnLoadGameTools_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

        }
    }
}
