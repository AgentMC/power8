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
    }
}
