﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using Power8.DataProviders;
using Power8.Helpers;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Power8.Views
{
    /// <summary>
    /// Singleton that represents ButtonStack - window with all those buttons, menus, lists...
    /// Inherits Window
    /// </summary>
    public partial class BtnStck : INotifyPropertyChanged
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
                    //Wait for MainWindow in case InitTree thread executed faster than it appeared
                    //This is needed because it's Main window who proceses system drive notifications
                    //So we need it's handle which is available when it's initialized
                    SettingsManager.BgrThreadLock.WaitOne();
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
        //Bindings sometimes are executed in parallel. So this dictionary contains data items for menued buttons
        private static readonly Dictionary<string, PowerItem> SpecialItems = new Dictionary<string, PowerItem>();

        //The collection of search results
        private readonly ObservableCollection<PowerItem> _searchData = new ObservableCollection<PowerItem>();
        //The view of search results
        private readonly ListCollectionView _searchView;

        /// <summary>
        /// Occurs when The Run button is pressed by user in Instance window.
        /// Used as centralized handling by MainWindow.
        /// </summary>
        public event EventHandler RunCalled;
        /// <summary>
        /// Occurs when the ButtonStack instance is created and all .ctor stuff is executed.
        /// This, among other stuff, includes lazy initialization of all data roots in 
        /// PowerItemTree and MfuList.
        /// Occurs only once as BtnStck is written as singleton (need 2 start menu instances?) 
        /// </summary>
        public static event EventHandler Instanciated;
        /// <summary>
        /// Implements <code>INotifyPropertyChanged</code>. Raised when a property is changed, and this
        /// property is intended to be binded to something.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
        #region Load, Unload, Show, Hide

        /// <summary>
        /// Buttonstack instance constructor. Except buidling of UI, it:
        /// - initializes search view;
        /// - subscribes to DwmCompositionChanged from system and to search events from Tree;
        /// - initializes the menu-buttons roots.
        /// After completion of instance construction, the next point of interesting is 
        /// OnSourceInitialized.
        /// </summary>
        public BtnStck()
        {
            Util.FpReset();
            InitializeComponent();

            _searchView = new ListCollectionView(_searchData);
            if(_searchView.GroupDescriptions != null)
                _searchView.GroupDescriptions.Add(new PropertyGroupDescription("Root.FriendlyName"));

            App.Current.DwmCompositionChanged += (app, e) => this.MakeGlassWpfWindow();
            PowerItemTree.WinSearchThreadCompleted += HandleSearch;
            PowerItemTree.WinSearchThreadStarted += HandleSearch;
            SettingsManager.ControlPanelByCategoryChanged += OnControlPanelByCategoryChanged;
            SettingsManager.DynamicLayoutChanged += (sender, e) => FirePropChanged("IsWindowAtTopOfScreen");
            SettingsManager.StartMenuStyleChanged += (sender, args) => FirePropChanged("StartMenuOldStyle");

            foreach (var mb in GetAllMenuButtons())
                mb.Item = GetSpecialItems(mb.Name);

            folderButtons.DataContext = SettingsManager.Instance;
            ButtonSwitchUser.Visibility = CheckForTsDiscon() ? Visibility.Visible : Visibility.Collapsed;
        }

// ReSharper disable RedundantAssignment
        /// <summary>
        /// WM filter hook. Curently used only to prevent arrow sizing cursors
        /// from appear at edges of the window. Uses both Un/Handled and 
        /// DefaultProc models from Windows Shell Team guidelines.
        /// Read MSDN about WndProc function on the method parameters.
        /// </summary>
        /// <returns>NULL ptr if not handled, HT* constant otherwise.</returns>
        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;
            if (msg == (int)API.WM.NCHITTEST)
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
        /// <summary>
        /// Rises SourceInitialized event, when unmanaged source window becomes availabe,
        /// then tries to create glass sheet around and adds a message filter <see cref="WndProc"/>
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.MakeGlassWpfWindow();
            this.RegisterHook(WndProc);
        }
        /// <summary>
        /// Overrides closing by hiding window unless P8 shuts doen now.
        /// </summary>
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = !MainWindow.ClosedW;
            if (e.Cancel)
                Hide();
            else
                Instance = null;
        }
        /// <summary>
        /// Hides window when it loses input focus
        /// </summary>
        private void WindowDeactivated(object sender, EventArgs e)
        {
            Hide();
            SearchBox.Text = string.Empty;
        }

        #endregion

        #region Handlers

        /// <summary>
        /// Handler of Hibernate button. Calls SetSuspendState() to put PC to hibernation
        /// </summary>
        private void ButtonHibernateClick(object sender, RoutedEventArgs e)
        {
            SetSuspendState(PowerState.Hibernate, sender);
        }
        /// <summary>
        /// Handler of Sleep button. Calls SetSuspendState() to put PC to sleep
        /// </summary>
        private void ButtonSleepClick(object sender, RoutedEventArgs e)
        {
            SetSuspendState(PowerState.Suspend, sender);
        }
        /// <summary>
        /// Handler of Shutdown button. Calls shutdown.exe to turn PC off
        /// </summary>
        private void ButtonShutdownClick(object sender, RoutedEventArgs e)
        {
            LaunchShForced("-s", sender);
        }
        /// <summary>
        /// Handler of Restart button. Calls shutdown.exe to reboot PC
        /// </summary>
        private void ButtonRestartClick(object sender, RoutedEventArgs e)
        {
            LaunchShForced("-r", sender);
        }
        /// <summary>
        /// Handler of Log Off button. Calls shutdown.exe to log current user off
        /// </summary>
        private void ButtonLogOffClick(object sender, RoutedEventArgs e)
        {
            LaunchShForced("-l", sender);
        }
        /// <summary>
        /// Handler of Lock button. Calls LockWorkStation to lock the user session
        /// </summary>
        private void ButtonLockClick(object sender, RoutedEventArgs e)
        {
            StartConsoleHidden("rundll32.exe", "user32.dll,LockWorkStation");
        }
        /// <summary>
        /// Handler of ScreenSave button. Sends SC_SREENSAVE to Desktop to start the screensaver
        /// </summary>
        private void ButtonScreensaveClick(object sender, RoutedEventArgs e)
        {
            API.SendMessage(API.GetDesktopWindow(), API.WM.SYSCOMMAND, (int)API.SC.SCREENSAVE, 0);
        }
        /// <summary>
        /// Handler of Run button. Raises event RunCalled in case any handler is attached
        /// </summary>
        private void ButtonRunClick(object sender, RoutedEventArgs e)
        {
            var handler = RunCalled;
            if (handler != null)
                handler(this, null);
        }
        /// <summary>
        /// Handler of Switch User button. Executes system "Switch user" command application
        /// </summary>
        private void ButtonSwitchUserClick(object sender, RoutedEventArgs e)
        {
            StartConsoleHidden("tsdiscon.exe", null);
        }

        //------------------------------------------

        /// <summary>
        /// Handler that is called when a context menu is opened over Start menu or 
        /// MFU list, or JL item of MFU.
        /// </summary>
        /// <param name="sender">Context menu that is opening</param>
        /// <param name="e">Event Args that have the related power item deep inside</param>
        private void AllItemsMenuRootContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            App.Current.MenuDataContext = Util.ExtractRelatedPowerItem(e);
        }

        //------------------------------------------

        /// <summary>
        /// Starts or cancels asynchronous search and sets the data source for MFU list 
        /// depending on text in search field
        /// </summary>
        private void SearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            PowerItemTree.SearchTreeCancel(); //first always cancel the current search
            var q = SearchBox.Text.Trim().ToLowerInvariant(); //ignore spaces an case
            if (!String.IsNullOrWhiteSpace(q) && q.Length > 1) //we can probably start search
            {
                //Clear selection always before (re)starting search or preparing the WebSearch.
                //For the Search this will help P8 to handle Enter properly, because sometimes,
                //if you're REALLY fast, UI is repainted BEFORE Items collection is being actually 
                //updated, along with SelectedItem, so you see and try launching item different 
                //from one being actually launched.
                dataGrid.SelectedIndex = -1;
                dataGrid.SelectedItem = null;
                if (q[1] != ' ') //not web search
                {
                    dataGrid.ItemsSource = _searchView; //switch MFU list to search results and kisk search invoker
                    var t = PowerItemTree.SearchTreeCancel();
                    Util.ForkPool(() => PowerItemTree.SearchTree(q, _searchData, ExpandGroup, t), "Search root for " + q);
                }
                //else{} //web search - no actions from our side here, wait for enter
            }
            else //no search
            {
                dataGrid.ItemsSource = MfuItems; //show MFU list
                SearchMarker.Visibility = Visibility.Hidden; //Hide search marker if any
            }
        }
        /// <summary>
        /// Searches for the visual group in search results that is represented by 
        /// a PowerItem passed and ensures it's items are displayed.
        /// </summary>
        /// <param name="root">The PowerItem that serves as group root for the
        /// collection generated by one of the search threads. This can be obtained
        /// by calling Root property of any item that was actually put to the 
        /// collection. Obviously, different search threads search items using  
        /// different search roots which (roots) will then be used as group root
        /// for search results as well.</param>
        /// <param name="token">CancellationToken that can cancel the thread.</param>
        /// <remarks>From the visual tree of MFU list 
        /// searches for the first Expander control 
        /// under the ContentPresenter control 
        /// with Content that ReferenceEquals to the Group in SearchView
        /// whose (Group's) Name value-equals to the passed root's FriendlyName.
        /// Cancellation token is checked twice during the execution.
        /// Expander is searched and expanded only if the number of items 
        /// in related group is 20 or less (performance concideration).</remarks>
        private void ExpandGroup(PowerItem root, CancellationToken token)
        {
            var expander = GetExpanderForRoot(root, token);
            if (expander != null)                   //Expander was found?
            {
                expander.IsExpanded = true;         //expand it and set the selection
                if (dataGrid.SelectedIndex == -1)
                    dataGrid.SelectedIndex = 0;     //TODO: probably not 0, but idx(first available)?
            }
        }
        /// <summary>
        /// Switches the state of expander in search view from open to closed and back again
        /// </summary>
        /// <param name="root">Can be the <code>PowerItem</code> that lies in search view</param>
        private void ExpandCollapseGroup(PowerItem root)
        {
            var expander = GetExpanderForRoot(root, CancellationToken.None);
            if (expander != null)                   //Expander was found?
                expander.IsExpanded ^= true;        //expand it and set the selection
        }
        /// <summary>
        /// Tries to retrieve expander control for data grid in search state.
        /// See <code>ExpandGroup</code> method for details on parameters
        /// </summary>
        private Expander GetExpanderForRoot(PowerItem root, CancellationToken token)
        {
            if (token.IsCancellationRequested       //thread cancelled
                || _searchView.Groups == null
                || _searchView.Groups.Count == 0   //no groups available
                || root == null)                   //argument exception should be here...
                return null;
            var group = _searchView.Groups         //get group whose name equals to root's 
                .Cast<CollectionViewGroup>()       //Friendly name
                .FirstOrDefault(g => ((string)g.Name) == root.Root.FriendlyName);
            if (group == null || (group.ItemCount > 20 && token != CancellationToken.None))
                return null;                            //If no such group or it's to large to expand
            if (token.IsCancellationRequested)      // Just in case
                return null;
            return (Expander)dataGrid      //get the expander. Method is extension, so no exceptions here,
                .GetFirstVisualChildOfTypeByContent() //only null may be returned
                 .GetFirstVisualChildOfTypeByContent()
                  .GetFirstVisualChildOfTypeByContent()
                   .GetFirstVisualChildOfTypeByContent("ScrollContentPresenter")
                    .GetFirstVisualChildOfTypeByContent("ItemsPresenter")
                     .GetFirstVisualChildOfTypeByContent()
                      .GetFirstVisualChildOfTypeByContent(content: group)
                       .GetFirstVisualChildOfTypeByContent();
        }
        /// <summary>
        /// Handles specific key presses when caret is in search box:
        /// - Up and down move the selection in the grid;
        /// - Enter launches selected item in grid if there's such an item,
        /// or tries to launch command typed in search box;
        /// - Esc clears the text in search box or hides the ButtonStack 
        /// if the text has been cleared already.
        /// </summary>
        private void SearchBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                    DataGridPreviewKeyDown(sender, e);
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
                                Util.CreateProcess(data.Item1, data.Item2);
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
                case Key.Tab: //CTRL+Tab >> put path to item to search bar
                    if ((System.Windows.Forms.Control.ModifierKeys & Keys.Control) > 0)
                    {
                        DataGridPreviewKeyDown(sender, e);
                        return;
                    }
                    break;
            }
            e.Handled = false;
        }
        /// <summary>
        /// Launches doubleclicked PowerItem. does nothing if clicked not on the PowerItem.
        /// </summary>
        private void DataGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var i = Util.ExtractRelatedPowerItem(e);
            if (i != null)
                InvokeFromDataGrid(i);
        }
        /// <summary>
        /// Handles specific key presses when focus is in Data grid:
        /// - Enter starts the selected PowerItem, if any;
        /// - Tab/Shift-Tab moves focus to correct controls so that grid doesn't 
        /// hold focus inside. There are arrow buttons for that.
        /// </summary>
        private void DataGridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var pi = dataGrid.SelectedItem as PowerItem;
            switch (e.Key)
            {
                case Key.Enter:
                    if (pi != null && !pi.AreItemsDisplayed)
                    {
                        e.Handled = true;
                        InvokeFromDataGrid(pi);
                    }
                    break;
                case Key.Tab:
                    e.Handled = true; //todo: check why CTRL is tested differently in other places
                    if (pi != null && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                    {//CTRL+Tab >> put path to item to search bar
                        var t = "\"" + PowerItemTree.GetResolvedTarget(pi) + "\" ";
                        SearchBox.Text = t;
                        SearchBox.Focus();
                        SearchBox.CaretIndex = t.Length;
                    }
                    else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        AllItemsMenuRoot.Focus(); //SHIFT+Tab >> focus backwards
                    }
                    else
                    {
                        SearchBox.Focus(); //Tab >> focus forwards
                    }
                    break;
                case Key.Up:
                case Key.Down:
                    if (pi != null && pi.AreItemsDisplayed)
                        return; //Do not handle keys when JL is in focus
                    //Not in search view when we press Up/Down
                    if ((System.Windows.Forms.Control.ModifierKeys & Keys.Control) > 0 && dataGrid.ItemsSource.Equals(MfuItems))
                    {//with CTRL
                        if (dataGrid.SelectedIndex > -1 && SettingsManager.Instance.MfuIsCustom)
                        { //...and we have something selected, and we're in custom MFU...
                            var si = dataGrid.SelectedItem as PowerItem; //...and more than 1 in (un) pinned group
                            if (si != null && MfuItems.Count(m => m.IsPinned == si.IsPinned) > 1)
                            { //=> move item itself
                                e.Handled = true;
                                dataGrid.Focus(); //.Net 4.5 hack. Datagrid in .Net4.5 doesn't switch focus to new row
                                int increment = (e.Key == Key.Up ? -1 : 1);
                                int i = dataGrid.SelectedIndex;
                                PowerItem target = null;
                                do //search for nearest item with same pinning state
                                {
                                    i += increment;
                                    if (i == -1)
                                        i = MfuItems.Count - 1;
                                    else if (i == MfuItems.Count)
                                        i = 0;
                                    if (MfuItems[i].IsPinned == si.IsPinned)
                                        target = MfuItems[i];
                                } while (target == null);
                                MfuList.MoveCustomListItem(si, target);
                            }
                        }
                    }
                    else if ((System.Windows.Forms.Control.ModifierKeys & Keys.Control) == 0)//Not with CTRL,
                    {// regardless of source => just move selection
                        e.Handled = true;
                        var idx = dataGrid.SelectedIndex + (e.Key == Key.Up ? -1 : 1);
                        if (idx < 0)
                            idx = dataGrid.Items.Count - 1;
                        if (idx >= dataGrid.Items.Count)
                            idx = 0;
                        dataGrid.SelectedIndex = idx;
                    }
                    if (dataGrid.SelectedItem != null) //when in search view and no hits/in clear custom list
                        dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                    break;
                case Key.P:
                    if (pi != null && pi.IsMfuChild)
                    {
                        e.Handled = true;
                        pi.IsPinned ^= true;
                        PinInternal(pi);
                        dataGrid.Focus();
                    }
                    break;
                case Key.X:
                    if (pi != null && dataGrid.ItemsSource.Equals(_searchView))
                    {
                        e.Handled = true;
                        ExpandCollapseGroup(pi);
                        dataGrid.Focus();
                    }
                    break;
                case Key.Right:
                case Key.Left:
                    if (pi != null)
                    {
                        e.Handled = true;
                        pi.AreItemsDisplayed ^= true;
                    }
                    break;
            }
        }
        /// <summary>
        /// Handles search event raised by Windows Serach threads.
        /// Works with UI thus invokes Main Dispatcher for real work.
        /// </summary>
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
        /// <summary>
        /// Handles click on Pin icon. Pins or unpins element in the collection
        /// </summary>
        private void PinClick(object sender, EventArgs e)
        {
            var pi = Util.ExtractRelatedPowerItem(e);
            PinInternal(pi);
        }
        /// <summary>
        /// Handles the event that occurs when the elements of jump list are shown for 
        /// an item in the Recent list. The code sence: it moves focus so it is pointed to the 
        /// "menu item" representing the [>] jumplist root's 1st child item
        /// </summary>
        private void JlShown(object sender, RoutedEventArgs e)
        {
            ((System.Windows.Controls.MenuItem)sender).Focus();
            ((System.Windows.Controls.MenuItem)sender).MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
        }

        //------------------------------------------

        /// <summary>
        /// Update Control Panel menu button when ControlPanelByCategoryChanged event occured.
        /// </summary>
        void OnControlPanelByCategoryChanged(object sender, EventArgs e)
        {
            SpecialItems.Remove("ControlPanel");
            PowerItemTree.RefreshControlPanelRoot();
            ControlPanel.Item = GetSpecialItems("ControlPanel");
        }

        //------------------------------------------

        /// <summary>
        /// Handles the click on Apps button. Shows and focuses the popup.
        /// </summary>
        private void ButtonAppsMenuClick(object sender, RoutedEventArgs e)
        {
            AppsMenuPopup.MaxWidth = Screen.FromHandle(this.GetHandle()).Bounds.Width / 1.5;
            AppsMenuPopup.IsOpen = true;
            AppsMenuItems.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
        /// <summary>
        /// Handles the click on Metro App button (any button in Apps popup)
        /// </summary>
        private void AppButtonClick(object sender, RoutedEventArgs e)
        {
            Util.ExtractRelatedPowerItem(e).Invoke();
        }
        /// <summary>
        /// Allows to exit the Apps popup by pressing the Esc button
        /// </summary>
        private void Button_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                AppsMenuPopup.IsOpen = false;
                AppsMenu.Focus();
            }
        }
        #endregion

        #region Helpers

        /// <summary>
        /// Launches Shutdown.exe with command passed as argument.
        /// If corresponding setting set, -f key is added to command.
        /// If command is not "-l", adds "-t 0" as well.
        /// Console window isn't shown.
        /// </summary>
        /// <param name="arg">Shutdown command, like "-s", "-r", "-l".</param>
        /// <param name="sender">The Button started action</param>
        private static void LaunchShForced(string arg, object sender)
        {
            if (PowerActionConfirmed((ContentControl) sender))
                StartConsoleHidden("shutdown.exe", arg +
                                                   (SettingsManager.Instance.ForcePowerActions ? " -f" : "") +
                                                   (arg == "-l" ? "" : " -t 0"));
        }

        private static void SetSuspendState(PowerState state, object sender)
        {
            if (PowerActionConfirmed((ContentControl) sender))
                System.Windows.Forms.Application.SetSuspendState(state,
                                                                 SettingsManager.Instance.ForcePowerActions,
                                                                 false);
        }

        private static bool PowerActionConfirmed(ContentControl sender)
        {
            return !SettingsManager.Instance.ConfirmPowerActions ||
                   new ConfirmActionWindow(sender.Content).ShowDialog().GetValueOrDefault();
        }

        /// <summary>
        /// Executes command without showing the console window or other windows
        /// </summary>
        /// <param name="exe">Application to launch</param>
        /// <param name="args">Command to execute</param>
        private static void StartConsoleHidden(string exe, string args)
        {
            var si = new ProcessStartInfo(exe, args) {CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden};
            Util.CreateProcess(startInfo: si);
        }
        /// <summary>
        /// Overrides Focus() to forward it to the search bar, 
        /// so when window is activated focus always goes there
        /// </summary>
        new public void Focus()
        {
            base.Focus();
            SearchBox.Focus();
            SearchMarker.Visibility = Visibility.Hidden;
        }
        /// <summary>
        /// Returns Root PowerItem based on the passed name of Menued button.
        /// Stores generated reference in cache so that no multiple comparisions 
        /// happens each time the binding occurs
        /// </summary>
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
                        var x = PowerItemTree.ImmersiveRoot; //todo!
                        break;
                    default:
                        return null;
                }
                SpecialItems[containerName] = mcItem;
            }
