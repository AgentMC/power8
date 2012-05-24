using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using Microsoft.Win32;
using Power8.Properties;

namespace Power8
{
    /// <summary>
    /// Interaction logic for UpdateNotifier.xaml
    /// </summary>
    public partial class UpdateNotifier
    {
        private readonly string _curVer, _newVer, _uri7z, _uriMsi;

        private UpdateNotifier()
        {
            InitializeComponent();
        }

        internal UpdateNotifier(string currentVer, string newVer, string uri7z, string uriMsi):this()
        {
            _curVer = currentVer;
            _newVer = newVer;
            _uri7z = uri7z;
            _uriMsi = uriMsi;
            Title = Properties.Resources.Stg_AppShortName + Properties.Resources.Str_UpdateAvailable;
        }

        private string CurrentVersion
        {
            get { return _curVer; }
        }

        private string NewVersion
        {
            get { return _newVer; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Settings.Default.IgnoreVer = NewVersion;
            Settings.Default.Save();
            Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Process.Start(Properties.Resources.Stg_Power8URI);
            Close();
        }

        private void InitDownload(string url, string where, Action successAction)
        {
            var wc = new WebClient();
            wc.DownloadFileCompleted += wc_DownloadFileCompleted;
            wc.DownloadProgressChanged += (sender, args) => Util.Send(() => progress.Value = args.ProgressPercentage);
            wc.DownloadFileAsync(new Uri(url), where, successAction);
            IsEnabled = false;
        }

        void wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Util.Send(() => IsEnabled = true);
            if (e.Cancelled)
            {
                MessageBox.Show("Download was cancelled." + (e.Error != null ? "\r\n" + e.Error.Message : ""),
                                Properties.Resources.Stg_AppShortName, 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
            }
            else
            {
                ((Action) e.UserState)();
                Util.Send(Close);
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            var obj = (sender == downMsi ? _uriMsi : _uri7z);
            var ofd = new SaveFileDialog
                          {
                              AddExtension = true,
                              DefaultExt = Path.GetExtension(obj),
                              FileName = "",
                              Title = Properties.Resources.Stg_AppShortName + ": choose where to download the file",
                              Filter = "Power8 update|*" + Path.GetExtension(obj),
                          };
            if (ofd.ShowDialog().Value)
                InitDownload(obj, ofd.FileName, () => Util.StartExplorerSelect(ofd.FileName));
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            var file = Environment.ExpandEnvironmentVariables("%temp%\\" + Path.GetFileName(_uriMsi));
            InitDownload(_uriMsi, file, () =>
                        {
                            Process.Start("msiexec.exe",
                                            string.Format("/i \"{0}\" APPDIR=\"{1}\"", file,
                                                        System.Windows.Forms.Application.StartupPath.Trim('"')));
                            Environment.Exit(0);
                        });
        }
    }
}
