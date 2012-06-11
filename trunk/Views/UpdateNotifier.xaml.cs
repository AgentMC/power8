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
        private readonly string _curVer, _newVer, _uri7Z, _uriMsi;

        private UpdateNotifier()
        {
            InitializeComponent();
        }

        internal UpdateNotifier(string currentVer, string newVer, string uri7Z, string uriMsi)
        {
            _curVer = currentVer;
            _newVer = newVer;
            _uri7Z = uri7Z;
            _uriMsi = uriMsi;
            InitializeComponent();
            Title = Properties.Resources.Stg_AppShortName + Properties.Resources.Str_UpdateAvailable;
        }

        public string CurrentVersion
        {
            get { return _curVer; }
        }

        public string NewVersion
        {
            get { return _newVer; }
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void IgnoreClick(object sender, RoutedEventArgs e)
        {
            Settings.Default.IgnoreVer = NewVersion;
            Settings.Default.Save();
            Close();
        }

        private void GoWebClick(object sender, RoutedEventArgs e)
        {
            Process.Start(Properties.Resources.Stg_Power8URI);
            Close();
        }

        private void InitDownload(string url, string where, Action successAction)
        {
            var wc = new WebClient();
            wc.DownloadFileCompleted += WcDownloadFileCompleted;
            wc.DownloadProgressChanged += (sender, args) => Util.Send(() => progress.Value = args.ProgressPercentage);
            wc.DownloadFileAsync(new Uri(url), where, successAction);
            IsEnabled = false;
        }

        void WcDownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Util.Send(() => IsEnabled = true);
            if (e.Cancelled)
            {
                MessageBox.Show(Properties.Resources.Err_DownloadCancelled + (e.Error != null ? "\r\n" + e.Error.Message : ""),
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

        private void DownloadFileClick(object sender, RoutedEventArgs e)
        {
            var obj = (sender == UNdownMsi ? _uriMsi : _uri7Z);
            var ofd = new SaveFileDialog
                          {
                              AddExtension = true,
                              DefaultExt = Path.GetExtension(obj),
                              FileName = "",
                              Title = Properties.Resources.Stg_AppShortName + Properties.Resources.Str_SaveDialogDescription,
                              Filter = Properties.Resources.Stg_AppShortName 
                                       + Properties.Resources.Str_SaveDialogFilter + Path.GetExtension(obj),
                          };
            if (ofd.ShowDialog().Value)
                InitDownload(obj, ofd.FileName, () => Util.StartExplorerSelect(ofd.FileName));
        }

        private void DownAndUpClick(object sender, RoutedEventArgs e)
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
