using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using Power8.Properties;

namespace Power8
{
    static class PowerItemTree
    {
        public const string SEPARATOR_NAME = "----";

        private static readonly string PathRoot = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        private static readonly string PathCommonRoot = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);

        private static readonly PowerItem StartMenuRootItem = new PowerItem {IsFolder = true};
        private static readonly ObservableCollection<PowerItem> StartMenuCollection =
            new ObservableCollection<PowerItem> {StartMenuRootItem};
        public static ObservableCollection<PowerItem> StartMenuRoot { get { return StartMenuCollection; } }

        private static PowerItem _adminToolsItem;
        public static PowerItem AdminToolsRoot
        {
            get
            {
                if (_adminToolsItem == null)
                {
                    var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools);
                    _adminToolsItem = SearchContainerByArgument(PathToBaseAndArg(path), StartMenuRootItem, false);
                    _adminToolsItem = SearchItemByArgument(path, true, _adminToolsItem);
                    _adminToolsItem.Argument = API.ShNs.AdministrationTools;
                    _adminToolsItem.ResourceIdString = Util.GetLocalizedStringResourceIdForClass(API.ShNs.AdministrationTools);
                    _adminToolsItem.SpecialFolderId = API.Csidl.COMMON_ADMINTOOLS;
                    _adminToolsItem.NonCachedIcon = true;
                    _adminToolsItem.Icon = ImageManager.GetImageContainerSync(_adminToolsItem, API.Shgfi.SMALLICON);
                    _adminToolsItem.Icon.ExtractLarge(); //as this is a reference to existing PowerItem, 
                    _adminToolsItem.HasLargeIcon = true; //we need both small and large icons available
                }
                return _adminToolsItem;
            }
        }

        private static PowerItem _librariesOrMyDocsItem;
        public static PowerItem LibrariesRoot
        {
            get
            {
                if (_librariesOrMyDocsItem == null)
                {
                    string path, ns;
                    if (Environment.OSVersion.Version.Major >= 6) //Win7+ -> return libraries
                    {
                        path = Util.ResolveKnownFolder(API.KnFldrIds.Libraries);
                        ns = API.ShNs.Libraries;
                    }
                    else                                          //XP or below -> return MyDocs
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

        private static PowerItem _controlPanelRoot;
        public static PowerItem ControlPanelRoot
        {
            get
            {
                if (_controlPanelRoot == null)
                {
                    //the item itself
                    _controlPanelRoot = new PowerItem
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
                                    _controlPanelRoot.Items.Add(new PowerItem
                                    {
                                        Argument = API.ShNs.AllControlPanelItems + "\\::" + cplguid,
                                        NonCachedIcon = true,
                                        Parent = _controlPanelRoot,
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
                                                var pIcon = Util.ResolveIconicResource("@" + cplArg + ",-" + cplIconIdx);
                                                var item = new PowerItem
                                                {
                                                    Argument = cplArg,
                                                    Parent = _controlPanelRoot,
                                                    FriendlyName = cplName,
                                                    Icon = ImageManager.GetImageContainerForIconSync(cplArg, pIcon)
                                                };
                                                _controlPanelRoot.Items.Add(item);
                                                Util.PostBackgroundIconDestroy(pIcon);
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
                        if (resolved.Item2 != null && _controlPanelRoot.Items.FirstOrDefault(p => p.FriendlyName == resolved.Item1) == null)
                            _controlPanelRoot.Items.Add(new PowerItem
                            {
                                Argument = cpl,
                                Parent = _controlPanelRoot,
                                FriendlyName = resolved.Item1,
                                Icon = resolved.Item2
                            });
                    }

                    _controlPanelRoot.SortItems();

                    if (Environment.OSVersion.Version.Major > 5) //XP only supports "All items"
                    {
                        //for 7+ we add "All Control Panel Items" + separator
                        _controlPanelRoot.Icon = ImageManager.GetImageContainerSync(_controlPanelRoot, API.Shgfi.SMALLICON);

                        _controlPanelRoot.Items.Insert(0, new PowerItem
                        {
                            Argument = API.ShNs.AllControlPanelItems,
                            SpecialFolderId = API.Csidl.CONTROLS,
                            ResourceIdString = Util.GetLocalizedStringResourceIdForClass(API.ShNs.AllControlPanelItems),
                            Parent = _controlPanelRoot,
                            Icon = _controlPanelRoot.Icon,
                            IsFolder = true
                        });

                        _controlPanelRoot.Items.Insert(1, new PowerItem
                        {
                            FriendlyName = SEPARATOR_NAME, 
                            Parent = _controlPanelRoot
                        });
                    }
                }
                return _controlPanelRoot;
            }
        }

        private static PowerItem _myComputerItem;
        public static PowerItem MyComputerRoot
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
                    _myComputerItem.Icon = ImageManager.GetImageContainerSync(_myComputerItem, API.Shgfi.LARGEICON);
                    _myComputerItem.Icon.ExtractSmall();
                    DriveManager.Init(FileChanged, FileRenamed, _myComputerItem);
                }
                return _myComputerItem;
            }
        }

        private static PowerItem _networkRoot;
        public static PowerItem NetworkRoot
        {
            get
            {
                if (_networkRoot == null)
                {
                    var xpNet7Wrkgrp = new PowerItem
                    {
                        SpecialFolderId = API.Csidl.NETWORK,
                        IsFolder = true,
                        HasLargeIcon = true,
                        NonCachedIcon = true,
                        Argument = API.ShNs.NetworkNeighbourhood,
                        Parent = ControlPanelRoot
                    };

                    var conString = Util.ResolveSpecialFolderName(API.Csidl.CONNECTIONS);
                    var connections =
                        ControlPanelRoot.Items.FirstOrDefault(i => i.FriendlyName == conString) ??
                        new PowerItem
                            {
                                SpecialFolderId = API.Csidl.CONNECTIONS,
                                NonCachedIcon = true,
                                IsFolder = true,
                                Argument = API.ShNs.NetworkConnections,
                                Parent = ControlPanelRoot
                            };

                    var xpWrkgrp7Net = new PowerItem
                    {
                        SpecialFolderId = API.Csidl.COMPUTERSNEARME,
                        IsFolder = true,
                        NonCachedIcon = true,
                        Parent = ControlPanelRoot
                    };

                    _networkRoot = Environment.OSVersion.Version.Major >= 6 ? xpWrkgrp7Net : xpNet7Wrkgrp;
                    _networkRoot.Items.Add(_networkRoot == xpWrkgrp7Net ? xpNet7Wrkgrp : xpWrkgrp7Net);
                    _networkRoot.Items.Add(connections);
                    _networkRoot.Items.Add(new PowerItem {FriendlyName = SEPARATOR_NAME, Parent = _networkRoot});

                    new Thread(() =>
                                   {
                                       if (Environment.OSVersion.Version.Major >= 6)
                                           xpNet7Wrkgrp.FriendlyName = NetManager.DomainOrWorkgroup;

                                       List<string> names;
                                       bool addMoreItem = false;
                                       if (NetManager.ComputersNearby.Count > 10)
                                       {
                                           addMoreItem = true;
                                           names = new List<string>();
                                           for (int i = 0; i < 10; i++)
                                               names.Add(NetManager.ComputersNearby[i]);
                                       }
                                       else
                                       {
                                           names = NetManager.ComputersNearby;
                                       }

                                       if (names.Count == 0) //remove separator if no items were added
                                       {
                                           Util.Post(() => _networkRoot.Items.RemoveAt(_networkRoot.Items.Count - 1));
                                           return;
                                       }
                                       names.Select(e => new PowerItem
                                                        {
                                                            Argument = "\\\\" + e,
                                                            IsFolder = true,
                                                            Parent = _networkRoot,
                                                            Icon = MyComputerRoot.Icon
                                                        })
                                           .ToList()
                                           .ForEach(i => Util.Post(() => _networkRoot.Items.Add(i)));

                                       if (addMoreItem)
                                           Util.Post(() =>
                                                    _networkRoot.Items.Add(new PowerItem
                                                    {
                                                        FriendlyName = Resources.Str_ShowMore,
                                                        Parent = _networkRoot,
                                                        SpecialFolderId = API.Csidl.POWER8CLASS,
                                                        Argument = "Power8.ComputerList"
                                                    }));

                                   }) { Name = "Network Scan Thread" }.Start();
                }
                return _networkRoot;
            }
        }

        private static void FileRenamed(object sender, RenamedEventArgs e)
        {
            FileChanged(sender,
                new FileSystemEventArgs(
                    WatcherChangeTypes.Deleted,
                    e.OldFullPath.TrimEnd(e.OldName.ToCharArray()),
                    e.OldName));
            FileChanged(sender,
                new FileSystemEventArgs(
                    WatcherChangeTypes.Created,
                    e.FullPath.TrimEnd(e.Name.ToCharArray()),
                    e.Name));
        }

        private static void FileChanged(object sender, FileSystemEventArgs e)
        {
#if DEBUG
            Debug.WriteLine("File {0}: {1}", e.ChangeType, e.FullPath);
#endif
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
            Util.Send(() => BtnStck.Instance.InvalidateVisual());
            var isDir = Directory.Exists(e.FullPath);
            var baseAndArg = PathToBaseAndArg(e.FullPath);
            if (baseAndArg.Item2 == null) 
                return;

            var roots = new List<PowerItem> {MyComputerRoot, StartMenuRootItem};
            foreach (var lib in LibrariesRoot.Items.Where(lib => !lib.AutoExpandIsPending))
                roots.AddRange(lib.Items);

            foreach (var root in roots)
            {
                var item = SearchContainerByArgument(baseAndArg, root,
                                                        e.ChangeType == WatcherChangeTypes.Created &&
                                                        root == StartMenuRootItem);
                if (item != null)
                {
                    Util.Send(() =>
                    {
                        switch (e.ChangeType)
                        {
                            case WatcherChangeTypes.Deleted:
                            case WatcherChangeTypes.Changed:
                                item = item.AutoExpandIsPending 
                                    ? null
                                    : item.Items.FirstOrDefault(
                                        j =>
                                        (j.IsFolder == isDir || e.ChangeType == WatcherChangeTypes.Deleted) &&
                                        j.Argument == baseAndArg.Item2);
                                if (e.ChangeType == WatcherChangeTypes.Deleted && item != null)
                                    item.Parent.Items.Remove(item);
                                else if (item != null)
                                    item.Update();
                                break;
                            case WatcherChangeTypes.Created:
                                AddSubItem(item, baseAndArg.Item1, e.FullPath, isDir);
                                break;
                        }
                    });
                }
            }
        }


        public static void InitTree()
        {
#if DEBUG
            var s = Stopwatch.StartNew();
#endif
            lock (StartMenuRootItem)
            {
                ScanFolderSync(StartMenuRootItem, PathRoot, true);
                ScanFolderSync(StartMenuRootItem, PathCommonRoot, true);
                StartMenuRootItem.SortItems();
            }
#if DEBUG
            Debug.WriteLine("InitTree - scanned in {0}", s.ElapsedMilliseconds);
#endif
            Util.Send(() => BtnStck.Instance.InvalidateVisual());
#if DEBUG
            Debug.WriteLine("InitTree - done in {0}", s.ElapsedMilliseconds);
            s.Stop();
#endif
        }

        public static void ScanFolder(PowerItem item, string basePath, bool recoursive = true)
        {
            ThreadPool.QueueUserWorkItem(o => ScanFolderSync(item, basePath, recoursive));
        }

        private static void ScanFolderSync(PowerItem item, string basePath, bool recoursive)
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
                            if (str.StartsWith("IconFile=") || str.StartsWith("IconResource="))
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
                    var fileIsLib = (Path.GetExtension(file) ?? "")
                                         .Equals(".library-ms", StringComparison.InvariantCultureIgnoreCase);
                    AddSubItem(item, basePath, file, fileIsLib,
                                fn != null && resources.ContainsKey(fn) ? resources[fn] : null,
                                fileIsLib);
                }
            }
            catch (UnauthorizedAccessException)
            { }//Don't care if user is not allowed to access fileor directory or it's contents
            catch (IOException)
            { }//Don't care as well if file was deleted on-the-fly, watcher will notify list
            finally
            {  //Explicitly set marker showing that enumeration operations may occur on Items from this moment
                item.AutoExpandIsPending = false;
            }
        }

        private static PowerItem AddSubItem(PowerItem item, string basePath, string fsObject, bool isFolder, string resourceId = null, bool autoExpand = false)
        {
            var argStr = fsObject.Substring(basePath.Length);
            var child = isFolder && !autoExpand
                ? item.Items.FirstOrDefault(i => 
                    string.Equals(i.Argument, argStr, StringComparison.CurrentCultureIgnoreCase) 
                    && i.IsFolder)
                : null;
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
                Util.Send(() => item.Items.Add(child));
            }
            return child;
        }


        public static ProcessStartInfo ResolveItem(PowerItem item, bool prioritizeCommons = false)
        {
            var psi = new ProcessStartInfo();
            var arg1 = item.Argument;
            if (item.IsSpecialObject || arg1 == null)
            {
                bool cplSucceeded = false;
                if (!item.IsFolder && item.Parent != null && item.Parent.SpecialFolderId == API.Csidl.CONTROLS)
                {
                    //Control panel flow item
                    var command = Util.GetOpenCommandForClass(arg1);
                    if (command != null)
                    {
                        psi.FileName = command.Item1;
                        psi.Arguments = command.Item2;
                        cplSucceeded = true;
                    }
                    else
                    {
                        var sysname = Util.GetCplAppletSysNameForClass(arg1);
                        if (!string.IsNullOrEmpty(sysname))
                        {
                            psi.FileName = "control.exe";
                            psi.Arguments = "/name " + sysname;
                            cplSucceeded = true;
                        }
                    }
                }
                if (!cplSucceeded)
                {
                    psi.FileName = "explorer.exe";
                    psi.Arguments = "/N," + (item.Argument ?? Util.ResolveSpecialFolder(item.SpecialFolderId));
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
                    if (arg1.StartsWith("\\\\") || Directory.Exists(arg1))
                        psi.Arguments = arg1;
                    else
                    {
                        if (Directory.Exists(arg2))
                            psi.Arguments = arg2;
                        else
                            throw new IOException(Resources.Err_GotNoFolder + item.Argument);
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
                            throw new IOException(Resources.Err_GotNoFile + item.Argument);
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
                       : new Tuple<string, string>(string.Empty, itemFullPath);
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
            if (!string.IsNullOrEmpty(collectionRoot.Argument)
                && baseAndArg.Item2.StartsWith(collectionRoot.Argument, StringComparison.InvariantCultureIgnoreCase)
                && baseAndArg.Item2 != collectionRoot.Argument)
            {
                baseAndArg = new Tuple<string, string>(baseAndArg.Item1,
                                                        baseAndArg.Item2.Substring(collectionRoot.Argument.Length + 1));
            }
            var sourceSplitted = baseAndArg.Item2.Split(new[] {'\\'}, StringSplitOptions.RemoveEmptyEntries);
            if (sourceSplitted.Length > 0 && sourceSplitted[0].EndsWith(":"))
                sourceSplitted[0] += "\\";
            var item = collectionRoot;
            for (int i = 0; i < sourceSplitted.Length - 1; i++)
            {
                var prevItem = item;
                item = item.AutoExpandIsPending 
                    ? null
                    : item.Items.FirstOrDefault(j =>
                                                j.IsFolder &&
                                                j.Argument.EndsWith(sourceSplitted[i],
                                                                    StringComparison.InvariantCultureIgnoreCase));
                if (item == null && autoGenerateSubItems && !string.IsNullOrEmpty(baseAndArg.Item1))
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
            if(container.AutoExpandIsPending)
                return null;
            var endExpr = Path.GetFileName(argument) ?? argument;
            return container.Items.FirstOrDefault(i =>
                    i.IsFolder == isFolder &&
                    i.Argument.EndsWith(endExpr, StringComparison.InvariantCultureIgnoreCase));
        }

        private static string[] GetLibraryDirectories(string libraryMs)
        {
            var xdoc = new XmlDocument();
            try
            {
                xdoc.Load(libraryMs);
            }
            catch (XmlException) //malformed libraries XMLs hanlded
            {
                return new string[0];
            }
            var nodeList = xdoc["libraryDescription"];
            if(nodeList == null)
                return new string[0];
            nodeList = nodeList["searchConnectorDescriptionList"];
            if(nodeList == null)
                return new string[0];
            var nodeList2 = nodeList.GetElementsByTagName("searchConnectorDescription");
            if (nodeList2.Count == 0)
                return new string[0];
            var temp = (from XmlNode node
                        in nodeList2
                        let xmlElement = node["simpleLocation"]
                        where xmlElement != null
                        let element = xmlElement["url"]
                        where element != null
                        select element.InnerText
                        ).ToList();
            var arr = new string[temp.Count];
            for (var i = 0; i < temp.Count; i++)
            {
                if (temp[i].StartsWith("knownfolder:", StringComparison.InvariantCultureIgnoreCase))
                    arr[i] = Util.ResolveKnownFolder(temp[i].Substring(12));
                else if (!temp[i].StartsWith("shell:", StringComparison.InvariantCultureIgnoreCase)) //Uninitialized library
                    arr[i] = temp[i];
            }
            return arr;
        }
    }
}
