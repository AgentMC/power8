using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Power8
{
    static class Util
    {
        public static IntPtr GetHandle(this Window w)
        {
            return new WindowInteropHelper(w).Handle;
        }

        public static IntPtr MakeGlassWpfWindow(this Window w)
        {
            var handle = w.GetHandle();
            HwndSource.FromHwnd(handle).CompositionTarget.BackgroundColor = Colors.Transparent;
            API.MakeGlass(handle);
            return handle;
        }

        public static IntPtr GetIconForFile(string file, API.Shgfi iconType)
        {
            var shinfo = new API.Shfileinfo();
            var hImgSmall = API.SHGetFileInfo(file, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(API.Shgfi.SHGFI_ICON | iconType));
            return hImgSmall == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero ? IntPtr.Zero : shinfo.hIcon;
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
                    Thread thread = new Thread(ShellExecuteFunction);
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
