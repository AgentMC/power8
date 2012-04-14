using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

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
            {//We ignore hiden data
                if (e.ChangeType != WatcherChangeTypes.Deleted && File.GetAttributes(e.FullPath).HasFlag(FileAttributes.Hidden))
                    return;
                //Ensuring buttonstack is created on Main thread
                Util.Send(new Action(() => BtnStck.Instance.InvalidateVisual()));
                var isDir = Directory.Exists(e.FullPath);
                var baseAndArg = PathToBaseAndArg(e.FullPath);
                if (baseAndArg.Item2 == null) 
                    return;
                var sourceSplitted = baseAndArg.Item2.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                var item = StartMenuRootItem;
                for (int i = 0; i < sourceSplitted.Length - 1; i++)
                {
                    var prevItem = item;
                    item =
                        item.Items.FirstOrDefault(j => j.IsFolder && j.FriendlyName == sourceSplitted[i]);
                    if (item == null && e.ChangeType == WatcherChangeTypes.Created)
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
                ScanFolder(StartMenuRootItem, PathRoot);
                ScanFolder(StartMenuRootItem, PathCommonRoot);
            }
        }

        public static void ScanFolder(PowerItem item, string basePath, bool recoursive = true)
        {
            Util.MainDisp.BeginInvoke(DispatcherPriority.Background,
                                    new Action(() => ScanFolderInternal(item, basePath, recoursive)));
        }

        public static void ScanFolderInternal(PowerItem item, string basePath, bool recoursive)
        {
            try
            {
                var curDir = basePath + item.Argument;
                foreach (var directory in Directory.GetDirectories(curDir))
                {
                    if (!(File.GetAttributes(directory).HasFlag(FileAttributes.Hidden)))
                    {
                        var subitem = AddSubItem(item, basePath, directory, true, autoExpand: !recoursive);
                        if (recoursive)
                            ScanFolder(subitem, basePath);
                    }
                }
                var resources = new Dictionary<string, string>();
                var dsktp = curDir + "\\desktop.ini";
                if (File.Exists(dsktp))
                {
                    using (var reader = new StreamReader(dsktp, System.Text.Encoding.Default, true))
                    {
                        string str;
                        while ((str = reader.ReadLine()) != null && !str.Contains("[LocalizedFileNames]"))
                        {
                            if (str.Contains("IconFile="))
                            {
                                item.NonCachedIcon = true;
                                item.Icon = null;
                            }
                        }
                        while ((str = reader.ReadLine()) != null && str.Contains("="))
                        {
                            var pair = str.Split(new[] {'='}, 2);
                            resources.Add(pair[0], pair[1].TrimStart('@'));
                        }
                    }
                }
                foreach (var file in Directory.GetFiles(curDir))
                {
                    if (!(File.GetAttributes(file).HasFlag(FileAttributes.Hidden)))
                    {
                        var fn = Path.GetFileName(file);
                        AddSubItem(item, basePath, file, false,
                                   fn != null && resources.ContainsKey(fn) ? resources[fn] : null);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {}//Don't care if user is not allowed to access fileor directory or it's contents
        }

        private static PowerItem AddSubItem(PowerItem item, string basePath, string fsObject, bool isFolder, string resourceId = null, bool autoExpand = false)
        {
            var argStr = fsObject.Substring(basePath.Length);
            var child = item.Items.FirstOrDefault(i => i.Argument == argStr && i.IsFolder == isFolder);
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
            if (item.Argument.StartsWith("::{"))
            {
                psi.FileName = "explorer.exe";
                psi.Arguments = item.Argument;
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
            var psi = ResolveItem(item, prioritizeCommons);
            return item.IsFolder ? psi.Arguments : psi.FileName;
        }

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
    }
}
