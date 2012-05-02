using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using System.Xml;

namespace Power8
{
    static class PowerItemTree
    {

        private static readonly string PathRoot = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        private static readonly string PathCommonRoot = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);

        private static readonly FileSystemWatcher Watcher = new FileSystemWatcher(PathRoot);
        private static readonly FileSystemWatcher CommonWatcher = new FileSystemWatcher(PathCommonRoot);

        private static readonly PowerItem StartMenuRootItem = new PowerItem {IsFolder = true, AutoExpand = false};
        private static readonly ObservableCollection<PowerItem> StartMenuCollection =
            new ObservableCollection<PowerItem> {StartMenuRootItem};
        public static ObservableCollection<PowerItem> StartMenuRoot { get { return StartMenuCollection; } }

        private static PowerItem _adminToolsRootItem;
        public static PowerItem AdminToolsRootItem
        {
            get
            {
                if (_adminToolsRootItem == null)
                {
                    var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools);
                    _adminToolsRootItem = SearchContainerByArgument(PathToBaseAndArg(path), StartMenuRootItem, false);
                    _adminToolsRootItem = SearchItemByArgument(path, true, _adminToolsRootItem);
                    _adminToolsRootItem.Argument = API.ShNs.AdministrationTools;
                    _adminToolsRootItem.ResourceIdString = Util.GetLocalizedStringResourceIdForClass(API.ShNs.AdministrationTools);
                    _adminToolsRootItem.SpecialFolderId = API.Csidl.COMMON_ADMINTOOLS;
                    _adminToolsRootItem.NonCachedIcon = true;
                    _adminToolsRootItem.Icon = ImageManager.GetImageContainerSync(_adminToolsRootItem, API.Shgfi.SMALLICON);
                    _adminToolsRootItem.Icon.ExtractLarge(); //as this is a reference to existing PowerItem, 
                    _adminToolsRootItem.HasLargeIcon = true; //we need both small and large icons available
                }
                return _adminToolsRootItem;
            }
        }

        private static PowerItem _librariesOrMyDocsItem;
        public static PowerItem LibrariesItem
        {
            get
            {
                if (_librariesOrMyDocsItem == null)
                {
                    string path, ns;
                    if (Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 1) //Win7+ -> return libraries
                    {
                        path = Util.ResolveKnownFolder(API.KnFldrIds.Libraries);
                        ns = API.ShNs.Libraries;
                    }
                    else                                          //Vista or below -> return MyDocs
                    {
                        path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        ns = API.ShNs.MyDocuments;
                    }
                    _librariesOrMyDocsItem = new PowerItem
                    {
                        Argument = path,
                        SpecialFolderId = API.Csidl.MYDOCUMENTS,
                        ResourceIdString = Util.GetLocalizedStringResourceIdForClass(ns),
                        NonCachedIcon = true,
                        HasLargeIcon = true,
                        IsFolder = true
                    };
                    ScanFolderSync(_librariesOrMyDocsItem, string.Empty, false);
                    _librariesOrMyDocsItem.Argument = ns;
                }
                return _librariesOrMyDocsItem;
            }
        }

        private static PowerItem _controlPanelItem;
        public static PowerItem ControlPanelItem
        {
            get
            {
                if (_controlPanelItem == null)
                {
                    //the item itself
                    _controlPanelItem = new PowerItem
                    {
                        Argument = Environment.OSVersion.Version.Major > 5 ? API.ShNs.ControlPanel : API.ShNs.AllControlPanelItems,
                        SpecialFolderId = API.Csidl.CONTROLS,
                        ResourceIdString = Util.GetLocalizedStringResourceIdForClass(API.ShNs.ControlPanel),
                        NonCachedIcon = true,
                        HasLargeIcon = true,
                        IsFolder = true
                    };

                    var cplCache = new List<string>();
                    //Flow items and CPLs from cache
                    using (var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                       @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace", false))
                    {
                        if (k != null)
                        {
                            foreach (var cplguid in k.GetSubKeyNames())
                            {
                                if(cplguid.StartsWith("{"))
                                {
                                    _controlPanelItem.Items.Add(new PowerItem
                                    {
                                        Argument = API.ShNs.AllControlPanelItems + "\\::" + cplguid,
                                        NonCachedIcon = true,
                                        Parent = _controlPanelItem,
                                        ResourceIdString = Util.GetLocalizedStringResourceIdForClass(cplguid, true)
                                    });
                                }
                                else
                                {
                                    using (var sk = k.OpenSubKey(cplguid, false))
                                    {
                                        if (sk != null)
                                        {
                                            var cplArg = (string) sk.GetValue("Module");
                                            var cplName = (string) sk.GetValue("Name");
                                            var cplIconIdx = sk.GetValue("IconIndex");
                                            if (cplIconIdx != null 
                                                && !string.IsNullOrEmpty(cplArg) 
                                                && !string.IsNullOrEmpty(cplName)
                                                && File.Exists(cplArg))
                                            {
                                                var item = new PowerItem
                                                {
                                                    Argument = cplArg,
                                                    Parent = _controlPanelItem,
                                                    FriendlyName = cplName,
                                                    Icon = new ImageManager.ImageContainer(
                                                        Util.ResolveIconicResource("@" + cplArg + ",-" + cplIconIdx))
                                                };
                                                _controlPanelItem.Items.Add(item);
                                                ImageManager.AddContainerToCache(item.Argument, item.Icon);
                                                cplCache.Add(cplArg);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //*.CPL items + separator
                    foreach (var cpl in Directory.EnumerateFiles(
                                             Environment.GetFolderPath(Environment.SpecialFolder.System),
                                             "*.cpl",
                                             SearchOption.TopDirectoryOnly))
                    {
                        if (cplCache.Contains(cpl, StringComparer.InvariantCultureIgnoreCase)) 
                            continue;
                        var resolved = Util.GetCplInfo(cpl);
                        if (resolved.Item2 != null && _controlPanelItem.Items.FirstOrDefault(p => p.FriendlyName == resolved.Item1) == null)
                            _controlPanelItem.Items.Add(new PowerItem
                            {
                                Argument = cpl,
                                Parent = _controlPanelItem,
                                FriendlyName = resolved.Item1,
                                Icon = resolved.Item2
                            });
                    }

                    _controlPanelItem.SortItems();

                    if (Environment.OSVersion.Version.Major > 5) //XP only supports "All items"
                    {
                        //for 7+ we add "All Control Panel Items" + separator
                        _controlPanelItem.Icon = ImageManager.GetImageContainerSync(_controlPanelItem, API.Shgfi.SMALLICON);

                        _controlPanelItem.Items.Insert(0, new PowerItem
                        {
                            Argument = API.ShNs.AllControlPanelItems,
                            SpecialFolderId = API.Csidl.CONTROLS,
                            ResourceIdString = Util.GetLocalizedStringResourceIdForClass(API.ShNs.AllControlPanelItems),
                            Parent = _controlPanelItem,
                            Icon = _controlPanelItem.Icon,
                            IsFolder = true
                        });

                        //TODO: separator in binded ObservableCollection?
                        _controlPanelItem.Items.Insert(1, new PowerItem { FriendlyName = "----" });
                    }

                }
                return _controlPanelItem;
            }
        }

        private static PowerItem _myComputerItem;
        public static PowerItem MyComputerRootItem
        {
            get
            {
                if (_myComputerItem == null)
                {
                    _myComputerItem = new PowerItem
                    {
                        Argument = API.ShNs.MyComputer,
                        ResourceIdString = Util.GetLocalizedStringResourceIdForClass(API.ShNs.MyComputer),
                        SpecialFolderId = API.Csidl.DRIVES,
                        IsFolder = true,
                        NonCachedIcon = true,
                        HasLargeIcon = true
                    };
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        switch (drive.DriveType)
                        {
                            case DriveType.Removable:
                            case DriveType.Fixed:
                            case DriveType.Ram:
                            case DriveType.Network:
                                _myComputerItem.Items.Add(new PowerItem
                                {
                                    Argument = drive.Name,
                                    AutoExpand = true,
                                    IsFolder = true,
                                    Parent = _myComputerItem,
                                    NonCachedIcon = true
                                });
                                break;
                        }
                    }
                }
                return _myComputerItem;
            }
        }
        
        static PowerItemTree()
        {
            Watcher.Created += FileChanged;
            CommonWatcher.Created += FileChanged;
            Watcher.Deleted += FileChanged;
            CommonWatcher.Deleted += FileChanged;
            Watcher.Changed += FileChanged;
            CommonWatcher.Changed += FileChanged;
            Watcher.Renamed += FileRenamed;
            CommonWatcher.Renamed += FileRenamed;
            Watcher.EnableRaisingEvents = true;
            CommonWatcher.EnableRaisingEvents = true;
            Watcher.IncludeSubdirectories = true;
            CommonWatcher.IncludeSubdirectories = true;
        }


        private static void FileRenamed(object sender, RenamedEventArgs e)
        {
            if(e.OldFullPath.StartsWith(PathRoot) || e.OldFullPath.StartsWith(PathCommonRoot))
                FileChanged(sender,
                    new FileSystemEventArgs(
                        WatcherChangeTypes.Deleted,
                        e.OldFullPath.TrimEnd(e.OldName.ToCharArray()),
                        e.OldName));
            if(e.FullPath.StartsWith(PathRoot) || e.FullPath.StartsWith(PathCommonRoot))
                FileChanged(sender,
                    new FileSystemEventArgs(
                        WatcherChangeTypes.Created,
                        e.FullPath.TrimEnd(e.Name.ToCharArray()),
                        e.Name));
        }

        private static void FileChanged(object sender, FileSystemEventArgs e)
        {
            lock (StartMenuRootItem)
            {
                try
                {
                    //We ignore hiden data
                    if (e.ChangeType != WatcherChangeTypes.Deleted && File.GetAttributes(e.FullPath).HasFlag(FileAttributes.Hidden))
                        return;
                }
                catch (Exception) //In case we have multiple change operations caused by links beung updated by installer
                {
                    return;
                }
                //Ensuring buttonstack is created on Main thread
                Util.Send(new Action(() => BtnStck.Instance.InvalidateVisual()));
                var isDir = Directory.Exists(e.FullPath);
                var baseAndArg = PathToBaseAndArg(e.FullPath);
                if (baseAndArg.Item2 == null) 
                    return;
                var item = SearchContainerByArgument(baseAndArg, StartMenuRootItem,
                                                e.ChangeType == WatcherChangeTypes.Created);
                if (item != null)
                {
                    Util.Send(new Action(() =>
                    {
                        switch (e.ChangeType)
                        {
                            case WatcherChangeTypes.Deleted:
                            case WatcherChangeTypes.Changed:
                                item =
                                    item.Items.FirstOrDefault(
                                        j =>
                                        (j.IsFolder == isDir || e.ChangeType == WatcherChangeTypes.Deleted) &&
                                        j.Argument == baseAndArg.Item2);
                                if (e.ChangeType == WatcherChangeTypes.Deleted && item != null)
                                    item.Parent.Items.Remove(item);
                                else if(item != null)
                                    item.Update();
                                break;
                            case WatcherChangeTypes.Created:
                                AddSubItem(item, baseAndArg.Item1, e.FullPath, isDir);
                                break;
                        }
                    }));
                }
            }
        }


        public static void InitTree()
        {
            lock (StartMenuRootItem)
            {
                ScanFolderSync(StartMenuRootItem, PathRoot, true);
                ScanFolderSync(StartMenuRootItem, PathCommonRoot, true);
                StartMenuRootItem.SortItems();
            }
        }

        public static void ScanFolder(PowerItem item, string basePath, bool recoursive = true)
        {
            Util.MainDisp.BeginInvoke(DispatcherPriority.Background,
                                    new Action(() => ScanFolderSync(item, basePath, recoursive)));
        }

        public static void ScanFolderSync(PowerItem item, string basePath, bool recoursive)
        {
            try
            {
                var curDir = basePath + (item.Argument ?? Util.ResolveSpecialFolder(item.SpecialFolderId));
                foreach (var directory in item.IsLibrary ? GetLibraryDirectories(curDir) : Directory.GetDirectories(curDir))
                {
                    if ((File.GetAttributes(directory).HasFlag(FileAttributes.Hidden)))
                        continue;

                    var subitem = AddSubItem(item, basePath, directory, true, autoExpand: !recoursive);
                    if (recoursive)
                        ScanFolderSync(subitem, basePath, true);
                }
                if (item.IsLibrary)
                    return;
                var resources = new Dictionary<string, string>();
                var dsktp = curDir + "\\desktop.ini";
                if (File.Exists(dsktp))
                {
                    using (var reader = new StreamReader(dsktp, System.Text.Encoding.Default, true))
                    {
                        string str;
                        while ((str = reader.ReadLine()) != null && !str.Contains("[LocalizedFileNames]"))
                        {
                            if (str.StartsWith("IconFile="))
                            {
                                item.NonCachedIcon = true;
                                item.Icon = null;
                            }
                            if (str.StartsWith("LocalizedResourceName="))
                            {
                                item.ResourceIdString = str.Substring(22);
                            }
                        }
                        while ((str = reader.ReadLine()) != null && str.Contains("="))
                        {
                            var pair = str.Split(new[] { '=' }, 2);
                            resources.Add(pair[0], pair[1]);
                        }
                    }
                }
                foreach (var file in Directory.GetFiles(curDir))
                {
                    if ((File.GetAttributes(file).HasFlag(FileAttributes.Hidden)))
                        continue;

                    var fn = Path.GetFileName(file);
                    var fileIsLib = file.EndsWith(".library-ms");
                    AddSubItem(item, basePath, file, fileIsLib,
                                fn != null && resources.ContainsKey(fn) ? resources[fn] : null,
                                fileIsLib);
                }
            }
            catch (UnauthorizedAccessException)
            { }//Don't care if user is not allowed to access fileor directory or it's contents
            catch (IOException)
            { }//Don't care as well if file was deleted on-the-fly, watcher will notify list
        }

        private static PowerItem AddSubItem(PowerItem item, string basePath, string fsObject, bool isFolder, string resourceId = null, bool autoExpand = false)
        {
            var argStr = fsObject.Substring(basePath.Length);
            var child = isFolder && !autoExpand ? item.Items.FirstOrDefault(i => i.Argument == argStr && i.IsFolder) : null;
            if(child == null)
            {
                child = new PowerItem
                            {
                                Argument = argStr,
                                Parent = item,
                                IsFolder = isFolder,
                                ResourceIdString = resourceId,
                                AutoExpand = autoExpand
                            };
                item.Items.Add(child);
            }
            return child;
        }


        public static ProcessStartInfo ResolveItem(PowerItem item, bool prioritizeCommons = false)
        {
            var psi = new ProcessStartInfo();
            var arg1 = item.Argument;
            if (item.IsSpecialObject)
            {
                bool cplSucceeded = false;
                if (!item.IsFolder && item.Parent != null && item.Parent.SpecialFolderId == API.Csidl.CONTROLS)
                {
                    //Control panel flow item
                    var command = Util.GetOpenCommandForClass(item.Argument);
                    if (command != null)
                    {
                        psi.FileName = command.Item1;
                        psi.Arguments = command.Item2;
                        cplSucceeded = true;
                    }
                    else
                    {
                        var sysname = Util.GetCplAppletSysNameForClass(item.Argument);
                        if(!string.IsNullOrEmpty(sysname))
                        {
                            psi.FileName = "control.exe";
                            psi.Arguments = "/name " + sysname;
                            cplSucceeded = true;
                        }
                    }
                }
                if(!cplSucceeded)
                {
                    psi.FileName = "explorer.exe";
                    psi.Arguments = "/N," + item.Argument;
                }
            }
            else
            {
                if (!(arg1.StartsWith("\\\\") || (arg1.Length > 1 && arg1[1] == ':')))
                    arg1 = PathRoot + item.Argument;
                var arg2 = PathCommonRoot + item.Argument;
                if (prioritizeCommons)
                    arg2 = Interlocked.Exchange(ref arg1, arg2);
                if (item.IsFolder)
                {
                    psi.FileName = "explorer.exe";
                    if (Directory.Exists(arg1))
                        psi.Arguments = arg1;
                    else
                    {
                        if (Directory.Exists(arg2))
                            psi.Arguments = arg2;
                        else
                            throw new IOException("Directory not found for " + item.Argument);
                    }
                }
                else
                {
                    if (File.Exists(arg1))
                        psi.FileName = arg1;
                    else
                    {
                        if (File.Exists(arg2))
                            psi.FileName = arg2;
                        else
                            throw new IOException("File not found for " + item.Argument);
                    }
                }

            }
            return psi;
        }

        public static string GetResolvedArgument(PowerItem item, bool prioritizeCommons)
        {
            if(item.IsSpecialObject)
                return item.Argument;
            var psi = ResolveItem(item, prioritizeCommons);
            return item.IsFolder ? psi.Arguments : psi.FileName;
        }

        /// <summary>
        /// Breaks full path into predicate base (one of User Start Menu and Common Start Menu) and the trailling stuff
        /// </summary>
        /// <param name="itemFullPath">file system object in its string representation</param>
        /// <returns>Tuple of base path and the other staff</returns>
        private static Tuple<string, string> PathToBaseAndArg(string itemFullPath)
        {
            if (itemFullPath.StartsWith(PathRoot))
            {
                return new Tuple<string, string>(PathRoot, itemFullPath.Substring(PathRoot.Length));
            }
            return itemFullPath.StartsWith(PathCommonRoot)
                       ? new Tuple<string, string>(PathCommonRoot, itemFullPath.Substring(PathCommonRoot.Length))
                       : new Tuple<string, string>(null, itemFullPath);
        }

        /// <summary>
        /// From collection passed, searches for a parent item (i.e. container) of the one that would represent the object
        /// described by passed tuple. Optionally tries to generate items in the middle.
        /// </summary>
        /// <param name="baseAndArg">Tuple returned by <code>PathToBaseAndArg</code></param>
        /// <param name="collectionRoot">The root of collection, like <code>StartMenuRootItem</code></param>
        /// <param name="autoGenerateSubItems">Should method generate proxy Folder items in between of last available 
        /// and required items?</param>
        /// <returns></returns>
        private static PowerItem SearchContainerByArgument(Tuple<string, string> baseAndArg, PowerItem collectionRoot, bool autoGenerateSubItems)
        {
            var sourceSplitted = baseAndArg.Item2.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var item = collectionRoot;
            for (int i = 0; i < sourceSplitted.Length - 1; i++)
            {
                var prevItem = item;
                item = item.Items.FirstOrDefault(j => j.IsFolder && j.FriendlyName == sourceSplitted[i]);
                if (item == null && autoGenerateSubItems)
                    // ReSharper disable AccessToModifiedClosure
                    item = Util.Eval(() =>
                            AddSubItem(prevItem,
                                       baseAndArg.Item1,
                                       baseAndArg.Item1 + prevItem.Argument + "\\" + sourceSplitted[i],
                                       true));
                    // ReSharper restore AccessToModifiedClosure
                else if (item == null)
                    break;
            }
            return item;
        }

        private static PowerItem SearchItemByArgument(string argument, bool isFolder, PowerItem container)
        {
            return container.Items.FirstOrDefault(
                i =>
                    i.IsFolder == isFolder &&
                    i.Argument.EndsWith(
                        Path.GetFileName(argument) ?? argument, 
                        StringComparison.InvariantCultureIgnoreCase));
        }

        private static string[] GetLibraryDirectories(string libraryMs)
        {
            var xdoc = new XmlDocument();
            xdoc.Load(libraryMs);
            var nodeList = xdoc["libraryDescription"]["searchConnectorDescriptionList"];
            if(nodeList == null)
                return new string[0];
            var nodeList2 = nodeList.GetElementsByTagName("searchConnectorDescription");
            if (nodeList2.Count == 0)
                return new string[0];
            var temp = (from XmlNode node
                        in nodeList2
                        select node["simpleLocation"]
                                   ["url"]
                                   .InnerText
                        ).ToList();
            var arr = new string[temp.Count];
            for (var i = 0; i < temp.Count; i++)
            {
                if (temp[i].StartsWith("knownfolder:", StringComparison.InvariantCultureIgnoreCase))
                    arr[i] = Util.ResolveKnownFolder(temp[i].Substring(12));
                else
                    arr[i] = temp[i];
            }
            return arr;
        }
    }
}
