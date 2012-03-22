using System.Threading;
using System.Windows;
using System;

namespace Power8
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public App()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                new Thread(PowerItemTree.InitTree).Start();
                return;
            }
            MessageBox.Show("Launched under XP or below won't work", "Power8", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(-1);
        }
    }
}
