using System.Windows;
using Power8.Helpers;

namespace Power8.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWnd
    {
        public SettingsWnd()
        {
            InitializeComponent();
            DataContext = SettingsManager.Instance;
            if(!Util.OsIs.EightOrMore)
                MWBlockMetro.Visibility = Visibility.Collapsed;
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static bool NotXp
        {
            get { return Util.OsIs.SevenOrMore; }
        }

        private void Browse(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog(); //TODO: Localize!!!
            ofd.Filter = "Images|*.png;*.gif;*.jpe*;*.tif*";
            ofd.Title = "Choose picture";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                pictureBox.Text = ofd.FileName;
        }

        private void Clear(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            pictureBox.Text = string.Empty;
        }
    }
}
