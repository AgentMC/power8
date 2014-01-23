using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Windows;
using System.Xml.Linq;
using Power8.Properties;
using Power8.Views;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.MessageBox;

namespace Power8.Helpers
{
    public class SettingsManager : INotifyPropertyChanged
    {
        private SettingsManager(){}

        #region Singletoning

        private static  SettingsManager _inst;
        private static readonly object Sync = new object();
        public static SettingsManager Instance
        {
            get
            {
                lock (Sync)
                {
                    return _inst ?? (_inst = new SettingsManager());
                }
            }
        } //needed to be passedd as Data Context

        #endregion

        #region Static vars, events and invokators

        public static readonly EventWaitHandle BgrThreadLock = new EventWaitHandle(false, EventResetMode.ManualReset);

        public static event EventHandler WatchRemovablesChanged;
        public static event EventHandler WarnMayHaveChanged;
        public static event EventHandler ImageChanged;
        public static event EventHandler ControlPanelByCategoryChanged;
        public static event EventHandler DynamicLayoutChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        private static readonly System.Windows.Forms.Screen Screen = System.Windows.Forms.Screen.PrimaryScreen;
        
        private static bool _blockMetro, _update;
        private static Thread _blockMetroThread, _updateThread;

        //---------------------------
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Static public methods

        public static void Init()
        {
            if (Instance.CheckForUpdatesEnabled)
                UpdateCheckThreadInit();
            if (Instance.BlockMetroEnabled && Util.OsIs.EightOrMore)
                BlockMetroThreadInit(); 
        }

        public static Single GetARModifier(bool taskbarIsHorizontal)
        {
            Single s;
            switch (Instance.ArSelectedIndex)
            {
                case 0:
                    s = (Single) Screen.Bounds.Width/Screen.Bounds.Height;
                    break;
                case 1:
                    s = 1;
                    break;
                case 2:
                    s = 4.0f/3;
                    break;
                case 3:
                    s = 16f/9;
                    break;
                default:
                    s = 16f/10;
                    break;
            }
            if (taskbarIsHorizontal && Instance.ArFollowsTaskbar)
                s = 1/s;
            return s;
        }

        
/*<P8SearchProviders>
    <Provider key="b">http://www.bing.com/search?q={0}</Provider>
  </P8SearchProviders>*/
        private const string SP_ROOT = "P8SearchProviders";
        private const string SP_NODE = "Provider";
        private const string SP_KEYY = "key";
        public static void SaveActiveSearchProviders()
        {
            var provs = Instance.WebSearchProviders;
            for (var i = provs.Count - 1; i >= 0; i--)
                if(provs[i].Key == Char.MinValue || string.IsNullOrWhiteSpace(provs[i].Query))
                    provs.RemoveAt(i);
            var d = new XDocument(new XElement(SP_ROOT));
// ReSharper disable PossibleNullReferenceException
            foreach (var searchProvider in provs)
                d.Root.Add(new XElement(SP_NODE, new XAttribute(SP_KEYY, searchProvider.Key) , searchProvider.Query));
// ReSharper restore PossibleNullReferenceException
            Settings.Default.SearchProviders = d.ToString();
            Settings.Default.Save();
        }

        #endregion

        #region Background threads

        private static void UpdateCheckThreadInit()
        {
            Util.BgrThreadInit(ref _updateThread, UpdateCheckThread, "Update thread");
        }

