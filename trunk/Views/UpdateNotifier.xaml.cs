using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using Microsoft.Win32;
using Power8.Properties;

namespace Power8.Views
{
    /// <summary>
    /// Window that is automatically shown when the update is available
    /// Inherits DisposabeWindow, so is implemented as lazy resource-friendly 
    /// singleton.
    /// </summary>
    public partial class UpdateNotifier:INotifyPropertyChanged
    {
        private readonly string _curVer, _newVer, _uri7Z, _uriMsi;
        public event PropertyChangedEventHandler PropertyChanged;
        //Constructor-----------------------------
        /// <summary>
        /// Needed for Designer
        /// </summary>
        public UpdateNotifier()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Main constructor of a form
        /// </summary>
        /// <param name="currentVer">String representation of curent application version</param>
        /// <param name="newVer">String representation of a version available for download</param>
        /// <param name="uri7Z">http:// URL (string) to z7 package</param>
        /// <param name="uriMsi">http:// URL (string) to msi package</param>
        internal UpdateNotifier(string currentVer, string newVer, string uri7Z, string uriMsi) : this()
        {
            _curVer = currentVer;
            _newVer = newVer;
            _uri7Z = uri7Z;
            _uriMsi = uriMsi;
            Title = NoLoc.Stg_AppShortName + Properties.Resources.Str_UpdateAvailable;
            PropertyChanged(this, new PropertyChangedEventArgs("CurrentVersion"));
            PropertyChanged(this, new PropertyChangedEventArgs("NewVersion"));
        }
        //Bindable properties---------------------
        /// <summary>
        /// Gets the current version passed to constructor
        /// </summary>
        public string CurrentVersion
        {
            get { return _curVer; }
        }
        /// <summary>
        /// Gets the available version passed to constructor
        /// </summary>
        public string NewVersion
        {
            get { return _newVer; }
        }
        //Handlers--------------------------------
        /// <summary>
        /// Closes the window
        /// </summary>
        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
        /// <summary>
        /// Handles Ignore this version button.
        /// Saves current version as ignorable, saves settings, and closes.
        /// </summary>
        private void IgnoreClick(object sender, RoutedEventArgs e)
        {
            Settings.Default.IgnoreVer = NewVersion;
            Settings.Default.Save();
            Close();
        }
        /// <summary>
        /// Handles both "Download 7z" and "Download MSI".
        /// Shows Save dialog, and if there's OK clicked there, 
        /// starts async download. After it is completed, the file 
        /// will be shown selected by Explorer.
        /// </summary>
        private void DownloadFileClick(object sender, RoutedEventArgs e)
        {
            var obj = (sender == UNdownMsi ? _uriMsi : _uri7Z);
            var ofd = new SaveFileDialog
                          {
                              AddExtension = true,
                              DefaultExt = Path.GetExtension(obj),
                              FileName = "",
                              Title = NoLoc.Stg_AppShortName + Properties.Resources.Str_SaveDialogDescription,
                              Filter = NoLoc.Stg_AppShortName 
                                       + Properties.Resources.Str_SaveDialogFilter + Path.GetExtension(obj),
                          };
            if (ofd.ShowDialog().Value)
                InitDownload(obj, ofd.FileName, () => Util.StartExplorerSelect(ofd.FileName));
        }
        /// <summary>
        /// The big and bold button handler. Downloads MSI to temporary location,
        /// then launches it passing current startup location as APPDIR var.
        /// Power8 will automatically exit when the installation is launched.
        /// </summary>
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
        /// <summary>
        /// Opens project website. Then closes the window.
        /// </summary>
        private void GoWebClick(object sender, RoutedEventArgs e)
        {
            Process.Start(NoLoc.Stg_Power8URI);
            Close();
        }
        //Download helpers------------------------
        /// <summary>
        /// Handles the event when download is completed or stopped in any other way.
        /// </summary>
        void WcDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Util.Send(() => IsEnabled = true); //Enable the UI. Since we're in some non-main thread, do this from main one.
            if (e.Cancelled) //i.e. the download wasn't successful:
            {
                MessageBox.Show(Properties.Resources.Err_DownloadCancelled + (e.Error != null ? "\r\n" + e.Error.Message : ""),
                                NoLoc.Stg_AppShortName, 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
            }
            else //the download succeeded
            {
                ((Action) e.UserState)(); //Invoke the passed action
                Util.Send(Close);         //Close the window from main thread
            }
        }
        /// <summary>
        /// Initializes the download process
        /// </summary>
        /// <param name="url">String URL what to download</param>
        /// <param name="where">String path where to download</param>
        /// <param name="successAction">Action lambda what to do after the 
        /// download is done. This it stored as "user token" in the async download 
        /// thread, so can be extracted later and executed by the completion 
        /// event handler. Be careful NOT to pass here any line working with the UI,
        /// since the handler will be called in background thread. Also keep in mind 
        /// that UI will be disabled during the download and will become enabled
        /// automatically when this is done, so you shouldn't bother about this.</param>
        private void InitDownload(string url, string where, Action successAction)
        {
            var wc = new WebClient();
            //Subscribe to two events. Both handler and event (method/lambda and webClient)
            //are owned by the window, so after it is disposed on close they can be garbaged out.
            wc.DownloadFileCompleted += WcDownloadFileCompleted;
            wc.DownloadProgressChanged += (sender, args) => Util.Send(() => progress.Value = args.ProgressPercentage);
            wc.DownloadFileAsync(new Uri(url), where, successAction); //passing action as token
            IsEnabled = false; //this is called from UI (Click starts it)
        }

    }
}
