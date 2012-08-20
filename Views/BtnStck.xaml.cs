using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Power8.Commands;

namespace Power8.Views
{
    /// <summary>
    /// Singleton that represents ButtonStack - window with all those buttons, menus, lists...
    /// Inherits Window
    /// </summary>
    public partial class BtnStck
    {
        private static BtnStck _instance;
        /// <summary>
        /// Gets the instance of ButtonStack. 
        /// Not thread-safe, but the instance should be ONLY created on main dispatcher thread.
        /// </summary>
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
        /// <summary>
        /// Returns boolean indicating if ButtonStack instance is available, without requesting of one to be created in deferred way.
        /// Window however may not finish Load or Instantiate itself when this returns true, use corresponding Instance's properties
        /// to check the window status.
        /// </summary>
        public static bool IsInstantited { get { return _instance != null; } }
        //Bindings sometimes are executed in parallel. So this dictionary conteins data items for menued buttons
        private static readonly Dictionary<string, PowerItem> SpecialItems = new Dictionary<string, PowerItem>();

        //The collection of search results
        private readonly ObservableCollection<PowerItem> _searchData = new ObservableCollection<PowerItem>();
        //The view of search results
        private readonly ListCollectionView _searchView;

        public event EventHandler RunCalled;
        public static event EventHandler Instanciated;

        #region Load, Unload, Show, Hide

        public BtnStck()
        {
            InitializeComponent();

            _searchView = new ListCollectionView(_searchData);
            if(_searchView.GroupDescriptions != null)
                _searchView.GroupDescriptions.Add(new PropertyGroupDescription("Root.FriendlyName"));

            App.Current.DwmCompositionChanged += (app, e) => this.MakeGlassWpfWindow();
            foreach (var mb in folderButtons.Children.OfType<MenuedButton>().Union(dataGridHeightMeasure.Children.OfType<MenuedButton>()))
                mb.Item = GetSpecialItems(mb.Name);

            PowerItemTree.WinSearchThreadCompleted += HandleSearch;
            PowerItemTree.WinSearchThreadStarted += HandleSearch;
        }

// ReSharper disable RedundantAssignment
        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;
            if (msg == (uint)API.WM.NCHITTEST)
            {
                handled = true;
                var htLocation = API.DefWindowProc(hwnd, msg, wParam, lParam);
                switch ((API.HT)htLocation)
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
            SearchBox.Text = string.Empty;
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
        
        private void ButtonRunClick(object sender, RoutedEventArgs e)
        {
            var handler = RunCalled;
            if (handler != null)
                handler(this, null);
        }

        //------------------------------------------

        private void AllItemsMenuRootContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            App.Current.MenuDataContext = Util.ExtractRelatedPowerItem(e);
        }

        //------------------------------------------

        private void SearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            PowerItemTree.SearchTreeCancel();
            var q = SearchBox.Text.Trim().ToLowerInvariant();
            if (!String.IsNullOrWhiteSpace(q) && q.Length > 1)
            {
                if (q[1] != ' ')
                {
                    dataGrid.ItemsSource = _searchView;
                    Util.Fork(() => PowerItemTree.SearchTree(q, _searchData, ExpandGroup), "Search root for " + q).Start();
                }
                else
                {
                    dataGrid.SelectedIndex = -1;
                }
            }
            else
            {
                dataGrid.ItemsSource = MfuItems;
                SearchMarker.Visibility = Visibility.Hidden;
            }
        }

        private void ExpandGroup(PowerItem root, CancellationToken token)
        {
            if(token.IsCancellationRequested 
                || _searchView.Groups == null 
                || _searchView.Groups.Count == 0
                || root == null)
                return;
            var group = _searchView.Groups
                .Cast<CollectionViewGroup>()
                .FirstOrDefault(g => ((string) g.Name) == root.Root.FriendlyName);
            if(group == null || group.ItemCount > 20)
                return;
            if(token.IsCancellationRequested) // Just in case
                return;
            var expander = (Expander)dataGrid
                .GetFirstVisualChildOfTypeByContent()
                 .GetFirstVisualChildOfTypeByContent()
                  .GetFirstVisualChildOfTypeByContent()
                   .GetFirstVisualChildOfTypeByContent("ScrollContentPresenter")
                    .GetFirstVisualChildOfTypeByContent("ItemsPresenter")
                     .GetFirstVisualChildOfTypeByContent()
                      .GetFirstVisualChildOfTypeByContent(content: group)
                       .GetFirstVisualChildOfTypeByContent();
            if (expander != null)
            {
                expander.IsExpanded = true;
                if (dataGrid.SelectedIndex == -1)
                    dataGrid.SelectedIndex = 0;
            }
        }

