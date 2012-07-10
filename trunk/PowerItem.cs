using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Power8.Properties;
using System.Linq;

namespace Power8
{
    public class PowerItem : INotifyPropertyChanged, IComparable<PowerItem>
    {
        private ImageManager.ImageContainer _icon;
        private readonly ObservableCollection<PowerItem> _items = new ObservableCollection<PowerItem>();
        private ObservableCollection<PowerItem> _cmdLines;
        private string _friendlyName, _resIdString, _resolvedLink;
        private bool _expanding, _hasLargeIcon, _autoExpand, _nonCachedIcon;
        private PowerItem _root;

        public string Argument { get; set; }
        public bool IsFolder { get; set; }
        public bool AutoExpandIsPending { get; set; }
        public API.Csidl SpecialFolderId { get; set; }



        public PowerItem()
        {
            SpecialFolderId = API.Csidl.INVALID;
        }

        public PowerItem (ObservableCollection<PowerItem> items ):this ()
        {
            _items = items;
        }

    

        //1st block - icon
        public ImageManager.ImageContainer Icon
        {
            get
            {
                if (_icon == null && (Argument != null || SpecialFolderId != API.Csidl.INVALID))
                    _icon = ImageManager.GetImageContainer(this, HasLargeIcon ? API.Shgfi.LARGEICON : API.Shgfi.SMALLICON);
                return _icon;
            }
            set
            {
                if(_icon != value)
                {
                    _icon = value;
                    OnPropertyChanged("Icon");
                }
            }
        }

        public bool HasLargeIcon 
        { 
            get
            {
                return _hasLargeIcon;
            } 
            set
            {
                _hasLargeIcon = value;
                OnPropertyChanged("Icon");
            }
         }

        public bool NonCachedIcon
        {
            get { return IsLibrary || _nonCachedIcon; } 
            set { _nonCachedIcon = value; }
        }



        //3rd block - children and parents
        public ObservableCollection<PowerItem> Items
        {
            get
            {
                if (_items.Count == 0 && !_expanding && AutoExpandIsPending)
                {
                    _expanding = true;
                    PowerItemTree.ScanFolder(this, string.Empty, false);
                }
                return _items;
            }
        }

        public bool AutoExpand
        {
            get { return _autoExpand; }
            set
            {
                if (_autoExpand != value)
                {
                    _autoExpand = value;
                    AutoExpandIsPending = value;
                }
            }
        }

        public PowerItem Parent { get; set; }

        public PowerItem Root
        {
            get
            {
                if (_root == null)
                {
                    _root = this;
                    while (_root.Parent != null)
                        _root = _root.Parent;
                }
                return _root;
            }
        }



        //2nd block - text
        public string ResourceIdString
        {
            get { return _resIdString; } 
            set
            {
                _resIdString = value;
                FriendlyName = null;
            }
        }

        public string FriendlyName
        {
            get
            {
                if(_friendlyName != null)
                    return _friendlyName;

                if (ResourceIdString != null)
                {
                    _friendlyName = Util.ResolveStringResource(ResourceIdString);
                    if (_friendlyName != null)
                        return _friendlyName;
                    ResourceIdString = null; //Operation failed somewhere, resourceId is invalid
                }

                if (SpecialFolderId != API.Csidl.INVALID)
                {
                    if (SpecialFolderId == API.Csidl.POWER8JLITEM)
                    {
                        if (Argument.StartsWith("/n,::"))
                            _friendlyName = Util.GetLongPathOrDisplayName(Argument.Substring(3));
                        if(string.IsNullOrEmpty(_friendlyName))
                            _friendlyName = Argument;
                        if (!string.IsNullOrEmpty(_friendlyName) && _friendlyName.Length > 60)
                        {
                            _friendlyName = _friendlyName.Substring(0, 28) +
                                            "…" +
                                            _friendlyName.Substring(_friendlyName.Length - 28, 28);
                        }
                    }
                    else
                    {
                        _friendlyName = Util.ResolveSpecialFolderName(SpecialFolderId);
                    }
                    if (_friendlyName != null)
                        return _friendlyName;
                }

                if (Parent == null) //main menu
                {
                    _friendlyName = Resources.Str_AllPrograms;
                    return _friendlyName;
                }
                
                if (Parent == MfuList.MfuSearchRoot //so it must have ARGUMENT...
                    && (IsLink || Argument.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase)))
                {
                    var container = PowerItemTree.SearchStartMenuItemSyncFast(Argument);
                    if (container != null)
                        _friendlyName = container.FriendlyName;

                    if (string.IsNullOrEmpty(_friendlyName)/*(still)*/ && !IsLink)
                    {
                        var ver = FileVersionInfo.GetVersionInfo(PowerItemTree.GetResolvedArgument(this));
                        if (!string.IsNullOrWhiteSpace(ver.FileDescription))
                            _friendlyName = ver.FileDescription;
                        else if (!string.IsNullOrWhiteSpace(ver.ProductName))
                            _friendlyName = ver.ProductName;
                    }
                }

                if (string.IsNullOrEmpty(_friendlyName)/*(still)*/)
                {//use fallback...
                    var path = IsLink || IsLibrary ? Path.GetFileNameWithoutExtension(Argument) : Path.GetFileName(Argument);
                    if(string.IsNullOrEmpty(path))
                    {
                        if ((Argument.Length > 1 && Argument[Argument.Length - 1] == ':')
                            ||
                            (Argument.Length > 2 && Argument.EndsWith(":\\"))) //drive name
                            _friendlyName = String.Format("{0} - {1}", Argument, DriveManager.GetDriveLabel(Argument));
                        else
                            _friendlyName = Argument;
                    }
                    else
                    {
                        _friendlyName = path;
                    }
                }
                
                return _friendlyName;
            }
            set
            {
                _friendlyName = value;
                OnPropertyChanged("FriendlyName");
            }
        }

