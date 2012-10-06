using System.Windows;
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
        }

        /// <summary>
        /// Closes the window. Settings dalog is done almost in Mac style,
        /// so your settings are applied immediately if it's possible at all.
        /// And you're not required to press "Apply" or "OK" to achieve this.
        /// </summary>
        private void OkClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Displays the OpenFileDialog with Title and Filter set.
        /// Filter allows images. If OFD is closed by OK, the returned 
        /// filename is being put into textbox.
        /// </summary>
        private void Browse(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = Properties.Resources.Str_PicDialogFilter + @"|*.png;*.gif;*.jpe*;*.tif*";
            ofd.Title = Properties.NoLoc.Stg_AppShortName + Properties.Resources.Str_PicDialogDescription;
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
