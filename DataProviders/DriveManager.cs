using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Power8.Helpers;
using Power8.Views;

namespace Power8
{
    /// <summary>
    /// This class monitors list of drives in the system. It also maintains file system watchers for each drive
    /// </summary>
    public static class DriveManager
    {
        private static readonly List<RemovableFileSystemWatcher> Watchers = new List<RemovableFileSystemWatcher>();
        private static readonly List<string> DriveNames = new List<string>();
        private static readonly List<string> BlackList = new List<string>();
        private static readonly ConcurrentQueue<FileSystemEventArgs> FsQueue = new ConcurrentQueue<FileSystemEventArgs>();

        private static FileSystemEventHandler _fileChanged;
        private static RenamedEventHandler _fileRenamed;
        private static PowerItem _drivesRoot;
        private static IntPtr _reportingHandle;
        
        /// <summary>
        /// This is used to cache last queried list of drives, to redce time to call GetDriveLabel()
        /// </summary>
        private static DriveInfo[] _drives;

        /// <summary>
        /// Runs the class. Starts the drive watcher thread and saves passed parameters
        /// </summary>
        /// <param name="changedHandler">Delegate to be called when a file or folder is changed/created/deleted in the system</param>
        /// <param name="renamedHandler">Dalegate to be called when a file or folder is renamed in the system</param>
        /// <param name="drivesRoot">PowerItem, under which drives list is located</param>
        public static void Init(FileSystemEventHandler changedHandler, RenamedEventHandler renamedHandler, PowerItem drivesRoot)
        {
            _fileChanged = changedHandler;
            _fileRenamed = renamedHandler;
            _drivesRoot = drivesRoot;
            SettingsManager.WatchRemovablesChanged += SettingsManagerOnWatchRemovablesChanged;
            Util.ForkStart(Worker, "DriveWatchThread");
            Util.ForkStart(FsWorker, "File system events dequeuer");
        }

        public static void SetReporter(IntPtr hWnd)
        {
            _reportingHandle = hWnd;
        }

        /// <summary>
        /// Reacts on the event raised by Settings Manager when user changes the "Watch removable drives" checkbox.
        /// Does nothing on true 
        /// </summary>
        private static void SettingsManagerOnWatchRemovablesChanged(object sender, EventArgs eventArgs)
        {
            if (!SettingsManager.Instance.WatchRemovableDrives) //Must remove items from collection and disose handles
            {
                lock (DriveNames)
                {
                    var rDrvNames = _drives.Where(d => d.DriveType == DriveType.Removable)
                                           .Select(d => d.Name)
                                           .ToList();
                    rDrvNames.ForEach(StopWatcher);
                }
            }
        }

