using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using Power8.Helpers;
using Power8.Properties;
using Application = System.Windows.Forms.Application;

namespace Power8.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow:INotifyPropertyChanged
    {
        public static bool ClosedW;

        private static readonly Window PlacementWnd = new Window { Width = 10, Height = 10 };
        private static readonly Point PlacementPoint = new Point(0, 0);
        
        public event PropertyChangedEventHandler PropertyChanged;

        private IntPtr _taskBar, _showDesktopBtn;
        private WelcomeArrow _arrow;
        private PlacementMode _placement = PlacementMode.MousePoint;

        #region Window (de)init 

        public MainWindow()
        {
            InitializeComponent();
            menu.DataContext = SettingsManager.Instance;

            Application.SetCompatibleTextRenderingDefault(true);
            Application.EnableVisualStyles();

            App.Current.SessionEnding += (sender, args) => Close();
            BtnStck.Instanciated += BtnStckInstanciated;

            SettingsManager.Init();
            if (Util.OsIs.SevenOrBelow)
            {
                SettingsManager.Instance.BlockMetroEnabled = false;
                MWBlockMetro.Visibility = Visibility.Collapsed;
            }
                
        }

        void BtnStckInstanciated(object sender, EventArgs e)
        {
            BtnStck.Instance.RunCalled += ShowRunDialog;
            b1.Cursor = System.Windows.Input.Cursors.Hand;
            if (!Settings.Default.FirstRunDone)
            {
                Settings.Default.FirstRunDone = true;
                Settings.Default.Save();

                _arrow = new WelcomeArrow();
                _arrow.Show();
                var p1 = PointToScreen(new Point(0, 0));//where the main button is actually located
                GetSetWndPosition(_arrow, new API.POINT { X = (int)p1.X, Y = (int)p1.Y }, false);
                var p2 = new Point(_arrow.Left + _arrow.Width / 2, _arrow.Top + _arrow.Height / 2);
                var initialAngle = p1.X < p2.X ? 135 : 45;
                _arrow.Rotation = p1.Y < p2.Y ? -initialAngle : initialAngle;
            }
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
            _taskBar = API.FindWindow(API.WndIds.TRAY_WND_CLASS, null);
            CheckWnd(_taskBar, API.WndIds.TRAY_WND_CLASS);
            if (Util.OsIs.SevenOrMore)
            {
                _showDesktopBtn = API.FindWindowEx(_taskBar, IntPtr.Zero, API.WndIds.TRAY_NTF_WND_CLASS, null);
                CheckWnd(_showDesktopBtn, API.WndIds.TRAY_NTF_WND_CLASS);
                _showDesktopBtn = API.FindWindowEx(_showDesktopBtn, IntPtr.Zero, API.WndIds.SH_DSKTP_WND_CLASS, null);
                CheckWnd(_showDesktopBtn, API.WndIds.SH_DSKTP_WND_CLASS);
            }
            else
            {
                _showDesktopBtn = API.FindWindowEx(_taskBar, IntPtr.Zero, API.WndIds.SH_DSKTP_START_CLASS, null);
                CheckWnd(_showDesktopBtn, API.WndIds.SH_DSKTP_START_CLASS);
            }

            Left = 0;
            Top = 0;
            Util.Fork(WatchDesktopBtn, "ShowDesktop button watcher").Start();

            SettingsManager.WarnMayHaveChanged += SettingsManagerOnWarnMayHaveChanged;
            SettingsManager.BgrThreadLock.Set();

            API.SetParent(this.MakeGlassWpfWindow(), _taskBar);

            API.RegisterHotKey(this.GetHandle(), 0, API.fsModifiers.MOD_ALT, Keys.Z);
            this.RegisterHook(WndProc);
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            ClosedW = true;
            if (BtnStck.IsInstantited)
                BtnStck.Instance.Close();
            KillArrow();
            Util.MainDisp.InvokeShutdown();
        }

        #endregion

        #region Handlers

        private void ShowButtonStack(object sender, RoutedEventArgs e)
        {
            if(!BtnStck.IsInstantited)
                return;

            KillArrow();

            if ((Control.ModifierKeys & Keys.Control) > 0)
            {
#if DEBUG
                if ((Control.ModifierKeys & Keys.Shift) > 0)
                {
                    new RestartExplorer().ShowDialog();
                    return;
                }
#endif
                ShowRunDialog(this, null);
                return;
            }

            MfuList.UpdateStartMfu();
            BtnStck.Instance.Show();//XP:955ms 0_o
            var screenPoint = new API.POINT();
            API.GetCursorPos(ref screenPoint);
            GetSetWndPosition(BtnStck.Instance, screenPoint, sender == Keyboard.PrimaryDevice);
        }

        private void ShowRunDialog(object sender, EventArgs e)
        {
            API.SHRunDialog(IntPtr.Zero, IntPtr.Zero, null, null, null, API.RFF.NORMAL);
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Close();
            SettingsManager.BgrThreadLock.WaitOne();
        }

        private void AboutClick(object sender, RoutedEventArgs e)
        {
            Util.InstanciateClass(t: typeof (About));
        }

        private void MainBtnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            KillArrow();
        }

        private void MainBtnLayoutUpdated(object sender, EventArgs e)
        {
            if (!IsLoaded || Util.OsIs.XPOrLess)
                return;
            var p = b1.PointToScreen(PlacementPoint);
            GetSetWndPosition(PlacementWnd, new API.POINT { X = (int)p.X, Y = (int)p.Y }, false);
            if ((int)PlacementWnd.Left != (int)p.X)//Taskbar vertical
                ContextPlacement = PlacementWnd.Left > p.X ? PlacementMode.Right : PlacementMode.Left;
            else                         //Taskbar horizontal
                ContextPlacement = PlacementWnd.Top > p.Y ? PlacementMode.Bottom : PlacementMode.Top;
        }

        private void ShowSettingsWindow(object sender, RoutedEventArgs e)
        {
            Util.InstanciateClass(t: typeof(SettingsWnd));
        }

        private void SettingsManagerOnWarnMayHaveChanged(object sender, EventArgs eventArgs)
        {
            FirePropChanged("Tip");
        }

        #endregion

        #region Background threads

        private void WatchDesktopBtn()
        {
            double width = -1, height = -1;

            while (!ClosedW)
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
                        var trd  = Util.Fork(() =>
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
                        }, "Restart explorer window");
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

        #endregion

        #region Bindable props

        public PlacementMode ContextPlacement
        {
            get { return _placement; }
            private set
            {
                if(_placement == value)
                    return;
                _placement = value;
                FirePropChanged("ContextPlacement");
            }
        }

        public string Tip
        {
            get
            {
                return Properties.Resources.CR_ButtonStack +
                        (SettingsManager.Instance.ShowWarn //TODO: move to resources
                            ? "\r\nYou may wish to change some settings to enhance your Power8 experience"
                            : string.Empty);
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
        private static void GetSetWndPosition(Window w, API.POINT screenPoint, bool ignoreTaskbarPosition)
        {
            var resPoint = new Point();
            
            var screen = Screen.FromPoint(new System.Drawing.Point(screenPoint.X, screenPoint.Y));
            //We show stack in the corner closest to the mouse
            bool isHideTaskBarOptionOn = (screen.WorkingArea.Width == screen.Bounds.Width &&
                                         screen.WorkingArea.Height == screen.Bounds.Height)
                                         || ignoreTaskbarPosition;
            
            //taskbar is vertical @ left or horizontal
            if ((isHideTaskBarOptionOn && screenPoint.X <= screen.WorkingArea.X + screen.WorkingArea.Width / 2)
                || screen.WorkingArea.X > screen.Bounds.X
                || (screen.WorkingArea.Width == screen.Bounds.Width & !isHideTaskBarOptionOn))
                resPoint.X = screen.WorkingArea.X;
            else    //vertical @ right
                resPoint.X = screen.WorkingArea.Width + screen.WorkingArea.X - w.Width;
            
            //taskbar is horizontal @ top or vertical
            if ((isHideTaskBarOptionOn && screenPoint.Y <= screen.WorkingArea.Y + screen.WorkingArea.Height / 2)
                || screen.WorkingArea.Y > screen.Bounds.Y
                || (screen.WorkingArea.Height == screen.Bounds.Height & !isHideTaskBarOptionOn))
                resPoint.Y = screen.WorkingArea.Y;
            else    //horizontal @ bottom
                resPoint.Y = screen.WorkingArea.Height + screen.WorkingArea.Y - w.Height;

            w.Left = resPoint.X;
            w.Top = resPoint.Y;
            if((int)w.Height != 10)
                w.Activate();
            var b = w as BtnStck;
            if (b != null)
                b.Focus();
        }

        private void KillArrow()
        {
            if (_arrow == null) 
                return;
            _arrow.Close();
            _arrow = null;
        }

        private void FirePropChanged(string propName)
        {
            var h = PropertyChanged;
            if (h != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        #endregion
    }
}
