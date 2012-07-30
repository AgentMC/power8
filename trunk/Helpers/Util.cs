﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Power8.Properties;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.MessageBox;
using System.Xml;

namespace Power8
{
    static class Util
    {
        public static Dispatcher MainDisp;

        private static readonly StringBuilder Buffer = new StringBuilder(1024);
        private static readonly Dictionary<Type, IComponent> Instances = new Dictionary<Type, IComponent>();
        private static Dictionary<char, string> _searchProviders;


        public static void Send(Action method)
        {
            MainDisp.Invoke(DispatcherPriority.Render, method);
        }

        public static void Post(Action method)
        {
            MainDisp.BeginInvoke(DispatcherPriority.Background, method);
        }

        public static void PostBackgroundReleaseResourceCall(Action method)
        {
#if DEBUG
            var mName = method.Method;
            method = (Action) Delegate.Combine(method, new Action(() => Debug.WriteLine("PBRRC invoked for " + mName)));
#endif
            MainDisp.BeginInvoke(DispatcherPriority.ApplicationIdle, method);
        }

        public static void PostBackgroundDllUnload(IntPtr hModule)
        {
            PostBackgroundReleaseResourceCall(() => API.FreeLibrary(hModule));
        }

        public static void PostBackgroundIconDestroy(IntPtr hIcon)
        {
            PostBackgroundReleaseResourceCall(() => API.DestroyIcon(hIcon));
        }

        public static T Eval<T>(Func<T> method)
        {
            return (T) MainDisp.Invoke(DispatcherPriority.DataBind, method);
        }

        public static Thread Fork(ThreadStart method, string name = "P8 forked")
        {
            return new Thread(() => 
            {
                try
                {
                    method(); 
                }
                catch (Exception ex)
                {
                    DispatchUnhandledException(ex);
                }
            }) { Name = name };
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
            if (!w.IsLoaded)
                return IntPtr.Zero;
            var source = w.GetHwndSource();
            if (OsIs.SevenOrMore &&  API.DwmIsCompositionEnabled())
            {
                if (source.CompositionTarget != null)
                {
                    w.Background = Brushes.Transparent;
                    source.CompositionTarget.BackgroundColor = Colors.Transparent;
                }
                MakeGlass(source.Handle);
            }
            //TODO: else - paint in system colors
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

        public static DependencyObject GetFirstVisualChildOfTypeByContent
            (this DependencyObject o, string shortTypeName = null, object content = null)
        {
            for (var i = 0; o != null && i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);
                if (shortTypeName == null || child.GetType().Name == shortTypeName)
                    if (content == null || (child is ContentControl && ((ContentControl)child).Content == content))
                        return child;
            }
            return null;
        }




        public static PowerItem ExtractRelatedPowerItem(EventArgs o)
        {
// ReSharper disable CanBeReplacedWithTryCastAndCheckForNull
            if (o is ContextMenuEventArgs)
            {
                var mi = o.GetType()
                          .GetProperty("TargetElement",
                                       BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty)
                          .GetValue(o, null)
                          as FrameworkElement;
                if (mi != null && mi.DataContext is PowerItem)
                    return (PowerItem)mi.DataContext;
            }
            if (o is RoutedEventArgs)
            {
                var obj = ((FrameworkElement)((RoutedEventArgs)o).OriginalSource).DataContext;
                if (obj is PowerItem)
                    return (PowerItem)obj;
            }
// ReSharper restore CanBeReplacedWithTryCastAndCheckForNull
            return null;
        }


        
        public static string ResolveLink(string link)
        {
            var shLink = new API.ShellLink();
            ((API.IPersistFile)shLink).Load(link, 0);
            var res = ResolveLink(((API.IShellLink) shLink));
            Marshal.FinalReleaseComObject(shLink);
            return res.Item1;
        }
        
