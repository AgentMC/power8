using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;

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
        public static extern void DwmEnableBlurBehindWindow (IntPtr hWnd, DWM_BLURBEHIND pBlurBehind);

        [DllImport("dwmapi.dll")]
        public static extern void DwmExtendFrameIntoClientArea (IntPtr hWnd, MARGINS pMargins);

	    [StructLayout(LayoutKind.Sequential)]
        public class MARGINS
		{public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;}

	    [StructLayout(LayoutKind.Sequential)]
        public class DWM_BLURBEHIND
        {
		    public uint dwFlags;
		    [MarshalAs(UnmanagedType.Bool)] public bool fEnable;
		    public IntPtr hRegionBlur;
		    [MarshalAs(UnmanagedType.Bool)] public bool fTransitionOnMaximized;
		    public const uint DWM_BB_ENABLE = 1;
		    public const uint DWM_BB_BLURREGION = 2;
		    public const uint DWM_BB_TRANSITIONONMAXIMIZED = 4;
	    }

        internal static void MakeGlass(IntPtr hWnd)
		{
            var bbhOff = new DWM_BLURBEHIND();
		    bbhOff.dwFlags = DWM_BLURBEHIND.DWM_BB_ENABLE | DWM_BLURBEHIND.DWM_BB_BLURREGION;
		    bbhOff.fEnable = false;
		    bbhOff.hRegionBlur = IntPtr.Zero;
		    DwmEnableBlurBehindWindow(hWnd, bbhOff);
            DwmExtendFrameIntoClientArea(hWnd, new MARGINS() { cxLeftWidth = -1, cxRightWidth = 0, cyTopHeight = 0, cyBottomHeight = 0 });
	    }

    }
}
