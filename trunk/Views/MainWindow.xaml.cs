using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Power8.Helpers;
using Power8.Properties;
using Application = System.Windows.Forms.Application;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using Point = System.Windows.Point;

namespace Power8.Views
{
    /// <summary>
    /// MainWindow - a Start Button of Power8. This class is the main one.
    /// </summary>
    public partial class MainWindow:INotifyPropertyChanged
    {
        public static bool ClosedW; //Indicates that App is going to init the shutdown soon...
        //Both used to set proper placement for Context Menu over Main Button
        private static readonly Window PlacementWnd = new Window { Width = 10, Height = 10 };
        private static readonly Point PlacementPoint = new Point(0, 0);
        public static float SystemScale = 1.0f;
        
        public event PropertyChangedEventHandler PropertyChanged;

        private IntPtr _taskBar, _showDesktopBtn, _midPanel, _startBtn; //system windows handles
        private WelcomeArrow _arrow;
        private PlacementMode _placement = PlacementMode.MousePoint; //used for context menu
        private BitmapSource _bitmap; //picture on the Main Button
        private string _lastSource;   //file path of _bitmap

        #region Window (de)init 

        /// <summary>
        /// Constructor. Initializes layout of window, sets datacontext, switches on theming on 
        /// WinForms dialogs, and so on.
        /// </summary>
        public MainWindow()
        {
            Util.FpReset();
            InitializeComponent();
            b1.DataContext = SettingsManager.Instance;

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
        /// <summary>
        /// Handles event that occurs when ButtonStack instance was generated. This happens on startup,
        /// so is a part of initialization of MainWindow. 
        /// Changes cursor and shows welcome arrow if required.
        /// </summary>
        void BtnStckInstanciated(object sender, EventArgs e)
        {
            Analytics.PostEvent(Analytics.Category.Runtime, "Start", null, 1);
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
        /// <summary> Hooks WndProc to react on HOTKEY passed (Alt+Z) </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;
            if (msg == (uint)API.WM.HOTKEY)
            {
                handled = true;
                if (BtnStck.IsInstantited && BtnStck.Instance.IsActive)
                {
                    Activate(); //WXP requires this
                    b1.Focus();
                }
                else if (BtnStck.IsInstantited)
                {
                    ShowButtonStack(Keyboard.PrimaryDevice, null);
                }
            }
            return IntPtr.Zero;
        }
// ReSharper restore RedundantAssignment
        /// <summary>
        /// Locates Windows system windows, like TaskBar and locates Power8 MainButton inside it.
        /// Starts location update thread. Subscribes to Settings Manager events and registers HotKey.
        /// </summary>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            _taskBar = API.FindWindow(API.WndIds.TRAY_WND_CLASS, null);
            CheckWnd(_taskBar, API.WndIds.TRAY_WND_CLASS);
            SystemScale = 1f / 96;
            using (var g = Graphics.FromHwnd(_taskBar))
            {
                SystemScale *= g.DpiX;
            }

            if (Util.OsIs.SevenOrMore)
            {
                _midPanel = API.FindWindowEx(_taskBar, IntPtr.Zero, API.WndIds.TRAY_REBAR_WND_CLASS, null);
                CheckWnd(_midPanel, API.WndIds.TRAY_REBAR_WND_CLASS);
                if(Util.OsIs.EightBlueOrMore)
                {
                    _startBtn = API.FindWindowEx(_taskBar, IntPtr.Zero, API.WndIds.SH_W8_1_START_CLASS, null);
                    CheckWnd(_startBtn, API.WndIds.SH_W8_1_START_CLASS);
                }
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
            API.SetParent(this.MakeGlassWpfWindow(), _taskBar);
            Util.ForkStart(WatchDesktopBtn, "ShowDesktop button watcher");
            
            SettingsManager.WarnMayHaveChanged += SettingsManagerOnWarnMayHaveChanged;
            SettingsManager.ImageChanged += SettingsManagerOnImageChanged;
            SettingsManager.PicStretchChanged += SettingsManagerOnImageStretchChanged;
            SettingsManager.BgrThreadLock.Set();

            API.RegisterHotKey(this.GetHandle(), 0, API.fsModifiers.MOD_ALT, Keys.Z);
            this.RegisterHook(WndProc);
        }

        /// <summary>
        /// Sync point when closing the application. If the main window was closed by Alt+F4,
        /// by killing Explorer, or by shutdown of the system - controlls that still all the 
        /// threads will exit by invoking Main dispatcher shutdown.
        /// </summary>
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

        /// <summary>
        /// Handles click on the Main button. By default shows/hides Buttonstack, 
        /// with CTRL pressed shows Run dialog instead, and with CTRL+SHIFT in DEBUG
        /// mode shows RestartExplorer instance for purposes of localization
        /// </summary>
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
            GetSetWndPosition(BtnStck.Instance, screenPoint, 
                sender == Keyboard.PrimaryDevice && SettingsManager.Instance.AltZAutoCornerEnabled);
            //  ^^^ means "Command came from KB and Auto-cornering enabled"
        }
        /// <summary>
        /// Shows Run dialog
        /// </summary>
        private void ShowRunDialog(object sender, EventArgs e)
        {
            Util.UpdateEnvironment();
            API.SHRunDialog(IntPtr.Zero, IntPtr.Zero, null, null, null, API.RFF.NORMAL);
        }
        /// <summary>
        /// Closes Main window exiting application
        /// </summary>
        private void ExitClick(object sender, RoutedEventArgs e)
        {
            if(SettingsManager.Instance.BlockMetroEnabled)
                SettingsManager.BgrThreadLock.Reset();//Main Window must not be closed while this is running
            Close();
            SettingsManager.BgrThreadLock.WaitOne();
        }
        /// <summary>
        /// Shows About dialog
        /// </summary>
        private void AboutClick(object sender, RoutedEventArgs e)
        {
            Util.InstanciateClass(t: typeof (About));
        }
        /// <summary>
        /// Shows About dialog
        /// </summary>
        private void DonateClick(object sender, RoutedEventArgs e)
        {
            Util.InstanciateClass(t: typeof(Donate));
        }
        /// <summary>
        /// Shows Settings dialog
        /// </summary>
        private void SettingsClick(object sender, RoutedEventArgs e)
        {
            Util.InstanciateClass(t: typeof(SettingsWnd));
        }
        /// <summary>
        /// Hides WelcomeArrow when mouse howers Main button
        /// </summary>
        private void MainBtnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            KillArrow();
        }
        /// <summary>
        /// Fixes placement of Main Button's Context Menu depending on Taskbar layout
        /// </summary>
        private void MainBtnLayoutUpdated(object sender, EventArgs e)
        {
            if (!IsLoaded || Util.OsIs.XPOrLess)
                return;
            var p = b1.PointToScreen(PlacementPoint);
            if (Util.OsIs.SevenOrBelow && !Util.OsIs.XPOrLess && !API.DwmIsCompositionEnabled())
            {
                p.X -= 2;
                p.Y -= 2;
            }
            GetSetWndPosition(PlacementWnd, new API.POINT { X = (int)p.X, Y = (int)p.Y }, false);
            if ((int) PlacementWnd.Left == (int) p.X && (int) PlacementWnd.Top == (int) p.Y)
            {    //Top left corner, taskbar hidden, vertical or horizontal
                API.RECT r;
                API.GetWindowRect(_taskBar, out r);
                ContextPlacement = (r.Bottom - r.Top > r.Right - r.Left) ? PlacementMode.Right : PlacementMode.Bottom;
            }
            else if ((int) PlacementWnd.Left != (int) p.X) //Taskbar vertical
                ContextPlacement = PlacementWnd.Left > p.X ? PlacementMode.Right : PlacementMode.Left;
            else //Taskbar horizontal
                ContextPlacement = PlacementWnd.Top > p.Y ? PlacementMode.Bottom : PlacementMode.Top;
        }
        /// <summary>
        /// Reacts on the event indicating that settings state may had changed, so warning 
        /// icon and text must be displayed or hidden
        /// </summary>
        private void SettingsManagerOnWarnMayHaveChanged(object sender, EventArgs eventArgs)
        {
            FirePropChanged("Tip");
            FirePropChanged("WarningIconVisibility");
        }
        /// <summary>
        /// Invokes PropertyChanged for StartImage property making UI react on user changing 
        /// the picture in settings
        /// </summary>
        private void SettingsManagerOnImageChanged(object sender, EventArgs eventArgs)
        {
            FirePropChanged("StartImage");
        }
        /// <summary>
        /// Invokes PropertyChanged for CustomPicStretch property making UI react on user changing 
        /// the picture stretching in settings
        /// </summary>
        private void SettingsManagerOnImageStretchChanged(object sender, EventArgs eventArgs)
        {
            FirePropChanged("CustomPicStretch");
        }
        /// <summary>
        /// Handles DragEnter/DragOver events to provide proper feedback to drag source
        /// </summary>
        private void Power8Drag(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if ((e.AllowedEffects & DragDropEffects.Link) > 0)
                    e.Effects = DragDropEffects.Link; //we prefer Link because it better demonstrates what will be done
                else
                    e.Effects = DragDropEffects.Copy | DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        /// <summary>
        /// Handles DragDrop event putting file names to custom MFU list
        /// </summary>
        private void Power8Drop(object sender, DragEventArgs e)
        {
            foreach (var fileOrFolder in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                MfuList.Add2Custom(null, fileOrFolder);
            }
            MfuList.UpdateStartMfu(); //we use 2nd way to invoke Add2Cutsom, so must 
            //update the MFU list explicitly
        }

        #endregion

        #region Background threads

        /// <summary>
        /// Thread watches Taskbar and configures layout of Main button accordingly
        /// </summary>
        private void WatchDesktopBtn()
        {
            double width = -1, height = -1;
            API.RECT r;
            var p = default(Point);
            //Hide start button for Win8.1+
            if(Util.OsIs.EightBlueOrMore)
            {
                API.ShowWindow(_startBtn, API.SWCommands.HIDE);
            }
            while (!ClosedW)
            {
                if (!API.GetWindowRect(_showDesktopBtn, out r))
                {//looks like explorer.exe is dead!
                    Thread.Sleep(10000);//let's wait for explorer to auto-restart
                    var explorers = Process.GetProcessesByName("explorer");
                    if (explorers.Any(e => e.SessionId == MfuList.ProcessSessionId))
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
                //The success way
                //Size the button by ShowDesktop buton
                var curHeight = (r.Bottom - r.Top)/SystemScale;
                var curWidth = (r.Right - r.Left)/SystemScale;
                bool taskBarVertical = curHeight < curWidth;

                if (SettingsManager.Instance.SquareStartButton)
                {//Apply user size modification
                    var sizeCoef = SettingsManager.GetARModifier(!taskBarVertical);
                    if (curHeight > curWidth) //vertical button, horiz. bar
                        curWidth = (int)(curHeight * sizeCoef);
                    else                      //horiz. button, vertical bar
                        curHeight = (int)(curWidth / sizeCoef); 
                }
                //A kind of "Minimum size" for Win8.1
                if (Util.OsIs.EightBlueOrMore)
                {
                    const float w8DefShDsktpWdt = 15f;
                    curWidth = Math.Max(curWidth, w8DefShDsktpWdt);
                    curHeight = Math.Max(curHeight, w8DefShDsktpWdt);
                }
// ReSharper disable CompareOfFloatsByEqualityOperator
                if (Math.Round(width) != Math.Round(curWidth))
                {
                    width = curWidth;
                    Dispatcher.Invoke(new Action(() =>  b1.Width = curWidth));
                }
                if (Math.Round(height) != Math.Round(curHeight))
                {
                    height = curHeight;
                    Dispatcher.Invoke(new Action(() =>  b1.Height = curHeight));
                }
// ReSharper restore CompareOfFloatsByEqualityOperator
                
                //Check if the Main Button location changed within screen
                if (BtnStck.IsInstantited && ((int)p.Y != r.Top || (int)p.X != r.Left))
                {
                    p.Y = r.Top;
                    p.X = r.Left;
                    if (Util.OsIs.SevenOrMore && taskBarVertical)
                    {
                        BtnStck.Instance.IsWindowAtTopOfScreen = true;
                    }
                    else //only 25% but that's defaults...
                    {
                        var activeScreen = Screen.FromPoint(new System.Drawing.Point(r.Left, r.Top));
                        BtnStck.Instance.IsWindowAtTopOfScreen = (double)activeScreen.Bounds.Height/2 > r.Top;
                    }
                }

                //If required, apply move to taskbar rebar
                if (Util.OsIs.EightOrMore || (SettingsManager.Instance.SquareStartButton && Util.OsIs.SevenOrMore))
                    MoveReBar(taskBarVertical, (int) (curHeight*SystemScale), (int) (curWidth*SystemScale));

                Thread.Sleep(100);
            }
            //restoring taskbar on exit
            if (Util.OsIs.EightOrMore && API.GetWindowRect(_showDesktopBtn, out r))
            {//free space to the left/up is the same as in right/down, which is show desktop btn
                var curHeight = r.Bottom - r.Top;
                var curWidth = r.Right - r.Left;
                bool isVertical = curHeight < curWidth;
                //For Win8.1 we still determine isVertical by ShowDesktop, but position is determined by Start button
                if(Util.OsIs.EightBlueOrMore && API.GetWindowRect(_startBtn, out r))
                {
                    curHeight = r.Bottom - r.Top;
                    curWidth = r.Right - r.Left;
                }
                MoveReBar(isVertical, curHeight, curWidth);
            }
            //Restoring start button for win 8.1
            if (Util.OsIs.EightBlueOrMore)
            {
                API.ShowWindow(_startBtn, API.SWCommands.SHOW);
            }
        }
        
        #endregion

        #region Bindable props

        /// <summary>
        /// Returns currently desired placement of MainButton Context menu
        /// in telation to this button
        /// </summary>
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
        /// <summary>
        /// Returns tooltip that must be shown currently over the Main Button
        /// </summary>
        public string Tip
        {
            get { return Properties.Resources.CR_ButtonStack + SettingsManager.Instance.WarnText; }
        }
        /// <summary>
        /// Returns value indicating should warning icon be displayed at the moment
        /// </summary>
        public Visibility WarningIconVisibility
        {
            get
            {
                return SettingsManager.Instance.ShowWarn ? Visibility.Visible : Visibility.Hidden;
            }
        }
        /// <summary>
        /// Returns BitmapSource containing user-chosen picture placed onto MainButton
        /// </summary>
        public BitmapSource StartImage
        {
            get
            {
                if(!SettingsManager.Instance.SquareStartButton)
                    return null;
                if (_lastSource == SettingsManager.Instance.ImageString)
                    return _bitmap;
                _lastSource = SettingsManager.Instance.ImageString;
                if (string.IsNullOrWhiteSpace(_lastSource))
                {
                    _bitmap = null;
                }
                else
                {
                    try
                    {
                        _bitmap = new BitmapImage(new Uri(_lastSource, UriKind.Absolute));
                    }
                    catch
                    {
                        _bitmap = null;
                    }
                }
                return _bitmap;
            }
        }
        /// <summary>
        /// Returns Stretch mode for custom user picture
        /// </summary>
        public System.Windows.Media.Stretch CustomPicStretch
        {
            get { return (System.Windows.Media.Stretch) SettingsManager.Instance.PicStretchSelectedIndex; }
        }

        #endregion

        #region Helpers
// ReSharper disable UnusedParameter.Local
        /// <summary>
        /// Kills application if passed window handle is NULL with coresponding message.
        /// </summary>
        /// <param name="wnd">Window handle to be checked</param>
        /// <param name="className">Class name of window (or any other identifier). 
        /// Used for logging purposes.</param>
        private static void CheckWnd(IntPtr wnd, string className)
        {
            if (wnd == IntPtr.Zero)
                Util.Die(className + " not found");
        }
// ReSharper restore UnusedParameter.Local
        /// <summary>
        /// Locates the window passed as if it is a Progman, the Start Menu.
        /// First, by screenPoint passed calculates active screen.
        /// By it's bounds determines the location of TaskBar.
        /// Finally, calculates the position of target window.
        /// Shows window except if it is a Placement rectangle.
        /// Focuses window if it is a ButtonStack.
        /// </summary>
        /// <param name="w">Window to locate</param>
        /// <param name="screenPoint">Unmanaged struct containing absolute screen coordinates, in px.</param>
        /// <param name="ignoreTaskbarPosition">Flag instructing method to ignore the location of task bar
        /// locating w simply in the corner closest to the screenPoint. E.g. to show BtnStch by Alt+Z.</param>
        private static void GetSetWndPosition(Window w, API.POINT screenPoint, bool ignoreTaskbarPosition)
        {
            var resPoint = new Point();
            var screen = Screen.FromPoint(new System.Drawing.Point(screenPoint.X, screenPoint.Y));
            //We show stack in the corner closest to the mouse
            bool isHideTaskBarOptionOn = (screen.WorkingArea.Width == screen.Bounds.Width &&
                                         screen.WorkingArea.Height == screen.Bounds.Height)
                                         || ignoreTaskbarPosition;
            
            //taskbar is vertical @ left or horizontal
            if ((isHideTaskBarOptionOn && screenPoint.X <= screen.WorkingArea.X + screen.WorkingArea.Width/2)
                || screen.WorkingArea.X > screen.Bounds.X
                || (screen.WorkingArea.Width == screen.Bounds.Width & !isHideTaskBarOptionOn))
                resPoint.X = screen.WorkingArea.X;
            else //vertical @ right
                resPoint.X = (screen.WorkingArea.Width + screen.WorkingArea.X - w.Width*SystemScale)/SystemScale;
            
            //taskbar is horizontal @ top or vertical
            if ((isHideTaskBarOptionOn && screenPoint.Y <= screen.WorkingArea.Y + screen.WorkingArea.Height/2)
                || screen.WorkingArea.Y > screen.Bounds.Y
                || (screen.WorkingArea.Height == screen.Bounds.Height & !isHideTaskBarOptionOn))
                resPoint.Y = screen.WorkingArea.Y;
            else //horizontal @ bottom
                resPoint.Y = (screen.WorkingArea.Height + screen.WorkingArea.Y - w.Height*SystemScale)/SystemScale;

            w.Left = resPoint.X;
            w.Top = resPoint.Y;
// ReSharper disable PossibleUnintendedReferenceComparison
            if(w != PlacementWnd)
                w.Activate();
// ReSharper restore PossibleUnintendedReferenceComparison
            var b = w as BtnStck;
            if (b != null)
                b.Focus();//Focus() is __new on ButtonStack, must explicitly type
        }
        /// <summary>
        /// Hides WelcomeArrow if it is available
        /// </summary>
        private void KillArrow()
        {
            if (_arrow == null) 
                return;
            _arrow.Close();
            _arrow = null;
        }
        /// <summary>
        /// Invokes PropertyChanged event
        /// </summary>
        /// <param name="propName">Name of the property that had changed</param>
        private void FirePropChanged(string propName)
        {
            var h = PropertyChanged;
            if (h != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        /// <summary>
        /// Moves "rebar" panel that hosts applications' buttons and different launch bars on taskbar
        /// </summary>
        /// <param name="taskBarVertical">Is taskbar located vertical or horizontal</param>
        /// <param name="curHeight">Desired Height of the gap to the left/top of app buttons</param>
        /// <param name="curWidth">Desired Width of the gap to the left/top of app buttons</param>
        private void MoveReBar(bool taskBarVertical, int curHeight, int curWidth)
        {
            API.RECT r, r2;
            API.GetWindowRect(_midPanel, out r); //absolute coord of rebar
            API.GetWindowRect(_taskBar, out r2); //absolute coord of taskbar
            if (Util.OsIs.SevenOrBelow && !API.DwmIsCompositionEnabled())
            {//This doesn't work on exit since it is called only for Win8 on exit
                //Have no idea why, but there's some kind of automatic margin applied in classic style
                r2.Left += 4;
                r2.Top += 4;
            }
            r.Top -= r2.Top; //getting relative coordinates...
            r.Left -= r2.Left;
            r.Right -= r2.Left;
            r.Bottom -= r2.Top;
            if (taskBarVertical && r.Top + 4 != curHeight) //start moving!
            {
                //move rebar down (up on exit)
                int delta = (curHeight - 4) - r.Top;
                API.MoveWindow(_midPanel, r.Left, r.Top + delta, r.Right - r.Left, r.Bottom - r.Top - delta, true);
            }
            else if (!taskBarVertical && r.Left + 4 != curWidth)
            {
                //move rebar right (left on exit)
                int delta = (curWidth - 4) - r.Left;
                API.MoveWindow(_midPanel, r.Left + delta, r.Top, r.Right - r.Left - delta, r.Bottom - r.Top, true);
            }
        }

        #endregion

    }
}