        public static Tuple<string, string> ResolveLink(API.IShellLink shellLink)
        {
            lock (Buffer.Clear())
            {
                API.WIN32_FIND_DATAW sd;
                shellLink.GetPath(Buffer, 512, out sd, API.SLGP_FLAGS.SLGP_UNCPRIORITY);
                var i1 = Buffer.ToString();
                shellLink.GetArguments(Buffer, 512);
                return new Tuple<string, string>(i1, Buffer.ToString());
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
            lock (Buffer.Clear())
            {
                API.SHGetSpecialFolderPath(IntPtr.Zero, Buffer, id, false);
                return Buffer.ToString();
            }
        }

        public static string ResolveSpecialFolderName(API.Csidl id)
        {
            var ppIdl = IntPtr.Zero;
            var hRes = API.SHGetSpecialFolderLocation(IntPtr.Zero, id, ref ppIdl);
#if DEBUG
            Debug.WriteLine("RSFN: SHGetSp.F.Loc. for id<={0} returned result code {1}", id, hRes);
#endif
            var pwstr = IntPtr.Zero;
            var info = new API.Shfileinfo();
            var zeroFails = new IntPtr(1);
            if (hRes == 0 && OsIs.SevenOrMore && API.SHGetNameFromIDList(ppIdl, API.SIGDN.NORMALDISPLAY, ref pwstr) == 0)
            {
                info.szDisplayName = Marshal.PtrToStringUni(pwstr);
                Marshal.FreeCoTaskMem(pwstr);
            }
            else
            {
                zeroFails = (hRes != 0
                            ? IntPtr.Zero
                            : API.SHGetFileInfo(ppIdl, 0, ref info, (uint) Marshal.SizeOf(info),
                                                API.Shgfi.DISPLAYNAME | API.Shgfi.PIDL | API.Shgfi.USEFILEATTRIBUTES));
            }
            Marshal.FreeCoTaskMem(ppIdl);
#if DEBUG
            Debug.WriteLine("RSFN: ShGetFileInfo returned " + zeroFails);      
#endif
            return zeroFails == IntPtr.Zero ? null : info.szDisplayName;
        }

        public static string GetLongPathOrDisplayName(string path)
        {
            if (path.Contains("~") || path.StartsWith("::"))
            {
                IntPtr ppidl;
                lock (Buffer.Clear())
                {
                    API.SFGAO nul;
                    API.SHParseDisplayName(path, IntPtr.Zero, out ppidl, API.SFGAO.NULL, out nul);
                    API.SHGetPathFromIDList(ppidl, Buffer);
                    path = Buffer.ToString();
                }
                if (string.IsNullOrEmpty(path))
                {
                    var info = new API.Shfileinfo();
                    API.SHGetFileInfo(ppidl, 0, ref info, (uint) Marshal.SizeOf(info),
                                      API.Shgfi.DISPLAYNAME | API.Shgfi.PIDL | API.Shgfi.USEFILEATTRIBUTES);
                    path = info.szDisplayName;
                }
            }
            return path;
        }



        public static void DisplaySpecialFolder(API.Csidl id)
        {
            new Thread(DisplaySpecialFolderSync).Start(id);
        }

        public static void DisplaySpecialFolderSync(object id)
        {
            var pidl = IntPtr.Zero;
            var res = API.SHGetSpecialFolderLocation(IntPtr.Zero, (API.Csidl)id, ref pidl);
            if (res != 0)
            {
#if DEBUG
                Debug.WriteLine("Can't SHget folder PIDL, error " + res);
#endif
                return;
            }

            if (OsIs.XPOrLess)
            {
                API.IShellWindows shWndList = null;
                API.IServiceProvider provider = null;
                API.IShellBrowser browser = null;
                try
                {
                    shWndList  = (API.IShellWindows)new API.ShellWindows();
                    var wndCount = shWndList.Count;
                    var launchNew = true;
                    if (wndCount == 0)
                    {
                        launchNew = false;
                        StartExplorer("/N");
                        while (shWndList.Count == 0)
                            Thread.Sleep(40);
                    }
                    provider = (API.IServiceProvider)shWndList.Item(0);
                    var sidBrowser = new Guid(API.Sys.IdSTopLevelBrowser);
                    var iidBrowser = new Guid(API.Sys.IdIShellBrowser);
                    provider.QueryService(ref sidBrowser, ref iidBrowser, out browser);
                    browser.BrowseObject(pidl, launchNew ? API.SBSP.NEWBROWSER : API.SBSP.SAMEBROWSER);
                }
                catch (Exception e)
                {
#if DEBUG
                    Debug.WriteLine(e.ToString());
#endif
                }
                finally
                {
                    if(shWndList != null)
                        Marshal.ReleaseComObject(shWndList);
                    if(provider != null)
                        Marshal.ReleaseComObject(provider);
                    if(browser != null)
                        Marshal.ReleaseComObject(browser);
                    Marshal.FreeCoTaskMem(pidl);
                }
            }
            else
            {
                API.IExplorerBrowser browser = null;
                try
                {
                    browser = (API.IExplorerBrowser)new API.ExplorerBrowser();
                    browser.BrowseToIDList(pidl, API.SBSP.NEWBROWSER);
                }
                catch (Exception e)
                {
#if DEBUG
                    Debug.WriteLine(e.ToString());
#endif
                }
                finally
                {
                    if (browser != null) 
                        Marshal.ReleaseComObject(browser);
                    Marshal.FreeCoTaskMem(pidl);
                }
            }
        }



        public static void InstanciateClass(string className = null, Type t = null, Func<IComponent> ctor = null)
        {
            try
            {
                if(t == null && !string.IsNullOrEmpty(className))
                    t = Type.GetType(className);
                
                if (t == null)
                    throw new Exception(NoLoc.Err_GotNoTypeObject);

                if(!t.GetInterfaces().Contains(typeof(IComponent)))
                    throw new Exception(NoLoc.Err_TypeIsNotIComponent);

                IComponent inst;
                if (Instances.ContainsKey(t))
                {
                    inst = Instances[t];
                }
                else
                {
                    inst = ctor != null ? ctor() : (IComponent) Activator.CreateInstance(t);
                    inst.Disposed += (sender, args) => Instances.Remove(sender.GetType());
                    Instances.Add(t, inst);
                }
                var wnd = inst as Window;
                if(wnd != null)
                {
                    if(wnd.IsVisible) wnd.Hide(); //XP hack
                    wnd.Show();
                    if(wnd.WindowState == WindowState.Minimized)
                        wnd.WindowState = WindowState.Normal;
                    wnd.Activate();
                    return;
                }
                var frm = inst as Form;
                if (frm != null)
                {
                    if(frm.Visible) frm.Hide(); //XP hack
                    frm.Show();
                    if(frm.WindowState == FormWindowState.Minimized)
                        frm.WindowState = FormWindowState.Normal;
                    frm.Activate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Resources.Err_CantInstanciateClassFormatString, className, ex.Message));
            }
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
            return CommandToFilenameAndArgs(command);
        }