        private void SearchBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
                e.Handled = true;
                switch (e.Key)
                {
                    case Key.Up:
                        if (dataGrid.Items.Count > 0)
                        {
                            dataGrid.SelectedIndex = dataGrid.SelectedIndex <= 0
                                                     ? dataGrid.Items.Count - 1
                                                     : dataGrid.SelectedIndex - 1;
                                             
                            dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                        }
                        return;
                    case Key.Down:
                        if (dataGrid.Items.Count > 0)
                        {
                            dataGrid.SelectedIndex = dataGrid.SelectedIndex >= dataGrid.Items.Count - 1
                                                     ? 0
                                                     : dataGrid.SelectedIndex + 1;
                            dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                        }
                        return;
                    case Key.Enter:
                        if (dataGrid.SelectedItem != null)
                        {
                            InvokeFromDataGrid((PowerItem)dataGrid.SelectedItem);
                        }
                        else
                        {
                            var data = Util.CommandToFilenameAndArgs(SearchBox.Text);
                            if(data != null)
                            {
                                try
                                {
                                    Process.Start(data.Item1, data.Item2);
                                }
                                catch (Exception ex)
                                {
                                    Util.DispatchCaughtException(ex);
                                }
                                Hide();
                            }
                        }
                        return;
                    case Key.Escape:
                        if(SearchBox.Text == string.Empty)
                            Close();
                        else
                            SearchBox.Text = string.Empty;
                        return;
                }
            e.Handled = false;
        }

        private void DataGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var i = Util.ExtractRelatedPowerItem(e);
            if (i != null)
                InvokeFromDataGrid(i);
        }

        private void DataGridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var pi = Util.ExtractRelatedPowerItem(e);
                if (pi != null)
                {
                    e.Handled = true;
                    InvokeFromDataGrid(pi);
                }
            }
            if (e.Key == Key.Tab)
            {
                e.Handled = true;
                if (Keyboard.IsKeyDown(Key.LeftShift))
                    AllItemsMenuRoot.Focus();
                else
                    SearchBox.Focus();
            }

        }

        private void HandleSearch(object sender, PowerItemTree.WinSearchEventArgs args)
        {
            Util.Send(() =>
                          {
                              if (args.SearchCompleted)
                              {
                                  SearchMarker.Visibility = Visibility.Hidden;
                                  ExpandGroup(args.Root, args.Token);
                              }
                              else
                              {
                                  SearchMarker.Visibility = Visibility.Visible;
                              }
                          });
        }

        private void PinClick(object sender, EventArgs e)
        {
            var pi = Util.ExtractRelatedPowerItem(e);
            try
            {
                MfuList.PinUnpin(pi);
                dataGrid.ScrollIntoView(pi);
            }
            catch (IndexOutOfRangeException)
            {
                Util.DispatchCaughtException(new Exception(Properties.Resources.Err_NoPiExtracted));
            }
        }
        #endregion

        #region Helpers
        
        private static void LaunchShForced(string arg)
        {
            StartConsoleHidden("shutdown.exe", arg + " -f" + (arg == "-l" ? "" : " -t 0"));
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

        private void InvokeFromDataGrid(PowerItem item)
        {
            try
            {
                item.Invoke();
            }
            catch (Exception ex)
            {
                Util.DispatchCaughtException(ex);
            }
            Hide();
        }

        #endregion

        #region Bindable props
        public ObservableCollection<PowerItem> Items
        {
            get { return PowerItemTree.StartMenuRoot; }
        }

        public ObservableCollection<PowerItem> MfuItems
        {
            get { return MfuList.StartMfu; }
        }

        public ObservableCollection<PowerItem> SearchData
        {
            get { return _searchData; }
        }

        private readonly MenuItemClickCommand _cmd = new MenuItemClickCommand();
        public MenuItemClickCommand ClickCommand
        {
            get { return _cmd; }
        }
        #endregion
    }
}
