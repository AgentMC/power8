using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Power8
{
    static class Util
    {
        private static readonly StringBuilder Builder = new StringBuilder(1024);

        public static IntPtr GetHandle(this Window w)
        {
            return new WindowInteropHelper(w).Handle;
        }

        public static IntPtr MakeGlassWpfWindow(this Window w)
        {
            var handle = w.GetHandle();
            HwndSource.FromHwnd(handle).CompositionTarget.BackgroundColor = Colors.Transparent;
            if (Environment.OSVersion.Version.Major >= 6) 
                MakeGlass(handle);
            return handle;
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

        public static IntPtr GetIconForFile(string file, API.Shgfi iconType)
        {
            var shinfo = new API.Shfileinfo();
            var hImgSmall = API.SHGetFileInfo(file, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(API.Shgfi.SHGFI_ICON | iconType));
            return hImgSmall == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero ? IntPtr.Zero : shinfo.hIcon;
        }

        public static string ResolveLink(string link)
        {
            var shLink = new API.ShellLink();
            API.WIN32_FIND_DATAW sd;
            ((API.IPersistFile)shLink).Load(link, 0);
            ((API.IShellLink) shLink).GetPath(Builder, 512, out sd, API.SLGP_FLAGS.SLGP_UNCPRIORITY);
            return Builder.ToString();
        }

        public class ShellExecuteHelper
        {
            private readonly API.ShellExecuteInfo _executeInfo;
            private int _errorCode;
            private bool _succeeded;

            public int ErrorCode
            {
                get
                {
                    return _errorCode;
                }
            }

            public ShellExecuteHelper(API.ShellExecuteInfo executeInfo)
            {
                _executeInfo = executeInfo;
            }

            private void ShellExecuteFunction()
            {
                if ((_succeeded = API.ShellExecuteEx(_executeInfo)) == true)
                    return;
                _errorCode = Marshal.GetLastWin32Error();
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
    }
}