        public static Tuple<string, string> CommandToFilenameAndArgs(string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                if (File.Exists(command))
                    return new Tuple<string, string>(command, "");
                if (command[1] == ' ')//web search?
                {
                    if (_searchProviders == null)
                    {
                        _searchProviders = new Dictionary<char, string>();
                        var doc = new XmlDocument();
                        doc.LoadXml(Settings.Default.SearchProviders);
// ReSharper disable PossibleNullReferenceException
                        foreach (XmlElement prov in doc["P8SearchProviders"].GetElementsByTagName("Provider"))
                            _searchProviders.Add(prov.GetAttribute("key")[0], prov.InnerText);
// ReSharper restore PossibleNullReferenceException
                    }

                    string prefix = _searchProviders.ContainsKey(command[0]) ? _searchProviders[command[0]] : null;
                    if (prefix != null)
                        return new Tuple<string, string>(
                            string.Format(prefix, Uri.EscapeUriString(command.Substring(2))), 
                            null);
                }
                //normal file and args
                var argPtr = command[0] == '"' ? command.IndexOf('"', 1) + 1 : command.IndexOf(' ');
                if (argPtr > 0)
                    return new Tuple<string, string>(command.Substring(0, argPtr).Trim('"'),
                                                     command.Substring(argPtr).TrimStart(' '));
                return new Tuple<string, string>(command, null);
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
            if (!string.IsNullOrEmpty(clsidOrApiShNs))
            {
                try
                {
                    var key = GetRegistryContainer(clsidOrApiShNs);
                    if (key != null)
                    {
                        using (var k = Microsoft.Win32.Registry.ClassesRoot
                                .OpenSubKey(key + subkey, false))
                        {
                            if (k != null)
                                return ((string)k.GetValue(valueName, null));
                        }
                    }
                }
                catch (Exception){}
            }
// ReSharper restore EmptyGeneralCatchClause
            return null;
        }

        private static string GetRegistryContainer(string pathClsidGuidOrApishnamespace)
        {
            var i = pathClsidGuidOrApishnamespace.LastIndexOf(".", StringComparison.Ordinal);
            if (i < 0)
            {
                return "CLSID\\" + NameSpaceToGuidWithBraces(pathClsidGuidOrApishnamespace);
            }
            using (var k = Microsoft.Win32.Registry.ClassesRoot
                .OpenSubKey(pathClsidGuidOrApishnamespace.Substring(i), false))
            {
                return (k != null)
                           ? ((string) k.GetValue(String.Empty, null))
                           : null;
            }
        }


