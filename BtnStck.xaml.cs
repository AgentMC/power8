using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Diagnostics;

namespace Power8
{
    /// <summary>
    /// Interaction logic for BtnStck.xaml
    /// </summary>
    public partial class BtnStck
    {
        private static BtnStck _instance;
        public static BtnStck Instance
        {
            get { return _instance ?? (_instance = new BtnStck()); }
            private set { _instance = value; }
        }

        public BtnStck()
        {
            InitializeComponent();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !MainWindow.ClosedW;
            if (e.Cancel)
                Hide();
            else
                Instance = null;
        }

        private void WindowDeactivated(object sender, EventArgs e)
        {
            Hide();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            var hlpr = new WindowInteropHelper(this);
            HwndSource.FromHwnd(hlpr.Handle).CompositionTarget.BackgroundColor = Colors.Transparent;
            API.MakeGlass(hlpr.Handle);
            MinHeight = Height;
            MaxHeight = MinHeight;
            MinWidth = Width;
            MaxWidth = MinWidth;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Application.SetSuspendState(System.Windows.Forms.PowerState.Hibernate, true, false);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Application.SetSuspendState(System.Windows.Forms.PowerState.Suspend, true, false);
        }

        private void LaunchShForced(string arg)
        {
            StartConsoleHidden("shutdown.exe", arg + " -f -t 0");
        }

        private void StartConsoleHidden(string exe, string args)
        {
            var si = new ProcessStartInfo(exe, args);
            si.CreateNoWindow=true;
            si.WindowStyle=ProcessWindowStyle.Hidden;
            Process.Start(si);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            LaunchShForced("-s");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            LaunchShForced("-r");
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            LaunchShForced("-l");
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            StartConsoleHidden(@"C:\WINDOWS\system32\rundll32.exe", "user32.dll,LockWorkStation");
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            API.SendMessage(API.GetDesktopWindow(), API.WM_SYSCOMMAND, API.SC_SCREENSAVE, 0);
        }
    }
}