        private static void UpdateCheckThread()
        {
            BgrThreadLock.WaitOne(); //wait until main window is instantiated
            
            int failcount = 0;
            DateTime scheduled = DateTime.Now; //check immediately after the thread was started
            var client = new WebClient();
            _update = true;
            while (_update && !MainWindow.ClosedW)
            {
                if (DateTime.Now >= scheduled)
                {
                    try
                    {//parsing
                        var info =
                            new System.IO.StringReader(
                                client.DownloadString(NoLoc.Stg_Power8URI + NoLoc.Stg_AssemblyInfoURI));
                        string line, verLine = null, uri7Z = null, uriMsi = null;
                        while ((line = info.ReadLine()) != null)
                        {
                            if (line.StartsWith("[assembly: AssemblyVersion("))
                                verLine = line.Substring(28).TrimEnd(new[] { ']', ')', '"' });
                            else if (line.StartsWith(@"//7zuri="))
                                uri7Z = line.Substring(8);
                            else if (line.StartsWith(@"//msuri="))
                                uriMsi = line.Substring(8);
                        }
                        if (verLine != null)
                        {//updating?
                            if (new Version(verLine) > new Version(Application.ProductVersion) && Settings.Default.IgnoreVer != verLine)
                            {//updating!
                                if (uri7Z == null || uriMsi == null) //old approach
                                {
                                    switch (MessageBox.Show(Resources.CR_UNUpdateAvailableLong + string.Format(
                                                Resources.Str_UpdateAvailableFormat, Application.ProductVersion, verLine),
                                            NoLoc.Stg_AppShortName + Resources.Str_UpdateAvailable,
                                            MessageBoxButton.YesNoCancel, MessageBoxImage.Information))
                                    {
                                        case MessageBoxResult.Cancel:
                                            Settings.Default.IgnoreVer = verLine;
                                            Settings.Default.Save();
                                            break;
                                        case MessageBoxResult.Yes:
                                            Process.Start(NoLoc.Stg_Power8URI);
                                            break;
                                    }
                                }
                                else
                                {
                                    Util.Send(() =>
                                              Util.InstanciateClass(
                                                  t: typeof(UpdateNotifier),
                                                  ctor: () => new UpdateNotifier(
                                                                  Application.ProductVersion, verLine,
                                                                  uri7Z,
                                                                  uriMsi)));
                                }
                            }

                        }
                        scheduled = DateTime.Now.AddHours(12); //check in 12 hours again
                        failcount = 0;
                        Instance.ShowUpdateWarn = false;
                    }
                    catch 
#if DEBUG
                           (Exception ex)
#endif
                    {
                        
                        failcount++;
#if DEBUG
                        Log.Fmt("Can't check for updates (chance:{0}):{1}", failcount, ex.Message);
#endif
                        switch (failcount) //Checks 3 times during 1.5 hours, then gives up
                        {
                            case 1:
                                scheduled = DateTime.Now.AddMinutes(5);
                                break;
                            case 2:
                                scheduled = DateTime.Now.AddMinutes(30);
                                break;
                            case 3:
                                scheduled = DateTime.Now.AddMinutes(55);
                                break;
                            default:
                                failcount = 0; //forget it, recheck in 12 hours
                                scheduled = DateTime.Now.AddHours(12);
                                Instance.ShowUpdateWarn = true;
                                break;
                        }
                    }
                }
                Thread.Sleep(1500);
            }
        }

        private static void BlockMetroThreadInit()
        {
            Util.BgrThreadInit(ref _blockMetroThread, BlockMetroThread, "Block Metro thread");
        }

        private static void BlockMetroThread()
        {
            BgrThreadLock.WaitOne(); //wait until main window is instantiated

            //search for all metro windows (9 on RP)
            var handles = new Dictionary<IntPtr, API.RECT>();
            IntPtr last = IntPtr.Zero, desk = API.GetDesktopWindow();
            do
            {
                var current = API.FindWindowEx(desk, last, API.WndIds.METRO_EDGE_WND, null);
                if (current != IntPtr.Zero && !handles.ContainsKey(current))
                {
                    API.RECT r;
                    API.GetWindowRect(current, out r);
                    handles.Add(current, r);
                    last = current;
                }
                else
                {
                    last = IntPtr.Zero;
                }
            } while (last != IntPtr.Zero);

            _blockMetro = true;
            while (_blockMetro && !MainWindow.ClosedW) //MAIN CYCLE
            {
                foreach (var wnd in handles)
                    API.MoveWindow(wnd.Key, wnd.Value.Left, wnd.Value.Top, 0, 0, false);
                Thread.Sleep(1000);
            }

            //deinit - restore all window rects
            foreach (var wnd in handles)
                API.MoveWindow(wnd.Key, wnd.Value.Left, wnd.Value.Top,
                               wnd.Value.Right - wnd.Value.Left,
                               wnd.Value.Bottom - wnd.Value.Top, true);
            BgrThreadLock.Set(); //now Main window may close 
            //(it will Reset() the lock in case this thread runs)
        }

        #endregion

        #region Private props

        private bool _showUpdateWarn;
        private bool ShowUpdateWarn 
        {
            get { return _showUpdateWarn; }
            set 
            {
                _showUpdateWarn = value;
                WarnMayHaveChanged(this, null);
            } 
        }

        private bool ShowSettingsWarn
        {
            get { return ReportBadSettings && (!AutoStartEnabled || !CheckForUpdatesEnabled); }
        }

        #endregion

        #region Public instance bindable properties

        public bool AutoStartEnabled
        {
            get
            {
                var k = Microsoft.Win32.Registry.CurrentUser;
                k = k.OpenSubKey(NoLoc.Stg_RegKeyRun, false);
                return k != null &&
                       string.Equals((string)k.GetValue(NoLoc.Stg_AppShortName),
                                     Application.ExecutablePath,
                                     StringComparison.InvariantCultureIgnoreCase);
            }
            set
            {
                if (value == AutoStartEnabled)
                    return;
                var k = Microsoft.Win32.Registry.CurrentUser;
                k = k.OpenSubKey(NoLoc.Stg_RegKeyRun, true);
                if (k == null)
                    return;
                if (value)
                    k.SetValue(NoLoc.Stg_AppShortName, Application.ExecutablePath);
                else
                    k.DeleteValue(NoLoc.Stg_AppShortName);
                WarnMayHaveChanged(this, null);
            }
        }