        private static string NameSpaceToGuidWithBraces(string ns)
        {
            ns = ns.Substring(ns.LastIndexOf('\\') + 1).TrimStart(':');
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
                lock (Buffer.Clear())
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
                IntPtr icon = resData.Item3 == 0xFFFFFFFF 
                                  ? API.ExtractIcon(resData.Item2, resData.Item1, 0) 
                                  : API.LoadIcon(resData.Item2, resData.Item3);
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
                ? 0xFFFFFFFF
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



        public static Tuple<string, ImageManager.ImageContainer> GetCplInfo(string cplFileName)
        {
            string name = null;
            ImageManager.ImageContainer container = null;
            var info = new API.CplInfo {lData = new IntPtr(0xDEADC0D)};
            
            var hModule = API.LoadLibrary(cplFileName, IntPtr.Zero, API.LLF.AS_REGULAR_LOAD_LIBRARY);
#if DEBUG
            Debug.WriteLine("GCplI: begin 4 {1}, hModule<={0}", hModule, cplFileName);
#endif

            if(hModule != IntPtr.Zero)
            {
                var cplProcAddress = API.GetProcAddress(hModule, "CPlApplet");
                if (cplProcAddress != IntPtr.Zero)
                {
                    var cplProc = (API.CplAppletProc) Marshal.GetDelegateForFunctionPointer(
                                                                cplProcAddress, 
                                                                typeof (API.CplAppletProc));
                    if (cplProc != null)
                    {
                        var hWnd = API.GetDesktopWindow();
#if DEBUG
                        Debug.WriteLine("GCplI: doing INIT...");
#endif
                        var res = cplProc(hWnd, API.CplMsg.INIT, IntPtr.Zero, IntPtr.Zero);
                        if (res != 0)
                        {
#if DEBUG
                            Debug.WriteLine("GCplI: doing GETCOUNT...");
#endif
                            res = cplProc(hWnd, API.CplMsg.GETCOUNT, IntPtr.Zero, IntPtr.Zero);
                            if (res > 0)
                            {
#if DEBUG
                                Debug.WriteLine("GCplI: GETCOUNT returned {0}, doing INQUIRE...", res);
#endif
                                var structSize = Marshal.SizeOf(typeof (API.CplInfo));
                                var hMem = Marshal.AllocHGlobal(structSize);
                                ZeroMemory(hMem, structSize);
                                cplProc(hWnd, API.CplMsg.INQUIRE, IntPtr.Zero, hMem);
                                Marshal.PtrToStructure(hMem, info);
#if DEBUG
                                Debug.WriteLine("GCplI: INQUIRE returned {0},{1},{2}", info.idIcon, info.idInfo, info.idName);
#endif

                                var idIcon = info.idIcon;
                                var idName = info.idName == 0 ? info.idInfo : info.idName;
                                var unmanagedIcon = IntPtr.Zero;

                                if (idIcon == 0 || idName == 0)
                                {
                                    structSize = Marshal.SizeOf(typeof (API.NewCplInfoW)); //v- just in case...
                                    hMem = Marshal.ReAllocHGlobal(hMem, new IntPtr(structSize*2));
                                    ZeroMemory(hMem, structSize*2);
#if DEBUG
                                    Debug.WriteLine("GCplI: doing NEWINQUIRE...");
#endif
                                    cplProc(hWnd, API.CplMsg.NEWINQUIRE, IntPtr.Zero, hMem);
                                    var infoNew =
                                        (API.NewCplInfoW) Marshal.PtrToStructure(hMem, typeof (API.NewCplInfoW));

                                    if (infoNew.dwSize == structSize)
                                    {
#if DEBUG
                                        Debug.WriteLine("GCplI: got NewCplInfoW: {0},{1},{2}", infoNew.hIcon, infoNew.szInfo, infoNew.szName);
#endif
                                        unmanagedIcon = infoNew.hIcon;
                                        name = infoNew.szName ?? infoNew.szInfo;
                                    }
                                    else if (infoNew.dwSize == Marshal.SizeOf(typeof(API.NewCplInfoA)))
                                    {
                                        var infoNewA =
                                            (API.NewCplInfoA)Marshal.PtrToStructure(hMem, typeof(API.NewCplInfoA));
#if DEBUG
                                        Debug.WriteLine("GCplI: got NewCplInfoA: {0},{1},{2}", infoNewA.hIcon, infoNewA.szInfo, infoNewA.szName);
#endif
                                        unmanagedIcon = infoNewA.hIcon;
                                        name = infoNewA.szName ?? infoNewA.szInfo;
                                    }
#if DEBUG
                                    else
                                    {
                                        Debug.WriteLine("GCplI: NEWINQUIRE: structure size not supported: 0x{0:x} with IntPtr size 0x{1:x}", infoNew.dwSize, IntPtr.Size);
                                    }
#endif
                                }
                                Marshal.FreeHGlobal(hMem);
#if DEBUG
                                Debug.WriteLine("GCplI: freed, conditional load string...");
#endif

                                if (name == null)
                                {
                                    lock (Buffer.Clear())
                                    {
                                        if (0 < API.LoadString(hModule, idName, Buffer, Buffer.Capacity))
                                            name = Buffer.ToString();
                                    }
                                }
#if DEBUG
                                Debug.WriteLine("GCplI: name={0}, conditional load icon...", new object[]{name});
#endif

                                if(unmanagedIcon == IntPtr.Zero)
                                    unmanagedIcon = API.LoadIcon(hModule, idIcon);
#if DEBUG
                                Debug.WriteLine("GCplI: icon={0}", unmanagedIcon);
#endif
                                if(unmanagedIcon != IntPtr.Zero)
                                {
                                    container = ImageManager.GetImageContainerForIconSync(cplFileName, unmanagedIcon);
                                    PostBackgroundIconDestroy(unmanagedIcon);
                                }

#if DEBUG
                                Debug.WriteLine("GCplI: doing STOP...");
#endif
                                cplProc(hWnd, API.CplMsg.STOP, IntPtr.Zero, info.lData);
                            }
#if DEBUG
                            Debug.WriteLine("GCplI: doing EXIT...");
#endif
                            cplProc(hWnd, API.CplMsg.EXIT, IntPtr.Zero, IntPtr.Zero);
                        }
                    }
                }
                PostBackgroundDllUnload(hModule);
            }

            return new Tuple<string, ImageManager.ImageContainer>(name, container);
        }


        private static void ZeroMemory (IntPtr hMem, int cb)
        {
            for (var i = 0; i < cb; i++)
                Marshal.WriteByte(hMem, i, 0);
        }


        public class ShellExecuteHelper
        {
            private readonly API.ShellExecuteInfo _executeInfo;
            private bool _succeeded;

            public int ErrorCode { get; private set; }
            public string ErrorText { get; private set; }

            public ShellExecuteHelper(API.ShellExecuteInfo executeInfo)
            {
                _executeInfo = executeInfo;
            }

            private void ShellExecuteFunction()
            {
                _succeeded = API.ShellExecuteEx(_executeInfo);
                if (_succeeded)
                    return;
                ErrorCode = Marshal.GetLastWin32Error();
                ErrorText = new Win32Exception(ErrorCode).Message;
            }

            public bool ShellExecuteOnSTAThread()
            {
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    var thread = Fork(ShellExecuteFunction, "ShExec");
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                }
                else
                    ShellExecuteFunction();
                return _succeeded;
            }
        }