#if DEBUG
            Log.Raw("done in " + s.ElapsedMilliseconds, containerName);
            s.Stop();
#endif
            return SpecialItems[containerName];
        }
        /// <summary>
        /// Safely invokes passed item and hides the butonstack - as it is done for the grid
        /// Used in other code as well, like in the search bar handlers, because they behave 
        /// like invoking something from the grid.  
        /// </summary>
        /// <param name="item">Grid's selected item (usually)</param>
        private void InvokeFromDataGrid(PowerItem item)
        {
            try
            {
                PowerItemTree.SearchTreeCancel(); //if search thread is really slow compared to this UI one,
                //the following call may execute before search was finished, and MFUList may receive
                //"process launched" event and update itself, which will cause InvalidOperationException
                item.Invoke();
            }
            catch (Exception ex)
            {
                Util.DispatchCaughtException(ex);
            }
            Hide();
        }
        /// <summary>
        /// Returns enumerable collection of all Menued butons for current window
        /// </summary>
        private IEnumerable<MenuedButton> GetAllMenuButtons()
        {
            return dataGridHeightMeasure.Children.OfType<MenuedButton>();
        }
        /// <summary>
        /// Checks wheather tsdiscon utility is available on system
        /// </summary>
        private static bool CheckForTsDiscon()
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            var wDir = Environment.GetEnvironmentVariable("windir");
            if (path == null || wDir == null)
                return false;

            var paths = path.Split(';').Select(p => p.ToLower().TrimEnd('\\'));
            var wDirs = System.IO.Directory.GetDirectories(wDir)
                                           .Select(d => d.ToLower())
                                           .Where(x => x.Contains("system"));
            paths = paths.Union(wDirs);
            return paths.Any(p => System.IO.File.Exists(p + @"\tsdiscon.exe"));
        }
        /// <summary>
        /// Routine to be performed after some PowerItem's IsPinned property was changed
        /// </summary>
        /// <param name="pi">Item whose property has changed</param>
        private void PinInternal(PowerItem pi)
        {
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
        /// <summary>
        /// Raises PropertyChanged event.
        /// </summary>
        /// <param name="propName">String representing property name</param>
        private void FirePropChanged(string propName)
        {
            var h = PropertyChanged;
            if (h != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        #endregion

        #region Bindable props

        /// <summary>
        /// Bindable property that wraps StartMenuOldStyle property from the settings.
        /// </summary>
        public bool StartMenuOldStyle
        {
            get { return SettingsManager.Instance.StartMenuOldStyle; }
        }
        /// <summary>
        /// Source for Start Menu
        /// </summary>
        public ObservableCollection<PowerItem> Items
        {
            get { return PowerItemTree.StartMenuRoot; }
        }

        /// <summary>
        /// Source for Apps Menu
        /// </summary>
        public ObservableCollection<PowerItem> AppsItems
        {
            get
            {
                return PowerItemTree.ImmersiveRoot.Items;
            }
        }
        /// <summary>
        /// Source for Recent list
        /// </summary>
        public ObservableCollection<PowerItem> MfuItems
        {
            get { return MfuList.StartMfu; }
        }
        //backing field
        private bool _isWindowAtTopOfScreen = true;
        /// <summary>
        /// Notifies buttonstack whether the start button was moved to above or below
        /// the center of the /*earth*/ screen. This <i>may</i> influence on whether
        /// Start menu and Search bar exchange their locations.
        /// </summary>
        public bool IsWindowAtTopOfScreen
        {
            get { return !SettingsManager.Instance.DynamicLayout || _isWindowAtTopOfScreen; }
            set
            {
                if(value == _isWindowAtTopOfScreen)
                    return;
                _isWindowAtTopOfScreen = value;
                FirePropChanged("IsWindowAtTopOfScreen");
            }
        }

        /// <summary>
        /// Conditionally hides or shows Pseudo-start button depending on OS
        /// </summary>
        public Visibility IsPseudoStartButtonVisible
        {
            get { return ImmersiveAppsProvider.Any ? Visibility.Visible : Visibility.Collapsed; }
        }

        public System.Windows.Media.Brush PseudoStartButtonColor
        {
            get { return Util.OsIs.TenOrMore ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White; }
        }
        #endregion
    }
}
