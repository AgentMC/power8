using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Power8
{
    static class PowerItemTree
    {
        private static readonly string PathRoot = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        private static readonly string PathCommonRoot = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);

        private static readonly FileSystemWatcher Watcher = new FileSystemWatcher(PathRoot);
        private static readonly FileSystemWatcher CommonWatcher = new FileSystemWatcher(PathCommonRoot);

        private static readonly PowerItem RootItem = new PowerItem();

        private static readonly ObservableCollection<PowerItem> ItemsRootCollection =
            new ObservableCollection<PowerItem> {RootItem};

        public static ObservableCollection<PowerItem> ItemsRoot { get { return ItemsRootCollection; } }  

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
            return;

            if (e.FullPath.StartsWith(PathRoot) || e.FullPath.StartsWith(PathCommonRoot))
            {
                bool isDir = Directory.Exists(e.FullPath);
                string sourceForSplit = null;
                if (e.OldFullPath.StartsWith(PathRoot))
                    sourceForSplit = e.OldFullPath.Substring(PathRoot.Length);
                else if(e.OldFullPath.StartsWith(PathCommonRoot))
                    sourceForSplit = e.OldFullPath.Substring(PathCommonRoot.Length);
                if(sourceForSplit != null)
                {
                    var sourceSplitted = sourceForSplit.Split(new[] {'\\'}, StringSplitOptions.RemoveEmptyEntries);
                    var item2Remove = RootItem;
                    for (int i = 0; i < sourceSplitted.Length-2; i++)
                    {
                        item2Remove =
                            item2Remove.Items.First(item => item.IsFolder && item.FriendlyName == sourceSplitted[i]);
                    }
                    item2Remove =
                        item2Remove.Items.First(
                            item => item.IsFolder == isDir && item.Argument == sourceSplitted[sourceSplitted.Length - 1]);
                    item2Remove.Parent.Items.Remove(item2Remove);
                }
            }
        }

        private static void FileChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            throw new NotImplementedException();
        }


        public static void InitTree()
        {
            ScanFolder(RootItem, PathRoot);
            ScanFolder(RootItem, PathCommonRoot);
        }

        private static void ScanFolder(PowerItem item, string basePath)
        {
            foreach (var directory in Directory.GetDirectories(basePath + item.Argument))
            {
                if(!(File.GetAttributes(directory).HasFlag(FileAttributes.Hidden)))
                    ScanFolder(AddSubItem(item, basePath, directory, true), basePath);
            }
            foreach (var file in Directory.GetFiles(basePath + item.Argument))
            {
                if (!(File.GetAttributes(file).HasFlag(FileAttributes.Hidden)))
                    AddSubItem(item, basePath, file, false);
            }
        }

        private static PowerItem AddSubItem(PowerItem item, string basePath, string fsObject, bool isFolder)
        {
            var argStr = fsObject.Substring(basePath.Length);
            var child = item.Items.FirstOrDefault(i => i.Argument == argStr && i.IsFolder == isFolder);
            if(child == null)
            {
                child = new PowerItem {Argument = argStr, Parent = item, IsFolder = isFolder};
                item.Items.Add(child);
            }
            return child;
        }


        public static ProcessStartInfo ResolveItem(PowerItem item)
        {
            var psi = new ProcessStartInfo();
            var arg1 = PathRoot + item.Argument;
            var arg2 = PathCommonRoot + item.Argument;
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
            return psi;
        }
    }
}