        public Double MinWidth
        {
            get { return Parent == null ? 300 : 0; }
        }



        //0th block - Pin
        public bool IsPinned { get; set; }



        //4th block - Arguments
        public ObservableCollection<PowerItem> JumpList
        {
            get
            {
                if(_cmdLines == null)
                {
                    _cmdLines = new ObservableCollection<PowerItem>();
                    MfuList.GetRecentListFor(this);
                }
                return _cmdLines;
            }
        }



        //Not a visual block - binding and resolving helpers
        public bool IsFile
        {
            get { return Argument != null && !IsFolder; }
        }

        public bool IsLink
        {
            get { return IsFile && 
                  !string.IsNullOrEmpty(Argument) && 
                  Argument.EndsWith(".lnk", StringComparison.InvariantCultureIgnoreCase); }
        }

        public string ResolvedLink
        {
            get
            {
                if(IsLink && _resolvedLink == null)
                    _resolvedLink = Util.ResolveLink(PowerItemTree.ResolveItem(this).FileName).ToLowerInvariant();
                return _resolvedLink;
            }
        }

        public bool IsSpecialObject
        {
            get { return IsLibrary 
                        || (!string.IsNullOrEmpty(Argument) && Argument.StartsWith("::{")) 
                        || SpecialFolderId == API.Csidl.POWER8CLASS; }
        }

        public bool IsLibrary
        {
            get { return IsFolder && 
                  !string.IsNullOrEmpty(Argument) && 
                  Argument.EndsWith(".library-ms", StringComparison.InvariantCultureIgnoreCase); }
        }

        public bool IsNotPureControlPanelFlowItem
        {
            get
            {
                if (SpecialFolderId == API.Csidl.POWER8CLASS)
                    return false; //Hide properties and Open Location for special class objects
                if (!IsControlPanelChildItem)
                    return true;
                if(Argument != null)
                {
                    if(Argument.EndsWith(".cpl", StringComparison.InvariantCultureIgnoreCase))
                        return true;
                    var cmd = Util.GetOpenCommandForClass(Argument);
                    if(cmd != null && cmd.Item1 != null && !cmd.Item1.ToLower().Contains("rundll"))
                        return true;
                }
                return false;
            }
        }

        public bool IsControlPanelChildItem
        {
            get { return !IsFolder && Parent != null && Parent.SpecialFolderId == API.Csidl.CONTROLS; }
        }

        public bool IsFolderUnderStartMenu
        {
            get
            {
                return IsFolder &&
                       PowerItemTree.StartMenuRoot.Count > 0 &&
                       Root == PowerItemTree.StartMenuRoot[0];
            }
        }



        public int CompareTo(PowerItem other)
        {
            return String.CompareOrdinal(FriendlyName, other.FriendlyName);
        }

        public override string ToString()
        {
            return FriendlyName;
        }

        public override int GetHashCode()
        {
            return (FriendlyName + IsFolder + Argument).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var p = obj as PowerItem;
            if(p == null)
                return false;
            return p.GetHashCode() == GetHashCode();
        }



        public void Invoke()
        {
            InvokeVerb(null);
        }

        public void InvokeVerb(string verb)
        {
            if (SpecialFolderId != API.Csidl.INVALID)
            {
                if (string.IsNullOrEmpty(Argument))
                {
                    Util.DisplaySpecialFolder(SpecialFolderId);
                    return;
                }
                if (SpecialFolderId == API.Csidl.POWER8CLASS)
                {
                    Util.InstanciateClass(Argument);
                    return;
                }
            }
            var psi = PowerItemTree.ResolveItem(this, IsFolder && verb == API.SEVerbs.RunAsAdmin);
            if (!string.IsNullOrEmpty(verb) && IsFile)
                psi.Verb = verb;
            if (psi.Arguments.StartsWith("\\\\"))
                psi.UseShellExecute = false;
            try
            {
                Process.Start(psi);
            }
            catch (Win32Exception w32E)
            {
                if (w32E.NativeErrorCode == 0x483) //1155, e.g. when doing "runas" on "*.hlp"
                {
                    psi.Verb = null;
                    Process.Start(psi);
                    throw new InvalidProgramException(Resources.Err_StartAsAdminFailed + w32E.Message);
                }
                throw;
            }
        }

        public void Update()
        {
            Icon = null;
            FriendlyName = null;
        }

        public void SortItems()
        {
            foreach (var powerItem in _items)
            {
                powerItem.SortItems();
            }
            var lf = new List<PowerItem>(_items.Where(i => i.IsFolder));
            var li = new List<PowerItem>(_items.Where(i => !i.IsFolder));
            lf.Sort();
            li.Sort();
            _items.Clear();
            lf.ForEach(_items.Add);
            li.ForEach(_items.Add);
        }

        public bool Match(string query)
        {//TODO: rewrite Match()!
            return (FriendlyName != null && FriendlyName.ToLowerInvariant().Contains(query))
                || (Argument != null && Argument.ToLowerInvariant().Contains(query))
                || (IsLink && ResolvedLink.Contains(query));
        }



        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string property)
        {   
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(property));
        }
    }
}
