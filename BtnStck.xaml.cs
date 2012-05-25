using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Linq;

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
            get
            {
                if (_instance == null)
                {
                    _instance = new BtnStck();
                    var h = Instanciated;
                    if (h != null)
                        h(null, null);
                }
                return _instance;
            }
            private set { _instance = value; }
        }
        public static bool IsInitDone { get { return _instance != null; } }
        private static readonly Dictionary<string, PowerItem> SpecialItems = new Dictionary<string, PowerItem>();

        private readonly MenuItemClickCommand _cmd = new MenuItemClickCommand();

        public event EventHandler RunCalled;
        public static event EventHandler Instanciated;

        #region Load, Unload, Show, Hide

        public BtnStck()
        {
            InitializeComponent();
            App.Current.DwmCompositionChanged += (app, e) => this.MakeGlassWpfWindow();
            foreach (var mb in folderButtons.Children.OfType<MenuedButton>())
                mb.Item = GetSpecialItems(mb.Name);
        }

// ReSharper disable RedundantAssignment
        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;
            if (msg == (uint)API.WM.NCHITTEST)
            {
                handled = true;
                var htLocation = API.DefWindowProc(hwnd, msg, wParam, lParam);
                switch ((API.HT)Enum.Parse(typeof(API.HT), htLocation.ToString()))
                {
                    case API.HT.BOTTOM:
                    case API.HT.BOTTOMLEFT:
                    case API.HT.BOTTOMRIGHT:
                    case API.HT.LEFT:
                    case API.HT.RIGHT:
                    case API.HT.TOP:
                    case API.HT.TOPLEFT:
                    case API.HT.TOPRIGHT:
                        htLocation = new IntPtr((int)API.HT.BORDER);
                        break;
                }
                return htLocation;
            }
            return IntPtr.Zero;
        }
// ReSharper restore RedundantAssignment

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.MakeGlassWpfWindow();
            this.RegisterHook(WndProc);
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

        #endregion

        #region Handlers

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
            API.SendMessage(API.GetDesktopWindow(), API.WM.SYSCOMMAND, (int)API.SC.SCREENSAVE, 0);
        }

        private void AllItemsMenuRootContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            App.Current.MenuDataContext = Util.ExtractRelatedPowerItem(e);
        }
        
        private void ButtonRunClick(object sender, RoutedEventArgs e)
        {
            var handler = RunCalled;
            if (handler != null)
                handler(this, null);
        }

        #endregion

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

        new public void Focus()
        {
            base.Focus();
            SearchBox.Focus();
        }

        private static PowerItem GetSpecialItems(string containerName)
        {
#if DEBUG
            var s = Stopwatch.StartNew();
#endif
            if (!SpecialItems.ContainsKey(containerName))
            {
                PowerItem mcItem;
                switch (containerName)
                {
                    case "MyComputer":
                        mcItem = PowerItemTree.MyComputerRoot;
                        break;
                    case "AdminTools":
                        mcItem = PowerItemTree.AdminToolsRoot;
                        break;
                    case "Libraries":
                        mcItem = PowerItemTree.LibrariesRoot;
                        break;
                    case "ControlPanel":
                        mcItem = PowerItemTree.ControlPanelRoot;
                        break;
                    case "NetItems":
                        mcItem = PowerItemTree.NetworkRoot;
                        break;
                    default:
                        return null;
                }
                SpecialItems[containerName] = mcItem;
            }
#if DEBUG
            Debug.WriteLine("BtnStck:GSI - done for {0} as of {1}", containerName, s.ElapsedMilliseconds);
            s.Stop();
#endif
            return SpecialItems[containerName];
        }

        #endregion

        #region Bindable props
        public ObservableCollection<PowerItem> Items
        {
            get { return PowerItemTree.StartMenuRoot; }
        } 

        public MenuItemClickCommand ClickCommand
        {
            get { return _cmd; }
        }
        #endregion

    }
}
