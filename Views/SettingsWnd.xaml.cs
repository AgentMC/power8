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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "Images|*.png";
            ofd.Title = "Open picture";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                pictureBox.Text = ofd.FileName;
        }
    }
}
