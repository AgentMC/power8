using System;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;


namespace Power8
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool ClosedW = false;
        private const string TRAY_WND_CLASS = "Shell_TrayWnd";
        private const string TRAY_NTF_WND_CLASS = "TrayNotifyWnd";
        private const string SH_DSKTP_WND_CLASS = "TrayShowDesktopButtonWClass";

        private bool _watch;
        private IntPtr _taskBar, _showDesktopBtn;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void CheckWnd(IntPtr wnd, string className)
        {
            if (wnd == IntPtr.Zero)
                Environment.FailFast(className + " not found");
        }


        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //BtnStck.Instance.Hide();
            BtnStck.Instance.Show();
            var screenPoint = PointToScreen(Mouse.GetPosition(this));
            var screen = Screen.FromPoint(new System.Drawing.Point((int)screenPoint.X, (int)screenPoint.Y));
            if (screenPoint.X + BtnStck.Instance.Width > screen.Bounds.Width + screen.Bounds.Left)
                screenPoint.X -= BtnStck.Instance.Width;
            if (screenPoint.Y + BtnStck.Instance.Height > screen.Bounds.Height + screen.Bounds.Top)
                screenPoint.Y -= BtnStck.Instance.Height;
            BtnStck.Instance.Left = screenPoint.X;
            BtnStck.Instance.Top = screenPoint.Y;
            BtnStck.Instance.Focus();

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ClosedW = true;
            BtnStck.Instance.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
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
            new Thread(WatchDesktopBtn).Start();

            var hlpr = new WindowInteropHelper(this);
            HwndSource.FromHwnd(hlpr.Handle).CompositionTarget.BackgroundColor = Colors.Transparent;
            API.MakeGlass(hlpr.Handle);
            API.SetParent(hlpr.Handle, _taskBar);
        }

        private void WatchDesktopBtn()
        {
            double width = -1, height = -1, curWidth = 0, curHeight = 0;
            API.RECT r;

            while (_watch)
            {
                API.GetWindowRect(_showDesktopBtn, out r);
                curHeight = r.Bottom - r.Top;
                curWidth = r.Right - r.Left;
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


        private void button1_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //b1.Opacity = 0.5;
        }

        private void button1_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
           // b1.Opacity = 0.1;
        }
    }
}
