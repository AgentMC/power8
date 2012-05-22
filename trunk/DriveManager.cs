using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Power8
{
    public static class DriveManager
    {
        private static readonly List<FileSystemWatcher> Watchers = new List<FileSystemWatcher>();
        private static readonly List<string> DriveNames = new List<string>();
        private static FileSystemEventHandler _fileChanged;
        private static RenamedEventHandler _fileRenamed;
        private static PowerItem _drivesRoot;

        public static void Init(FileSystemEventHandler changedHandler, RenamedEventHandler renamedHandler, PowerItem drivesRoot)
        {
            _fileChanged = changedHandler;
            _fileRenamed = renamedHandler;
            _drivesRoot = drivesRoot;
            new Thread(Worker) {Name = "DriveWatchThread"}.Start();
        }

        private static void Worker ()
        {
begin:
            var drives = DriveInfo.GetDrives();

            //Have some drives been removed?
            for (int i = DriveNames.Count - 1; i >= 0; i--)
            {
                var dName = DriveNames[i];
                if (drives.All(d => d.Name != dName))   //drive removed
                {
                    var watcher = Watchers.FirstOrDefault(w => w.Path == dName);
                    if (watcher != null)                //stop drive watcher
                    {
                        watcher.Dispose();
                        Watchers.Remove(watcher);
                    }
                    Util.Send(() =>                     //remove PowerItem
                                  {
                                      var item =
                                          _drivesRoot.Items.FirstOrDefault(j => j.Argument == dName);
                                      if (item != null)
                                          _drivesRoot.Items.Remove(item);
                                  });

                    DriveNames.RemoveAt(i);             //remove drive from collection
                }
            }

            //Have some drives been addded?
            foreach (var driveInfo in drives)
            {
                var dName = driveInfo.Name;
                if (DriveNames.All(d => d != dName))     //drive added
                {
                    var dNameLcase = dName.ToLowerInvariant();
                    if (new[]{  DriveType.Fixed, 
                                DriveType.Network, 
                                DriveType.Ram, 
                                DriveType.Removable }.Contains(driveInfo.DriveType)
                        && dNameLcase != "A:\\"
                        && dNameLcase != "B:\\")
                    {
                        DriveNames.Add(dName);          //Add drive to collection

                        Util.Send(() =>                 //Add drive to UI
                                  _drivesRoot.Items.Add(new PowerItem
                                                            {
                                                                Argument = dName,
                                                                AutoExpand = true,
                                                                IsFolder = true,
                                                                Parent = _drivesRoot,
                                                                NonCachedIcon = true
                                                            }));
                                                        //Add drive watcher
                        var w = new FileSystemWatcher(dName);
                        w.Created += _fileChanged;
                        w.Deleted += _fileChanged;
                        w.Changed += _fileChanged;
                        w.Renamed += _fileRenamed;
                        w.IncludeSubdirectories = true;
                        if (BtnStck.IsInitDone)
                            w.EnableRaisingEvents = true;
                        else
                            BtnStck.Instanciated += (sender, args) => w.EnableRaisingEvents = true;
                        Watchers.Add(w);
                    }
                }
            }

            Thread.Sleep(5000);

            if (!Util.MainDisp.HasShutdownStarted)
                goto begin;
        }

    }
}
