using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        #region Load, Unload, Show, Hide
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
        #endregion

        #region Buttons handlers
        private void ButtonHibernateClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Application.SetSuspendState(System.Windows.Forms.PowerState.Hibernate, true, false);
        }

        private void ButtonSleepClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Application.SetSuspendState(System.Windows.Forms.PowerState.Suspend, true, false);
        }

        private void ButtonShutdownClick(object sender, RoutedEventArgs e)
        {
            LaunchShForced("-s");
        }

        private void ButtonRestartClick(object sender, RoutedEventArgs e)
        {
            LaunchShForced("-r");
        }

        private void ButtonLogOffClick(object sender, RoutedEventArgs e)
        {
            LaunchShForced("-l");
        }

        private void ButtonLockClick(object sender, RoutedEventArgs e)
        {
            StartConsoleHidden(@"C:\WINDOWS\system32\rundll32.exe", "user32.dll,LockWorkStation");
        }

        private void ButtonScreensaveClick(object sender, RoutedEventArgs e)
        {
            API.SendMessage(API.GetDesktopWindow(), API.WM_SYSCOMMAND, API.SC_SCREENSAVE, 0);
        }
        #endregion

        public ObservableCollection<PowerItem> Items
        {
            get { return PowerItemTree.ItemsRoot; }
        } 

        #region Helpers
        private static void LaunchShForced(string arg)
        {
            StartConsoleHidden("shutdown.exe", arg + " -f -t 0");
        }

        private static void StartConsoleHidden(string exe, string args)
        {
            var si = new ProcessStartInfo(exe, args) {CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden};
            Process.Start(si);
        }
        #endregion


        private void MenuItemClick(object sender, EventArgs e)
        {
            var powerItem = ((PowerItem) ((FrameworkElement) sender).DataContext);
            if (powerItem.Parent != null && (!powerItem.IsFolder || (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))))
            {
                powerItem.Invoke();
                Hide();
            }

        }

    }
}