        public static void DispatchCaughtException(Exception ex)
        {
            MessageBox.Show(ex.Message, NoLoc.Stg_AppShortName, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static void DispatchUnhandledException(Exception ex)
        {
            var str = ex.ToString();
            MessageBox.Show(str, NoLoc.Stg_AppShortName, MessageBoxButton.OK, MessageBoxImage.Error);
            Die(NoLoc.Err_UnhandledGeneric + str);
        }

        public static void Restart(string reason)
        {
            Process.Start(Application.ExecutablePath);
            Die(reason);
        }

        public static void Die(string becauseString)
        {
            EventLog.WriteEntry("Application Error", 
                                string.Format(NoLoc.Str_FailFastFormat, becauseString),
                                EventLogEntryType.Error);
            Environment.Exit(1);
        }

        public static void StartExplorer(string command = null)
        {
            const string explorerString = "explorer.exe";
            if (command != null)
                Process.Start(explorerString, command);
            else
                Process.Start(explorerString);
        }

        public static void StartExplorerSelect(string objectToSelect)
        {
            StartExplorer("/select,\"" + objectToSelect + "\"");
        }




        public static class OsIs
        {
            static readonly Version Ver = Environment.OSVersion.Version;

            public static bool XPOrLess { get { return Ver.Major < 6; } }
            public static bool VistaExact { get { return Ver.Major == 6 && Ver.Minor == 0; } }
            public static bool SevenOrMore { get { return Ver.Major > 6 || (Ver.Major == 6 && Ver.Minor >= 1); } }
            public static bool SevenOrBelow { get { return Ver.Major < 6 || (Ver.Major == 6 && Ver.Minor <= 1); } }
            public static bool EightOrMore { get { return Ver.Major > 6 || (Ver.Major == 6 && Ver.Minor >= 2); } }
            public static bool EightRpOrMore { get { return Ver >= new Version(6, 2, 8400); } }
        }

    }
}
