using Power8.Views;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
#if DEBUG
using System.Diagnostics;
#endif

namespace Power8
{
    /// <summary>
    /// Bootstrapper for the application
    /// </summary>
    public partial class App
    {   
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

            //Move settings from previous ver
            var std = Power8.Properties.Settings.Default;
            if (!std.FirstRunDone)
            {
                std.Upgrade();
                std.Save();//FirstRunDone is updated later in Main Window code
            }

            //Initialize standard folder icon
            foreach (Environment.SpecialFolder sf in Enum.GetValues(typeof(Environment.SpecialFolder)))
            {//we just seek for appropriate special folder to get folder icon...
                bool isOk = false;
                switch (sf)
                {
                    case Environment.SpecialFolder.Desktop:
                    case Environment.SpecialFolder.DesktopDirectory:
                    case Environment.SpecialFolder.CommonDesktopDirectory:
                    case Environment.SpecialFolder.MyComputer:
                        break; //from switch
                    default:
                        var path = Environment.GetFolderPath(sf);
                        if (!File.Exists(path + @"\desktop.ini")) //we need generic folder, not the customized one
                        {
                            ImageManager.GetImageContainerSync(new PowerItem {Argument = path, IsFolder = true},
                                                           API.Shgfi.SMALLICON);
                            isOk = true;
                        }
                        break; //from switch
                }
                if(isOk)
                    break; //from foreach
            }

            //Build tree
            Util.Fork(PowerItemTree.InitTree, "InitTree").Start();

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
