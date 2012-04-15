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


        //Shell namespaces=========================================================================
        //http://www.codeproject.com/Articles/13280/How-to-display-Windows-Explorer-objects-in-one-com
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
        }
// ReSharper restore InconsistentNaming
    }
}