        /// <summary>
        /// The background thread that watches the drives in the system, querying them every 5 seconds
        /// </summary>
        private static void Worker ()
        {
begin:
            lock (DriveNames) //To ensure no InvalidOperationException occurs in GetDriveLabel()
            {                 //Also syncs handler above
                _drives = DriveInfo.GetDrives();

                //Have some drives been removed?
                for (int i = DriveNames.Count - 1; i >= 0; i--)
                {
                    var dName = DriveNames[i];
                    if (_drives.All(d => d.Name != dName)) //drive removed
                    {
                        StopWatcher(dName); //stop the COM watcher and remove drive from collection
                    }
                }
                //Has blacklisted drive been removed?
                BlackList.RemoveAll(bl => _drives.All(d => d.Name != bl));

                //Have some drives been addded?
                foreach (var driveInfo in _drives)
                {
                    var dName = driveInfo.Name;
                    if (DriveNames.All(d => d != dName)) //drive added
                    {
                        if (IsDriveValid(driveInfo))
                        {
                            RemovableFileSystemWatcher w;
                            try
                            {
                                w = new RemovableFileSystemWatcher(dName, _reportingHandle);
                            }
                            catch (ArgumentException ex)
                            {
                                Debug.WriteLine("DriveManager: cannot inistantiate watcher for {0}, reason:\r\n{1}",
                                    dName, ex.Message);
                                BlackList.Add(dName);
                                continue;
                            }
                            DriveNames.Add(dName); //Add drive to collection
                            var fn = GetFormattedDriveLabel(dName); //override deadlock
                            Util.Send(() => //Add drive to UI
                                      _drivesRoot.Items.Add(new PowerItem
                                                                {
                                                                    Argument = dName,
                                                                    AutoExpand = true,
                                                                    IsFolder = true,
                                                                    Parent = _drivesRoot,
                                                                    NonCachedIcon = true,
                                                                    FriendlyName = fn
                                                                }));
                            //Add drive watcher
                            w.Created += PushEvent;
                            w.Deleted += PushEvent;
                            w.Changed += PushEvent;
                            w.Renamed += PushEvent;
                            w.IncludeSubdirectories = true;
                            Watchers.Add(w);
                            if (BtnStck.IsInstantited)
                                StartWatcher(w);
                            else
                                BtnStck.Instanciated += (sender, args) => StartWatcher(w);
                        }
                    }
                }
            }

            if (!Util.MainDisp.HasShutdownStarted)
            {
                Thread.Sleep(5000);
                goto begin; //just don't want unneeded code nesting here, anyway IL will be same.
            }
            DriveNames.ForEach(StopWatcher);
        }

        /// <summary>
        /// Handles file system event to be processed later by FsWorker
        /// </summary>
        private static void PushEvent(object sender, FileSystemEventArgs e)
        {
            if (e != null) FsQueue.Enqueue(e);
        }

        /// <summary>
        /// The background thread that reacts on file system events and forwards events to actual handlers
        /// </summary>
        private static void FsWorker()
        {
            begin:

            while (FsQueue.Count > 0 && !Util.MainDisp.HasShutdownStarted)
            {
                FileSystemEventArgs e;
                if (FsQueue.TryDequeue(out e))
                {
                    var x = e as RenamedEventArgs;
                    if (x == null)
                        _fileChanged(null, e);
                    else
                        _fileRenamed(null, x);
                }
                else
                {
                    break;
                }
            }

            if (!Util.MainDisp.HasShutdownStarted)
            {
                Thread.Sleep(333);
                goto begin; //just don't want unneeded code nesting here, anyway IL will be same.
            }
        }

        /// <summary>
        /// Gets the vaue indicating can Power8 handle the drive or not
        /// </summary>
        /// <param name="driveInfo">The structure describing the drive</param>
        private static bool IsDriveValid(DriveInfo driveInfo)
        {
            var dNameUcase = driveInfo.Name.ToUpper();
            return
                (( driveInfo.DriveType == DriveType.Fixed 
                || driveInfo.DriveType == DriveType.Network
                || driveInfo.DriveType == DriveType.Ram)
                ||(driveInfo.DriveType == DriveType.Removable
                   && SettingsManager.Instance.WatchRemovableDrives))
                && dNameUcase != "A:\\"
                && dNameUcase != "B:\\"
                && driveInfo.IsReady
                && !BlackList.Contains(driveInfo.Name);
        }

        /// <summary>
        /// Tries to start running the prepared RFileSystemWatcher.
        /// If this fails, blacklists the drive and calls StopWatcher,
        /// which cleans up everything related to this drive.
        /// </summary>
        /// <param name="w">RFileSystemWatcher to start.</param>
        private static void StartWatcher(RemovableFileSystemWatcher w)
        {
            Exception e = null;
            try
            {
                w.EnableRaisingEvents = true;
            }
            catch (IOException ex) //drive unavailable
            {
                e = ex;
            }
            catch (ApplicationException ex) //drive is incompatible
            {
                e = ex;
            }
            if (e != null) //something bad happened
            {
                var dName = w.Path;
                Debug.WriteLine("DriveManager: cannot inistantiate watcher for {0}, reason:\r\n{1}", dName, e.Message);
                BlackList.Add(dName);
                StopWatcher(dName);
            }
        }