        public bool BlockMetroEnabled
        {
            get { return Settings.Default.BlockMetro; }
            set
            {
                if (value == BlockMetroEnabled)
                    return;
                Settings.Default.BlockMetro = value;
                Settings.Default.Save();
                if (value)
                    BlockMetroThreadInit();
                else
                    _blockMetro = false;
            }
        }

        public bool CheckForUpdatesEnabled
        {
            get { return Settings.Default.CheckForUpdates; }
            set
            {
                if (value == CheckForUpdatesEnabled)
                    return;
                Settings.Default.CheckForUpdates = value;
                Settings.Default.Save();
                ShowUpdateWarn = false;
                if (value)
                    UpdateCheckThreadInit();
                else
                    _update = false;
            }
        }
        
        public bool SquareStartButton
        {
            get { return Settings.Default.SquareButton; }
            set
            {
                if(value == SquareStartButton)
                    return;
                Settings.Default.SquareButton = value;
                Settings.Default.Save();
                ImageChanged(this, null);
            }
        }

        public bool ArFollowsTaskbar
        {
            get { return Settings.Default.ArFollowsTaskbar; }
            set
            {
                if (value == ArFollowsTaskbar)
                    return;
                Settings.Default.ArFollowsTaskbar = value;
                Settings.Default.Save();
            }
        }

        public int ArSelectedIndex
        {
            get { return Settings.Default.ArIndex; }
            set
            {
                if (value == ArSelectedIndex)
                    return;
                Settings.Default.ArIndex = value;
                Settings.Default.Save();
            }
        }

        public bool WatchRemovableDrives
        {
            get { return Settings.Default.WatchRemovables; }
            set
            {
                if (value == WatchRemovableDrives)
                    return;
                Settings.Default.WatchRemovables = value;
                Settings.Default.Save();
                WatchRemovablesChanged(this, null);
            }
        }
        
        public bool ReportBadSettings
        {
            get { return Settings.Default.WarnBadConfig; }
            set
            {
                if (value == ReportBadSettings)
                    return;
                Settings.Default.WarnBadConfig = value;
                Settings.Default.Save();
                WarnMayHaveChanged(this, null);
            }
        }

        public bool ShowDonateMenuItem
        {
            get { return Settings.Default.ShowDonateMenuItem; }
            set
            {
                if (value == ShowDonateMenuItem)
                    return;
                Settings.Default.ShowDonateMenuItem = value;
                Settings.Default.Save();
                OnPropertyChanged("ShowDonateMenuItem");
            }
        }

        public bool ShowWarn
        {
            get { return ShowSettingsWarn || ShowUpdateWarn; }
        }

        public string WarnText
        {
            get
            {
                return
                    (ShowSettingsWarn ? "\r\n" + Resources.Str_TipWarn : string.Empty) + 
                    (ShowUpdateWarn ? "\r\n" + Resources.Err_CantCheckUpdates : string.Empty);
            }
        }

        public string ImageString
        {
            get { return Settings.Default.StartImage; }
            set
            {
                if(value == ImageString)
                    return;
                Settings.Default.StartImage = value;
                Settings.Default.Save();
                ImageChanged(this, null);
            }
        }

        public bool ShowMbComputer
        {
            get { return Settings.Default.ShowMbComputer; }
            set
            {
                if (value == ShowMbComputer)
                    return;
                Settings.Default.ShowMbComputer = value;
                Settings.Default.Save();
                OnPropertyChanged("ShowMbComputer");
            }
        }

        public bool ShowMbDocs
        {
            get { return Settings.Default.ShowMbDocs; }
            set
            {
                if (value == ShowMbDocs)
                    return;
                Settings.Default.ShowMbDocs = value;
                Settings.Default.Save();
                OnPropertyChanged("ShowMbDocs");
            }
        }

        public bool ShowMbCpl
        {
            get { return Settings.Default.ShowMbCpl; }
            set
            {
                if (value == ShowMbCpl)
                    return;
                Settings.Default.ShowMbCpl = value;
                Settings.Default.Save();
                OnPropertyChanged("ShowMbCpl");
            }
        }

        public bool ShowMbAdminTools
        {
            get { return Settings.Default.ShowMbAdminTools; }
            set
            {
                if (value == ShowMbAdminTools)
                    return;
                Settings.Default.ShowMbAdminTools = value;
                Settings.Default.Save();
                OnPropertyChanged("ShowMbAdminTools");
            }
        }

