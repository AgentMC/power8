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
    /// <summary>
    /// The main data providing class, model unit and universal viewmodel simultaneously
    /// </summary>
    public class PowerItem : INotifyPropertyChanged, IComparable<PowerItem>
    {
        private readonly ObservableCollection<PowerItem> _items = new ObservableCollection<PowerItem>();//children

        private ImageManager.ImageContainer _icon;
        private ObservableCollection<PowerItem> _cmdLines; //Jump list
        private string _friendlyName, _resIdString, _resolvedLink, _camels, _raws;
        private bool _expanding, _hasLargeIcon, _autoExpand, _nonCachedIcon, _pin;
        private PowerItem _root; //root is always the same, this is just cache


        /// <summary>
        /// Default Constructor. Puts CSIDL.INVALID to SpecialFolderId
        /// </summary>
        public PowerItem()
        {
            SpecialFolderId = API.Csidl.INVALID;
        }
        /// <summary> Extended ctor </summary>
        /// <param name="items">Reference to collection of PowerItems</param>
        public PowerItem (ObservableCollection<PowerItem> items ):this ()
        {
            _items = items;
        }



        #region Distinctivity Properties

        #region Core

        /// <summary>
        /// The string that uniquely identifies this instance. If this is null or empty,
        /// only SpecialFolderId may help. This may contain either file path, or shell-style
        /// namespace id or path, or CLSID, etc. Auto-property.
        /// </summary>
        public string Argument { get; set; }
        /// <summary>
        /// Determines should this instance be treated like folder or not. Auto-property.
        /// </summary>
        public bool IsFolder { get; set; }
        /// <summary>
        /// If true, means that Items will be asynchronously auto-populated when 
        /// expanded and though should be treated as null and under no conditions 
        /// shall be enumerated. Auto-property.
        /// </summary>
        public bool AutoExpandIsPending { get; set; }
        /// <summary>
        /// In rare cases there's no shell namespace for certain virtual folder,
        /// and Argument won't help us this way. However we can use corresponding
        /// CSIDL to get PIDL of VF, and obtain all information required via it.
        /// Auto-property.
        /// </summary>
        public API.Csidl SpecialFolderId { get; set; }

        #endregion

        #region Icon

        /// <summary>
        /// The icon of the PowerItem. If there's no icon, it is extracted asynchronously,
        /// depending on HasLargeIcon value. When set, propagated to binding target.
        /// Note that you may receive multiple PropertyChanged events with this, since 
        /// the extraction is async and to return something, extractor invoker puts null
        /// as placeholder for future extracted icon. 
        /// </summary>
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
        /// <summary>
        /// Gets or sets value indicating if large (24x24|32x32) icon is available,
        /// or only small one (16x16) is applicable. Small by default, when changed,
        /// invokes PropertyChanged event for Icon property, but does not null icon
        /// (likely small) extracted previously. 
        /// </summary>
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
        /// <summary>
        /// If true, means that icon should not be returned from the cache of
        /// ImageManager, but rather extracted independently via shell.
        /// Always true when IsLibrary == true.
        /// </summary>
        public bool NonCachedIcon
        {
            get { return IsLibrary || _nonCachedIcon; } 
            set { _nonCachedIcon = value; }
        }

        #endregion

        #region Text

        /// <summary>
        /// Gets or sets the resource ID string for current PowerItem.
        /// Consult Util.ResolveResourceCommon() and dependent methods
        /// for more information. When set, clears Friendly name 
        /// (but doesn't propagate change to its binding target).
        /// </summary>
        public string ResourceIdString
        {
            get { return _resIdString; } 
            set
            {
                _resIdString = value;
                FriendlyName = null;
            }
        }
        /// <summary>
        /// Gets display name of this instance.
        /// First, tries to get text from resource ID, if any.
        /// Then, if this fails or resId is unavailable, 
        /// tries special treatment for special folder and MFU items.
        /// Then, tries to parse Argument into a Path and get related parts of it.
        /// Finally if this is impossible, returns Argument.
        /// Propagates changed value on Set to binding Target.
        /// </summary>
        public string FriendlyName
        {
            get
            {
                if(_friendlyName != null) //Return from cache
                    return _friendlyName;

                if (ResourceIdString != null) //Return based on resId
                {
                    _friendlyName = Util.ResolveStringResource(ResourceIdString);
                    if (_friendlyName != null)
                        return _friendlyName;
                    ResourceIdString = null; //Operation failed somewhere, resourceId is invalid
                }

                if (SpecialFolderId != API.Csidl.INVALID) //Resolve for a special folder, if any
                {
                    if (SpecialFolderId == API.Csidl.POWER8JLITEM) //For jump list item <IShellLink>
                    {
                        if (Argument.StartsWith("/n,::")) //in part., for explorer shell NSs
                            _friendlyName = Util.ResolveLongPathOrDisplayName(Argument.Substring(3));
                        if(string.IsNullOrEmpty(_friendlyName)) //Otherwise, set display name to link target
                            _friendlyName = Argument;
                        if (!string.IsNullOrEmpty(_friendlyName) && _friendlyName.Length > 60)
                        { //finally, if it is too long, cut it from the middle
                            _friendlyName = _friendlyName.Substring(0, 28) +
                                            "…" +
                                            _friendlyName.Substring(_friendlyName.Length - 28, 28);
                        }
                    }
                    else //for all the other special folders
                    {
                        _friendlyName = Util.ResolveSpecialFolderName(SpecialFolderId);
                    }
                    if (_friendlyName != null)
                        return _friendlyName;
                }

                //If no SpecialFolderId or resolving failed
                if (Parent == null) //main menu
                {//this basically should never happen since the moment when desktop.ini parsing is implemented
                    _friendlyName = Resources.Str_AllPrograms;
                    return _friendlyName;
                }
                
                //For Recent list...
                if (IsMfuChild //so it must have ARGUMENT...
                    && (IsLink || Argument.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase)))
                {//...that is either link or exe (UserAssist doesn't return others but who knows)
                    var container = PowerItemTree.SearchStartMenuItemSyncFast(Argument); //yeah, "fast"... :(
                    if (container != null)
                        _friendlyName = container.FriendlyName;

                    if (string.IsNullOrEmpty(_friendlyName)/*(still)*/ && !IsLink)
                    {//get file version info table and extract data from there. Costly but provides valuable results.
                        var ver = FileVersionInfo.GetVersionInfo(PowerItemTree.GetResolvedArgument(this));
                        if (!string.IsNullOrWhiteSpace(ver.FileDescription)) //try description, if not fallback to...
                            _friendlyName = ver.FileDescription;
                        else if (!string.IsNullOrWhiteSpace(ver.ProductName)) //...Product. This is specifically for...
                            _friendlyName = ver.ProductName;                  //...NFS.Run and the kind of.
                    }
                }

                if (string.IsNullOrEmpty(_friendlyName)/*(yeah, still; or not special folder, not MFU item)*/)
                {//use fallback... Name+Extension for file, just name for link or library
                    var path = IsLink || IsLibrary ? Path.GetFileNameWithoutExtension(Argument) : Path.GetFileName(Argument);
                    if(string.IsNullOrEmpty(path)) //and if this fails...
                    {
                        if ((Argument.Length > 1 && Argument[Argument.Length - 1] == ':')
                            ||
                            (Argument.Length > 2 && Argument.EndsWith(":\\"))) //drive name
                            _friendlyName = String.Format("{0} - {1}", Argument, DriveManager.GetDriveLabel(Argument));
                        else
                            _friendlyName = Argument; //finally we have no f***ng idea what this PowerItem is, just display Arg.
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
        /// <summary>
        /// Returns suggested minimum width for item. 300 for roots, 0 for others.
        /// </summary>
        public Double MinWidth
        {
            get { return Parent == null ? 300 : 0; }
        }

        #endregion

        #region Related data

        /// <summary>
        /// Gets or sets value describing if the instance is pinned by user to Recent list.
        /// At the moment setter doesn't invoke MfuList's methods, it is done by UI event 
        /// handlers, since the filtering which PIs can be pinned and which can be not 
        /// is pure View's business at the moment. May be changed in future.
        /// However, changed  value is propagated to binding target when set.
        /// </summary>
        public bool IsPinned
        {
            get { return _pin; }
            set
            {
                if (value == _pin)
                    return;
                _pin = value;
                OnPropertyChanged("IsPinned");
            }
        }
        /// <summary>
        /// Gets the JumpList for current item. It's done by joining system 
        /// RECENT and FREQUENT data with P8's own JL implementation.
        /// On 1st call, returns empty Collection, which can be populated 
        /// in the nearest time after this, or my not. 
        /// The population is done on background thread, but straightforward Add()
        /// is called on main dispatcher, so this can be binded.
        /// </summary>
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

        #endregion

        #region Children-Parents

        /// <summary>
        /// Children of this PowerItem. Can be auto-populated, so don't query this unless
        /// AutoExpandingIsPending is false. Populated on background thread, but items are added
        /// on Main dispatcher, so can be bound.
        /// </summary>
        public ObservableCollection<PowerItem> Items
        {
            get
            {
                if (_items.Count == 0 && !_expanding && AutoExpandIsPending)
                {
                    _expanding = true; //need no lock here, since the code works with this via
                    //AutoExpandIsPending, and theonly other accessor is Binding, which is always 
                    //invoked from one thread
                    PowerItemTree.ScanFolder(this, string.Empty, false);
                }
                return _items;
            }
        }
        /// <summary>
        /// Sets the value indicating the auto-expanding status for this item at the construction 
        /// moment. When set, affects the AutoExpandIsPending propagating value there. Thus,
        /// may also afect Items behavior.
        /// </summary>
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
        /// <summary>
        /// Auto-property. Gets or sets the parent PowerItem.
        /// Note that there's no auto-relation between parent and child,
        /// i.e when Parent is set, this is not added to Parent's Items,
        /// and on the PowerItem level, when Item is added, it's Parent
        /// is not updated (however, this is done automatically if you 
        /// use PowerItemTree.AddSubItem()).
        /// </summary>
        public PowerItem Parent { get; set; }
        /// <summary>
        /// Parent of parent of parent.... while it is not null.
        /// Returns root PowerItem in the tree. Collection of Roots 
        /// available via PowerItemTree.
        /// </summary>
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

        #endregion

        #endregion

        #region Helper properties

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

        public bool IsMfuChild
        {
            get { return Parent == MfuList.MfuSearchRoot; }
        }

        #endregion

        #region Implementations and overrides

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

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string property)
        {   
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(property));
        }

        #endregion

        #region Methods
        //run
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
        //Invalidate
        public void Update()
        {
            Icon = null;
            FriendlyName = null;
        }
        //Sort
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
        //Search
        public bool Match(string query)
        {
            return MatchCamelCheck(query) || MatchRawCheck(query);
        }
        private bool MatchCamelCheck(string query)
        {
            if (_camels == null)
            {
                _camels = string.Empty;
                if(IsLink || (Argument != null && Argument.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase)))
                {
                    foreach (var s in new[] {FriendlyName, Argument, ResolvedLink})
                    {
                        if (!string.IsNullOrEmpty(s))
                        {
                            bool lastDelim = false;
                            foreach (var cch in s)
                            {
                                if (lastDelim || char.IsUpper(cch) || char.IsNumber(cch))
                                {
                                    _camels += cch;
                                    lastDelim = false;
                                }
                                else if (char.IsSeparator(cch) || char.IsPunctuation(cch))
                                {
                                    lastDelim = true;
                                }
                            }
                        }
                    }
                    _camels = _camels.ToLowerInvariant();
                }
            }
            return _camels.Contains(query);
        }
        private bool MatchRawCheck(string query)
        {
            if (_raws == null)
                _raws = (string.Empty + Argument + FriendlyName + ResolvedLink).ToLowerInvariant();
            return _raws.Contains(query);
        }

        #endregion
    }
}
