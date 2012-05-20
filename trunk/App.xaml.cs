using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace Power8
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        internal readonly Thread InitTreeThread = new Thread(PowerItemTree.InitTree) {Name = "InitTree"};
        
        public App()
        {
            if(Environment.OSVersion.Version.ToString().StartsWith("6.0"))
            {
                MessageBox.Show(
                    Power8.Properties.Resources.Err_VistaDetected,
                    Power8.Properties.Resources.Stg_AppShortName, MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(2);
            }

            Util.MainDisp = Dispatcher;
            //Error handling and detection
#if DEBUG
            var l = new TextWriterTraceListener(Environment.ExpandEnvironmentVariables(@"%temp%\p8log.txt"));
            Debug.AutoFlush = true;
            Debug.Listeners.Add(l);
#endif
            DispatcherUnhandledException += (sender, args) => MessageBox.Show(args.Exception.ToString());
            //Move settings from previous ver
            var std = Power8.Properties.Settings.Default;
            if (!std.FirstRunDone)
            {
                std.Upgrade();
                std.FirstRunDone = true;
                std.Save();
            }
            //Initialize standard folder icon
            foreach (Environment.SpecialFolder sf in Enum.GetValues(typeof(Environment.SpecialFolder)))
            {
                bool isOk = false;
                switch (sf)
                {
                    case Environment.SpecialFolder.Desktop:
                    case Environment.SpecialFolder.DesktopDirectory:
                    case Environment.SpecialFolder.CommonDesktopDirectory:
                    case Environment.SpecialFolder.MyComputer:
                        break;
                    default:
                        var path = Environment.GetFolderPath(sf);
                        if (!File.Exists(path + @"\desktop.ini"))
                        {
                            ImageManager.GetImageContainerSync(new PowerItem {Argument = path, IsFolder = true},
                                                           API.Shgfi.SMALLICON);
                            isOk = true;
                        }
                        break;
                }
                if(isOk)
                    break;
            }
            //Build tree
            InitTreeThread.Start();
            //react on DwmCompositionChanged event
            ComponentDispatcher.ThreadFilterMessage += WndProc;
        }

// ReSharper disable RedundantAssignment
        private void WndProc(ref MSG msg, ref bool handled)
        {
            if (msg.message == (int)API.WM.DWMCOMPOSITIONCHANGED)
            {
                OnDwmCompositionChanged();
                handled = true;
                return;
            }
            handled = false;
        }
// ReSharper restore RedundantAssignment

        public event EventHandler DwmCompositionChanged ;

        protected virtual void OnDwmCompositionChanged()
        {
            var handler = DwmCompositionChanged;
            if (handler != null) handler(this, null);
        }


        private void RunRunasOpenOpencommonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var n = ((MenuItem) sender).Name;
                if (n == "AppRun" || n == "AppOpenFolder")
                    Util.ExtractRelatedPowerItem(sender).Invoke();
                else
                    Util.ExtractRelatedPowerItem(sender).InvokeVerb(API.SEIVerbs.SEV_RunAsAdmin);
            }
            catch (Exception ex)
            {
                Util.DispatchCaughtException(ex);
            }
        }

        private void ShowPropertiesClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var pi = Util.ExtractRelatedPowerItem(sender);
                var info = new API.ShellExecuteInfo
                {
                    fMask =
                        API.SEIFlags.SEE_MASK_INVOKEIDLIST | API.SEIFlags.SEE_MASK_NOCLOSEPROCESS |
                        API.SEIFlags.SEE_MASK_FLAG_NO_UI | API.SEIFlags.SEE_MASK_NOASYNC,
                    hwnd = BtnStck.Instance.GetHandle(),
                    lpVerb = API.SEIVerbs.SEV_Properties,
                    lpFile = PowerItemTree.GetResolvedArgument(pi, false),
                    nShow = API.SWCommands.HIDE
                };
                if (pi.IsLink && ((MenuItem)sender).Name == "AppShowTargetProperties")
                    info.lpFile = Util.ResolveLink(info.lpFile);
                var executer = new Util.ShellExecuteHelper(info);
                if (!executer.ShellExecuteOnSTAThread())
                    throw new ExternalException(string.Format(
                        Power8.Properties.Resources.Err_ShellExecExErrorFormatString, executer.ErrorCode, executer.ErrorText));
            }
            catch (Exception ex)
            {
                Util.DispatchCaughtException(ex);
            }
        }

        private void OpenContainerClick(object sender, RoutedEventArgs e)
        {
            var item = Util.ExtractRelatedPowerItem(sender);
            string arg = null;
            if (!item.IsNotControlPanelFlowItem)
            {
                var executor = Util.GetOpenCommandForClass(item.Argument);
                if (executor != null && File.Exists(executor.Item1))
                    arg = executor.Item1;
            }
            if(arg == null)
                arg = PowerItemTree.GetResolvedArgument(item, false);
            if (item.IsLink && ((MenuItem)sender).Name == "AppOpenTargetContainer")
                arg = Util.ResolveLink(arg);
            Util.StartExplorer("/select,\"" + arg + "\"");
        }


        public object MenuDataContext
        {
            get { return ((ContextMenu) Resources["fsMenuItemsContextMenu"]).DataContext; }
            set { ((ContextMenu) Resources["fsMenuItemsContextMenu"]).DataContext = value; }
        }

        public static new App Current
        {
            get { return (App) Application.Current; }
        }
    }
}