        public bool ShowMbNet
        {
            get { return Settings.Default.ShowMbNet; }
            set
            {
                if (value == ShowMbNet)
                    return;
                Settings.Default.ShowMbNet = value;
                Settings.Default.Save();
                OnPropertyChanged("ShowMbNet");
            }
        }

        public bool ShowMbCtrlByCat
        {
            get { return Settings.Default.ShowMbCtrlByCat; }
            set
            {
                if (value == ShowMbCtrlByCat)
                    return;
                Settings.Default.ShowMbCtrlByCat = value;
                Settings.Default.Save();
                ControlPanelByCategoryChanged(this, null);
            }
        }

        public bool AutoSortTrees
        {
            get { return Settings.Default.AutoSortTrees; }
            set
            {
                if (value == AutoSortTrees)
                    return;
                Settings.Default.AutoSortTrees = value;
                Settings.Default.Save();
            }
        }

        public string StartMenuText
        {
            get
            {
                var s = Settings.Default.StartMenuText;
                return string.IsNullOrWhiteSpace(s) ? null : s;
            }
            set
            {
                if (value == StartMenuText)
                    return;
                Settings.Default.StartMenuText = value;
                Settings.Default.Save();
                if(!string.IsNullOrWhiteSpace(value))
                    PowerItemTree.StartMenuRoot[0].FriendlyName = value;
                else
                    PowerItemTree.StartMenuRoot[0].Update();
            }
        }

        private List<SearchProvider> _searchData;
        public List<SearchProvider> WebSearchProviders
        {
            get
            {
                if (_searchData == null)
                {
                    _searchData = new List<SearchProvider>();
// ReSharper disable PossibleNullReferenceException
                    foreach (var provider in XDocument.Parse(Settings.Default.SearchProviders).Root.Elements(SP_NODE))
                        _searchData.Add(new SearchProvider {Key = provider.Attribute(SP_KEYY).Value[0], Query = provider.Value});
// ReSharper restore PossibleNullReferenceException
                }
                return _searchData;
            }
        }

        public bool AutoRestart
        {
            get { return Settings.Default.AutoRestart; }
            set
            {
                if (value == AutoRestart)
                    return;
                Settings.Default.AutoRestart = value;
                Settings.Default.Save();
            }
        }

        public bool MfuIsSystem
        {
            get { return Settings.Default.MfuFromUserAssist; }
            set
            {
                if (value == MfuIsSystem)
                    return;
                Settings.Default.MfuFromUserAssist = value;
                Settings.Default.Save();
            }
        }

        public bool MfuIsInternal
        {
            get { return Settings.Default.MfuFromP8JL; }
            set
            {
                if (value == MfuIsInternal)
                    return;
                Settings.Default.MfuFromP8JL = value;
                Settings.Default.Save();
            }
        }

        public bool MfuIsCustom
        {
            get { return Settings.Default.MfuFromCustomData; }
            set
            {
                if (value == MfuIsCustom)
                    return;
                Settings.Default.MfuFromCustomData = value;
                Settings.Default.Save();
            }
        }

        public ObservableCollection<MfuList.StringWrapper> MfuInternalExclusions
        {
            get { return MfuList.ExclList; }
        }

        public bool TryFpReset
        {
            get { return Settings.Default.TryFpResetBeforeUiCtors; }
            set
            {
                if (value == Settings.Default.TryFpResetBeforeUiCtors)
                    return;
                Settings.Default.TryFpResetBeforeUiCtors = value;
                Settings.Default.Save();
            }
        }

        public bool DynamicLayout
        {
            get { return Settings.Default.DynamicLayout; }
            set
            {
                if(value == Settings.Default.DynamicLayout)
                    return;
                Settings.Default.DynamicLayout = value;
                Settings.Default.Save();
                DynamicLayoutChanged(this, null);
            }
        }

        public bool DontFreeLibs
        {
            get { return Settings.Default.DoNotFreeLibraries; }
            set
            {
                if (value == Settings.Default.DoNotFreeLibraries)
                    return;
                Settings.Default.DoNotFreeLibraries = value;
                Settings.Default.Save();
            }
        }

        #endregion
    }

    public class SearchProvider
    {
        public const string INITIAL_URI = @"http://test.org?query=",
                            FORMATTER = @"{0}";
        private string _q = INITIAL_URI + FORMATTER;
        private char _k = '?';

        public char Key
        {
            get { return _k; }
            set { _k = Char.ToLower(value); }
        }

        public string Query { 
            get { return _q; } 
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    value = INITIAL_URI;
                if (!value.Contains(FORMATTER))
                    value += FORMATTER;
                _q = value;
            }
        }
    }
}
