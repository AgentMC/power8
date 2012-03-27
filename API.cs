using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Power8
{
    public static class API
    {
// ReSharper disable InconsistentNaming
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

        public static void MakeGlass(IntPtr hWnd)
        {
            var bbhOff = new DwmBlurbehind
                            {
                                dwFlags = DwmBlurbehind.DWM_BB_ENABLE | DwmBlurbehind.DWM_BB_BLURREGION,
                                fEnable = false,
                                hRegionBlur = IntPtr.Zero
                            };
            DwmEnableBlurBehindWindow(hWnd, bbhOff);
            DwmExtendFrameIntoClientArea(hWnd, new Margins { cxLeftWidth = -1, cxRightWidth = 0, cyTopHeight = 0, cyBottomHeight = 0 });
        }

        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        public const int SC_SCREENSAVE = 0xF140;
        public const int WM_SYSCOMMAND = 0x0112;


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
        public enum Shgfi
        {
           SHGFI_ICON = 0x100,
           SHGFI_LARGEICON = 0x0, // 'Large icon
           SHGFI_SMALLICON = 0x1 // 'Small icon
        }

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref Shfileinfo psfi, uint cbSizeFileInfo, uint uFlags);


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
// ReSharper restore InconsistentNaming
    }
}