        /// <summary>
        /// Stops FS watcher for corresponding Path and removes it from Watchers collection.
        /// Then removes corresponding item from interface.
        /// </summary>
        /// <param name="dName">DriveInfo.Name, i.e. Watcher.Path for the one to stop</param>
        private static void StopWatcher(string dName)
        {
            lock (DriveNames)
            {
                //Stop watcher
                var watcher = Watchers.FirstOrDefault(w => w.Path == dName);
                if (watcher != null) //stop drive watcher
                {
                    //unsubscribe the handlers to prevent memory leak
                    watcher.Changed -= PushEvent;
                    watcher.Created -= PushEvent;
                    watcher.Deleted -= PushEvent;
                    watcher.Renamed -= PushEvent;
                    //destroy COM component and remove watcher from the collection
                    watcher.Dispose();
                    Watchers.Remove(watcher);
                }
                Util.Send(() => //remove PowerItem from UI
                {
                    var item = _drivesRoot.Items.FirstOrDefault(j => j.Argument == dName);
                    if (item != null)
                        _drivesRoot.Items.Remove(item);
                });
                //Remove the name for failed drive from the actual drives list
                DriveNames.Remove(dName);
            }
        }

        /// <summary>
        /// Thread-safely returns drive label for drive name
        /// </summary>
        /// <param name="driveName">Drive name in format "C:\"</param>
        /// <returns>Drive label if any, empty string otherwise. This doesn't return OS-set descriptions like "system", "pagefile", etc.</returns>
        public static string GetDriveLabel(string driveName)
        {
            DriveInfo drv;
            lock (DriveNames)
            {
                if(_drives == null)
                    _drives = DriveInfo.GetDrives();
                drv = _drives.FirstOrDefault(d => d.Name == driveName);
            }
            if (drv == null)
            {
                return string.Empty;
            }
            try
            {
                return drv.VolumeLabel;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Thread-safely returns formatted label for drive PowerItem
        /// </summary>
        /// <param name="driveName">Drive name in format "C:\"</param>
        /// <returns>Drive label if any, empty string otherwise. This doesn't return OS-set descriptions like "system", "pagefile", etc.</returns>
        public static string GetFormattedDriveLabel(string driveName)
        {
            return String.Format("{0} - {1}", driveName, GetDriveLabel(driveName));
        }

        public static bool HandleDeviceNotification(IntPtr wParam, IntPtr lParam)
        {
            lock (DriveNames)
            {
                return Watchers.Any(watcher => watcher.ProcessDeviceNotification(wParam, lParam));
            }
        }
    }

    class RemovableFileSystemWatcher : FileSystemWatcher
    {
        private readonly IntPtr _reporter;
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

        private IntPtr _hNotification = IntPtr.Zero;
        private FileStream _hDrive;

        private void Unlock()
        {
            if (_hNotification != IntPtr.Zero)
            {
                API.UnregisterDeviceNotification(_hNotification);
                _hNotification = IntPtr.Zero;
            }
            if (_hDrive != null)
            {
                _hDrive.Dispose();
                _hDrive = null;
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
                return; //might be real fixed drive
            }

            _hDrive = new FileStream(new SafeFileHandle(file, true), FileAccess.Read);
            var msg = new API.DEV_BROADCAST_HANDLE { dbch_handle = file };

            _hNotification = API.RegisterDeviceNotification(_reporter, msg, API.RDNFlags.DEVICE_NOTIFY_WINDOW_HANDLE);
            if (_hNotification == IntPtr.Zero || _hNotification.ToInt32() == -1)
            {
                Unlock(); //this drive appears to be non-notifiable
            }
            Log.Raw("Locked " + Path);
        }

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
