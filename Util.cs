using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Application = System.Windows.Forms.Application;

namespace Power8
{
    static class Util
    {
        public static Dispatcher MainDisp;

        private static readonly StringBuilder Buffer = new StringBuilder(1024);


        public static void Send(Delegate method)
        {
            MainDisp.Invoke(DispatcherPriority.Render, method);
        }

        public static void Post(Delegate method)
        {
            MainDisp.BeginInvoke(DispatcherPriority.Background, method);
        }

        public static T Eval<T>(Func<T> method)
        {
            return (T) MainDisp.Invoke(DispatcherPriority.DataBind, method);
        }


        public static IntPtr GetHandle(this Window w)
        {
            return new WindowInteropHelper(w).Handle;
        }

        public static HwndSource GetHwndSource(this Window w)
        {
            return HwndSource.FromHwnd(w.GetHandle());
        }

        public static IntPtr MakeGlassWpfWindow(this Window w)
        {
            var source = w.GetHwndSource();
            if (source.CompositionTarget != null) 
                source.CompositionTarget.BackgroundColor = Colors.Transparent;
            if (Environment.OSVersion.Version.Major >= 6) 
                MakeGlass(source.Handle);
            return source.Handle;
        }
        
        public static void MakeGlass(IntPtr hWnd)
        {
            var bbhOff = new API.DwmBlurbehind
                            {
                                dwFlags = API.DwmBlurbehind.DWM_BB_ENABLE | API.DwmBlurbehind.DWM_BB_BLURREGION,
                                fEnable = false,
                                hRegionBlur = IntPtr.Zero
                            };
            API.DwmEnableBlurBehindWindow(hWnd, bbhOff);
            API.DwmExtendFrameIntoClientArea(hWnd, new API.Margins { cxLeftWidth = -1, cxRightWidth = 0, cyTopHeight = 0, cyBottomHeight = 0 });
        }

        public static void RegisterHook(this Window w, HwndSourceHook hook)
        {
            w.GetHwndSource().AddHook(hook);
        }



        public static PowerItem ExtractRelatedPowerItem(object o)
        {
            if (o is MenuItem)
                return (PowerItem)((MenuItem)o).DataContext;
            if (o is ContextMenuEventArgs)
            {
                var mi = o.GetType()
                          .GetProperty("TargetElement",
                                       BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty)
                          .GetValue(o, null) as MenuItem;
                if(mi != null)
                    return (PowerItem)(mi.DataContext);
            }
            return null;
        }

        public static string ResolveLink(string link)
        {
            var shLink = new API.ShellLink();
            ((API.IPersistFile)shLink).Load(link, 0);
            lock (Buffer)
            {
                API.WIN32_FIND_DATAW sd;
                ((API.IShellLink) shLink).GetPath(Buffer, 512, out sd, API.SLGP_FLAGS.SLGP_UNCPRIORITY);
                return Buffer.ToString();
            }
        }

        public static string GetLocalizedStringResourceIdForClass(string clsidOrApiShNs)
        {
            return GetResourceIdForClassCommon(clsidOrApiShNs, "", "LocalizedString");
        }

        public static string GetDefaultIconResourceIdForClass(string clsidOrApiShNs)
        {
            return GetResourceIdForClassCommon(clsidOrApiShNs, "\\DefaultIcon", "");
        }

        private static string GetResourceIdForClassCommon(string clsidOrApiShNs, string subkey, string valueName)
        {
// ReSharper disable EmptyGeneralCatchClause
            try
            {
                using (var k = Microsoft.Win32.Registry.ClassesRoot
                        .OpenSubKey("CLSID\\" + NameSpaceToGuidWithBraces(clsidOrApiShNs) + subkey, false))
                {
                    if (k != null)
                        return ((string)k.GetValue(valueName, null)).TrimStart('@');
                }
            }
            catch (Exception){}
// ReSharper restore EmptyGeneralCatchClause
            return null;
        }

        public static string NameSpaceToGuidWithBraces(string ns)
        {
            ns = ns.Substring(ns.LastIndexOf('\\') + 1);
            ns = ns.TrimStart(':', '\\');
            if (!ns.StartsWith("{"))
                ns = "{" + ns + "}";
            return ns;
        }

        public static string ResolveStringResource(string localizeableResourceId)
        {
            var resData = ResolveResourceCommon(localizeableResourceId);
            if (resData.Item1 != IntPtr.Zero)
            {
                lock (Buffer)
                {
                    var number = API.LoadString(resData.Item1, resData.Item2, Buffer, Buffer.Capacity);
                    API.FreeLibrary(resData.Item1);
                    if (number > 0)
                        return Buffer.ToString();
                }
            }
            return null;
        }

        public static IntPtr ResolveIconicResource(string localizeableResourceId)
        {
            var resData = ResolveResourceCommon(localizeableResourceId);
            if (resData.Item1 != IntPtr.Zero)
            {
                var icon = API.LoadIcon(resData.Item1, resData.Item2);
                API.FreeLibrary(resData.Item1);
                return icon;
            }
            return IntPtr.Zero;
        }

        private static Tuple<IntPtr, uint> ResolveResourceCommon(string resourceString)
        {
            //ResId = %ProgramFiles%\Windows Defender\EppManifest.dll,-1000
            var lastCommaIdx = resourceString.LastIndexOf(',');
            var resDll = Environment.ExpandEnvironmentVariables(resourceString.Substring(0, lastCommaIdx));
            var resId = uint.Parse(resourceString.Substring(lastCommaIdx + 2));
            var dllHandle = API.LoadLibrary(resDll, IntPtr.Zero,
                                            API.LLF.LOAD_LIBRARY_AS_DATAFILE | API.LLF.LOAD_LIBRARY_AS_IMAGE_RESOURCE);
            return new Tuple<IntPtr, uint>(dllHandle, resId);
        }

        public class ShellExecuteHelper
        {
            private readonly API.ShellExecuteInfo _executeInfo;
            private int _errorCode;
            private bool _succeeded;

            public int ErrorCode{get { return _errorCode; }}

            public ShellExecuteHelper(API.ShellExecuteInfo executeInfo)
            {
                _executeInfo = executeInfo;
            }

            private void ShellExecuteFunction()
            {
// ReSharper disable RedundantBoolCompare
                if ((_succeeded = API.ShellExecuteEx(_executeInfo)) == true)
                    return;
                _errorCode = Marshal.GetLastWin32Error();
// ReSharper restore RedundantBoolCompare
            }

            public bool ShellExecuteOnSTAThread()
            {
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    var thread = new Thread(ShellExecuteFunction);
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                }
                else
                    ShellExecuteFunction();
                return _succeeded;
            }
        }


        
        public static void Restart(string reason)
        {
            Process.Start(Application.ExecutablePath);
            Die(reason);
        }

        public static void Die(string becauseString)
        {
            Environment.FailFast(string.Format(Properties.Resources.FailFastFormat, becauseString));
        }
    }
}
