using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
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
        private const string TRAY_WND_CLASS = "Shell_TrayWnd";
        private const string TRAY_NTF_WND_CLASS = "TrayNotifyWnd";
        private const string SH_DSKTP_WND_CLASS = "TrayShowDesktopButtonWClass";

        private bool _watch, _update;
        private IntPtr _taskBar, _showDesktopBtn;
        private Thread _updateThread;

        #region Window (de)init 
        public MainWindow()
        {
            InitializeComponent();
            menu.DataContext = this;
            if (CheckForUpdatesEnabled)
                UpdateCheckThreadInit();
        }
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            _taskBar = API.FindWindow(TRAY_WND_CLASS, null);
            CheckWnd(_taskBar, TRAY_WND_CLASS);
            _showDesktopBtn = API.FindWindowEx(_taskBar, IntPtr.Zero, TRAY_NTF_WND_CLASS, null);
            CheckWnd(_showDesktopBtn, TRAY_NTF_WND_CLASS);
            _showDesktopBtn = API.FindWindowEx(_showDesktopBtn, IntPtr.Zero, SH_DSKTP_WND_CLASS, null);
            CheckWnd(_showDesktopBtn, SH_DSKTP_WND_CLASS);

            Left = 0;
            Top = 0;
            _watch = true;
            new Thread(WatchDesktopBtn){Name = "ShowDesktop button watcher"}.Start();

            var hlpr = new WindowInteropHelper(this);
            HwndSource.FromHwnd(hlpr.Handle).CompositionTarget.BackgroundColor = Colors.Transparent;
            API.MakeGlass(hlpr.Handle);
            API.SetParent(hlpr.Handle, _taskBar);
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            ClosedW = true;
            _watch = false;
            BtnStck.Instance.Close();
        }
        #endregion

        #region Handlers
        private void ShowButtonStack(object sender, RoutedEventArgs e)
        {
            BtnStck.Instance.Show();
            var screenPoint = PointToScreen(Mouse.GetPosition(this));
            var screen = Screen.FromPoint(new System.Drawing.Point((int) screenPoint.X, (int) screenPoint.Y));

                //vertical @ left or horizontal
            if (screen.WorkingArea.X > screen.Bounds.X || screen.WorkingArea.Width == screen.Bounds.Width)
                screenPoint.X = screen.WorkingArea.X;
            else                                                                                            //vertical @ right
                screenPoint.X = screen.WorkingArea.Width + screen.WorkingArea.X - BtnStck.Instance.Width;
                //horizontal @ top or vertical
            if (screen.WorkingArea.Y > screen.Bounds.Y || screen.WorkingArea.Height == screen.Bounds.Height)
                screenPoint.Y = screen.WorkingArea.Y;
            else                                                                                            //horizontal @ bottom
                screenPoint.Y = screen.WorkingArea.Height + screen.WorkingArea.Y - BtnStck.Instance.Height;

            if (screenPoint.X + BtnStck.Instance.Width > screen.Bounds.Width + screen.Bounds.Left)
                screenPoint.X -= BtnStck.Instance.Width;
            if (screenPoint.Y + BtnStck.Instance.Height > screen.Bounds.Height + screen.Bounds.Top)
                screenPoint.Y -= BtnStck.Instance.Height;

            BtnStck.Instance.Left = screenPoint.X;
            BtnStck.Instance.Top = screenPoint.Y;
            BtnStck.Instance.Focus();
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
                API.GetWindowRect(_showDesktopBtn, out r);
                var curHeight = r.Bottom - r.Top;
                var curWidth = r.Right - r.Left;
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
        private static void CheckWnd(IntPtr wnd, string className)
        {
            if (wnd == IntPtr.Zero)
                Environment.FailFast(className + " not found");
        }
        
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
