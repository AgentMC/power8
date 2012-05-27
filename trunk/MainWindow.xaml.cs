﻿using System;
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

        private readonly EventWaitHandle _updateThreadLock = new EventWaitHandle(false, EventResetMode.AutoReset);

        #region Window (de)init 

        public MainWindow()
        {
            InitializeComponent();
            menu.DataContext = this;

            Application.SetCompatibleTextRenderingDefault(true);
            Application.EnableVisualStyles();

            App.Current.SessionEnding += (sender, args) => Close();
            //deferred btnstack instance generation
            BtnStck.Instanciated += (sender, args) => BtnStck.Instance.RunCalled += ShowRunDialog;

            if (CheckForUpdatesEnabled)
                UpdateCheckThreadInit();
        }

// ReSharper disable RedundantAssignment
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;
            if (msg == (uint)API.WM.HOTKEY)
            {
                handled = true;
                if (BtnStck.Instance.IsActive)
                    b1.Focus();
                else
                    ShowButtonStack(Keyboard.PrimaryDevice, null);
            }
            return IntPtr.Zero;
        }
// ReSharper restore RedundantAssignment

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
#warning context menu position should be handeled well
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
            _updateThreadLock.Set();
            _updateThreadLock.Close();

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
            var screenPoint = new API.POINT();
            API.GetCursorPos(ref screenPoint);
            var screen = Screen.FromPoint(new System.Drawing.Point(screenPoint.X, screenPoint.Y));
            //We show stack in the corner closest to the mouse
            bool isHideTaskBarOptionOn = (screen.WorkingArea.Width == screen.Bounds.Width &&
                                         screen.WorkingArea.Height == screen.Bounds.Height)
                                         || (sender == Keyboard.PrimaryDevice);
// ReSharper disable PossibleLossOfFraction
                    //taskbar is vertical @ left or horizontal
            if ((isHideTaskBarOptionOn && screenPoint.X <= screen.WorkingArea.X + screen.WorkingArea.Width / 2)
                || screen.WorkingArea.X > screen.Bounds.X
                || (screen.WorkingArea.Width == screen.Bounds.Width & !isHideTaskBarOptionOn))
                screenPoint.X = screen.WorkingArea.X;
            else    //vertical @ right
                screenPoint.X = (int) (screen.WorkingArea.Width + screen.WorkingArea.X - BtnStck.Instance.Width);
                    //taskbar is horizontal @ top or vertical
            if ((isHideTaskBarOptionOn && screenPoint.Y <= screen.WorkingArea.Y + screen.WorkingArea.Height / 2) 
                || screen.WorkingArea.Y > screen.Bounds.Y 
                || (screen.WorkingArea.Height == screen.Bounds.Height & !isHideTaskBarOptionOn))
                screenPoint.Y = screen.WorkingArea.Y;
            else    //horizontal @ bottom
                screenPoint.Y = (int) (screen.WorkingArea.Height + screen.WorkingArea.Y - BtnStck.Instance.Height);
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
#warning hackfix. Env.Exit() shouldn't be required.
            Environment.Exit(0); 
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
                                Util.StartExplorer();
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
            if(!_updateThreadLock.SafeWaitHandle.IsClosed)
                _updateThreadLock.WaitOne();

            int cycles = 0;
            var client = new WebClient();
            _update = true;
            while (_update && _watch)
            {
                if (cycles == 0)
                {
                    try
                    {//parsing
                        var info =
                            new System.IO.StringReader(
                                client.DownloadString(Properties.Resources.Stg_Power8URI + Properties.Resources.Stg_AssemblyInfoURI));
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
                        if(verLine != null)
                        {//updating?
                            if (new Version(verLine) > new Version(Application.ProductVersion) && Settings.Default.IgnoreVer != verLine)
                            {//updating!
                                if (uri7Z == null || uriMsi == null) //old approach
                                {
                                    switch (MessageBox.Show(Properties.Resources.CR_UNUpdateAvailableLong + string.Format(
                                                Properties.Resources.Str_UpdateAvailableFormat, Application.ProductVersion, verLine),
                                            Properties.Resources.Stg_AppShortName + Properties.Resources.Str_UpdateAvailable,
                                            MessageBoxButton.YesNoCancel, MessageBoxImage.Information))
                                    {
                                        case MessageBoxResult.Cancel:
                                            Settings.Default.IgnoreVer = verLine;
                                            Settings.Default.Save();
                                            break;
                                        case MessageBoxResult.Yes:
                                            Process.Start(Properties.Resources.Stg_Power8URI);
                                            break;
                                    }
                                }
                                else
                                {
                                    Util.Send(()=> new UpdateNotifier(Application.ProductVersion, verLine, uri7Z,
                                                                      uriMsi).Show());
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(Properties.Resources.Err_CantCheckUpdates + ex.Message,
                                        Properties.Resources.Stg_AppShortName, MessageBoxButton.OK,
                                        MessageBoxImage.Exclamation);
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
                k = k.OpenSubKey(Properties.Resources.Stg_RegKeyRun, false);
                return k != null &&
                       string.Equals((string) k.GetValue(Properties.Resources.Stg_AppShortName),
                                     Application.ExecutablePath,
                                     StringComparison.InvariantCultureIgnoreCase);
            }
            set
            {
                if (value == AutoStartEnabled)
                    return;
                var k = Microsoft.Win32.Registry.CurrentUser;
                k = k.OpenSubKey(Properties.Resources.Stg_RegKeyRun, true);
                if (k == null) 
                    return;
                if (value)
                    k.SetValue(Properties.Resources.Stg_AppShortName, Application.ExecutablePath);
                else
                    k.DeleteValue(Properties.Resources.Stg_AppShortName);
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