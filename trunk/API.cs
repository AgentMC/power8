using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Power8
{
    public static class API
    {
// ReSharper disable InconsistentNaming

        //Windows positioning=====================================================================================
        public const string TRAY_WND_CLASS = "Shell_TrayWnd";
        public const string TRAY_NTF_WND_CLASS = "TrayNotifyWnd";
        public const string SH_DSKTP_WND_CLASS = "TrayShowDesktopButtonWClass";
        public const string SH_DSKTP_START_CLASS = "Button";

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        //Aero Glass===============================================================================
        [DllImport("dwmapi.dll")]
        public static extern void DwmEnableBlurBehindWindow (IntPtr hWnd, DwmBlurbehind pBlurBehind);

        [DllImport("dwmapi.dll")]
        public static extern void DwmExtendFrameIntoClientArea (IntPtr hWnd, Margins pMargins);

        [StructLayout(LayoutKind.Sequential)]
        public class Margins
        {public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;}

        [StructLayout(LayoutKind.Sequential)]
        public class DwmBlurbehind
        {
            public uint dwFlags;
            [MarshalAs(UnmanagedType.Bool)] public bool fEnable;
            public IntPtr hRegionBlur;
            [MarshalAs(UnmanagedType.Bool)] public bool fTransitionOnMaximized;
            public const uint DWM_BB_ENABLE = 1;
            public const uint DWM_BB_BLURREGION = 2;
            public const uint DWM_BB_TRANSITIONONMAXIMIZED = 4;
        }


        //Utility User32 functions and data, needed in different places============================
        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, WM msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public enum WM : uint
        {
//excerpt
            SYSCOMMAND = 0x0112,
            HOTKEY = 0x312,

            NCCREATE = 0x0081,
            NCDESTROY = 0x0082,
            NCCALCSIZE = 0x0083,
            NCHITTEST = 0x0084,
            NCPAINT = 0x0085,
            NCACTIVATE = 0x0086,
            GETDLGCODE = 0x0087,
            SYNCPAINT = 0x0088,
            NCMOUSEMOVE = 0x00A0,
            NCLBUTTONDOWN = 0x00A1,
            NCLBUTTONUP = 0x00A2,
            NCLBUTTONDBLCLK = 0x00A3,
            NCRBUTTONDOWN = 0x00A4,
            NCRBUTTONUP = 0x00A5,
            NCRBUTTONDBLCLK = 0x00A6,
            NCMBUTTONDOWN = 0x00A7,
            NCMBUTTONUP = 0x00A8,
            NCMBUTTONDBLCLK = 0x00A9,
            NCXBUTTONDOWN = 0x00AB,
            NCXBUTTONUP = 0x00AC,
            NCXBUTTONDBLCLK = 0x00AD,

            DWMCOMPOSITIONCHANGED = 0x031E,
            DWMNCRENDERINGCHANGED = 0x031F,
            DWMCOLORIZATIONCOLORCHANGED = 0x0320,
            DWMWINDOWMAXIMIZEDCHANGE = 0x0321
        }

        public enum SC
        {
            SIZE = 0xF000,
            MOVE = 0xF010,
            MINIMIZE = 0xF020,
            MAXIMIZE = 0xF030,
            NEXTWINDOW = 0xF040,
            PREVWINDOW = 0xF050,
            CLOSE = 0xF060,
            VSCROLL = 0xF070,
            HSCROLL = 0xF080,
            MOUSEMENU = 0xF090,
            KEYMENU = 0xF100,
            ARRANGE = 0xF110,
            RESTORE = 0xF120,
            TASKLIST = 0xF130,
            SCREENSAVE = 0xF140,
            HOTKEY = 0xF150,
            DEFAULT = 0xF160,
            MONITORPOWER = 0xF170,
            CONTEXTHELP = 0xF180,
            SEPARATOR = 0xF00F
        }
        
        public enum HT
        {
            ERROR = (-2),
            TRANSPARENT = (-1),
            NOWHERE = 0,
            CLIENT = 1,
            CAPTION = 2,
            SYSMENU = 3,
            GROWBOX = 4,
            SIZE = GROWBOX,
            MENU = 5,
            HSCROLL = 6,
            VSCROLL = 7,
            MINBUTTON = 8,
            MAXBUTTON = 9,
            LEFT = 10,
            RIGHT = 11,
            TOP = 12,
            TOPLEFT = 13,
            TOPRIGHT = 14,
            BOTTOM = 15,
            BOTTOMLEFT = 16,
            BOTTOMRIGHT = 17,
            BORDER = 18,
            REDUCE = MINBUTTON,
            ZOOM = MAXBUTTON,
            SIZEFIRST = LEFT,
            SIZELAST = BOTTOMRIGHT,
            OBJECT = 19,
            CLOSE = 20,
            HELP = 21
        }


        //Getting icons============================================================================
        [StructLayout(LayoutKind.Sequential)]
        public struct Shfileinfo
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        [Flags]
        public enum Shgfi : uint
        {
            ICON              = 0x000000100,     // get icon
            DISPLAYNAME       = 0x000000200,     // get display name
            TYPENAME          = 0x000000400,     // get type name
            ATTRIBUTES        = 0x000000800,     // get attributes
            ICONLOCATION      = 0x000001000,     // get icon location
            EXETYPE           = 0x000002000,     // return exe type
            SYSICONINDEX      = 0x000004000,     // get system icon index
            LINKOVERLAY       = 0x000008000,     // put a link overlay on icon
            SELECTED          = 0x000010000,     // show icon in selected state
            ATTR_SPECIFIED    = 0x000020000,     // get only specified attributes
            LARGEICON         = 0x000000000,     // get large icon
            SMALLICON         = 0x000000001,     // get small icon
            OPENICON          = 0x000000002,     // get open icon
            SHELLICONSIZE     = 0x000000004,     // get shell size icon
            PIDL              = 0x000000008,     // pszPath is a pidl
            USEFILEATTRIBUTES = 0x000000010,     // use passed dwFileAttribute
            ADDOVERLAYS       = 0x000000020,     // apply the appropriate overlays
            OVERLAYINDEX      = 0x000000040      // Get the index of the overlay in the upper 8 bits of the iIcon
        }

        public enum Csidl : uint
        {
            DESKTOP                   = 0x0000,        // <desktop>
            INTERNET                  = 0x0001,        // Internet Explorer (icon on desktop)
            PROGRAMS                  = 0x0002,        // Start Menu\Programs
            CONTROLS                  = 0x0003,        // My Computer\Control Panel
            PRINTERS                  = 0x0004,        // My Computer\Printers
            PERSONAL                  = 0x0005,        // My Documents
            FAVORITES                 = 0x0006,        // <user name>\Favorites
            STARTUP                   = 0x0007,        // Start Menu\Programs\Startup
            RECENT                    = 0x0008,        // <user name>\Recent
            SENDTO                    = 0x0009,        // <user name>\SendTo
            BITBUCKET                 = 0x000a,        // <desktop>\Recycle Bin
            STARTMENU                 = 0x000b,        // <user name>\Start Menu
            MYDOCUMENTS               = PERSONAL,      //  Personal was just a silly name for My Documents
            MYMUSIC                   = 0x000d,        // "My Music" folder
            MYVIDEO                   = 0x000e,        // "My Videos" folder
            DESKTOPDIRECTORY          = 0x0010,        // <user name>\Desktop
            DRIVES                    = 0x0011,        // My Computer
            NETWORK                   = 0x0012,        // Network Neighborhood (My Network Places)
            NETHOOD                   = 0x0013,        // <user name>\nethood
            FONTS                     = 0x0014,        // windows\fonts
            TEMPLATES                 = 0x0015,
            COMMON_STARTMENU          = 0x0016,        // All Users\Start Menu
            COMMON_PROGRAMS           = 0x0017,        // All Users\Start Menu\Programs
            COMMON_STARTUP            = 0x0018,        // All Users\Startup
            COMMON_DESKTOPDIRECTORY   = 0x0019,        // All Users\Desktop
            APPDATA                   = 0x001a,        // <user name>\Application Data
            PRINTHOOD                 = 0x001b,        // <user name>\PrintHood
            LOCAL_APPDATA             = 0x001c,        // <user name>\Local Settings\Applicaiton Data (non roaming)
            ALTSTARTUP                = 0x001d,        // non localized startup
            COMMON_ALTSTARTUP         = 0x001e,        // non localized common startup
            COMMON_FAVORITES          = 0x001f,
            INTERNET_CACHE            = 0x0020,
            COOKIES                   = 0x0021,
            HISTORY                   = 0x0022,
            COMMON_APPDATA            = 0x0023,        // All Users\Application Data
            WINDOWS                   = 0x0024,        // GetWindowsDirectory()
            SYSTEM                    = 0x0025,        // GetSystemDirectory()
            PROGRAM_FILES             = 0x0026,        // C:\Program Files
            MYPICTURES                = 0x0027,        // C:\Program Files\My Pictures
            PROFILE                   = 0x0028,        // USERPROFILE
            SYSTEMX86                 = 0x0029,        // x86 system directory on RISC
            PROGRAM_FILESX86          = 0x002a,        // x86 C:\Program Files on RISC
            PROGRAM_FILES_COMMON      = 0x002b,        // C:\Program Files\Common
            PROGRAM_FILES_COMMONX86   = 0x002c,        // x86 Program Files\Common on RISC
            COMMON_TEMPLATES          = 0x002d,        // All Users\Templates
            COMMON_DOCUMENTS          = 0x002e,        // All Users\Documents
            COMMON_ADMINTOOLS         = 0x002f,        // All Users\Start Menu\Programs\Administrative Tools
            ADMINTOOLS                = 0x0030,        // <user name>\Start Menu\Programs\Administrative Tools
            CONNECTIONS               = 0x0031,        // Network and Dial-up Connections
            COMMON_MUSIC              = 0x0035,        // All Users\My Music
            COMMON_PICTURES           = 0x0036,        // All Users\My Pictures
            COMMON_VIDEO              = 0x0037,        // All Users\My Video
            RESOURCES                 = 0x0038,        // Resource Direcotry
            RESOURCES_LOCALIZED       = 0x0039,        // Localized Resource Direcotry
            COMMON_OEM_LINKS          = 0x003a,        // Links to All Users OEM specific apps
            CDBURN_AREA               = 0x003b,        // USERPROFILE\Local Settings\Application Data\Microsoft\CD Burning
            INVALID                   = 0x003c,        // Incorrect, used for init of IconContainer, reserved in API
            COMPUTERSNEARME           = 0x003d,        // Computers Near Me (computered from Workgroup membership)
            FLAG_CREATE               = 0x8000,        // combine with CSIDL_ value to force folder creation in SHGetFolderPath()
            FLAG_DONT_VERIFY          = 0x4000,        // combine with CSIDL_ value to return an unverified folder path
            FLAG_DONT_UNEXPAND        = 0x2000,        // combine with CSIDL_ value to avoid unexpanding environment variables
            FLAG_NO_ALIAS             = 0x1000,        // combine with CSIDL_ value to insure non-alias versions of the pidl
            FLAG_PER_USER_INIT        = 0x0800,        // combine with CSIDL_ value to indicate per-user init (eg. upgrade)
            FLAG_MASK                 = 0xFF00,        // mask for all possible flag values
        }

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, 
            ref Shfileinfo psfi, uint cbSizeFileInfo, Shgfi uFlags);

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(IntPtr pIdList, uint dwFileAttributes, 
            ref Shfileinfo psfi, uint cbSizeFileInfo, Shgfi uFlags);

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern int SHGetSpecialFolderLocation(IntPtr hwndOwner, Csidl nFolder, ref IntPtr ppidl);

        [DllImport("user32.dll")]
        public static extern int DestroyIcon(IntPtr hIcon);

        [DllImport("Kernel32.dll")]
        public static extern Boolean CloseHandle(IntPtr handle);


        //Invoking specific verbs (show properties)================================================
        [StructLayout(LayoutKind.Sequential)]
        public class ShellExecuteInfo
        {
            public int cbSize;
            public SEIFlags fMask;
            public IntPtr hwnd = (IntPtr)0;
            [MarshalAs(UnmanagedType.LPTStr)]
            public String lpVerb = "";
            [MarshalAs(UnmanagedType.LPTStr)]
            public String lpFile = "";
            [MarshalAs(UnmanagedType.LPTStr)]
            public String lpParameters = "";
            [MarshalAs(UnmanagedType.LPTStr)]
            public String lpDirectory = "";
            public SWCommands nShow;
            public IntPtr hInstApp = (IntPtr)0;
            public IntPtr lpIDList = (IntPtr)0;
            [MarshalAs(UnmanagedType.LPTStr)]
            public String lpClass = "";
            public IntPtr hkeyClass = (IntPtr)0;
            public int dwHotKey;
            public IntPtr hIcon = (IntPtr)0;
            public IntPtr hProcess = (IntPtr)0;

            public ShellExecuteInfo()
            {
                cbSize = Marshal.SizeOf(this);
            }
        }

        [Flags]
        public enum SEIFlags
        {
            SEE_MASK_DEFAULT = 0x00000000,
            SEE_MASK_CLASSNAME = 0x00000001,
            SEE_MASK_CLASSKEY = 0x00000003,
            SEE_MASK_IDLIST = 0x00000004,
            SEE_MASK_INVOKEIDLIST = 0x0000000C,
            SEE_MASK_ICON = 0x00000010,
            SEE_MASK_HOTKEY = 0x00000020,
            SEE_MASK_NOCLOSEPROCESS = 0x00000040,
            SEE_MASK_CONNECTNETDRV = 0x00000080,
            SEE_MASK_NOASYNC = 0x00000100,
            [EditorBrowsable(EditorBrowsableState.Never)]
            SEE_MASK_FLAG_DDEWAIT = 0x00000100,
            SEE_MASK_DOENVSUBST = 0x00000200,
            SEE_MASK_FLAG_NO_UI = 0x00000400,
            SEE_MASK_UNICODE = 0x00004000,
            SEE_MASK_NO_CONSOLE = 0x00008000,
            SEE_MASK_ASYNCOK = 0x00100000,
            SEE_MASK_NOQUERYCLASSSTORE = 0x01000000,
            SEE_MASK_HMONITOR = 0x00200000,
            SEE_MASK_NOZONECHECKS = 0x00800000,
            SEE_MASK_WAITFORINPUTIDLE = 0x02000000,
            SEE_MASK_FLAG_LOG_USAGE = 0x04000000,
        }

        [Flags]
        public enum SWCommands
        {
            SW_HIDE = 0,
            SW_MAXIMIZE = 3,
            SW_MINIMIZE = 6,
            SW_RESTORE = 9,
            SW_SHOW = 5,
            SW_SHOWDEFAULT = 10,
            SW_SHOWMAXIMIZED = 3,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOWNORMAL = 1,
        }

        public static class SEIVerbs
        {
            public const string SEV_Edit = "edit";
            public const string SEV_Explore = "explore";
            public const string SEV_Find = "find";
            public const string SEV_Open = "open";
            public const string SEV_OpenAs = "openas";
            public const string SEV_OpenNew = "opennew";
            public const string SEV_Print = "print";
            public const string SEV_Properties = "properties";
            public const string SEV_RunAsAdmin = "runas";
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = false)]
        public static extern bool ShellExecuteEx(ShellExecuteInfo info);


        //Resolving links==========================================================================
        const string CLSID_ShellLink = "00021401-0000-0000-C000-000000000046";
        const string IID_IPersistFile = "0000010b-0000-0000-C000-000000000046";
        const string IID_IPersist = "0000010c-0000-0000-c000-000000000046";
        const string IID_IShellLinkW = "000214F9-0000-0000-C000-000000000046";

        [Flags]
        public enum SLGP_FLAGS
        {
            SLGP_SHORTPATH = 0x1,
            SLGP_UNCPRIORITY = 0x2,
            SLGP_RAWPATH = 0x4,
            SLGP_RELATIVEPRIORITY = 0x8
        }

        [Flags]
        public enum SLR_FLAGS
        {
            /// <summary>
            /// Do not display a dialog box if the link cannot be resolved. When SLR_NO_UI is set,
            /// the high-order word of fFlags can be set to a time-out value that specifies the
            /// maximum amount of time to be spent resolving the link. The function returns if the
            /// link cannot be resolved within the time-out duration. If the high-order word is set
            /// to zero, the time-out duration will be set to the default value of 3,000 milliseconds
            /// (3 seconds). To specify a value, set the high word of fFlags to the desired time-out
            /// duration, in milliseconds.
            /// </summary>
            SLR_NO_UI = 0x1,
            /// <summary>Obsolete and no longer used</summary>
            SLR_ANY_MATCH = 0x2,
            /// <summary>If the link object has changed, update its path and list of identifiers.
            /// If SLR_UPDATE is set, you do not need to call IPersistFile::IsDirty to determine
            /// whether or not the link object has changed.</summary>
            SLR_UPDATE = 0x4,
            /// <summary>Do not update the link information</summary>
            SLR_NOUPDATE = 0x8,
            /// <summary>Do not execute the search heuristics</summary>
            SLR_NOSEARCH = 0x10,
            /// <summary>Do not use distributed link tracking</summary>
            SLR_NOTRACK = 0x20,
            /// <summary>Disable distributed link tracking. By default, distributed link tracking tracks
            /// removable media across multiple devices based on the volume name. It also uses the
            /// Universal Naming Convention (UNC) path to track remote file systems whose drive letter
            /// has changed. Setting SLR_NOLINKINFO disables both types of tracking.</summary>
            SLR_NOLINKINFO = 0x40,
            /// <summary>Call the Microsoft Windows Installer</summary>
            SLR_INVOKE_MSI = 0x80
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public long ftCreationTime;
            public long ftLastAccessTime;
            public long ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [ComImport, Guid(IID_IPersist),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPersist
        {
            void GetClassID(out Guid pClassID);
        }

        [ComImport, Guid(IID_IPersistFile),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPersistFile : IPersist
        {
            new void GetClassID(out Guid pClassID);
            bool IsDirty();
            void Load([In, MarshalAs(UnmanagedType.LPWStr)]string pszFileName, uint dwMode);
            void Save([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
                [In, MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([In, MarshalAs(UnmanagedType.LPWStr)] string ppszFileName);
        }

        /// <summary>The IShellLink interface allows Shell links to be created, modified, and resolved</summary>
        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid(IID_IShellLinkW)]
        public interface IShellLink
        {
            /// <summary>Retrieves the path and file name of a Shell link object</summary>
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATAW pfd, SLGP_FLAGS fFlags);
            /// <summary>Retrieves the list of item identifiers for a Shell link object</summary>
            void GetIDList(out IntPtr ppidl);
            /// <summary>Sets the pointer to an item identifier list (PIDL) for a Shell link object.</summary>
            void SetIDList(IntPtr pidl);
            /// <summary>Retrieves the description string for a Shell link object</summary>
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            /// <summary>Sets the description for a Shell link object. The description can be any application-defined string</summary>
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            /// <summary>Retrieves the name of the working directory for a Shell link object</summary>
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            /// <summary>Sets the name of the working directory for a Shell link object</summary>
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            /// <summary>Retrieves the command-line arguments associated with a Shell link object</summary>
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            /// <summary>Sets the command-line arguments for a Shell link object</summary>
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            /// <summary>Retrieves the hot key for a Shell link object</summary>
            void GetHotkey(out short pwHotkey);
            /// <summary>Sets a hot key for a Shell link object</summary>
            void SetHotkey(short wHotkey);
            /// <summary>Retrieves the show command for a Shell link object</summary>
            void GetShowCmd(out int piShowCmd);
            /// <summary>Sets the show command for a Shell link object. The show command sets the initial show state of the window.</summary>
            void SetShowCmd(int iShowCmd);
            /// <summary>Retrieves the location (path and index) of the icon for a Shell link object</summary>
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
                int cchIconPath, out int piIcon);
            /// <summary>Sets the location (path and index) of the icon for a Shell link object</summary>
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            /// <summary>Sets the relative path to the Shell link object</summary>
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            /// <summary>Attempts to find the target of a Shell link, even if it has been moved or renamed</summary>
            void Resolve(IntPtr hwnd, SLR_FLAGS fFlags);
            /// <summary>Sets the path and file name of a Shell link object</summary>
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        /// <summary> Implements IShellLink and IPersistFile COM interfaces</summary>
        [ComImport, ClassInterface(ClassInterfaceType.None)]
        [Guid(CLSID_ShellLink)]
        public class ShellLink { }

        
        //Loading native resources=================================================================
        [Flags]
        public enum LLF:uint
        {
            DONT_RESOLVE_DLL_REFERENCES         =0x00000001,
            LOAD_LIBRARY_AS_DATAFILE            =0x00000002,
            LOAD_WITH_ALTERED_SEARCH_PATH       =0x00000008,
            LOAD_IGNORE_CODE_AUTHZ_LEVEL        =0x00000010,
            LOAD_LIBRARY_AS_IMAGE_RESOURCE      =0x00000020,
            LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE  =0x00000040,
            LOAD_LIBRARY_REQUIRE_SIGNED_TARGET  =0x00000080
        }

        [DllImport("Kernel32.dll", CharSet=CharSet.Unicode, EntryPoint = "LoadLibraryExW")]
        public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName, IntPtr hFile, LLF dwFlags);

        [DllImport("Kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);
        
        [DllImport("user32.dll")]
        public static extern int LoadString(IntPtr hInstance, uint resourceID, StringBuilder lpBuffer, int nBufferMax);

        [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint = "LoadIconW")]
        public static extern IntPtr LoadIcon(IntPtr hInstance, string lpIconName);

        [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint = "LoadIconW")]
        public static extern IntPtr LoadIcon(IntPtr hInstance, uint zeroHiWordIdLoWord);

        //HotKey===================================================================================
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, fsModifiers fsModifiers, System.Windows.Forms.Keys vk);

        public enum fsModifiers
        {
            MOD_NULL = 0,
            MOD_ALT = 1,
            MOD_CONTROL = 2,
            MOD_SHIFT = 4,
            MOD_WIN = 8,
        }


        //Undocumented shell API===================================================================
        [Flags]
        public enum RFF
        {
            /// <summary>
            /// No changes to run dialog
            /// </summary>
            NORMAL = 0,
            /// <summary>
            /// Removes the browse button.
            /// </summary>
            NOBROWSE = 1,
            /// <summary>
            /// No MRU item selected.
            /// </summary>
            NODEFAULT = 2,
            /// <summary>
            /// Calculates the working directory from the file name.
            /// </summary>
            CALCDIRECTORY = 4,
            /// <summary>
            /// Removes the edit box label.
            /// </summary>
            NOLABEL = 8,
            /// <summary>
            /// Removes the Separate Memory Space check box (Windows NT only).
            /// </summary>
            NOSEPARATEMEM = 16 //14 originally at http://www.swissdelphicenter.ch/en/showcode.php?id=1181
        }

        [DllImport("shell32.dll", EntryPoint = "#61")]
        public static extern
        void SHRunDialog(IntPtr hWnd, IntPtr hIcon, string sDir, string szTitle, string szPrompt, RFF uFlags);


        //Shell namespaces, known folders, etc=====================================================
        public static class ShNs
        {//Explorer /N,{ns}
            public const string MyComputer = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
            public const string ControlPanel = @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{21EC2020-3AEA-1069-A2DD-08002B30309D}";
            public const string PrintersAndtelecopiers = @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{21EC2020-3AEA-1069-A2DD-08002B30309D}\::{2227A280-3AEA-1069-A2DE-08002B30309D}";
            public const string Fonts = @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{21EC2020-3AEA-1069-A2DD-08002B30309D}\::{D20EA4E1-3957-11d2-A40B-0C5020524152}";
            public const string ScannersAndCameras = @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{21EC2020-3AEA-1069-A2DD-08002B30309D}\::{E211B736-43FD-11D1-9EFB-0000F8757FCD}";
            public const string NetworkNeighbourhood = @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{21EC2020-3AEA-1069-A2DD-08002B30309D}\::{7007ACC7-3202-11D1-AAD2-00805FC1270E}";
            public const string AdministrationTools = @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{21EC2020-3AEA-1069-A2DD-08002B30309D}\::{D20EA4E1-3957-11d2-A40B-0C5020524153}";
            public const string TasksScheduler = @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{21EC2020-3AEA-1069-A2DD-08002B30309D}\::{D6277990-4C6A-11CF-8D87-00AA0060F5BF}";
            public const string WebFolders = @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\::{BDEADF00-C265-11D0-BCED-00A0C90AB50F}";
            public const string MyDocuments = "::{450D8FBA-AD25-11D0-98A8-0800361B1103}";
            public const string RecycleBin = "::{645FF040-5081-101B-9F08-00AA002F954E}";
            public const string NetworkFavorites = "::{208D2C60-3AEA-1069-A2D7-08002B30309D}";
            public const string DefaultNavigator = "::{871C5380-42A0-1069-A2EA-08002B30309D}";
            public const string ComputerSearchResultsFolder = "::{1F4DE370-D627-11D1-BA4F-00A0C91EEDBA}";
            public const string NetworkSearchResultsComputer = "::{E17D4FC0-5564-11D1-83F2-00A0C90DC849}";
            public const string Libraries = "::{031E4825-7B94-4dc3-B131-E946B44C8DD5}";
        }

        public enum KFF : uint
        {
            NO_APPCONTAINER_REDIRECTION = 0x00010000,
            CREATE = 0x00008000,
            DONT_VERIFY = 0x00004000,
            DONT_UNEXPAND = 0x00002000,
            NO_ALIAS = 0x00001000,
            INIT = 0x00000800,
            DEFAULT_PATH = 0x00000400,
            NOT_PARENT_RELATIVE = 0x00000200,
            SIMPLE_IDLIST = 0x00000100,
            ALIAS_ONLY = 0x80000000,
            NORMAL = 0
        }

        public static class KnFldrIds
        {
            public static string AddNewPrograms	 = "{de61d971-5ebc-4f02-a3a9-6c82895e5c04}";
            public static string AdminTools	 = "{724EF170-A42D-4FEF-9F26-B60E846FBA4F}";
            public static string ApplicationShortcuts	 = "{A3918781-E5F2-4890-B3D9-A7E54332328C}";
            public static string AppsFolder	 = "{1e87508d-89c2-42f0-8a7e-645a0f50ca58}";
            public static string AppUpdates	 = "{a305ce99-f527-492b-8b1a-7e76fa98d6e4}";
            public static string CDBurning	 = "{9E52AB10-F80D-49DF-ACB8-4330F5687855}";
            public static string ChangeRemovePrograms	 = "{df7266ac-9274-4867-8d55-3bd661de872d}";
            public static string CommonAdminTools	 = "{D0384E7D-BAC3-4797-8F14-CBA229B392B5}";
            public static string CommonOEMLinks	 = "{C1BAE2D0-10DF-4334-BEDD-7AA20B227A9D}";
            public static string CommonPrograms	 = "{0139D44E-6AFE-49F2-8690-3DAFCAE6FFB8}";
            public static string CommonStartMenu	 = "{A4115719-D62E-491D-AA7C-E74B8BE3B067}";
            public static string CommonStartup	 = "{82A5EA35-D9CD-47C5-9629-E15D2F714E6E}";
            public static string CommonTemplates	 = "{B94237E7-57AC-4347-9151-B08C6C32D1F7}";
            public static string ComputerFolder	 = "{0AC0837C-BBF8-452A-850D-79D08E667CA7}";
            public static string ConflictFolder	 = "{4bfefb45-347d-4006-a5be-ac0cb0567192}";
            public static string ConnectionsFolder	 = "{6F0CD92B-2E97-45D1-88FF-B0D186B8DEDD}";
            public static string Contacts	 = "{56784854-C6CB-462b-8169-88E350ACB882}";
            public static string ControlPanelFolder	 = "{82A74AEB-AEB4-465C-A014-D097EE346D63}";
            public static string Cookies	 = "{2B0F765D-C0E9-4171-908E-08A611B84FF6}";
            public static string Desktop	 = "{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}";
            public static string DeviceMetadataStore	 = "{5CE4A5E9-E4EB-479D-B89F-130C02886155}";
            public static string DocumentsLibrary	 = "{7B0DB17D-9CD2-4A93-9733-46CC89022E7C}";
            public static string Downloads	 = "{374DE290-123F-4565-9164-39C4925E467B}";
            public static string Favorites	 = "{1777F761-68AD-4D8A-87BD-30B759FA33DD}";
            public static string Fonts	 = "{FD228CB7-AE11-4AE3-864C-16F3910AB8FE}";
            public static string Games	 = "{CAC52C1A-B53D-4edc-92D7-6B2E8AC19434}";
            public static string GameTasks	 = "{054FAE61-4DD8-4787-80B6-090220C4B700}";
            public static string History	 = "{D9DC8A3B-B784-432E-A781-5A1130A75963}";
            public static string HomeGroup	 = "{52528A6B-B9E3-4ADD-B60D-588C2DBA842D}";
            public static string HomeGroupCurrentUser	 = "{9B74B6A3-0DFD-4f11-9E78-5F7800F2E772}";
            public static string ImplicitAppShortcuts	 = "{BCB5256F-79F6-4CEE-B725-DC34E402FD46}";
            public static string InternetCache	 = "{352481E8-33BE-4251-BA85-6007CAEDCF9D}";
            public static string InternetFolder	 = "{4D9F7874-4E0C-4904-967B-40B0D20C3E4B}";
            public static string Libraries	 = "{1B3EA5DC-B587-4786-B4EF-BD1DC332AEAE}";
            public static string Links	 = "{bfb9d5e0-c6a9-404c-b2b2-ae6db6af4968}";
            public static string LocalAppData	 = "{F1B32785-6FBA-4FCF-9D55-7B8E7F157091}";
            public static string LocalAppDataLow	 = "{A520A1A4-1780-4FF6-BD18-167343C5AF16}";
            public static string LocalizedResourcesDir	 = "{2A00375E-224C-49DE-B8D1-440DF7EF3DDC}";
            public static string Music	 = "{4BD8D571-6D19-48D3-BE97-422220080E43}";
            public static string MusicLibrary	 = "{2112AB0A-C86A-4FFE-A368-0DE96E47012E}";
            public static string NetHood	 = "{C5ABBF53-E17F-4121-8900-86626FC2C973}";
            public static string NetworkFolder	 = "{D20BEEC4-5CA8-4905-AE3B-BF251EA09B53}";
            public static string OriginalImages	 = "{2C36C0AA-5812-4b87-BFD0-4CD0DFB19B39}";
            public static string PhotoAlbums	 = "{69D2CF90-FC33-4FB7-9A0C-EBB0F0FCB43C}";
            public static string PicturesLibrary	 = "{A990AE9F-A03B-4E80-94BC-9912D7504104}";
            public static string Pictures	 = "{33E28130-4E1E-4676-835A-98395C3BC3BB}";
            public static string Playlists	 = "{DE92C1C7-837F-4F69-A3BB-86E631204A23}";
            public static string PrintersFolder	 = "{76FC4E2D-D6AD-4519-A663-37BD56068185}";
            public static string PrintHood	 = "{9274BD8D-CFD1-41C3-B35E-B13F55A758F4}";
            public static string Profile	 = "{5E6C858F-0E22-4760-9AFE-EA3317B67173}";
            public static string ProgramData	 = "{62AB5D82-FDC1-4DC3-A9DD-070D1D495D97}";
            public static string ProgramFiles	= "{905e63b6-c1bf-494e-b29c-65b732d3d21a}";
            public static string ProgramFilesX64	= "{6D809377-6AF0-444b-8957-A3773F02200E}";
            public static string ProgramFilesX86	= "{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}";
            public static string ProgramFilesCommon = "{F7F1ED05-9F6D-47A2-AAAE-29D317C6F066}";
            public static string ProgramFilesCommonX64	 = "{6365D5A7-0F0D-45E5-87F6-0DA56B6A4F7D}";
            public static string ProgramFilesCommonX86	= "{DE974D24-D9C6-4D3E-BF91-F4455120B917}";
            public static string Programs	 = "{A77F5D77-2E2B-44C3-A6A2-ABA601054A51}";
            public static string Public	 = "{DFDF76A2-C82A-4D63-906A-5644AC457385}";
            public static string PublicDesktop	 = "{C4AA340D-F20F-4863-AFEF-F87EF2E6BA25}";
            public static string PublicDocuments	 = "{ED4824AF-DCE4-45A8-81E2-FC7965083634}";
            public static string PublicDownloads	 = "{3D644C9B-1FB8-4f30-9B45-F670235F79C0}";
            public static string PublicGameTasks	 = "{DEBF2536-E1A8-4c59-B6A2-414586476AEA}";
            public static string PublicLibraries	 = "{48DAF80B-E6CF-4F4E-B800-0E69D84EE384}";
            public static string PublicMusic	 = "{3214FAB5-9757-4298-BB61-92A9DEAA44FF}";
            public static string PublicPictures	 = "{B6EBFB86-6907-413C-9AF7-4FC2ABF07CC5}";
            public static string PublicRingtones	 = "{E555AB60-153B-4D17-9F04-A5FE99FC15EC}";
            public static string PublicUserTiles	 = "{0482af6c-08f1-4c34-8c90-e17ec98b1e17}";
            public static string PublicVideos	 = "{2400183A-6185-49FB-A2D8-4A392A602BA3}";
            public static string QuickLaunch	 = "{52a4f021-7b75-48a9-9f6b-4b87a210bc8f}";
            public static string Recent	 = "{AE50C081-EBD2-438A-8655-8A092E34987A}";
            public static string RecordedTVLibrary	 = "{1A6FDBA2-F42D-4358-A798-B74D745926C5}";
            public static string RecycleBinFolder	 = "{B7534046-3ECB-4C18-BE4E-64CD4CB7D6AC}";
            public static string ResourceDir	 = "{8AD10C31-2ADB-4296-A8F7-E4701232C972}";
            public static string Ringtones	 = "{C870044B-F49E-4126-A9C3-B52A1FF411E8}";
            public static string RoamingAppData	 = "{3EB685DB-65F9-4CF6-A03A-E3EF65729F3D}";
            public static string RoamingTiles	 = "{00BCFC5A-ED94-4e48-96A1-3F6217F21990}";
            public static string SampleMusic	 = "{B250C668-F57D-4EE1-A63C-290EE7D1AA1F}";
            public static string SamplePictures	 = "{C4900540-2379-4C75-844B-64E6FAF8716B}";
            public static string SamplePlaylists	 = "{15CA69B3-30EE-49C1-ACE1-6B5EC372AFB5}";
            public static string SampleVideos	 = "{859EAD94-2E85-48AD-A71A-0969CB56A6CD}";
            public static string SavedGames	 = "{4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4}";
            public static string SavedSearches	 = "{7d1d3a04-debb-4115-95cf-2f29da2920da}";
            public static string SEARCH_CSC	 = "{ee32e446-31ca-4aba-814f-a5ebd2fd6d5e}";
            public static string SEARCH_MAPI	 = "{98ec0e18-2098-4d44-8644-66979315a281}";
            public static string SearchHome	 = "{190337d1-b8ca-4121-a639-6d472d16972a}";
            public static string SendTo	 = "{8983036C-27C0-404B-8F08-102D10DCFD74}";
            public static string SidebarDefaultParts	 = "{7B396E54-9EC5-4300-BE0A-2482EBAE1A26}";
            public static string SidebarParts	 = "{A75D362E-50FC-4fb7-AC2C-A8BEAA314493}";
            public static string StartMenu	 = "{625B53C3-AB48-4EC1-BA1F-A1EF4146FC19}";
            public static string Startup	 = "{B97D20BB-F46A-4C97-BA10-5E3608430854}";
            public static string SyncManagerFolder	 = "{43668BF8-C14E-49B2-97C9-747784D784B7}";
            public static string SyncResultsFolder	 = "{289a9a43-be44-4057-a41b-587a76d7e7f9}";
            public static string SyncSetupFolder	 = "{0F214138-B1D3-4a90-BBA9-27CBC0C5389A}";
            public static string System	 = "{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}";
            public static string SystemX86	 = "{D65231B0-B2F1-4857-A4CE-A8E7C6EA7D27}";
            public static string Templates	 = "{A63293E8-664E-48DB-A079-DF759E0509F7}";
            public static string UserPinned	 = "{9E3995AB-1F9C-4F13-B827-48B24B6C7174}";
            public static string UserProfiles	 = "{0762D272-C50A-4BB0-A382-697DCD729B80}";
            public static string UserProgramFiles	 = "{5CD7AEE2-2219-4A67-B85D-6C9CE15660CB}";
            public static string UserProgramFilesCommon	 = "{BCBD3057-CA5C-4622-B42D-BC56DB0AE516}";
            public static string UsersFiles	 = "{f3ce0f7c-4901-4acc-8648-d5d44b04ef8f}";
            public static string UsersLibraries	 = "{A302545D-DEFF-464b-ABE8-61C8648D939B}";
            public static string UserTiles	 = "{008ca0b1-55b4-4c56-b8a8-4de4b299d3be}";
            public static string Videos	 = "{18989B1D-99B5-455B-841C-AB7C74E4DDFC}";
            public static string VideosLibrary	 = "{491E922F-5643-4AF4-A7EB-4E7A138D8174}";
            public static string Windows = "{F38BF404-1D43-42F2-9305-67DE0B28FC23}";
        }

        [DllImport("shell32.dll", CharSet=CharSet.Unicode)]
        public static extern
        void SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, KFF dwFlags, IntPtr hToken, out IntPtr ppwszPath);
// ReSharper restore InconsistentNaming
    }
}
