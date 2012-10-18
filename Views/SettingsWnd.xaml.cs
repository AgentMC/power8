﻿using System.Windows;
using Power8.Helpers;

namespace Power8.Views
{
    /// <summary>
    /// Interaction logic for Settings window.
    /// Guess it's the first window in project where only 
    /// interaction logic is placed in codebehind :)
    /// </summary>
    public partial class SettingsWnd
    {
        /// <summary>
        /// Constructor. Sets window's data context to Settings Manager's current instance
        /// and hides BlockMetro box if not Win8.
        /// </summary>
        public SettingsWnd()
        {
            InitializeComponent();
            DataContext = SettingsManager.Instance;
            if(!Util.OsIs.EightOrMore)
                MWBlockMetro.Visibility = Visibility.Collapsed;
            //System-wide localization
            StgCbAdmT.Content = PowerItemTree.AdminToolsRoot.FriendlyName;
            StgCbComp.Content = PowerItemTree.MyComputerRoot.FriendlyName;
            StgCbCpnl.Content = PowerItemTree.ControlPanelRoot.FriendlyName;
            StgCbDocs.Content = PowerItemTree.LibrariesRoot.FriendlyName;
            StgCbNtwk.Content = PowerItemTree.NetworkRoot.FriendlyName;
        }

        /// <summary>
        /// Handles closing of the window. Initiates the SearchProviders saving
        /// </summary>
        private void WindowClosed(object sender, System.EventArgs e)
        {
            SettingsManager.SaveActiveSearchProviders();
        }

        /// <summary>
        /// Displays the OpenFileDialog with Title and Filter set.
        /// Filter allows images. If OFD is closed by OK, the returned 
        /// filename is being put into textbox.
        /// </summary>
        private void Browse(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog
                          {
                              Filter = Properties.Resources.Str_PicDialogFilter + @"|*.png;*.gif;*.jpe*;*.tif*",
                              Title = Properties.NoLoc.Stg_AppShortName + Properties.Resources.Str_PicDialogDescription
                          };
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                pictureBox.Text = ofd.FileName;
        }

        /// <summary>
        /// Handles text box doubleclick. Clears the box.
        /// </summary>
        private void Clear(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            pictureBox.Text = string.Empty;
        }

        /// <summary>
        /// Bindable propertyto disable the "Configure start button" checkbox.
        /// It is disabled on XP or below.
        /// </summary>
        public static bool NotXp
        {
            get { return Util.OsIs.SevenOrMore; }
        }
    }
}