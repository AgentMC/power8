using System;
using System.Diagnostics;
using System.IO;
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

        public static void PostBackgroundReleaseResourceCall(Delegate method)
        {
#if DEBUG
            var mName = method.Method;
            method = Delegate.Combine(method, new Action(() => Debug.WriteLine("PBRRC invoked for " + mName)));
#endif
            MainDisp.BeginInvoke(DispatcherPriority.ApplicationIdle, method);
        }

        public static void PostBackgroundDllUnload(IntPtr hModule)
        {
            PostBackgroundReleaseResourceCall(new Action(() => API.FreeLibrary(hModule)));
        }

        public static void PostBackgroundIconDestroy(IntPtr hIcon)
        {
            PostBackgroundReleaseResourceCall(new Action(() => API.DestroyIcon(hIcon)));
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

        public static string ResolveKnownFolder(string apiKnFldr)
        {
            IntPtr pwstr;
            API.SHGetKnownFolderPath(new Guid(apiKnFldr), API.KFF.NORMAL, IntPtr.Zero, out pwstr);
            var res = Marshal.PtrToStringUni(pwstr);
            Marshal.FreeCoTaskMem(pwstr);
            return res;
        }

        public static string ResolveSpecialFolder(API.Csidl id)
        {
            IntPtr pwstr;
            API.SHGetSpecialFolderPath(IntPtr.Zero, out pwstr, id, false);
            var res = Marshal.PtrToStringUni(pwstr);
            Marshal.FreeCoTaskMem(pwstr);
            return res;
        }

        public static string ResolveSpecialFolderName(API.Csidl id)
        {
            var ppIdl = IntPtr.Zero;
            var info = new API.Shfileinfo();
            var hRes = API.SHGetSpecialFolderLocation(IntPtr.Zero, id, ref ppIdl);
#if DEBUG
            Debug.WriteLine("RSFN: SHGetSp.F.Loc. for id<={0} returned result code {1}", id, hRes);
#endif
            var zeroFails = (hRes != 0
                                ? IntPtr.Zero
                                : API.SHGetFileInfo(ppIdl, 0, ref info, (uint) Marshal.SizeOf(info),
                                                    API.Shgfi.DISPLAYNAME | API.Shgfi.PIDL | API.Shgfi.USEFILEATTRIBUTES));
            Marshal.FreeCoTaskMem(ppIdl);
#if DEBUG
            Debug.WriteLine("RSFN: ShGetFileInfo returned " + zeroFails);      
#endif
            return zeroFails == IntPtr.Zero ? null : info.szDisplayName;
        }



        public static string GetLocalizedStringResourceIdForClass(string clsidOrApiShNs, bool fallbackToInfoTip = false)
        {
            var ls = GetResourceIdForClassCommon(clsidOrApiShNs, "", "LocalizedString");
            if(ls == null && fallbackToInfoTip)
                return GetResourceIdForClassCommon(clsidOrApiShNs, "", "InfoTip");
            return ls;
        }

        public static string GetDefaultIconResourceIdForClass(string clsidOrApiShNs)
        {
            return GetResourceIdForClassCommon(clsidOrApiShNs, "\\DefaultIcon", "");
        }

        public static Tuple<string, string> GetOpenCommandForClass(string clsidOrApiShNs)
        {
            var command = GetResourceIdForClassCommon(clsidOrApiShNs, "\\Shell\\Open\\Command", "");
            if (!string.IsNullOrEmpty(command))
            {
                if (File.Exists(command))
                    return new Tuple<string, string>(command, "");
                var argPtr = command[0] == '"' ? command.IndexOf('"', 1) + 1 : command.IndexOf(' ');
                if (argPtr > 0)
                {
                    return new Tuple<string, string>(command.Substring(0, argPtr).Trim('"'),
                                                     command.Substring(argPtr).TrimStart(' '));
                }
            }
            return null;
        }
        
        public static string GetCplAppletSysNameForClass(string clsidOrApiShNs)
        {
            return GetResourceIdForClassCommon(clsidOrApiShNs, "", "System.ApplicationName");
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
                        return ((string)k.GetValue(valueName, null));
                }
            }
            catch (Exception){}
// ReSharper restore EmptyGeneralCatchClause
            return null;
        }

        private static string NameSpaceToGuidWithBraces(string ns)
        {
            ns = ns.Substring(ns.LastIndexOf('\\') + 1);
            ns = ns.TrimStart(':', '\\');
            if (!ns.StartsWith("{"))
                ns = "{" + ns + "}";
            return ns;
        }


        
        public static string ResolveStringResource(string localizeableResourceId)
        {
            if(!(localizeableResourceId.StartsWith("@")))
                return localizeableResourceId; //when non-@ string is used for id
            var resData = ResolveResourceCommon(localizeableResourceId);
            if (resData.Item2 != IntPtr.Zero)
            {
                lock (Buffer)
                {
                    var number = API.LoadString(resData.Item2, resData.Item3, Buffer, Buffer.Capacity);
#if DEBUG
                    Debug.WriteLine("RSR: number => " + number + ", data: " + Buffer);
#endif
                    PostBackgroundDllUnload(resData.Item2);
                    if (number > 0)
                        return Buffer.ToString();
                }
            }
            return null;
        }

        public static IntPtr ResolveIconicResource(string localizeableResourceId)
        {
            var resData = ResolveResourceCommon(localizeableResourceId);
            if (resData.Item2 != IntPtr.Zero)
            {
                var icon = API.LoadIcon(resData.Item2, resData.Item3);
#if DEBUG
                Debug.WriteLine("RIR: icon => " + icon);
#endif
                PostBackgroundDllUnload(resData.Item2);
                if(icon != IntPtr.Zero)
                    return icon;

                var shinfo = new API.Shfileinfo();
                API.SHGetFileInfo(resData.Item1, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), API.Shgfi.ICON | API.Shgfi.LARGEICON);
                return shinfo.hIcon;
            }
            return IntPtr.Zero;
        }

        private static Tuple<string, IntPtr, uint> ResolveResourceCommon(string resourceString)
        {
            //ResId = @%ProgramFiles%\Windows Defender\EppManifest.dll,-1000 (genaral case)
            //or like @C:\data\a.dll,-2000#embedding8
            //or      @B:\wakawaka\foo.dlx
            //or      @%windir%\msm.dll,8 => 8 == -8
#if DEBUG
            Debug.WriteLine("RRC: in => " + resourceString);
#endif
            resourceString = resourceString.TrimStart('@');

            var lastCommaIdx = Math.Max(resourceString.LastIndexOf(','), 0);
            var lastSharpIdx = resourceString.LastIndexOf('#');
            
            var resDll =
                Environment.ExpandEnvironmentVariables(
                    resourceString.Substring(0, lastCommaIdx > 0 ? lastCommaIdx : resourceString.Length));

            var resId = 
                lastCommaIdx == 0
                ? 0
                : uint.Parse((lastSharpIdx > lastCommaIdx
                        ? resourceString.Substring(lastCommaIdx + 1, lastSharpIdx - (lastCommaIdx + 1))
                        : resourceString.Substring(lastCommaIdx + 1)).TrimStart('-'));

            var dllHandle = API.LoadLibrary(resDll, IntPtr.Zero,
                                            API.LLF.LOAD_LIBRARY_AS_DATAFILE | API.LLF.LOAD_LIBRARY_AS_IMAGE_RESOURCE);
#if DEBUG
            Debug.WriteLine("RRC: hModule<={0}, resId<={1}, resDll<={2}", dllHandle, resId, resDll);
#endif

            return new Tuple<string, IntPtr, uint>(resDll, dllHandle, resId);
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
