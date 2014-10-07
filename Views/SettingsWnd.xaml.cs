using System.Windows;
using Power8.Converters;
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
        /// and hides BlockMetro box if not Win8. Localizes UI partially.
        /// </summary>
        public SettingsWnd()
        {
            Util.FpReset();
            InitializeComponent();
            DataContext = SettingsManager.Instance;
            if(!Util.OsIs.EightFamily)
                MWBlockMetro.Visibility = Visibility.Collapsed;
            if (!Util.OsIs.SevenOrMore)
                StgCbCtrl.IsEnabled = false; //No Category view for CPL for XP; overrides binding.
            //The following is needed to protect users migrating 8==>10. They won't be able to change 
            //the setting, and in case we don't clear it - the button will be malformed.
            if (Util.OsIs.TresholdOrMore) SettingsManager.Instance.SquareStartButton = false;
            //System-wide localization
            StgCbAdmT.Content = PowerItemTree.AdminToolsRoot.FriendlyName;
            StgCbComp.Content = PowerItemTree.MyComputerRoot.FriendlyName;
            StgCbCpnl.Content = PowerItemTree.ControlPanelRoot.FriendlyName;
            StgCbDocs.Content = PowerItemTree.LibrariesRoot.FriendlyName;
            StgCbNtwk.Content = PowerItemTree.NetworkRoot.FriendlyName;
            //localization for headers (columns don't have names)
            StgWseColKey.Header = NameToResourceConverter.Instance.Convert("StgWseColKey", null, null, null);
            StgWseColVal.Header = NameToResourceConverter.Instance.Convert("StgWseColVal", null, null, null);
            StgMfuColExcl.Header = NameToResourceConverter.Instance.Convert("StgMfuColExcl", null, null, null);
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
        /// It is disabled on XP or below + on W10 for now.
        /// </summary>
        public static bool NotXp
        {
            get { return Util.OsIs.SevenOrMore && !Util.OsIs.TresholdOrMore; }
        }
    }
}
