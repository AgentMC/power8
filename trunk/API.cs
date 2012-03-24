using System;
using System.Runtime.InteropServices;

namespace Power8
{
    public static class API
    {
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
// ReSharper disable InconsistentNaming
           SHGFI_ICON = 0x100,
           SHGFI_LARGEICON = 0x0, // 'Large icon
           SHGFI_SMALLICON = 0x1 // 'Small icon
// ReSharper restore InconsistentNaming
        }

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref Shfileinfo psfi, uint cbSizeFileInfo, uint uFlags);

        public static IntPtr GetIconForFile(string file, Shgfi iconType)
        {
            var shinfo = new Shfileinfo();
            var hImgSmall = SHGetFileInfo(file, 0, ref shinfo,(uint)Marshal.SizeOf(shinfo), (uint) (Shgfi.SHGFI_ICON | iconType));
            return hImgSmall == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero ? IntPtr.Zero : shinfo.hIcon;
        }
                

    }
}
