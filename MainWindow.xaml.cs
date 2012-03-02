using System;
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
	public partial class MainWindow
	{
		public static bool ClosedW;
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


		private void Button1Click(object sender, RoutedEventArgs e)
		{
			//BtnStck.Instance.Hide();
			BtnStck.Instance.Show();
			var screenPoint = PointToScreen(Mouse.GetPosition(this));
			var screen = Screen.FromPoint(new System.Drawing.Point((int) screenPoint.X, (int) screenPoint.Y));

				//vertical @ left or horizontal
			if (screen.WorkingArea.X > screen.Bounds.X || screen.WorkingArea.Width == screen.Bounds.Width)
				screenPoint.X = screen.WorkingArea.X;
			else //vertical @ right
				screenPoint.X = screen.WorkingArea.Width + screen.WorkingArea.X - BtnStck.Instance.Width;
				//horizontal @ top or vertical
			if (screen.WorkingArea.Y > screen.Bounds.Y || screen.WorkingArea.Height == screen.Bounds.Height)
				screenPoint.Y = screen.WorkingArea.Y;
			else //horizontal @ bottom
				screenPoint.Y = screen.WorkingArea.Height + screen.WorkingArea.Y - BtnStck.Instance.Height;

			if (screenPoint.X + BtnStck.Instance.Width > screen.Bounds.Width + screen.Bounds.Left)
				screenPoint.X -= BtnStck.Instance.Width;
			if (screenPoint.Y + BtnStck.Instance.Height > screen.Bounds.Height + screen.Bounds.Top)
				screenPoint.Y -= BtnStck.Instance.Height;

			BtnStck.Instance.Left = screenPoint.X;
			BtnStck.Instance.Top = screenPoint.Y;
			BtnStck.Instance.Focus();
		}

		private void WindowClosed(object sender, EventArgs e)
		{
			ClosedW = true;
			BtnStck.Instance.Close();
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
			new Thread(WatchDesktopBtn).Start();

			var hlpr = new WindowInteropHelper(this);
			HwndSource.FromHwnd(hlpr.Handle).CompositionTarget.BackgroundColor = Colors.Transparent;
			API.MakeGlass(hlpr.Handle);
			API.SetParent(hlpr.Handle, _taskBar);
		}

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
					Dispatcher.Invoke(new Action(() => b1.Width = curWidth));
				}
				if (height != curHeight)
				{
					height = curHeight;
					Dispatcher.Invoke(new Action(() => b1.Height = curHeight));
				}
				Thread.Sleep(100);
			}
		}

	}
}
