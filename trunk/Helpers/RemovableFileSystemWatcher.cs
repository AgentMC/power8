using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Power8.Helpers
{
    /// <summary>
    /// This is the FileSystemWatcher that can react on the system "undock device" notifications.
    /// To work it requires:
    /// 1) a window with WndProc to Register the notifications
    /// 2) this window to call ProcessDeviceNotification when corresponding event arrives
    /// Currently this bind is done via DriveManager.
    /// </summary>
    class RemovableFileSystemWatcher : FileSystemWatcher
    {
        private readonly IntPtr _reporter;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path">Root of what to watch. See parent class docs for help 
        /// on this parameter.</param>
        /// <param name="reportingHwnd">HWND of the window that will e used as reporting
        /// proxy. See ProcessDeviceNotification() method documentation, and also the 
        /// DriveManager class for more info.</param>
        public RemovableFileSystemWatcher(string path, IntPtr reportingHwnd) : base(path)
        {
            _reporter = reportingHwnd;
        }

        new public bool EnableRaisingEvents
        {
            get { return base.EnableRaisingEvents; }
            set
            {
                Log.Raw("Value passed: " + value);
                base.EnableRaisingEvents = value;
                if (value) Lock();
                else Unlock();
            }
        }

        private IntPtr _hNotification = IntPtr.Zero, _hDrive = IntPtr.Zero;

        private void Unlock()
        {
            if (_hNotification != IntPtr.Zero)
            {
                API.UnregisterDeviceNotification(_hNotification);
                _hNotification = IntPtr.Zero;
            }
            if (_hDrive != IntPtr.Zero)
            {
                API.CloseHandle(_hDrive);
                _hDrive = IntPtr.Zero;
            }
            Log.Raw("Unlocked " + Path);
        }

        private void Lock()
        {
            var file = API.CreateFile(string.Format(@"\\.\{0}:", Path[0]),
                                      FileAccess.Read,
                                      FileShare.ReadWrite,
                                      IntPtr.Zero,
                                      FileMode.Open,
                                      0,
                                      IntPtr.Zero);
            if (file == IntPtr.Zero || file.ToInt32() == -1)
            {
                Log.Raw("Cannot CreateFile " + Path);
                return; //might be real fixed drive
            }

            _hDrive = file;

            var msg = new API.DEV_BROADCAST_HANDLE { dbch_handle = file };
            _hNotification = API.RegisterDeviceNotification(_reporter, msg, API.RDNFlags.DEVICE_NOTIFY_WINDOW_HANDLE);
            
            if (_hNotification == IntPtr.Zero || _hNotification.ToInt32() == -1)
            {
                Log.Raw("Cannot lock " + Path);
                Unlock(); //this drive appears to be non-notifiable
            }
            else
            {
                Log.Raw("Locked " + Path);
            }
        }

        /// <summary>
        /// This method must be called from the WndProcof a window identified by HWND passed to
        /// constructor of this class in case of WM_DEVICECHANGE event occurs. See DriveManager
        /// class for more details on parameters.
        /// </summary>
        /// <param name="wParam">The wParam from message, as is</param>
        /// <param name="lParam">The lParam from message, as is</param>
        /// <returns>Logical value, indicating whether this instance should be the last one
        /// that processes the message. True may be returned because the message itself doesn't  
        /// satisfy the requirements of a class, or because this instance is identified itself  
        /// against the message and performed targeted processing. False means that themessage  
        /// should be also passed for processing to some other instance of this class within the 
        /// application.</returns>
        public bool ProcessDeviceNotification(IntPtr wParam, IntPtr lParam)
        {
            var code = (API.DeviceChangeMessages) wParam;

            switch (code)
            {
                case API.DeviceChangeMessages.DBT_DEVICEQUERYREMOVE:
                case API.DeviceChangeMessages.DBT_DEVICEQUERYREMOVEFAILED:
                case API.DeviceChangeMessages.DBT_CUSTOMEVENT:
                    return Process(code, lParam);
                default:
                    return true;
            }
        }

        private bool Process(API.DeviceChangeMessages wParam, IntPtr lParam)
        {
            var o = new API.DEV_BROADCAST_HANDLE();
            if (lParam != IntPtr.Zero) Marshal.PtrToStructure(lParam, o);

            if (o.dbch_hdevnotify != _hNotification)
            {
                return false;
            }

            Log.Fmt("Device event {0}, custom event guid {1}", wParam, o.dbch_eventguid);

            switch (wParam)
            {
                case API.DeviceChangeMessages.DBT_DEVICEQUERYREMOVE:
                    EnableRaisingEvents = false;
                    break;
                case API.DeviceChangeMessages.DBT_DEVICEQUERYREMOVEFAILED:
                    EnableRaisingEvents = true;
                    break;
                case API.DeviceChangeMessages.DBT_CUSTOMEVENT:
                    if (API.DevEvent.Queryable.Contains(o.dbch_eventguid))
                    {
                        EnableRaisingEvents = false;
                    }
                    else if (API.DevEvent.Failed.Contains(o.dbch_eventguid))
                    {
                        EnableRaisingEvents = true;
                    }
                    break;
            }
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            Log.Raw("Dispose called");
            try
            {
                Unlock();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}