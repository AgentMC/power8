using System;
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
using Microsoft.Win32;
using Power8.Helpers;
using Power8.Properties;
using Power8.Views;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.MessageBox;
using ThreadState = System.Threading.ThreadState;

namespace Power8
{
    /// <summary>
    /// Contains helping methods
    /// </summary>
    static class Util
    {
        /// <summary>
        /// The Main UI thread dispatcher. Public since many stuff is related to program shutdown.
        /// </summary>
        public static Dispatcher MainDisp;

        //2-kilobyte string buffer for string operations
        private static readonly StringBuilder Buffer = new StringBuilder(1024); 
        //Singleton window manager
        private static readonly Dictionary<Type, IComponent> Instances = new Dictionary<Type, IComponent>(); 


        #region Cross-thread operations

        /// <summary>
        /// Invokes the delegate on the main UI thread. Execution is immediate, but control is not
        /// returned to the caller until the callee is done. The execution priority is set to Render.
        /// </summary>
        /// <param name="method">Any parameterless action, lambda or delegate</param>
        public static void Send(Action method)
        {
            MainDisp.Invoke(DispatcherPriority.Render, method);
        }
        /// <summary>
        /// Invokes the delegate on the main UI thread. Execution may be delayed, but control is
        /// immediately returned to the caller. The execution priority is set to Background.
        /// </summary>
        /// <param name="method">Any parameterless action, lambda or delegate</param>
        public static void Post(Action method)
        {
            MainDisp.BeginInvoke(DispatcherPriority.Background, method);
        }
        /// <summary>
        /// Executes an Action with almost lowest priority (AppIdle). Use this to free 
        /// unmanaged resources.
        /// </summary>
        /// <param name="method">Any parameterless action, lambda or delegate</param>
        private static void PostBackgroundReleaseResourceCall(Action method)
        {
#if DEBUG
            var mName = method.Method.Name; //In debug, we log the resource release usage
            method = (Action) Delegate.Combine(method, new Action(() => Log.Raw("...invoked", mName)));
#endif
            MainDisp.BeginInvoke(DispatcherPriority.SystemIdle, method);
        }
        /// <summary>
        /// Initiates the procedure of unloading the unmanaged dll, and immediately returns.
        /// The resource will be freed likely later, when the Application becomes idle.
        /// </summary>
        /// <param name="hModule">A handle to the DLL obtained by LoadLibrary() call.</param>
        public static void PostBackgroundDllUnload(IntPtr hModule)
        {
            if (!SettingsManager.Instance.DontFreeLibs)
                PostBackgroundReleaseResourceCall(() => API.FreeLibrary(hModule));
        }
        /// <summary>
        /// Initiates the procedure of unloading the unmanaged icon, and immediately returns.
        /// The resource will be freed likely later, when the Application becomes idle.
        /// </summary>
        /// <param name="hIcon">A handle to the icon obtained by LoadIcon() or similar call.</param>
        public static void PostBackgroundIconDestroy(IntPtr hIcon)
        {
            PostBackgroundReleaseResourceCall(() => API.DestroyIcon(hIcon));
        }
        /// <summary>
        /// Executes the function on the main thread with the almost highest priority.
        /// The caling thread is blocked until the callee returns the result.
        /// </summary>
        /// <typeparam name="T">The type of return result.</typeparam>
        /// <param name="method">Any parameterless Func, including lambda and the returning delegate</param>
        /// <returns>The value returned by the method invoked</returns>
        public static T Eval<T>(Func<T> method)
        {
            return (T) MainDisp.Invoke(DispatcherPriority.DataBind, method);
        }
        /// <summary>
        /// Creates a Thread from a delegate, with the given thread name, wrapping it in the 
        /// try-catch block, without starting it. 
        /// The catch calls <code>DispatchUnhandledException()</code>
        /// </summary>
        /// <param name="method">The delegate which can be used as non-parametrized thread start</param>
        /// <param name="name">Optional. Managed name of a Thread being created, default is "P8 forked"</param>
        /// <returns>The Thread class instance created, ready to be started</returns>
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
                                  })
                       {
                           Name = name,
#if DEBUG
                           CurrentCulture = Thread.CurrentThread.CurrentCulture,
                           CurrentUICulture = Thread.CurrentThread.CurrentUICulture
#endif
                       };
        }
        /// <summary>
        /// A shortcut fo Fork(something).Start()
        /// </summary>
        /// <param name="method">The delegate which can be used as non-parametrized thread start</param>
        /// <param name="name">Optional. Managed name of a Thread being created, default is "P8 forked"</param>
        public static void ForkStart(ThreadStart method, string name = "unnamed")
        {
            Fork(method, name).Start();
        }
        /// <summary>
        /// Unkile ForkStart() or simple Fork(), uses thread pool to lower system load when multiple threads are initialized. 
        /// </summary>
        /// <param name="method">The delegate which can be used as non-parametrized thread start</param>
        /// <param name="name">Optional. Managed name of a Thread being created, default is "P8 forked"</param>
        public static void ForkPool(ThreadStart method, string name = "unnamed")
        {
            Log.Raw("Thread forked to pool: " + name);
#if DEBUG
            var cc = Thread.CurrentThread.CurrentCulture;
            var cuic = Thread.CurrentThread.CurrentUICulture;
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    Thread.CurrentThread.CurrentCulture = cc;
                    Thread.CurrentThread.CurrentUICulture = cuic;
#else
            ThreadPool.QueueUserWorkItem(state => 
            {
                try
                {

#endif
                    Log.Raw("Started execution of task " + name);
                    method();
                }
                catch (Exception ex)
                {
                    DispatchUnhandledException(ex);
                }
                Log.Raw("Done execution of task " + name);

            });
        }
        /// <summary>
        /// Forks background thread with given name and starts it, but only in case referenced thread 
        /// doesn't exist or is stopped already
        /// </summary>
        /// <param name="thread">Thread variable which holds or will hold the thread reference</param>
        /// <param name="pFunc">Thread delegate</param>
        /// <param name="threadName">Name of newly created thread</param>
        public static void BgrThreadInit(ref Thread thread, ThreadStart pFunc, string threadName)
        {
            if (thread == null || thread.ThreadState == ThreadState.Stopped)
            {
                thread = Fork(pFunc, threadName);
                thread.IsBackground = true;
            }
            thread.Start();
        }

        #endregion

        #region WPF-User32 interactions

        /// <summary>
        /// Extension. Returns unmanaged handle for the User32 window that hosts
        /// current WPF Window.
        /// </summary>
        /// <param name="w">The Window to get handle for</param>
        /// <returns>IntPtr which represents the HWND of the caller</returns>
        public static IntPtr GetHandle(this Window w)
        {
            return new WindowInteropHelper(w).Handle;
        }
        /// <summary>
        /// Extension. Returns HwndSource object for current WPF Window.
        /// </summary>
        /// <param name="w">Window to get HwndSource for.</param>
        public static HwndSource GetHwndSource(this Window w)
        {
            return HwndSource.FromHwnd(w.GetHandle());
        }
        /// <summary>
        /// Extension. Makes the Window glassed in the environment which support such action.
        /// In case this is not supported, ensures that the Window will have proper background.
        /// </summary>
        /// <param name="w">The Window that must be glassified</param>
        /// <returns>Unmanaged HWND of the glassified window, in the form of IntPtr.
        /// Returns NULL (IntPtr.Zero) in case Window has not yet initialized completely.</returns>
        public static IntPtr MakeGlassWpfWindow(this Window w)
        {
            if (!w.IsLoaded)
                return IntPtr.Zero;
            var source = w.GetHwndSource();
            if (OsIs.SevenOrMore && API.DwmIsCompositionEnabled())
            {
                if (source.CompositionTarget != null)
                {
                    w.Background = Brushes.Transparent;
                    source.CompositionTarget.BackgroundColor = Colors.Transparent;
                }
                MakeGlass(source.Handle);
            }
            else
            {
                w.Background = w is MainWindow ? SystemColors.ActiveCaptionBrush : SystemColors.ControlBrush;
            }
            return source.Handle;
        }
        /// <summary>
        /// Calls the DWM API to make the target window, represented by a handle, the glass one.
        /// </summary>
        /// <param name="hWnd">HWND of a window, in the form of IntPtr</param>
        public static void MakeGlass(IntPtr hWnd)
        {
            var bbhOff = new API.DwmBlurbehind
                            {
                                dwFlags = API.DwmBlurbehind.DWM_BB_ENABLE | API.DwmBlurbehind.DWM_BB_BLURREGION,
                                fEnable = false,
                                hRegionBlur = IntPtr.Zero
                            };
            API.DwmEnableBlurBehindWindow(hWnd, bbhOff);            //vvvHere goes special "MARGIN{-1}" structure;
            API.DwmExtendFrameIntoClientArea(hWnd, new API.Margins { cxLeftWidth = -1, cxRightWidth = 0, cyTopHeight = 0, cyBottomHeight = 0 });
        }
        /// <summary>
        /// Extension. For the given Window attaches Message Filter hook to the 
        /// native WndProc sink of a host native window. 
        /// </summary>
        /// <param name="w">Window whose messages you need to filter</param>
        /// <param name="hook"><code>HwndSourceHook</code> instance.</param>
        public static void RegisterHook(this Window w, HwndSourceHook hook)
        {
            w.GetHwndSource().AddHook(hook);
        }
        /// <summary>
        /// Scans the VisualTree of a Window or Control, etc. to find the Visual that complies
        /// to the passed parameters. When <code>content</code> parameter is used, the child must 
        /// be derived from ContentControl to be tested for the value of content. 
        /// Something like GetWindow()/FindWindowEx() but bit smarter.
        /// </summary>
        /// <param name="o">The parent of the hierarchy searched.</param>
        /// <param name="shortTypeName">Optional. The <code>child.GetType().Name</code> of the 
        /// required child.</param>
        /// <param name="content">The object that shall be reference-equal to the desired 
        /// Content of the control being searched.</param>
        /// <returns>Returns first child available if no parameters specified.
        /// When single paramemeter is specified returns first Visual child that
        /// satisfies the parameter passed.
        /// If no children are available, or noone satisfies the conditions passed,
        /// returns null.</returns>
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

        #endregion

        #region Shell items resolution

        /// <summary>
        /// Returns the target of a shell shortcut, using COM component to get data.
        /// </summary>
        /// <param name="link">Full path to *.LNK file</param>
        public static string ResolveLink(string link)
        {
            var shLink = new API.ShellLink();
            ((API.IPersistFile)shLink).Load(link, 0);
            var res = ResolveLink(((API.IShellLink) shLink));
            Marshal.FinalReleaseComObject(shLink);
            return res.Item1;//The target of Link
        }
        /// <summary>
        /// Extracts data from IShellLink instance, in Unicode format. Does NOT automatically 
        /// release the COM instance.
        /// </summary>
        /// <param name="shellLink">Initialized instance of IShellLink, with the data already loaded.</param>
        /// <returns>The tuple of 2 strings:
        /// - the target of the shortcut (512 characters max);
        /// - the command line to the target, except 0th argument (the target itself), also
        /// limited to 512 chars max</returns>
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
        /// <summary>
        /// Cals ResolveLink() swallowing all exceptions inside. Use when you don't care
        /// abour the results of such resolution.
        /// </summary>
        /// <param name="link">Full path to *.LNK file</param>
        /// <returns>The target of a shell shortcul on success and null if failed</returns>
        public static string ResolveLinkSafe(string link)
        {
            string res;
            try
            {
                res = ResolveLink(link);
            }
            catch 
#if DEBUG
                   (Exception ex)
#endif
            {
#if DEBUG 
                Log.Raw("failed safely.\r\n" + ex, link);
#endif
                res = null;
            }
            return res;
        }
        /// <summary> Gets the path of a known folder (Win7 or higher) </summary>
        /// <param name="apiKnFldr">One of GUIDs in <code>API.KnFldrIds</code></param>
        /// <returns>FQ-Path of the FS folder for existing initialized Known Folders, or null</returns>
        public static string ResolveKnownFolder(string apiKnFldr)
        {
            IntPtr pwstr;
            API.SHGetKnownFolderPath(new Guid(apiKnFldr), API.KFF.NORMAL, IntPtr.Zero, out pwstr);
            var res = Marshal.PtrToStringUni(pwstr);
            Marshal.FreeCoTaskMem(pwstr);
            return res;
        }
        /// <summary> Gets the path of a special folder </summary>
        /// <param name="id">Corresponding folder ID</param>
        /// <returns>FQ-Path of the FS folder for existing available Special Folder, or null</returns>
        public static string ResolveSpecialFolder(API.Csidl id)
        {
            lock (Buffer.Clear())
            {
                API.SHGetSpecialFolderPath(IntPtr.Zero, Buffer, id, false);
                return Buffer.ToString();
            }
        }
        /// <summary>
        /// Gets the display name of the Special folder, using the desktop.ini configuration
        /// and in system locale. For Win7 and upper OS, tries fast shell function first, and 
        /// if it fails or it is XP, uses general <code>SHGetFileInfo()</code>.
        /// </summary>
        /// <param name="id">The ID for the folder whose display name is needed.</param>
        /// <returns>String, equal to how the folder will be displayed in Explorer, 
        /// or null if such information is not available.</returns>
        public static string ResolveSpecialFolderName(API.Csidl id)
        {
            var ppIdl = IntPtr.Zero; //Sinse some of special folders are virtual, we'll use PIDLs
            var hRes = API.SHGetSpecialFolderLocation(IntPtr.Zero, id, ref ppIdl); //Obtain PIDL to folder
            Log.Fmt("SHGetSp.F.Loc. returned result code "+ hRes, id.ToString());
            var pwstr = IntPtr.Zero;
            var info = new API.ShfileinfoW();
            var zeroFails = new IntPtr(1); 
            //Fast shell call for Win7+
            if (hRes == 0 && OsIs.SevenOrMore && API.SHGetNameFromIDList(ppIdl, API.SIGDN.NORMALDISPLAY, ref pwstr) == 0)
            {
                info.szDisplayName = Marshal.PtrToStringUni(pwstr);
                Marshal.FreeCoTaskMem(pwstr);
            }
            else //If failed or XP
            {
                zeroFails = (hRes != 0 //if(!SUCCEEDED(SHGetSp.F.Loc.)) return NULL;
                            ? IntPtr.Zero
                            : API.SHGetFileInfo(ppIdl, 0, ref info, (uint) Marshal.SizeOf(info),
                                                API.Shgfi.DISPLAYNAME | API.Shgfi.PIDL | API.Shgfi.USEFILEATTRIBUTES));
            }
            Marshal.FreeCoTaskMem(ppIdl);
            Log.Raw("ShGetFileInfo returned " + zeroFails, id.ToString());      
            return zeroFails == IntPtr.Zero ? null : info.szDisplayName;
        }
        /// <summary>
        /// Converts a path with 8.3 styled file- or foldernames to a long one.
        /// Or, gets the diplay name for shell namespace guids.
        /// </summary>
        /// <param name="path">Path to file or folder, either normal one (the function won't 
        /// do anything), or containing 8.3 styled names, like "C:\Users\MYUSER~1\", 
        /// or a shell namespage guid(s), one from <code>API.ShNs</code>.</param>
        /// <returns>If succeeds, returns expanded FQ-FS-path of a file or folder that was 
        /// passed  containing 8.3-styled names, or returns the display name for shell guid.
        /// If fails, returns either original passed data (if no changes were done, and the
        /// parameter is simply non-applicable), or null, or empty string in case something 
        /// gone wrong.</returns>
        public static string ResolveLongPathOrDisplayName(string path)
        {
            if (path.Contains("~") || path.StartsWith("::"))
            {
                IntPtr ppidl;
                lock (Buffer.Clear()) 
                {
                    API.SFGAO nul;
                    API.SHParseDisplayName(path, IntPtr.Zero, out ppidl, API.SFGAO.NULL, out nul);
                    API.SHGetPathFromIDList(ppidl, Buffer);//for 8.3-styled
                    path = Buffer.ToString();
                }
                if (string.IsNullOrEmpty(path))//if the 8.3-conversion failed
                {
                    var info = new API.ShfileinfoW();
                    API.SHGetFileInfo(ppidl, 0, ref info, (uint) Marshal.SizeOf(info),
                                      API.Shgfi.DISPLAYNAME | API.Shgfi.PIDL | API.Shgfi.USEFILEATTRIBUTES);
                    path = info.szDisplayName;
                }
            }
            return path;
        }

        #endregion

        #region Shell items visualizing

        /// <summary>
        /// Initializes the background thread to display the required special folder, 
        /// and returns immediately
        /// </summary>
        public static void DisplaySpecialFolder(API.Csidl id)
        {
            ForkStart(() => DisplaySpecialFolderSync(id), "Async DisplaySpecialFolder executor");
        }
        /// <summary>
        /// Displays the special folder specified in Explorer window, blocking calling thread until 
        /// the coresponding window has not <b>begun</b> the displaying the special folder.
        /// Uses different strategies for XP and other OSs. For XP, it needs some Explorer window to exist
        /// and to be available for shell automation to work seamlessly. In case no such window is available,
        /// the new Explorer window is created, with something default inside, and then it is pointed to the 
        /// desired location. This can cause short blinking of the created window.
        /// For Win7 and later, the ExplorerBrowser instance is used, so no blinking and other side effects
        /// will be visible.
        /// </summary>
        /// <param name="id"><code>API.Csidl</code> representing the desired special folder</param>
        public static void DisplaySpecialFolderSync(object id)
        {
            var pidl = IntPtr.Zero;
            var res = API.SHGetSpecialFolderLocation(IntPtr.Zero, (API.Csidl)id, ref pidl);
            if (res != 0)
            {
                Log.Raw("Can't SHget folder PIDL, error " + res);
                return;
            }

            if (OsIs.XPOrLess) //Use old awfull shell COM hell from Raymond Chen...
            {
                API.IShellWindows shWndList = null;     //Opened Explorer windows, excl. Desktop, ProgMan...
                API.IServiceProvider provider = null;   //Browser accessor for Explorer window
                API.IShellBrowser browser = null;       //The browser that makes possible to kick Explorer
                                                        //whereever you need
                try
                {
                    shWndList  = (API.IShellWindows)new API.ShellWindows();
                    var wndCount = shWndList.Count;     //How many are opened at the moment?
                    var launchNew = true;               //To open our target in new window or in existing one?
                    if (wndCount == 0)                  //Will create a window and use it
                    {
                        launchNew = false;              //Use newly created window
                        StartExplorer("/N");            //Create a window
                        while (shWndList.Count == 0)    //Wait until dispatch stops snorring...
                            Thread.Sleep(40);
                    }
                    provider = (API.IServiceProvider)shWndList.Item(0); //Get browser from any available wnd
                    var sidBrowser = new Guid(API.Sys.IdSTopLevelBrowser);
                    var iidBrowser = new Guid(API.Sys.IdIShellBrowser);
                    provider.QueryService(ref sidBrowser, ref iidBrowser, out browser);
                                                        //Use browser to navigate where needed
                    browser.BrowseObject(pidl, launchNew ? API.SBSP.NEWBROWSER : API.SBSP.SAMEBROWSER);
                }
                catch 
#if DEBUG
                      (Exception e)
#endif
                {
#if DEBUG
                    Log.Raw(e.ToString());
#endif
                }
                finally                                 //Cleanup
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
            else    //Modern approach
            {
                API.IExplorerBrowser browser = null;
                try
                {   //Create a browser directly and go...
                    browser = (API.IExplorerBrowser)new API.ExplorerBrowser();
                    browser.BrowseToIDList(pidl, API.SBSP.NEWBROWSER);
                }
                catch
#if DEBUG
                      (Exception e)
#endif
                {
#if DEBUG
                    Log.Raw(e.ToString());
#endif
                }
                finally //Cleanup
                {
                    if (browser != null) 
                        Marshal.ReleaseComObject(browser);
                    Marshal.FreeCoTaskMem(pidl);
                }
            }
        }
        /// <summary>
        /// Opens Explorer window for the folder that can be represented by a path,
        /// or a default folder if no command specified,
        /// or simply starts Explorer if it's dead and no command specified
        /// </summary>
        /// <param name="command">Optional. FQ-path of a FS folder to display.</param>
        public static void StartExplorer(string command = null)
        {
            const string explorerString = "explorer.exe";
            CreateProcess(explorerString, command);
        }
        /// <summary>
        /// Displays in Explorer the folder that is a container for the FS element
        /// specified as parameter, and makes explorer select the mentioned FS element.
        /// </summary>
        /// <param name="objectToSelect">FQ-path to the file or a folder on FS</param>
        public static void StartExplorerSelect(string objectToSelect)
        {
            StartExplorer("/select,\"" + objectToSelect + "\"");
        }
        /// <summary>
        /// Creates an instance of the Power8 class according t the parameters passed. If the class
        /// is a WPF Window or a WinForms form, displays it by calling <code>Show()</code>. The class 
        /// must implement the <code>IComponent</code> interface to be created via this method. Only 
        /// one instance of the class may exist at the same time. Use this method with classes inherited 
        /// from <code>DisposableWindow</code> to create singleton WPF windows, which will be activated
        /// when user clicks again on their related button or menu item.
        /// </summary>
        /// <param name="className">Half-optional. Either this parameter or <code>t</code> must be 
        /// specified. The FQ-class name, such that can be passed as parameter to 
        /// <code>Type.GetType()</code>, for example, "Power8.Views.UltraWnd". Null by default.</param>
        /// <param name="t">Half-optional. Either this parameter or a valid <code>className</code>
        /// must be specified. The type of the class being created. Null by default.</param>
        /// <param name="ctor">Optional. The function that will return the instance of the class. If 
        /// not specified, the <code>Activator.CreateInstance(t)</code> will be executed. Passing only 
        /// this parameter is not enough even if the delegate is able to produce a valid instance.</param>
        public static void InstanciateClass(string className = null, Type t = null, Func<IComponent> ctor = null)
        {
            try
            {   //Testing parameters
                if (t == null && !string.IsNullOrEmpty(className))
                    t = Type.GetType(className);

                if (t == null)
                    throw new Exception(NoLoc.Err_GotNoTypeObject);

                if (!t.GetInterfaces().Contains(typeof(IComponent)))
                    throw new Exception(NoLoc.Err_TypeIsNotIComponent);

                IComponent inst;
                if (Instances.ContainsKey(t)) //If object of this type already exists, just use it
                {
                    inst = Instances[t];
                }
                else                          //Create it
                {
                    inst = ctor != null ? ctor() : (IComponent)Activator.CreateInstance(t);
                    //We'll automaticaly remove the object when it's not needed anymore
                    inst.Disposed += (sender, args) => Instances.Remove(sender.GetType());
                    Instances.Add(t, inst);
                }
                var wnd = inst as Window;
                if (wnd != null) //Show the Window
                {
                    if (wnd.IsVisible) wnd.Hide(); //XP hack
                    wnd.Show();
                    if (wnd.WindowState == WindowState.Minimized)
                        wnd.WindowState = WindowState.Normal;
                    wnd.Activate();
                    return;
                }
                var frm = inst as Form;
                if (frm != null) //Show the Form
                {
                    if (frm.Visible) frm.Hide(); //XP hack
                    frm.Show();
                    if (frm.WindowState == FormWindowState.Minimized)
                        frm.WindowState = FormWindowState.Normal;
                    frm.Activate();
                }
            }
            catch (Exception ex) //User won't ever see this I believe... so we can use not the Dispatch... methods
            {
                MessageBox.Show(string.Format(Resources.Err_CantInstanciateClassFormatString, className, ex.Message));
            }
        }
        /// <summary>
        /// Creates a process, from just command (e.g. exe to launch or url to open or file to load, etc.) or from command 
        /// (application) and command line, or from complete startup information. Command or startInfo must be set, but only 
        /// one of them must be. 
        /// Before creating the process, updates own envioronment to comply with Explorer. This means whenever you change
        /// environment variables in system properties dialog, new processes launched will have them already updated/added.
        /// </summary>
        /// <param name="command">Application to launch, documanet to open, etc. 
        /// See <code>Process.Start(string)</code> for details.</param>
        /// <param name="args">Argiments passed to application launched when <code>command</code> points to one.
        /// See <code>Process.Start(string, string)</code> for details.</param>
        /// <param name="startInfo">Complete startup information for process.
        /// See <code>Process.Start(StartupInfo)</code> for details.</param>
        public static void CreateProcess(string command = null, string args = null, ProcessStartInfo startInfo = null)
        {
            //validate
            if (command == null && startInfo == null)
                throw new Exception("CreateProcess: both process start info and command are null!");
            if (command != null && startInfo != null)
                throw new Exception("CreateProcess: launch mode undefined: both start info and command are set!");

            //update variables
            UpdateEnvironment();

            //run
            if (command != null)
            {
                if (args != null)
                    Process.Start(command, args);
                else
                    Process.Start(command);
            }
            else
            {
                Process.Start(startInfo);
            }
        }
        /// <summary>
        /// Updates environment variables from those stored by Explorer in registry, so Power8 has same environment 
        /// variables as Explorer does. Call this method before direct or indirect creation of new child processes.
        /// </summary>
        public static void UpdateEnvironment()
        {
            var keys = new[]
            {
                new {Key = Registry.LocalMachine, Path = @"SYSTEM\CurrentControlSet\Control\Session Manager\"},
                new {Key = Registry.CurrentUser, Path = string.Empty}
            };
            foreach (var key in keys)
            {
                using (var k = key.Key.OpenSubKey(key.Path + "Environment"))
                {
                    Log.Raw("Starting process, k = " + (k == null ? "" : "not ") + "null");
                    if (k != null)
                    {
                        foreach (var valueName in k.GetValueNames())
                        {
                            var value = k.GetValue(valueName).ToString();
                            if (Environment.GetEnvironmentVariable(valueName) == value)
                                continue;
                            Environment.SetEnvironmentVariable(valueName, value);
                            Log.Fmt("Updated variable '{0}' to '{1}'", valueName, value);
                        }
                    }
                    Log.Raw("Done");
                }
            }
        }
        
        #endregion

        #region Registry data resolution

        /// <summary>
        /// Gets a resource id string representing a kind of description for object
        /// represented by a guid, either directly in form of a CLSID or in a form
        /// of a shell namespace description.
        /// </summary>
        /// <param name="clsidOrApiShNs">Guid, guid with braces, shell namespace from
        /// <code>API.ShNs</code>. The path or file extension won't work, because the 
        /// description for them is stored a bit differently; the function may be 
        /// enhanced in future to support files/extensions as well.</param>
        /// <param name="fallbackToInfoTip">If set to true, the function tries to get 
        /// also "InfoTip" value, if "LocalizedString" is not available.
        /// If in this case the InfoTip is also unavailable, function checks if the
        /// default value (fallback description for class) contains any reasonable data
        /// and returns it if so. Such data may be directly put to FriendlyName, or 
        /// may be stored to ResourceIdString to be transferrd to FriendlyName automatically
        /// when required. Util supports plane text stored as ResourceId and puts it 
        /// to FriendlyName.</param>
        /// <returns>Resource Id that, when resolved, will contain the description
        /// for the class or shell namespace represented by passed parameter, or
        /// plane resource text that should be directly (or indirectly - by storing
        /// in ResourceIdString) transferred to FriendlyName.</returns>
        public static string GetLocalizedStringResourceIdForClass(string clsidOrApiShNs, bool fallbackToInfoTip = false)
        {
            var ls = GetResourceIdForClassCommon(clsidOrApiShNs, "", "LocalizedString");
            if(ls == null && fallbackToInfoTip)
            {
                ls = GetResourceIdForClassCommon(clsidOrApiShNs, "", "InfoTip") ??
                     GetResourceIdForClassCommon(clsidOrApiShNs, "", ""); //Fallback description
                if(string.IsNullOrWhiteSpace(ls)) //don't need empty or space resId
                    ls = null;
            }
            return ls;
        }
        /// <summary>
        /// Gets a resource id string pointing to a icon for object
        /// represented by a guid, either directly in form of a CLSID or in a form
        /// of a shell namespace description, or by a file name with extension.
        /// </summary>
        /// <param name="clsidOrApiShNs">Guid, guid with braces, shell namespace from
        /// <code>API.ShNs</code>, filename with extension, either with path or without.</param>
        /// <returns>Resource Id that, when resolved, will return the icon
        /// for the class or shell namespace represented by passed parameter</returns>
        public static string GetDefaultIconResourceIdForClass(string clsidOrApiShNs)
        {
            return GetResourceIdForClassCommon(clsidOrApiShNs, "\\DefaultIcon", "");
        }
        /// <summary>
        /// Gets a pair of strings that work as open command for object
        /// represented by a guid, either directly in form of a CLSID or in a form
        /// of a shell namespace description, or by a file name with extension.
        /// In case of file, the command may contain enumerated parameters, like "%1".
        /// </summary>
        /// <param name="clsidOrApiShNs">Guid, guid with braces, shell namespace from
        /// <code>API.ShNs</code>, filename with extension, either with path or without.</param>
        /// <returns>Tuple, where Item1 is an executable application, and Item2 is a 
        /// comand for this application passed to launch the given object.</returns>
        public static Tuple<string, string> GetOpenCommandForClass(string clsidOrApiShNs)
        {
            var command = GetResourceIdForClassCommon(clsidOrApiShNs, "\\Shell\\Open\\Command", "");
            return CommandToFilenameAndArgs(command);
        }
        /// <summary>
        /// Properly registered CPLs contain "system name", a string that, when passed to
        /// "control.exe /name {sysname}" launches this CPL item. This function returns 
        /// such name for a CPL represented by a guid or a shell namespace poiner.
        /// </summary>
        /// <param name="clsidOrApiShNs">Guid, guid with braces, shell namespace from
        /// <code>API.ShNs</code> with a guid appended. Parameter must represent the
        /// Control Panel Item.</param>
        /// <returns>String that is a system regisered name for a cpl.</returns>
        public static string GetCplAppletSysNameForClass(string clsidOrApiShNs)
        {
            return GetResourceIdForClassCommon(clsidOrApiShNs, "", "System.ApplicationName");
        }
        /// <summary>
        /// Helper function that finds a proper registry key for an object passed 
        /// and gets the string placed under that key, described by other parameters.
        /// </summary>
        /// <param name="clsidOrApiShNs">Guid, guid with braces, shell namespace from
        /// <code>API.ShNs</code>, filename with extension, either with path or without.</param>
        /// <param name="subkey">Subkey name where to find information, relative to the 
        /// location in the registry where the class description for specified object 
        /// resides. May be null or empty. If not, shall start with a backslash,
        /// e.g. "\DefaultIcon".</param>
        /// <param name="valueName">Name of the registry value that contains the desired
        /// data. May be empty, which means default value ("@"), but must not be null.</param>
        /// <returns>String contained in the registry value located as escribed by 
        /// parameters passed, without any conversion done on it. If fails, returns null.</returns>
        private static string GetResourceIdForClassCommon(string clsidOrApiShNs, string subkey, string valueName)
        {//That's a simple routine: get container-open key-open value-return
// ReSharper disable EmptyGeneralCatchClause
            if (!string.IsNullOrEmpty(clsidOrApiShNs))
            {
                try
                {
                    var key = GetRegistryContainer(clsidOrApiShNs);
                    if (key != null)
                    {
                        using (var k = Registry.ClassesRoot.OpenSubKey(key + subkey, false))
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
        /// <summary>
        /// Returns the registry key located under HKCR, but withour explicit "HKCR"
        /// mentioning, which contains descriptive data for a class represented by 
        /// a guid, shell namespace pointer, or a file with extension.
        /// </summary>
        /// <param name="pathClsidGuidOrApishnamespace">Guid, or guid with braces, shell 
        /// namespace from <code>API.ShNs</code>, shell namespace with additional \guid 
        /// appended, with 2 colons ("::") or without, a filename with extension, either 
        /// with path or without.</param>
        /// <returns>See Summary on what is rerturned on success. Null is returned on 
        /// failure.</returns>
        private static string GetRegistryContainer(string pathClsidGuidOrApishnamespace)
        {   //file?
            var i = pathClsidGuidOrApishnamespace.LastIndexOf(".", StringComparison.Ordinal);
            if (i < 0) //not file
            {                      //vvvThis will parse guid
                return "CLSID\\" + NameSpaceToGuidWithBraces(pathClsidGuidOrApishnamespace);
            }
            //file
            using (var k = Registry.ClassesRoot.OpenSubKey(pathClsidGuidOrApishnamespace.Substring(i), false))
            {//"hkcr\.doc\@" => "Word.document"
                return (k != null)
                           ? ((string) k.GetValue(String.Empty, null))
                           : null;
            }
        }
        /// <summary> Converts a guid, or shell namespace, to a registry-styled {GUID} </summary>
        /// <param name="ns">A textual representation for guid,or a shell namespace 
        /// description, possibly with many levels, possibly with double colons inside.</param>
        /// <returns>Something like "{AAAAAAA-BBBB-CCCC-DDDD-EEEEFFFF}". Shell 
        /// namespace descriptors are trimmed to the latest guid in chain.</returns>
        private static string NameSpaceToGuidWithBraces(string ns)
        {
            ns = ns.Substring(ns.LastIndexOf('\\') + 1).TrimStart(':');
            if (!ns.StartsWith("{"))
                ns = "{" + ns + "}";
            return ns;
        }

        #endregion

        #region Resources extraction

        /// <summary> Loads string resource identified by resource ID and returns it  </summary>
        /// <param name="localizeableResourceId">The resource ID for a string resource, e.g.
        /// the "LocalizableString" from desktop.ini</param>
        /// <returns>String resource contents on success, null on failure</returns>
        public static string ResolveStringResource(string localizeableResourceId)
        {
            if(!(localizeableResourceId.StartsWith("@")))
                return localizeableResourceId; //when non-@ string is used for id, it is already itself
            var resData = ResolveResourceCommon(localizeableResourceId);
            if (resData.Item2 != IntPtr.Zero) //the dl was loaded ok
            {//i2 = hModule of DLL, i3 = id of resource
                lock (Buffer.Clear())
                {
                    var number = API.LoadString(resData.Item2, (uint)Math.Abs(resData.Item3), Buffer, Buffer.Capacity);
                    Log.Fmt("number: {0}, data: {1}", number, Buffer);
                    PostBackgroundDllUnload(resData.Item2);
                    if (number > 0)
                        return Buffer.ToString();
                }
            }
            return null;
        }
        /// <summary>Loads ICON resource identified by resource ID and returns the handle to it</summary>
        /// <param name="localizeableResourceId">The resource ID for a string resource, e.g.
        /// the "DefaultIcon" from desktop.ini</param>
        /// <returns>Handle to unmanaged HICON on success, or IntPtr.Zero on fail.</returns>
        public static IntPtr ResolveIconicResource(string localizeableResourceId)
        {
            var resData = ResolveResourceCommon(localizeableResourceId);
            if (resData.Item2 != IntPtr.Zero) //the dll or exe was loaded ok
            {
                var icon = resData.Item3 >= 0 //e.g. the ID was not present or index provided in resource
                    ? API.ExtractIcon(resData.Item2, resData.Item1, (uint) resData.Item3) //Get icon by index
                    : API.LoadIcon(resData.Item2, (uint) Math.Abs(resData.Item3)); //otherwise load proper resource by ID
                Log.Raw("icon => " + icon);
                PostBackgroundDllUnload(resData.Item2);
                if(icon != IntPtr.Zero) //if all above succeeded
                    return icon;

                var shinfo = new API.ShfileinfoW(); //otherwise let's make system extract icon for us
                API.SHGetFileInfo(resData.Item1, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), API.Shgfi.ICON | API.Shgfi.LARGEICON);
                return shinfo.hIcon; //and return whatever comes as result
            }
            return IntPtr.Zero; //fail
        }
        /// <summary>
        /// Parses Resource ID and returns structure of data prepared for further usage
        /// </summary>
        /// <param name="resourceString">String like the following:
        /// ResId = @%ProgramFiles%\Windows Defender\EppManifest.dll,-1000 (genaral case)
        /// or like @C:\data\a.dll,-2000#embedding8
        /// or      @B:\wakawaka\foo.dlx
        /// or      @%windir%\msm.dll,8 => 8 is index, not id 
        /// Note that when Resource string doesn't start with @ it is verbatim string and this 
        /// method shan't be called. However no error will occur, you'll only get the 
        /// Tuple &lt;ResourceId,IntPtr.Zero,0xFFFFFFFF&gt;.
        /// </param>
        /// <returns>Tuple of three items: 
        /// - full path + name of resource contained (i.e. path to dll file);
        /// - handle to this DLL loaded by LoadLibraryExW();
        /// - id of resource if it was specified, or 0xFFFFFFFF otherwise.
        /// Note that the last tuple item at the moment doesn't explicitely include 
        /// Resource Indices, so the value returned may be an index. This is not
        /// implemented because usually it's not. Method may be enhanced in future.</returns>
        private static Tuple<string, IntPtr, int> ResolveResourceCommon(string resourceString)
        {
            Log.Raw("in => " + resourceString);
            resourceString = resourceString.TrimStart('@');

            var lastCommaIdx = Math.Max(resourceString.LastIndexOf(','), 0);
            var lastSharpIdx = resourceString.LastIndexOf('#');
            
            var resDll =
                Environment.ExpandEnvironmentVariables(
                    resourceString.Substring(0, lastCommaIdx > 0 ? lastCommaIdx : resourceString.Length));

            var resId = 
                lastCommaIdx == 0 //id/index not present => default to idx 0 which is described by special value
                ? 0
                : int.Parse((lastSharpIdx > lastCommaIdx //lastSharpIdx may be bigger or -1 (not found)
                        ? resourceString.Substring(lastCommaIdx + 1, lastSharpIdx - (lastCommaIdx + 1))
                        : resourceString.Substring(lastCommaIdx + 1)).TrimStart(' '));

            var dllHandle = API.LoadLibrary(resDll, IntPtr.Zero,
                                            API.LLF.LOAD_LIBRARY_AS_DATAFILE | API.LLF.LOAD_LIBRARY_AS_IMAGE_RESOURCE);
            Log.Fmt("hModule<={0}, resId<={1}, resDll<={2}", dllHandle, resId, resDll);
            return new Tuple<string, IntPtr, int>(resDll, dllHandle, resId);
        }

        #endregion

        #region Unsorted utilities

        /// <summary>
        /// Extracts the PowerItem underlying the control that raised some event,
        /// at least tries to. Supported are <code>ContextMenuEventArgs</code>
        /// (through Reflection) and diferent <code>RoutedEventArgs</code>.
        /// </summary>
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
                if (mi != null)
                {
                    return mi.DataContext as PowerItem;
                }
            }
            if (o is RoutedEventArgs)
            {
                var obj = ((FrameworkElement)((RoutedEventArgs)o).OriginalSource).DataContext;
                return obj as PowerItem;
            }
// ReSharper restore CanBeReplacedWithTryCastAndCheckForNull
            return null;
        }
        /// <summary>
        /// Splits command to a flename to be runned and argumets to be passed to the file.
        /// Also handles web search requests (like "w server" => go to wikipedia, find server).
        /// Query is considered a web search if 2nd char is space.
        /// </summary>
        /// <param name="command">Some command line, in general case contains the EXE to 
        /// run and some argument, like "C:\my.exe myArg"</param>
        /// <returns>Tuple of 2 items: the command to run, and the command parameters to pass.
        /// Returns null in case nothing useful was passed. 
        /// Returns [command, ""] in case the filename of the file available at the moment 
        /// was passed.
        /// Returns [invokable search URI, null] for the web search (char, space, text).
        /// Returns [command, null] for all other cases (and this likely indicates that the
        /// command is broken, or it is not the valid command for the system).</returns>
        public static Tuple<string, string> CommandToFilenameAndArgs(string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                if (File.Exists(command))
                    return new Tuple<string, string>(command, string.Empty);

                if (command[1] == ' ')//web search?
                {
                    var prefix = SettingsManager.Instance.WebSearchProviders.FirstOrDefault(sp => sp.Key == command[0]);
                    if (prefix != null) //the given key was present in dictionary
                        return new Tuple<string, string>(
                            string.Format(prefix.Query, Uri.EscapeUriString(command.Substring(2))), 
                            null);
                    //otherwise won't fail, just go on
                }
                //normal file and args
                var argPtr = command[0] == '"' ? command.IndexOf('"', 1) + 1 : command.IndexOf(' ');
                if (argPtr > 0) //if arguments were found at all
                    return new Tuple<string, string>(command.Substring(0, argPtr).Trim('"'),
                                                     command.Substring(argPtr).TrimStart(' '));
                return new Tuple<string, string>(command, null);
            }
            return null;
        }
        /// <summary> Extracts description and icon from *.CPL file </summary>
        /// <param name="cplFileName">FQ-FS-path to a *.CPL file</param>
        /// <returns>Tuple [CPL description, ImageContainer(CPL icon)].
        /// If certain parts of the CPL interop workflow failed, any item 
        /// or both of them can be null(s).</returns>
        public static Tuple<string, ImageManager.ImageContainer> GetCplInfo(string cplFileName)
        {
            string name = null;
            ImageManager.ImageContainer container = null;
            var info = new API.CplInfo {lData = new IntPtr(0xDEADC0D)}; //just for dbg
            //Load CPL as DLL and perform PE init
            var hModule = API.LoadLibrary(cplFileName, IntPtr.Zero, API.LLF.AS_REGULAR_LOAD_LIBRARY);
            Log.Raw("begin, hModule<=" + hModule, cplFileName);

            if(hModule != IntPtr.Zero) //SUCCEEDED()?
            {   //Get pointer to CPL wndProc as a delegate
                var cplProcAddress = API.GetProcAddress(hModule, "CPlApplet");
                if (cplProcAddress != IntPtr.Zero)
                {
                    var cplProc = (API.CplAppletProc) Marshal.GetDelegateForFunctionPointer(
                                                                cplProcAddress, 
                                                                typeof (API.CplAppletProc));
                    var hWnd = API.GetDesktopWindow();
                    Log.Raw("doing INIT...");
                    var res = cplProc(hWnd, API.CplMsg.INIT, IntPtr.Zero, IntPtr.Zero);
                    if (res != 0)
                    {
                        Log.Raw("doing GETCOUNT...");
                        //How many CPLs are available, basically Cpl.Windows.Any()
                        res = cplProc(hWnd, API.CplMsg.GETCOUNT, IntPtr.Zero, IntPtr.Zero);
                        if (res > 0)
                        {
                            Log.Fmt("GETCOUNT returned {0}, doing INQUIRE...", res);
                            //Will work with 1st available
                            var structSize = Marshal.SizeOf(typeof (API.CplInfo));
                            var hMem = Marshal.AllocHGlobal(structSize);
                            ZeroMemory(hMem, structSize);
                            //Reserved place and cleared space for data. Now do Inquire.
                            cplProc(hWnd, API.CplMsg.INQUIRE, IntPtr.Zero, hMem);
                            Marshal.PtrToStructure(hMem, info);
                            Log.Fmt("INQUIRE returned {0}, {1}, {2}", info.idIcon, info.idInfo, info.idName);

                            var idIcon = info.idIcon;
                            var idName = info.idName == 0 ? info.idInfo : info.idName;
                            var unmanagedIcon = IntPtr.Zero;

                            if (idIcon == 0 || idName == 0) //this means not fail, but just we need to 
                            {//perform very bad operation, not recommended by MS and so on, but still
                                //actively used by many-many-many...

                                //Ok, let's go. First the memory. Let's alloc twice we might need, to
                                //expect someone mixes byte- and char-functions for Unicode... 
                                structSize = Marshal.SizeOf(typeof (API.NewCplInfoW)); 
                                hMem = Marshal.ReAllocHGlobal(hMem, new IntPtr(structSize*2));
                                ZeroMemory(hMem, structSize*2);
                                Log.Raw("doing NEWINQUIRE...");
                                cplProc(hWnd, API.CplMsg.NEWINQUIRE, IntPtr.Zero, hMem);
                                //After we did the NewInquire without specifiing the dwSize, data will be 
                                //returned in the format CPL wants. Let's check for it.
                                var gotSize = Marshal.ReadInt32(hMem);

                                if (gotSize == structSize) //NewCplInfoW
                                {
                                    var infoNew =
                                        (API.NewCplInfoW) Marshal.PtrToStructure(hMem, typeof (API.NewCplInfoW));
                                    Log.Fmt("got NewCplInfoW: {0}, {1}, {2}", infoNew.hIcon, infoNew.szInfo, infoNew.szName);                                        unmanagedIcon = infoNew.hIcon;
                                    name = infoNew.szName ?? infoNew.szInfo;
                                }
                                else if (gotSize == Marshal.SizeOf(typeof(API.NewCplInfoA)))
                                {
                                    var infoNewA =
                                        (API.NewCplInfoA)Marshal.PtrToStructure(hMem, typeof(API.NewCplInfoA));
                                    Log.Fmt("got NewCplInfoA: {0},{1},{2}", infoNewA.hIcon, infoNewA.szInfo, infoNewA.szName);
                                    unmanagedIcon = infoNewA.hIcon;
                                    name = infoNewA.szName ?? infoNewA.szInfo;
                                }
                                else
                                {
                                    Log.Fmt("NEWINQUIRE: structure size not supported: 0x{0:x} with IntPtr size 0x{1:x}", gotSize, IntPtr.Size);
                                }
                            }
                            Marshal.FreeHGlobal(hMem);
                            Log.Raw("freed, conditional load string...");

                            if (name == null) //No NewInquire or failed or has no dynamic name
                            {
                                lock (Buffer.Clear()) //load resource from Inquire
                                {
                                    if (0 < API.LoadString(hModule, idName, Buffer, Buffer.Capacity))
                                        name = Buffer.ToString();
                                }
                            }
                            Log.Fmt("name={0}, conditional load icon...", new object[]{name});

                            if(unmanagedIcon == IntPtr.Zero) //No NewInquire or failed or has no dynamic icon
                                unmanagedIcon = API.LoadIcon(hModule, idIcon);
                            Log.Raw("icon=" + unmanagedIcon);
                            if(unmanagedIcon != IntPtr.Zero) //If we have icon anyway
                            {//convert to ImageContainer and free unmanaged one
                                container = ImageManager.GetImageContainerForIconSync(cplFileName, unmanagedIcon);
                                PostBackgroundIconDestroy(unmanagedIcon);
                            }

                            Log.Raw("doing STOP...");
                            //This will affect only curent instance of CPL. At least it should...
                            cplProc(hWnd, API.CplMsg.STOP, IntPtr.Zero, info.lData);
                        }
                        Log.Raw("doing EXIT...");
                        //Same as above
                        cplProc(hWnd, API.CplMsg.EXIT, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                PostBackgroundDllUnload(hModule);
            }
            //Whatever we got anything,
            return new Tuple<string, ImageManager.ImageContainer>(name, container);
        }
        /// <summary> Fills dedicated memory area with zeros </summary>
        /// <param name="hMem">Pointer to a memory region to be cleaned. In particular, 1st byte
        /// which shall be cleared. This byte, of course, is not always the 1st byte of 
        /// allocated region.</param>
        /// <param name="cb">Size in bytes to be cleared.</param>
        private static void ZeroMemory (IntPtr hMem, int cb)
        {//Todo: why not just proxy unmanaged method?
            for (var i = 0; i < cb; i++)
                Marshal.WriteByte(hMem, i, 0);
        }
        /// <summary>
        /// Used to reset FPU before invoking UI element constructors. Experimental.
        /// Requires C runtime 8+ to be installed.
        /// Hopefully will help fixing the issue with exceptions in floating-point operations.
        /// </summary>
        public static void FpReset()
        {
            if(!SettingsManager.Instance.TryFpReset)
                return;
            var l = IntPtr.Zero;
            try
            {
                var w = Environment.ExpandEnvironmentVariables("%windir%\\system32\\");
                var dll = Directory.GetFiles(w, "msvcr*.dll", SearchOption.TopDirectoryOnly)
                                    .Where(d => !d.Contains("d."))      //no debug
                                    .Where(d => !d.Contains("clr"))     //no clr-crt
                                    .Where(d => !d.Contains("msvcrt"))  //no 6.0-runtime
                                    .OrderBy(d => int.Parse(d.Substring(w.Length + 5, 3)
                                                             .TrimEnd('.')))
                                    .LastOrDefault(); //latest varsion
                if(dll == null)
                    throw new IOException("No C runtime available");
                Log.Raw("fpreset: chosen dll " + dll);
                l = API.LoadLibrary(dll, IntPtr.Zero, API.LLF.AS_REGULAR_LOAD_LIBRARY);
                Log.Raw("fpreset: loaded at " + l);
                if(l == IntPtr.Zero)
                    throw new Win32Exception();
                var f = API.GetProcAddress(l, "_fpreset");
                Log.Raw("fpreset: address at " + f);
                if (f == IntPtr.Zero)
                    throw new Win32Exception();
                Log.Raw("fpreset: getting delegate...");
                var fpreset = (API.FpReset) Marshal.GetDelegateForFunctionPointer(f, typeof (API.FpReset));
                Log.Raw("fpreset: invoking...");
                fpreset();
            }
            catch (Exception ex)
            {
                SettingsManager.Instance.TryFpReset = false; //this is only troubleshhoting option, if it fails better turn it off
                if (l != IntPtr.Zero)
                    API.FreeLibrary(l);
                DispatchCaughtException(ex);
            }
        }
        /// <summary>
        /// Returns path to folder that contains different P8 db files, unique clientID, etc.
        /// </summary>
        public static string GetSettingsIndependentDbRoot()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Power8_Team\\";
        }

        private static Version _verCache;
        /// <summary>
        /// Returns application version
        /// </summary>
        public static Version GetAppVersion()
        {
            return _verCache ?? (_verCache = Assembly.GetEntryAssembly().GetName().Version);
        }

        #endregion

        #region Errors handling and app lifecycle management

        /// <summary> Shows warning message with exception text </summary>
        /// <param name="ex">Caught exception</param>
        public static void DispatchCaughtException(Exception ex)
        {
            Log.Raw(ex.ToString());
            MessageBox.Show(ex.Message, NoLoc.Stg_AppShortName, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        /// <summary>
        /// Shows error message with popped up unhandled exception details.
        /// Then writes same data in event log as Application Error.
        /// Then, if configered, launches new instance of Power8.
        /// Finally, quits application with code 1. 
        /// </summary>
        /// <param name="ex">The exception caused the error.</param>
        public static void DispatchUnhandledException(Exception ex)
        {
            var str = ex.ToString();
            Log.Raw(str);
            MessageBox.Show(str, NoLoc.Stg_AppShortName, MessageBoxButton.OK, MessageBoxImage.Error);
            var reason = NoLoc.Err_UnhandledGeneric + str;
            if(SettingsManager.Instance.AutoRestart)
                Restart(reason);
            else
                Die(reason);
        }
        /// <summary> Restarts Power8 writing the reason of restarting into EventLog </summary>
        /// <param name="reason">Reason to restart</param>
        public static void Restart(string reason)
        {
            CreateProcess(Application.ExecutablePath);
            Die(reason);
        }
        /// <summary> Shuts down the Power8  writing the reason of exiting into EventLog </summary>
        /// <param name="becauseString">Reason to exit</param>
        public static void Die(string becauseString)
        {
            EventLog.WriteEntry("Application Error", 
                                string.Format(NoLoc.Str_FailFastFormat, becauseString),
                                EventLogEntryType.Error);
            Environment.Exit(1);
        }

        #endregion


        /// <summary>
        /// Helper class to determine the version of runtime OS 
        /// in scope of how it is needed by Power8.
        /// </summary>
        public static class OsIs
        {
            static readonly Version Ver = Environment.OSVersion.Version; //it won't change
            //I guess nothing to document here
            public static bool XPOrLess { get { return Ver.Major < 6; } }
            public static bool VistaExact { get { return Ver.Major == 6 && Ver.Minor == 0; } }
            public static bool SevenOrMore { get { return Ver.Major > 6 || (Ver.Major == 6 && Ver.Minor >= 1); } }
            public static bool SevenOrBelow { get { return Ver.Major < 6 || (Ver.Major == 6 && Ver.Minor <= 1); } }
            public static bool EightOrMore { get { return Ver.Major > 6 || (Ver.Major == 6 && Ver.Minor >= 2); } }
            public static bool EightRpOrMore { get { return Ver >= new Version(6, 2, 8400); } } //Win8ReleasePreview+
            public static bool EightBlueOrMore { get { return Ver.Major > 6 || (Ver.Major == 6 && Ver.Minor >= 3); } } //Win8.1
        }

        /// <summary> Helper class to call unmanaged ShellExecute on STA thread 
        /// (used for Verbs invokation) </summary>
        public class ShellExecuteHelper
        {
            private readonly API.ShellExecuteInfo _executeInfo;
            private bool _succeeded;

            /// <summary> Gets the code of error occured during execution </summary>
            public int ErrorCode { get; private set; }
            /// <summary> Gets the description of error occured during execution </summary>
            public string ErrorText { get; private set; }

            /// <summary> Class constructor. Pass it an instance of ShellExecuteInfo </summary>
            public ShellExecuteHelper(API.ShellExecuteInfo executeInfo)
            {
                _executeInfo = executeInfo;
            }

            private void ShellExecuteFunction()
            {
                _succeeded = API.ShellExecuteEx(_executeInfo);
                if (_succeeded)
                    return;
                //...and if not - let's try to get what was wrong
                ErrorCode = Marshal.GetLastWin32Error();
                ErrorText = new Win32Exception(ErrorCode).Message; //FormatError()
            }

            /// <summary>
            /// Executes ShellExecute with given Info. The method insures that execution takes
            /// place on STA thread, either this, or other one. The method won't return until 
            /// the execution is finished.
            /// </summary>
            /// <returns>Bool indicating were there errors during execution (false) 
            /// or it suceeded (true).</returns>
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
    }
}