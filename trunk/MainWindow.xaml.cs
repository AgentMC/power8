using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Power8.Properties;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.MessageBox;
using ThreadState = System.Threading.ThreadState;


namespace Power8
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static bool ClosedW;

        private bool _watch, _update;
        private IntPtr _taskBar, _showDesktopBtn;
        private Thread _updateThread;

        #region Window (de)init 

        public MainWindow()
        {
            InitializeComponent();
            menu.DataContext = this;

            Application.SetCompatibleTextRenderingDefault(true);
            Application.EnableVisualStyles();

            App.Current.SessionEnding += (sender, args) => Close();
            BtnStck.Instance.RunCalled += ShowRunDialog;

            if (CheckForUpdatesEnabled)
                UpdateCheckThreadInit();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;
            if (msg == (uint)API.WM.HOTKEY)
            {
                handled = true;
                if (BtnStck.Instance.IsActive)
                    b1.Focus();
                else
                    ShowButtonStack(this, null);
            }
            return IntPtr.Zero;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            _taskBar = API.FindWindow(API.TRAY_WND_CLASS, null);
            CheckWnd(_taskBar, API.TRAY_WND_CLASS);
            if (Environment.OSVersion.Version.Major >= 6)
            {
                _showDesktopBtn = API.FindWindowEx(_taskBar, IntPtr.Zero, API.TRAY_NTF_WND_CLASS, null);
                CheckWnd(_showDesktopBtn, API.TRAY_NTF_WND_CLASS);
                _showDesktopBtn = API.FindWindowEx(_showDesktopBtn, IntPtr.Zero, API.SH_DSKTP_WND_CLASS, null);
                CheckWnd(_showDesktopBtn, API.SH_DSKTP_WND_CLASS);
            }
            else
            {
                _showDesktopBtn = API.FindWindowEx(_taskBar, IntPtr.Zero, API.SH_DSKTP_START_CLASS, null);
                CheckWnd(_showDesktopBtn, API.SH_DSKTP_START_CLASS);
            }

            Left = 0;
            Top = 0;
            _watch = true;
            new Thread(WatchDesktopBtn){Name = "ShowDesktop button watcher"}.Start();

            API.SetParent(this.MakeGlassWpfWindow(), _taskBar);

            API.RegisterHotKey(this.GetHandle(), 0, API.fsModifiers.MOD_ALT, Keys.Z);
            this.RegisterHook(WndProc);
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            ClosedW = true;
            _watch = false;
            if (BtnStck.IsInitDone)
                BtnStck.Instance.Close();
        }

        #endregion

        #region Handlers
        private void ShowButtonStack(object sender, RoutedEventArgs e)
        {
            if (Keyboard.GetKeyStates(Key.LeftCtrl) == KeyStates.Down || Keyboard.GetKeyStates(Key.RightCtrl) == KeyStates.Down)
            {
                ShowRunDialog(this, null);
                return;
            }
            BtnStck.Instance.Show();
            var screenPoint = PointToScreen(Mouse.GetPosition(this));
            var screen = Screen.FromPoint(new System.Drawing.Point((int) screenPoint.X, (int) screenPoint.Y));
            bool isHideTaskBarOptionOn = screen.WorkingArea.Width == screen.Bounds.Width &&
                                         screen.WorkingArea.Height == screen.Bounds.Height;
// ReSharper disable PossibleLossOfFraction
                    //taskbar is vertical @ left or horizontal
            if ((isHideTaskBarOptionOn && screenPoint.X <= screen.WorkingArea.X + screen.WorkingArea.Width / 2) 
                || screen.WorkingArea.X > screen.Bounds.X 
                || (screen.WorkingArea.Width == screen.Bounds.Width & !isHideTaskBarOptionOn))
                screenPoint.X = screen.WorkingArea.X;
            else    //vertical @ right
                screenPoint.X = screen.WorkingArea.Width + screen.WorkingArea.X - BtnStck.Instance.Width;
                    //taskbar is horizontal @ top or vertical
            if ((isHideTaskBarOptionOn && screenPoint.Y <= screen.WorkingArea.Y + screen.WorkingArea.Height / 2) 
                || screen.WorkingArea.Y > screen.Bounds.Y 
                || (screen.WorkingArea.Height == screen.Bounds.Height & !isHideTaskBarOptionOn))
                screenPoint.Y = screen.WorkingArea.Y;
            else    //horizontal @ bottom
                screenPoint.Y = screen.WorkingArea.Height + screen.WorkingArea.Y - BtnStck.Instance.Height;
// ReSharper restore PossibleLossOfFraction
            BtnStck.Instance.Left = screenPoint.X;
            BtnStck.Instance.Top = screenPoint.Y;
            BtnStck.Instance.Activate();
            BtnStck.Instance.Focus();
        }

        private void ShowRunDialog(object sender, EventArgs e)
        {
            API.SHRunDialog(_taskBar, IntPtr.Zero, null, null, null, API.RFF.NORMAL);
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
        #endregion

        #region Background threads
        private void WatchDesktopBtn()
        {
            double width = -1, height = -1;

            while (_watch)
            {
                API.RECT r;
                if (!API.GetWindowRect(_showDesktopBtn, out r))
                {//looks like explorer.exe is dead!
                    Thread.Sleep(10000);//let's wait for explorer to auto-restart
                    var explorers = Process.GetProcessesByName("explorer");
                    var ses = Process.GetCurrentProcess().SessionId;
                    if (explorers.Any(e => e.SessionId == ses))
                    {//explorer is restarted already?
                        Util.Restart("explorer.exe restarted.");//need reinit handles tree
                    }
                    else
                    {//seems, user killed explorer so hard it is dead to death :)
                        var trd  = new Thread(() =>
                        {
                            var dialog = new RestartExplorer();
                            dialog.ShowDialog();
                            if (dialog.DialogResult == System.Windows.Forms.DialogResult.OK)
                            {
                                Process.Start("explorer.exe");
                                Thread.Sleep(2000);
                                Util.Restart("user have chosen to restart.");
                            }
                            else
                                Util.Die("no user-action was to restore normal workflow...");
                        });
                        trd.SetApartmentState(ApartmentState.STA);
                        trd.Start();
                        trd.Join();
                    }
                }
                var curHeight = r.Bottom - r.Top;
                var curWidth = r.Right - r.Left;
// ReSharper disable CompareOfFloatsByEqualityOperator
                if (width != curWidth)
                {
                    width = curWidth;
                    Dispatcher.Invoke(new Action(() =>  b1.Width = curWidth));
                }
                if (height != curHeight)
                {
                    height = curHeight;
                    Dispatcher.Invoke(new Action(() =>  b1.Height = curHeight));
                }
// ReSharper restore CompareOfFloatsByEqualityOperator
                Thread.Sleep(100);
            }
        }

        private void UpdateCheckThread()
        {
            int cycles = 0;
            var client = new WebClient();
            _update = true;
            while (_update)
            {
                if (cycles == 0)
                {
                    var info =
                        new System.IO.StringReader(
                            client.DownloadString(Properties.Resources.Power8URI + Properties.Resources.AssemblyInfoURI));
                    string line;
                    while ((line = info.ReadLine()) != null)
                    {
                        if (line.StartsWith("[assembly: AssemblyVersion("))
                        {
                            var verLine = line.Substring(28).TrimEnd(new[] {']', ')', '"'});
                            if (new Version(verLine) > new Version(Application.ProductVersion) && Settings.Default.IgnoreVer != verLine)
                            {
                                switch (MessageBox.Show(string.Format(
                                            Properties.Resources.UpdateAvailableFormat, Application.ProductVersion, verLine),
                                        Properties.Resources.AppShortName + Properties.Resources.UpdateAvailable,
                                        MessageBoxButton.YesNoCancel, MessageBoxImage.Information))
                                {
                                    case MessageBoxResult.Cancel:
                                        Settings.Default.IgnoreVer = verLine;
                                        Settings.Default.Save();
                                        break;
                                    case MessageBoxResult.Yes:
                                        Process.Start(Properties.Resources.Power8URI);
                                        break;
                                }
                            }
                            break;
                        }
                    }
                }
                Thread.Sleep(1000);
                cycles++;
                cycles %= 43200;
            }
        }
        #endregion

        #region Bindable props
        public bool AutoStartEnabled
        {
            get
            {
                var k = Microsoft.Win32.Registry.CurrentUser;
                k = k.OpenSubKey(Properties.Resources.RegKeyRun, false);
                return k != null && k.GetValue(Properties.Resources.AppShortName) != null;
            }
            set
            {
                if (value == AutoStartEnabled)
                    return;
                var k = Microsoft.Win32.Registry.CurrentUser;
                k = k.OpenSubKey(Properties.Resources.RegKeyRun, true);
                if (k == null) 
                    return;
                if (value)
                    k.SetValue(Properties.Resources.AppShortName, Application.ExecutablePath);
                else
                    k.DeleteValue(Properties.Resources.AppShortName);
            }
        }

        public bool CheckForUpdatesEnabled
        {
            get
            {
                return Settings.Default.CheckForUpdates;
            }
            set
            {
                if (value == CheckForUpdatesEnabled)
                    return;
                Settings.Default.CheckForUpdates = value;
                Settings.Default.Save();
                if (value)
                    UpdateCheckThreadInit();
                else
                    _update = false;
            }
        }
        #endregion

        #region Helpers
// ReSharper disable UnusedParameter.Local
        private static void CheckWnd(IntPtr wnd, string className)
        {
            if (wnd == IntPtr.Zero)
                Util.Die(className + " not found");
        }
// ReSharper restore UnusedParameter.Local
        
        private void UpdateCheckThreadInit()
        {
            if(_updateThread == null || _updateThread.ThreadState == ThreadState.Stopped)
                _updateThread = new Thread(UpdateCheckThread)
                {
                    IsBackground = true,
                    Name = "Update thread"
                };
            _updateThread.Start();
        }
        #endregion
    }
}
