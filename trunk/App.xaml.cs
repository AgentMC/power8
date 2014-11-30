using System.Linq;
using Power8.Helpers;
using Power8.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
#if DEBUG
using System.Runtime.ExceptionServices;
#endif


namespace Power8
{
    /// <summary>
    /// Bootstrapper for the application
    /// </summary>
    public partial class App
    {
        public readonly Process Proc = Process.GetCurrentProcess();

        /// <summary>
        /// Application initializer. Performs compatibility check, 
        /// starts diagnostics if required, works settings around,
        /// and initializes the process of generating of internal data structures.
        /// </summary>
        public App()
        {
            if(Util.OsIs.VistaExact) //If run on shit
            {
                MessageBox.Show(
                    Power8.Properties.Resources.Err_VistaDetected,
                    Power8.Properties.NoLoc.Stg_AppShortName, MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(2); //means: OS not found
            }
            //Global error mode setter - in 1st place to fix nasty startup error on Win10x86
            API.SetErrorMode(API.ErrMode.FailCriticalErrors);
            
            //Power8s in our session but with different pid
            foreach (var p in Process.GetProcessesByName("Power8")
                                     .Where(p => p.SessionId == Proc.SessionId && p.Id != Proc.Id))
            {
                p.Kill();
            }

            Util.MainDisp = Dispatcher; //store main thread dispatcher. Widely used in Application.

#if DEBUG
            //System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("ru");

            //Error handling and detection
            var l = new TextWriterTraceListener(Environment.ExpandEnvironmentVariables(@"%temp%\p8log.txt"));
            l.Write("\r\n\r\nPower8 Log opened at " + DateTime.Now + "\r\n\r\n");
            Debug.AutoFlush = true;
            Debug.Listeners.Add(l);
#endif
            
            DispatcherUnhandledException += (sender, e) => Util.DispatchUnhandledException(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandled;

            var dbRoot = Util.GetSettingsIndependentDbRoot();
            try
            {
                var ids = Directory.GetFiles(dbRoot, "*" + ClientIDExtension);
                string clientId;
                if (ids.Length == 0)
                {
                    clientId = Guid.NewGuid().ToString();
                    File.Create(dbRoot + "\\" + clientId + ClientIDExtension);
                }
                else
                {
                    clientId = Path.GetFileNameWithoutExtension(ids[0]);
                }
                Analytics.Init(TrackID, clientId, Power8.Properties.NoLoc.Stg_AppShortName,
                    Util.GetAppVersion().ToString());
            }
            catch (Exception ex)
            {
                Log.Raw("Unable to read client ID to init analytics: " + ex);
            }

            //Move settings from previous ver
            var std = Power8.Properties.Settings.Default;
            if (!std.FirstRunDone)
            {
                try
                {
                    std.Upgrade();
                }
                catch (Exception ex)
                {
                    Log.Raw("Unable to upgrade settings: " + ex);
                }
                std.Save();//FirstRunDone is updated later in Main Window code
                Analytics.PostEvent(Analytics.Category.Deploy, std.FirstRunDone ? "Update" : "Fresh", null, 1);
            }

            //Initialize standard folder icon
            ImageManager.GetImageContainerSync(new PowerItem { Argument = dbRoot, IsFolder = true }, API.Shgfi.SMALLICON);
            
            //Build tree
            Util.ForkPool(PowerItemTree.InitTree, "InitTree");

            //react on DwmCompositionChanged event
            ComponentDispatcher.ThreadFilterMessage += WndProc;
        }
        /// <summary>
        /// Gets the running instance of App
        /// </summary>
        public static new App Current
        {
            get { return (App) Application.Current; }
        }

        private const string ClientIDExtension = ".clientid";
        private const string TrackID = "UA-30314159-2";

        /// <summary>
        /// Handles Unhandled appdomain exception and calls the code to write that down everywhere.
        /// Undr DEBUG it also catches uncatchable exceptions
        /// </summary>
#if DEBUG
        [HandleProcessCorruptedStateExceptions]
#endif
        public void HandleUnhandled(object sender, UnhandledExceptionEventArgs e)
        {
            Util.DispatchUnhandledException(e.ExceptionObject as Exception);
        }

        #region DWM CompositionChanged event

        // ReSharper disable RedundantAssignment
        /// <summary>
        /// App WndProc. Filter. Used only to hande DWMCOMPOSITIONCHANGED event.
        /// </summary>
        /// <param name="msg">Structure with message, lparame, wparam, etc.</param>
        /// <param name="handled">IntPtr wrapped to bool as 1/0. Retval of WndProc.</param>
        private void WndProc(ref MSG msg, ref bool handled)
        {
            if (msg.message == (int)API.WM.DWMCOMPOSITIONCHANGED)
            {
                var h = DwmCompositionChanged;
                if (h != null) 
                    h(this, null);
                handled = true;
                return;
            }
            handled = false;
        }
// ReSharper restore RedundantAssignment
        /// <summary>
        /// WM_DWMCOMPOSITIONCHANGED converted to event model. e is always null.
        /// Sender is this App.
        /// </summary>
        public event EventHandler DwmCompositionChanged;

        #endregion

        //App stores global context menu as resource.
        #region Global Context menu

        /// <summary>
        /// Handles Run/Run As/Open/Open all users folder commands
        /// </summary>
        private void RunRunasOpenOpencommonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var n = ((MenuItem) sender).Name;
                if (n == "AppRun" || n == "AppOpenFolder") //TODO: maybe compare byref to menuitems?
                    Util.ExtractRelatedPowerItem(e).Invoke();
                else //Open common folder is also handled via "RunAsAdmin" command. See below.
                    Util.ExtractRelatedPowerItem(e).InvokeVerb(API.SEVerbs.RunAsAdmin);
                    //This was done to simplify the implementation. Passing this command switches
                    //the flag in ResolveItem() that exchanges discovered Common item (if exists)
                    //with User one. This relates to Start menu _folders_ explicitly and only.
                    //Along with that, this flag is passed to process start info, regardless of PowerItem 
                    //type (file/folder/link...). That type, however, influences the enabled state of 
                    //menu items, so you shouldn't be able to do something wrong.
            }
            catch (Exception ex)
            {
                Util.DispatchCaughtException(ex);
            }
        }
        /// <summary>
        /// Displays properties of a clicked object. Depending on which item
        /// was clicked, the automatic link resolution may take place.
        /// </summary>
        private void ShowPropertiesClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var info = new API.ShellExecuteInfo
                {
                    fMask = //need all them for Properties verb
                        API.SEIFlags.SEE_MASK_INVOKEIDLIST | API.SEIFlags.SEE_MASK_NOCLOSEPROCESS |
                        API.SEIFlags.SEE_MASK_FLAG_NO_UI | API.SEIFlags.SEE_MASK_NOASYNC,
                    hwnd = BtnStck.Instance.GetHandle(), //otherwise will be in background
                    nShow = API.SWCommands.HIDE, //hides some other window, kind of worker one
                    lpVerb = API.SEVerbs.Properties, 
                    lpFile = Args4PropsAndCont(Util.ExtractRelatedPowerItem(e), ((MenuItem)sender).Name)
                };
                var executer = new Util.ShellExecuteHelper(info); //Needed to be executed on STA htread
                if (!executer.ShellExecuteOnSTAThread())
                    throw new ExternalException(string.Format(
                        Power8.Properties.Resources.Err_ShellExecExErrorFormatString, executer.ErrorCode, executer.ErrorText));
            }
            catch (Exception ex)
            {
                Util.DispatchCaughtException(ex);
            }
        }
        /// <summary>
        /// Shows source PowerItem in a folder
        /// </summary>
        private void OpenContainerClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Util.StartExplorerSelect(Args4PropsAndCont(Util.ExtractRelatedPowerItem(e), ((MenuItem) sender).Name));
            }
            catch (Exception ex)
            {
                Util.DispatchCaughtException(ex);
            }
        }
        /// <summary>
        /// Handles click on "Remove item". Adds an exclusion to exclusions list.
        /// </summary>
        private void RemoveItemClick(object sender, RoutedEventArgs e)
        {
            MfuList.AddExclusion(Util.ExtractRelatedPowerItem(e));
        }
        /// <summary>
        /// Handles click on "Add item to custom list". Adds an item to user's custom MFU list.
        /// </summary>
        private void IncludeCustom(object sender, RoutedEventArgs e)
        {
            MfuList.Add2Custom(Util.ExtractRelatedPowerItem(e));
        }
        /// <summary>
        /// Handles click on "Remove item from custom list". Removes an item to user's custom MFU list.
        /// </summary>
        private void ExcludeCustom(object sender, RoutedEventArgs e)
        {
            MfuList.RemoveCustom(Util.ExtractRelatedPowerItem(e));
        }
        /// <summary>
        /// Returns string, of a Path kind, that can be passed to a system, and will
        /// represent the passed PowerItem. Depending on Caller Name, may invoke
        /// automatic Link resolution for Link PowerItems. "Denamespaces" the 
        /// passed ControlPanel item returning open command for it.
        /// </summary>
        /// <param name="item">The PowerItem which has to be located/properties for 
        /// which have to be shown.</param>
        /// <param name="callerName">String, the name of clicked menu item, hendler
        /// of which is calling this method. Recognizes "AppOpenTargetContainer" and
        /// "AppShowTargetProperties".</param>
        /// <returns>Path to binary FS object that represents the passed PowerItem or 
        /// the target of its link.</returns>
        private static string Args4PropsAndCont(PowerItem item, string callerName)
        {
            string arg = null;
            if (item.IsControlPanelChildItem)
            {
                var executor = Util.GetOpenCommandForClass(item.Argument);
                if (executor != null && File.Exists(executor.Item1))
                    arg = executor.Item1;
            }
            if (arg == null)
                arg = PowerItemTree.GetResolvedArgument(item);
            if (item.IsLink && (callerName == "AppOpenTargetContainer" 
                                || callerName == "AppShowTargetProperties"))
                arg = item.ResolvedLink;
            return arg;
        }
        /// <summary>
        /// Gets or sets Data Context for the whole menu. MUST be called in ALL 
        /// ContextMenuOpening event handlers with something like:
        /// App.Current.MDC = Util.ExtractRelatedPowerItem(e);
        /// </summary>
        public object MenuDataContext
        {
            get { return ((ContextMenu) Resources["fsMenuItemsContextMenu"]).DataContext; }
            set { ((ContextMenu) Resources["fsMenuItemsContextMenu"]).DataContext = value; }
        }

        #endregion

    }
}
