using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using Power8.Helpers;
using Power8.Properties;
using Power8.Views;

namespace Power8
{
    /// <summary>
    /// Class manages the different trees of PowerItem`s, and roots for start menu, menued buttons and so on.
    /// Class also provides way to search these lists and do other types of searches.
    /// </summary>
    static class PowerItemTree
    {
        //....Items.Add(New PowerItem{FriendlyName=SEPARATOR_NAME}) ==> adds the separator item, visualized in menus only.
        public const string SEPARATOR_NAME = "----";

        private static readonly string 
            PathRoot = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), //User start menu rioot
            PathCommonRoot = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu); //All users start menu root

        //Ignore changed in
        private static readonly string
            IcrAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).ToLowerInvariant(),
            IclAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).ToLowerInvariant(),
            IcTemp = Util.ResolveLongPathOrDisplayName(Environment.ExpandEnvironmentVariables("%temp%")).ToLowerInvariant();

        #region Roots

        //Single virtual root item for both Common and User Start Menus
        private static readonly PowerItem StartMenuRootItem = new PowerItem {IsFolder = true, Argument = @"\"};
        //Roots backing fields
        private static PowerItem _adminToolsItem,
                                 _librariesOrMyDocsItem,
                                 _controlPanelRoot,
                                 _myComputerItem,
                                 _networkRoot;

        public static readonly EventWaitHandle CplDone = new EventWaitHandle(false, EventResetMode.ManualReset);

        /// <summary>
        /// Bindable collection of items presented in Start Menu for current user, joined with the common Start menu entries
        /// </summary>
        public static readonly ObservableCollection<PowerItem> StartMenuRoot
            = new ObservableCollection<PowerItem> {StartMenuRootItem};
        
        /// <summary>
        /// Represents folder "Administrative tools" with children
        /// </summary>
        public static PowerItem AdminToolsRoot
        {
            get
            {
                if (_adminToolsItem == null)
                {   //No race condition with InitTree() since this is triggered from BtnStck binding initializer,
                    //and it will be triggered when window is shown, and window may be shown only after instance initialized,
                    //and the 1st initialization is automatically triggered right after InitTree() :)
                    var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools);
                    Log.Raw("CommonAdminTools=" + path);
                    var p2ba = PathToBaseAndArg(path);
                    Log.Fmt("Path2B&A returns 1:{0}, 2:{1}", p2ba.Item1, p2ba.Item2);
                    _adminToolsItem = SearchContainerByArgument(p2ba, StartMenuRootItem, false);
                    Log.Raw("SearchContainer returned " + (_adminToolsItem == null ? "null" : _adminToolsItem.FriendlyName));
                    if (_adminToolsItem == null)
                    {
                        _adminToolsItem = new PowerItem { Argument = path };
                        ScanFolderSync(_adminToolsItem, string.Empty, true);
                    }
                    else
                    {
                        _adminToolsItem = SearchItemByArgument(path, true, _adminToolsItem);
                    }
                    _adminToolsItem.Argument = API.ShNs.AdministrationTools; //Converting explicit FS item to shell-like
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

        /// <summary>
        /// Represents Windows Libraries window, acting however via simple Libraries parser.
        /// On WinXP represents MyDocuments folder
        /// </summary>
        public static PowerItem LibrariesRoot
        {
            get
            {
                if (_librariesOrMyDocsItem == null)
                {
                    string path, ns;
                    if (Util.OsIs.SevenOrMore) //Win7+ -> return libraries
                    {
                        path = Util.ResolveKnownFolder(API.KnFldrIds.Libraries);
                        ns = API.ShNs.Libraries;
                    }
                    else                       //XP or below -> return MyDocs
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
                    };                         //Deferred children initialization
                    ScanFolderSync(_librariesOrMyDocsItem, string.Empty, false);
                    _librariesOrMyDocsItem.Argument = ns; //For icon and similar
                }
                return _librariesOrMyDocsItem;
            }
        }

        /// <summary>
        /// Clears cached control panel item to rebuild it when required
        /// </summary>
        public static void RefreshControlPanelRoot()
        {
            _controlPanelRoot = null;
        }

        /// <summary>
        /// Element which represents OS-dependent control panel link with children
        /// </summary>
        public static PowerItem ControlPanelRoot
        {
            get
            {
                if (_controlPanelRoot == null)
                {
                    var sysMainItem = new PowerItem
                    {
                        Argument = Util.OsIs.SevenOrMore ? API.ShNs.ControlPanel : API.ShNs.AllControlPanelItems,
                        SpecialFolderId = API.Csidl.CONTROLS,
                        ResourceIdString = Util.GetLocalizedStringResourceIdForClass(API.ShNs.ControlPanel),
                        NonCachedIcon = true,
                        HasLargeIcon = true,
                        IsFolder = true
                    };
                    var sysExtendedItem = Util.OsIs.XPOrLess ? null : new PowerItem
                    {
                        Argument = API.ShNs.AllControlPanelItems,
                        SpecialFolderId = API.Csidl.CONTROLS,
                        ResourceIdString = Util.GetLocalizedStringResourceIdForClass(API.ShNs.AllControlPanelItems),
                        IsFolder = true
                    };

                    //the item itself
                    _controlPanelRoot = (Util.OsIs.XPOrLess || SettingsManager.Instance.ShowMbCtrlByCat)
                                            ? sysMainItem
                                            : sysExtendedItem;
                    Debug.Assert(_controlPanelRoot != null, "_controlPanelRoot != null");
                    var cplCache = new List<string>();
                    //Flow items (Vista-like) and CPLs from cache
                    using (var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                       @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace", false))
                    {
                        if (k != null)
                        {
                            foreach (var cplguid in k.GetSubKeyNames())
                            {
                                if(cplguid.StartsWith("{")) //registered guid
                                {
                                    _controlPanelRoot.Items.Add(new PowerItem
                                    {
                                        Argument = API.ShNs.AllControlPanelItems + "\\::" + cplguid,
                                        NonCachedIcon = true,
                                        Parent = _controlPanelRoot,
                                        ResourceIdString = Util.GetLocalizedStringResourceIdForClass(cplguid, true)
                                    });
                                }
                                else //named items, for example "Internet options" on XP. In general, this block
                                {    //is not documented on MSDN
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
                        var resolved = Util.GetCplInfo(cpl); //Something new...
                        if (resolved.Item2 != null && _controlPanelRoot.Items.All(p => p.FriendlyName != resolved.Item1))
                            _controlPanelRoot.Items.Add(new PowerItem
                            {
                                Argument = cpl,
                                Parent = _controlPanelRoot,
                                FriendlyName = resolved.Item1,
                                Icon = resolved.Item2
                            });
                    }

                    _controlPanelRoot.SortItems();

                    if (Util.OsIs.SevenOrMore) //XP only supports "All items"
                    {
                        //for 7+ we add "All Control Panel Items" + separator
                        _controlPanelRoot.Icon = ImageManager.GetImageContainerSync(sysMainItem, API.Shgfi.SMALLICON);

                        _controlPanelRoot.Items.Insert(0,
                                                       SettingsManager.Instance.ShowMbCtrlByCat
                                                           ? sysExtendedItem
                                                           : sysMainItem);
                        _controlPanelRoot.Items[0].Parent = _controlPanelRoot;
                        _controlPanelRoot.Items[0].Icon = _controlPanelRoot.Icon;
                        
                        _controlPanelRoot.Items.Insert(1, new PowerItem
                        {
                            FriendlyName = SEPARATOR_NAME, 
                            Parent = _controlPanelRoot
                        });
                    }
                    CplDone.Set();
                }
                return _controlPanelRoot;
            }
        }

        /// <summary>
        /// Represents Computer with drives inside
        /// </summary>
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
                    DriveManager.Init(FileChanged, FileRenamed, _myComputerItem);//Will add drives under MyComputer + watches the files
                }
                return _myComputerItem;
            }
        }

        /// <summary>
        /// The item, all network stuff is located under:
        /// - repsesents "Network neighbourhood"
        /// - under: workgroup/domain
        /// - under: connections
        /// - under: computers nearby
        /// </summary>
        public static PowerItem NetworkRoot
        {
            get
            {
                if (_networkRoot == null)
                {
                    //Same guids mean different stuff between xp and 7+.
                    //Fortunately there're only 2 of them :)
                    var xpNet7Wrkgrp = new PowerItem
                    {
                        SpecialFolderId = API.Csidl.NETWORK,
                        IsFolder = true,
                        HasLargeIcon = true,
                        NonCachedIcon = true,
                        Argument = API.ShNs.NetworkNeighbourhood
                    };

                    var xpWrkgrp7Net = new PowerItem
                    {
                        SpecialFolderId = API.Csidl.COMPUTERSNEARME,
                        IsFolder = true,
                        NonCachedIcon = true,
                        HasLargeIcon = true
                    };

                    //Choose Root and the Child based on OS
                    _networkRoot = Util.OsIs.SevenOrMore ? xpWrkgrp7Net : xpNet7Wrkgrp;

// ReSharper disable PossibleUnintendedReferenceComparison
                    var child = _networkRoot == xpWrkgrp7Net ? xpNet7Wrkgrp : xpWrkgrp7Net;
// ReSharper restore PossibleUnintendedReferenceComparison
                    _networkRoot.Items.Add(child);
                    child.Parent = _networkRoot;

                    //Try to get chached Connections PowerItem
                    var conString = Util.ResolveSpecialFolderName(API.Csidl.CONNECTIONS);
                    var connections =
                        ControlPanelRoot.Items.FirstOrDefault(i => i.FriendlyName == conString) ??
                        new PowerItem
                        {
                            SpecialFolderId = API.Csidl.CONNECTIONS,
                            NonCachedIcon = true,
                            IsFolder = true,
                            Argument = API.ShNs.NetworkConnections,
                            Parent = _networkRoot
                        };
                    _networkRoot.Items.Add(connections);
                                        
                    _networkRoot.Items.Add(new PowerItem {FriendlyName = SEPARATOR_NAME, Parent = _networkRoot});


                    //Search for computers
                    Util.ForkPool(() =>
                                  {
                                      if (Util.OsIs.SevenOrMore) //Name won't be resolved automatically on 7+
                                          xpNet7Wrkgrp.FriendlyName = NetManager.DomainOrWorkgroup;

                                      List<string> names;
                                      bool addMoreItem = false; //if number of computers > 10
                                      if (NetManager.ComputersNearby.Count > 10)//indirectly refresh computers cache
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
                                      //otherwise convert all computers into the PIs and add to the net root
                                      names.Select(e => new PowerItem
                                                            {
                                                                Argument = "\\\\" + e,
                                                                IsFolder = true,
                                                                Parent = _networkRoot,
                                                                Icon = MyComputerRoot.Icon
                                                            })
                                          .ToList()
                                          .ForEach(i => Util.Post(() => _networkRoot.Items.Add(i)));

                                      if (addMoreItem) //Add special class-link PowerItem
                                          Util.Post(() =>
                                            {
                                                _networkRoot.Items[0].Icon = //workgroup/domain's icon
                                                    ImageManager.GetImageContainerSync(_networkRoot.Items[0], API.Shgfi.SMALLICON);
                                                _networkRoot.Items.Add(new PowerItem
                                                                        {
                                                                            FriendlyName = Resources.Str_ShowMore,
                                                                            Parent = _networkRoot,
                                                                            SpecialFolderId =
                                                                                API.Csidl.POWER8CLASS,
                                                                            IsFolder = true,
                                                                            Argument = typeof(ComputerList).FullName,
                                                                            Icon = _networkRoot.Items[0].Icon
                                                                        });
                                            });
                                  }, "Network Scan Thread");
                }
                return _networkRoot;
            }
        }

        #endregion

        #region FS Events handlers

        //Handles event when a file is renamed under any of drives watched
        private static void FileRenamed(object sender, RenamedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Name)
                || string.IsNullOrEmpty(e.FullPath)
                || string.IsNullOrEmpty(e.OldName)
                || string.IsNullOrEmpty(e.OldFullPath))
            {
                Log.Fmt("FileRenamed: Name: {0}, FullPath: {1}, Old: {2}, OldFP: {3}",
                                    e.Name, e.FullPath, e.OldName, e.OldFullPath);
                return; //Sometimes this happens
            }
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

        //Handles event when a file is created, deleted or changed in any other way under any of drives watched
        private static void FileChanged(object sender, FileSystemEventArgs e)
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

            //Veryfying file changed not under any of appdata
            var fpLow = e.FullPath.ToLowerInvariant();
            if(fpLow.StartsWith(IcrAppData) || fpLow.StartsWith(IclAppData) || fpLow.StartsWith(IcTemp))
                return;
            Log.Fmt("File {0}: {1}", e.ChangeType, e.FullPath);
            //Ensuring buttonstack is created on Main thread
            if(!BtnStck.IsInstantited)
                Util.Send(() => BtnStck.Instance.InvalidateVisual());

            var isDir = Directory.Exists(e.FullPath);
            var baseAndArg = PathToBaseAndArg(e.FullPath);
            if (baseAndArg.Item2 == null) 
                return; //Even %startmenu% is changed, here will be "/", so null means error

            var roots = new List<PowerItem> {MyComputerRoot, StartMenuRootItem}; //look only under these + libraries
            foreach (var lib in LibrariesRoot.Items.Where(lib => !lib.AutoExpandIsPending)) //if expanded already
                roots.AddRange(lib.Items);

            foreach (var root in roots)
            {
// ReSharper disable PossibleUnintendedReferenceComparison
                var item = SearchContainerByArgument(baseAndArg, root,
                                                     e.ChangeType == WatcherChangeTypes.Created &&
                                                     root == StartMenuRootItem);
                //Create intermediate folders only for Start Menu and only in casethe file was created
// ReSharper restore PossibleUnintendedReferenceComparison
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
                                    item.Parent.Items.Remove(item); //Lol, eh?
                                else if (item != null)
                                    item.Update(); //when icon for folder is set, the folder changed event arrives
                                break;
                            case WatcherChangeTypes.Created:
                                AddSubItem(item, baseAndArg.Item1, e.FullPath, isDir);
                                if(SettingsManager.Instance.AutoSortTrees) 
                                    item.SortItems(); //We're interested in Add and Rename for this. Both will go here.
                                break;
                        }
                    });
                }
            }
        }

        #endregion

        #region Tree builder methods
        
        /// <summary>
        /// Initially builds start menu tree and then starts the Button stack initialization
        /// </summary>
        public static void InitTree()
        {
#if DEBUG
            var s = Stopwatch.StartNew();
#endif
            ScanFolderSync(StartMenuRootItem, PathRoot, true);
            ScanFolderSync(StartMenuRootItem, PathCommonRoot, true);
            StartMenuRootItem.SortItems();
            //Set configurable name. Proxy logic is put into manager, so in case 
            //nothing is configured, null will be returned, which will cause
            //this item to regenerate Friendly name, i.e. re-resolve the resourceId string
            StartMenuRootItem.FriendlyName = SettingsManager.Instance.StartMenuText;
#if DEBUG
            Log.Fmt("InitTree - scanned in {0}", s.ElapsedMilliseconds);
#endif
            Util.Send(() => BtnStck.Instance.InvalidateVisual());
#if DEBUG
            Log.Fmt("InitTree - done in {0}", s.ElapsedMilliseconds);
            s.Stop();
#endif
        }

        /// <summary>
        /// Initializes an asynchronous scan of some FS folder
        /// </summary>
        /// <param name="item">A PowerItem that represents an FS folder or a Windows Library</param>
        /// <param name="basePath">If item passed represents folder under Start Menu, place here corresponding 
        /// (Common or User) special folder path. Empty string otherwise. See AddSubItem for details.</param>
        /// <param name="recoursive">True to scan subdirectories. True by default.</param>
        public static void ScanFolder(PowerItem item, string basePath, bool recoursive = true)
        {
            ThreadPool.QueueUserWorkItem(o => ScanFolderSync(item, basePath, recoursive));
        }

        /// <summary>
        /// Synchronously scanns some FS folder, filling Items list of item passed
        /// </summary>
        /// <param name="item">A PowerItem that represents an FS folder, special folder or a Windows Library</param>
        /// <param name="basePath">If item passed represents folder under Start Menu, place here corresponding 
        /// (Common or User) special folder path. Empty string otherwise. See AddSubItem for details.</param>
        /// <param name="recoursive">True to scan subdirectories.</param>
        private static void ScanFolderSync(PowerItem item, string basePath, bool recoursive)
        {
            try
            {   //Obtain full fs path to current location
                var curDir = basePath + (item.Argument ?? Util.ResolveSpecialFolder(item.SpecialFolderId));
                Log.Raw("In: " + curDir, item.ToString());
                //Parse child directories and recoursively call the ScanFolderSync
                foreach (var directory in item.IsLibrary ? GetLibraryDirectories(curDir) : Directory.GetDirectories(curDir))
                {   //Skip hidden directories
                    if ((File.GetAttributes(directory).HasFlag(FileAttributes.Hidden)))
                    {
                        Log.Raw("Skipped because item appears to be hidden");
                        continue;
                    }
                    //Otherwise add the directory item to this PowerItem
                    var subitem = AddSubItem(item, basePath, directory, true, autoExpand: !recoursive);
                    if (recoursive)
                        ScanFolderSync(subitem, basePath, true);
                }
                if (item.IsLibrary) //Since Libraries are actually files, but were already parsed as folders, we shan't continue...
                    return;
                //Proceed with files
                var resources = new Dictionary<string, string>();
                var dsktp = curDir + "\\desktop.ini";
                if (File.Exists(dsktp)) //Let's parse Desktop.ini if it exists
                {
                    using (var reader = new StreamReader(dsktp, System.Text.Encoding.Default, true))
                    {
                        string str; //TODO: rewrite!!! Currently elements located after LFN section aren't parsed!
                        while ((str = reader.ReadLine()) != null && !str.Contains("[LocalizedFileNames]"))
                        {
                            if (str.StartsWith("IconFile=") || str.StartsWith("IconResource="))
                            {
                                Util.Post(() =>
                                              {
                                                item.NonCachedIcon = true;
                                                item.Icon = null;
                                              });
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
                //Let's scan files now 
                foreach (var file in Directory.GetFiles(curDir))
                {
                    if ((File.GetAttributes(file).HasFlag(FileAttributes.Hidden)))
                        continue; //Skip hidden files

                    var fn = Path.GetFileName(file);
                    var fileIsLib = (Path.GetExtension(file) ?? "")
                                         .Equals(".library-ms", StringComparison.InvariantCultureIgnoreCase);
                    AddSubItem(item, basePath, file, fileIsLib,
                                fn != null && resources.ContainsKey(fn) ? resources[fn] : null,
                                fileIsLib);
                }
            }
            catch (UnauthorizedAccessException)
            { Log.Raw("UnauthorizedAccessException"); }//Don't care if user is not allowed to access fileor directory or it's contents
            catch (IOException)
            { Log.Raw("IOException"); }//Don't care as well if file was deleted on-the-fly, watcher will notify list
            finally
            {  //Explicitly set marker showing that enumeration operations may occur on Items from this moment
                item.AutoExpandIsPending = false;
            }
        }

        /// <summary>
        /// For the given parent PowerItem, searches child for given parameters, or creates a new one
        /// if search is unsuccessful or cannot be executed, and returns the child obtained.
        /// Thread-safe, UI-thread-safe.
        /// </summary>
        /// <param name="item">parent PowerItem, typically a filesystem folder, a Library or a special folder</param>
        /// <param name="basePath">if 'item' represents a folder under Start menu (and so has relative Argument),
        /// pass here a full-qualified path to a User or Common Start Menu (the one under which a folder is 
        /// actually located). Use PathRoot and PathCommonRoot fields to simpolify the task. Empty string otherwise.</param>
        /// <param name="fsObject">Non-virtual non-junction file system object (file or folder), not null, not empty
        /// (this is the most meaningful parameter). If you need to add a virtual item (e.g. Computer element), use
        /// direct access to proprties (parent.Items.Add(child); child.Parent=parent;) with respect to 
        /// IsAutoExpandPending property. Child search parameter.</param>
        /// <param name="isFolder">Sets child's IsFolder property to a value passed. Child search parameter.</param>
        /// <param name="resourceId">Localized resource identifier in a standard "[@]Library,-resId[__varData*]" form.
        /// Null by default. Since 0.4 can be set to a required string directly, but in this case it is recommended to 
        /// set FriendlyName explicitly</param>
        /// <param name="autoExpand">True to mark the item as deferred-expandable. This means it's children won't be enumerated
        /// synchronously and will be loaded automatically later, when requested by the user. False by default.</param>
        private static PowerItem AddSubItem(PowerItem item, string basePath, string fsObject, bool isFolder, string resourceId = null, bool autoExpand = false)
        {
            var argStr = fsObject.Substring(basePath.Length); //Expected relative argument in case of Start Menu item
            Log.Raw("In: " + fsObject, item.ToString());
            var child = autoExpand || item.AutoExpandIsPending //Searching...
                    ? null
                    : item.Items.FirstOrDefault(i =>
                                string.Equals(i.Argument, argStr, StringComparison.CurrentCultureIgnoreCase)
                                && i.IsFolder == isFolder);
            Log.Raw("child: " + (child == null ? "null" : child.ToString()), item.ToString());
            if(child == null) //Generating...
            {
                child = new PowerItem
                            {
                                Argument = argStr,
                                Parent = item,
                                IsFolder = isFolder,
                                ResourceIdString = resourceId,
                                AutoExpand = autoExpand
                            };
                Util.Send(() => item.Items.Add(child)); //Synchronously add item in UI thread
            }
            return child;
        }

        #endregion

        #region Tree Utilities

        /// <summary>Converts PowerItem passed into runnable ProcessStartInfo structure.</summary>
        /// <param name="item">The PowerItem that needs to be started.</param>
        /// <param name="prioritizeCommons">For StartMenu items, use this to indicate that 
        /// the common item is desired to be runned in case both are available.</param>
        /// <returns>ProcessStartInfo structure which can be used to "launch" passed PowerItem.
        /// This includes but is not limited to:
        /// - execute program 'item' points to;
        /// - execute program associated with format of the file 'item' points to;
        /// - open folder 'item' points to;
        /// - execute ControlPanel item, the 'item' points to
        /// ...and so on.</returns>
        public static ProcessStartInfo ResolveItem(PowerItem item, bool prioritizeCommons = false)
        {
            var psi = new ProcessStartInfo();
            var arg1 = item.Argument;
            if (item.IsSpecialObject || arg1 == null) //CPL item or some ::Special//::Folder
            {
                bool cplSucceeded = false;
                if (item.IsControlPanelChildItem)
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
                    {//Sysname-registered CPL items
                        var sysname = Util.GetCplAppletSysNameForClass(arg1);
                        if (!string.IsNullOrEmpty(sysname))
                        {
                            psi.FileName = "control.exe";
                            psi.Arguments = "/name " + sysname;
                            cplSucceeded = true;
                        }
                    }
                }
                if (!cplSucceeded) //Special folder?
                {
                    psi.FileName = "explorer.exe";
                    psi.Arguments = "/N," + (item.Argument ?? Util.ResolveSpecialFolder(item.SpecialFolderId));
                }
            }
            else if (item.SpecialFolderId == API.Csidl.POWER8JLITEM)
            {//P8 internal-implementation jump list's command
                psi = ResolveItem(item.Parent, prioritizeCommons);
                psi.Arguments = item.Argument; //simply invoke parent with command equal to 'item''s Argument
            }
            else //File or folder probably
            {
                if (!(arg1.StartsWith("\\\\") || (arg1.Length > 1 && arg1[1] == ':')))
                    arg1 = PathRoot + item.Argument; //form full path, depending on what Argument is
                var arg2 = PathCommonRoot + item.Argument; //arg2 ALWAYS starts with common path
                if (prioritizeCommons) //so, if you set prioritizeCommons, you GUARANTEE it's StartMenu item under Common one
                    arg2 = Interlocked.Exchange(ref arg1, arg2);
                if (item.IsFolder)
                {//if folder exists, or it's network path, or the secondary path exists - open in explorer, otherwise fail
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
                else // if file exists, or secondary evaluated file exists - run, otherwise fail
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

        /// <summary>Safely converts PowerItem to a string representation of it's target</summary>
        /// <param name="item">The PowerItem to convert</param>
        /// <returns>Something that actually will be opened: 
        /// - Argument for SpecialObjects 
        /// - Runnable parent for P8 JL implementation
        /// - Folder path for folder-pointing items
        /// - Full-path argumet for file-pointing items</returns>
        public static string GetResolvedArgument(PowerItem item)
        {
            if(item.IsSpecialObject)
                return item.Argument;
            if (item.SpecialFolderId == API.Csidl.POWER8JLITEM)
                return GetResolvedArgument(item.Parent);
            var psi = ResolveItem(item);
            return item.IsFolder ? psi.Arguments : psi.FileName;
        }

        /// <summary>
        /// Breaks full path into predicate base (one of User Start Menu and Common Start Menu) and the trailling stuff
        /// </summary>
        /// <param name="itemFullPath">file system object in its string representation</param>
        /// <returns>Tuple of base path and the other stuff. Base can be empty string in case item isn't located 
        /// under Start menu for current user or the Common one</returns>
        private static Tuple<string, string> PathToBaseAndArg(string itemFullPath)
        {
            if (itemFullPath.StartsWith(PathRoot))
                return new Tuple<string, string>(PathRoot, itemFullPath.Substring(PathRoot.Length));
            return itemFullPath.StartsWith(PathCommonRoot)
                ? new Tuple<string, string>(PathCommonRoot, itemFullPath.Substring(PathCommonRoot.Length))
                : new Tuple<string, string>(string.Empty, itemFullPath);
        }

        /// <summary>
        /// Parses *.library-ms file to return the incorporated pathes
        /// </summary>
        /// <param name="libraryMs">Fully-qualified path to the library file</param>
        /// <returns>Array of strings, each one is a File System directory.</returns>
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
                    arr[i] = Util.ResolveKnownFolder(temp[i].Substring(12)); //Expand known folders
                else if (!temp[i].StartsWith("shell:", StringComparison.InvariantCultureIgnoreCase)) //Uninitialized library
                    arr[i] = temp[i];
            }
            return arr;
        }

        #endregion

        #region Search

        /// <summary>
        /// Event arguments to be used with WinSearchThread* events
        /// </summary>
        public class WinSearchEventArgs:EventArgs
        {
            public readonly PowerItem Root;
            public readonly CancellationToken Token;
            public readonly bool SearchCompleted;
            public WinSearchEventArgs(PowerItem root, CancellationToken token, bool searchCompleted)
            {
                Root = root;
                Token = token;
                SearchCompleted = searchCompleted;
            }
        }

        private static CancellationTokenSource _lastSearchToken;
        private static object _searchTokenSyncLock = new object();

        //Indicate the state chenage of Windows Search thread.
        public static event EventHandler<WinSearchEventArgs> WinSearchThreadCompleted, WinSearchThreadStarted;

        /// <summary>
        /// From collection tree passed, searches for a parent item (i.e. container) of the one that would
        /// represent the object described by passed tuple. Optionally tries to generate items in the middle.
        /// </summary>
        /// <param name="baseAndArg">Tuple returned by <code>PathToBaseAndArg</code></param>
        /// <param name="collectionRoot">The root of collection, like <code>StartMenuRootItem</code></param>
        /// <param name="autoGenerateSubItems">Should method generate proxy Folder items in between of last available 
        /// and required items?</param>
        private static PowerItem SearchContainerByArgument(Tuple<string, string> baseAndArg, PowerItem collectionRoot, bool autoGenerateSubItems)
        {
            if (!string.IsNullOrEmpty(collectionRoot.Argument)
                && baseAndArg.Item2.StartsWith(collectionRoot.Argument, StringComparison.InvariantCultureIgnoreCase)
                && baseAndArg.Item2 != collectionRoot.Argument)
            {//Make argument relative to Root
                baseAndArg = new Tuple<string, string>(baseAndArg.Item1,
                                                        baseAndArg.Item2.Substring(collectionRoot.Argument.Length + 1));
            }
            var sourceSplitted = baseAndArg.Item2.Split(new[] {'\\'}, StringSplitOptions.RemoveEmptyEntries);
            if (sourceSplitted.Length > 0 && sourceSplitted[0].EndsWith(":"))
                sourceSplitted[0] += "\\"; //For MyComputer children
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
                    //TODO: really Eval()? UI in ASI kicked from Send, probably don't need this...
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
        
        /// <summary>
        /// From a container's children selects the one that seems to be discribed by passed parameters
        /// </summary>
        /// <param name="argument">Search parameter: child's expected Argument. Case-insensitive.</param>
        /// <param name="isFolder">Search parameter: child's IsFolder value.</param>
        /// <param name="container">The PowerItem to search children from.</param>
        /// <returns></returns>
        private static PowerItem SearchItemByArgument(string argument, bool isFolder, PowerItem container)
        {
            if(container.AutoExpandIsPending)
                return null;
            var endExpr = Path.GetFileName(argument) ?? argument;
            return container.Items.FirstOrDefault(i =>
                    i.IsFolder == isFolder &&
                    i.Argument.EndsWith(endExpr, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// From the StartMenu, searches for the only item that will Match() the given 'argument'.
        /// This is used to quickly find Start Menu element corresponding to some MFU item.
        /// Not thread-safe, not UI-thread-safe, call ONLY from UI thread!
        /// </summary>
        /// <param name="argument">Some string to compare to</param>
        /// <returns>The only Start Menu PowerItem the 'argument' matches, or null.</returns>
        public static PowerItem SearchStartMenuItemSyncFast(string argument)
        {
            var list = new Collection<PowerItem>();
            SearchRootFastSync(argument.ToLowerInvariant(), StartMenuRootItem, list);
            return list.Count == 1 ? list[0] : null;
        }

        //----------------

        /// <summary>
        /// From the all data sources available, searches PowerItems that Match() to 'query' and stores results in 'destination'.
        /// Search goes on in separate thread for each root. Each thread as well as this initialier thread is cancellable.
        /// Search takes into account all rootitems available in P8 UI + Windows Search data. Thread-safe/UI-thread-safe.
        /// </summary>
        /// <param name="query">Something, found elements should Match() to.</param>
        /// <param name="destination">The IList ofPowerItems, where to store the search results.</param>
        /// <param name="callback">The callback to be executed after the some root search thread is completed.
        /// NOTE: this is not valid for WinSearch, use WinThread* events for it.</param>
        public static void SearchTree(string query, IList<PowerItem> destination, Action<PowerItem, CancellationToken> callback)
        {
            var token = SearchTreeCancel(); //Stop previous searches and initializes new search token
            lock (destination)  //Just hang here until previous searches are all done
            {                   //...and BTW, pre-pause all child threads
                Util.Send(destination.Clear);
                string ext = null; //requested extension for filtered WinSearch ("exe|paint net")
                if(!query.Contains("|")) //not filtered, regular search
                {
                    foreach (var root in new[] { MyComputerRoot, StartMenuRootItem, ControlPanelRoot, 
                                                 NetworkRoot, LibrariesRoot, MfuList.MfuSearchRoot })
                    {
                        var r = root;
                        Util.ForkPool(() =>
                            {
                                SearchItems(query, r, destination, token);
                                if (!token.IsCancellationRequested)
                                    Util.Post(() => callback(r, token));
                            },
                            "Tree search in " + r.FriendlyName + " for " + query);
                    }
                }
                else //type-filtered WinSearch, splitter part
                {
                    var pair = query.Split(new[] {'|'}, 2);
                    ext = pair[0];
                    query = pair[1];
                }
                if (query.Length >= 3) //init WinSearch
                    Util.ForkPool(() => SearchWindows(query, ext, destination, token), 
                                "WinSearch worker for (" + ext + ")/" + query);
            }
        }

        /// <summary>
        /// Cancels previously started Tree search, including WinSearch query
        /// </summary>
        public static CancellationToken SearchTreeCancel()
        {
            lock (_searchTokenSyncLock)
            {
                if (_lastSearchToken != null)
                    _lastSearchToken.Cancel();
                _lastSearchToken = new CancellationTokenSource();
                return _lastSearchToken.Token;
            }
        }

        /// <summary>
        /// From the 'source' given, recoursively searches the PowerItem that would Match() the 'query', including
        /// the 'source' itself, storing results in 'destination'. Finds folders unless they're under Start Menu.
        /// Cancellable. Intended to be run async. Thread/UI-thread-safe.
        /// </summary>
        /// <param name="query">Something searched results should Match() to.</param>
        /// <param name="source">Tree to search from, including the collection root item passed 
        /// (but not the source.Root itself if the passed item isn't the root of the collection)</param>
        /// <param name="destination">Collection to store data in</param>
        /// <param name="stop">Cancellation token from initializer thread</param>
        private static void SearchItems(string query, PowerItem source, IList<PowerItem> destination, CancellationToken stop)
        {
            if(stop.IsCancellationRequested)
                return;
            if ((!source.IsFolder || source.Root != StartMenuRootItem) && source.Match(query))
            {//return ((folders for not StartMenu children) or files) that Match() the query
                lock (destination)
                {
                    if (!stop.IsCancellationRequested && //this item wasn't added before
                        !destination.Any(d => d.FriendlyName == source.FriendlyName && d.IsFolder == source.IsFolder))
                            Util.Send(() => { if (!stop.IsCancellationRequested) destination.Add(source); });
                }
            }
            if (!source.AutoExpandIsPending)
                foreach (var powerItem in source.Items)
                    SearchItems(query, powerItem, destination, stop);
        }

        //Proxy for SearchStartMenuSyncFast. Code literally copypasted from SearchItems and simplified.
        //Not thread-safe.
        private static void SearchRootFastSync(string query, PowerItem source, ICollection<PowerItem> destination)
        {
            if (!source.IsFolder && source.Match(query) && destination.All(d => d.FriendlyName != source.FriendlyName))
                destination.Add(source);
            if (!source.AutoExpandIsPending)
                foreach (var powerItem in source.Items)
                    SearchRootFastSync(query, powerItem, destination);
        }

        /// <summary>
        /// Queries Windows Search service indexer for the items that will mathc the query. Sorts results by search rank.
        /// Cancellable. Intended to be run from background thread. UI-thread-safe.
        /// </summary>
        /// <param name="query">Something the results should match to.</param>
        /// <param name="ext">When you want to filter only specific types of files, set this to file extension 
        /// (like "wmv"). Set to null otherwise.</param>
        /// <param name="destination">The collection to store results in.</param>
        /// <param name="stop">Cancellation token to break the execution.</param>
        private static void SearchWindows(string query, string ext, IList<PowerItem> destination, CancellationToken stop)
        {
            if(stop.IsCancellationRequested)
                return;

            const string connText = "Provider=Search.CollatorDSO;Extended Properties='Application=Windows'"; //See MSDN 4 details
            var comText = @"SELECT TOP 100 System.ItemUrl FROM SYSTEMINDEX WHERE System.Search.Store='FILE' and " +
                (ext == null ? string.Empty : "System.FileName like '%." + ext + "' and ") +
                "(FREETEXT ('" + query + "') OR System.FileName like '%" + query + "%') " +
                "ORDER BY RANK DESC"; 
            //I.e., get 100 files represented by their File:/// -style URI, probably of a given type, 
            //and looking similar to query or matched to it by full-text indexer, ordered by system rank

            Thread.Sleep(666); //let's give user the possibility to enter something more...
            OleDbDataReader rdr = null;
            OleDbConnection connection = null;
            PowerItem groupItem = null; //needed for visual representation

            try
            {
                if(!stop.IsCancellationRequested)
                {//Begin search andnotify subscribers
                    connection = new OleDbConnection(connText);
                    connection.Open();
                    var command = new OleDbCommand(comText, connection) {CommandTimeout = 0};

                    var h = WinSearchThreadStarted;
                    if (h != null) 
                        h(null, new WinSearchEventArgs(null, stop, false));
                    if(!stop.IsCancellationRequested)
                        rdr = command.ExecuteReader();
                }
                if (rdr != null)
                { //Build children list
                    groupItem = new PowerItem {FriendlyName = Resources.Str_WindowsSearchResults};
                    var added = 0;
                    var bs = Path.DirectorySeparatorChar;
                    while (!stop.IsCancellationRequested && added < 50 && rdr.Read())
                    {
                        lock (destination)
                        {
                            var data = rdr[0].ToString()
                                             .Substring(5)
                                             .TrimStart(new[] { '/' })
                                             .Replace('/', bs); //uri => path
                            if (data.Length > 1 && data[1] != ':') //UNC?
                                data = bs + bs + data;
                            var source = new PowerItem
                                             {
                                                 Argument = data,
                                                 Parent = groupItem,
                                                 IsFolder = Directory.Exists(data)
                                             };
                            if (!destination.Contains(source))
                            { //switch to UI context
                                Util.Send(() => { if (!stop.IsCancellationRequested) destination.Add(source); });
                                added++;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Raw(e.ToString());
            }
            finally
            {
                if(rdr != null)
                    rdr.Close();
                if(connection != null && connection.State == ConnectionState.Open)
                    connection.Close();
            }

            //Notify subscribers the search is conmpleted
            if (!stop.IsCancellationRequested)
            {
                var h = WinSearchThreadCompleted;
                if (h != null) 
                    h(null, new WinSearchEventArgs(groupItem, stop, true));
            }
        }

        #endregion
    }
}
