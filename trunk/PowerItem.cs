using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
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
        /// Gets or Sets value indicating if the asynchronous Friendly Name evaluation is possible.
        /// Auto-property, false by default. Set to true ONLY when you are 100% sure this item won't ever
        /// be used in sorting by SortItems().
        /// </summary>
        public bool AllowAsyncFriendlyName { get; set; }
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
        /// When <code>AllowAsyncFriendlyName</code> flag is set, some of these steps
        /// are executed in the background on async threads. Of course this prohibits 
        /// item from being used in <code>SortItems()</code>. Though it doesn't check
        /// the flag itself.
        /// </summary>
        public string FriendlyName
        {
            get
            {
                if(_friendlyName != null) //Return from cache
                    return _friendlyName;

                if (!AllowAsyncFriendlyName) //Run heavy part 'n'sync unless explicitly allowed to fork
                    _friendlyName = TryExtractFriendlyNameAsync();
                if (!string.IsNullOrEmpty(_friendlyName))
                    return _friendlyName;

                if (string.IsNullOrEmpty(Argument)) //FN will be extracted in background, based on SFID
                {
                    _friendlyName = string.Empty;
                }
                else
                { //we can provide at least temporary text
                    var path = IsLink || IsLibrary ? Path.GetFileNameWithoutExtension(Argument) : Path.GetFileName(Argument);
                    if (string.IsNullOrEmpty(path)) //and if this fails...
                    {
                        if ((Argument.Length > 1 && Argument[Argument.Length - 1] == ':')
                            ||
                            (Argument.Length > 2 && Argument.EndsWith(":\\"))) //drive name
                            _friendlyName = String.Format("{0} - {1}", Argument, DriveManager.GetDriveLabel(Argument));
                        else
                            _friendlyName = Argument; //finally we have no f***ng idea what this PowerItem is, just display Arg.
                    }                                 //and hope FN will be asynch'ed
                    else
                    {
                        _friendlyName = path; //file name or filename with extension
                    }
                }

                if(AllowAsyncFriendlyName) //Launch async extraction if allowed
                    Util.Fork(() => 
                    {
                        var f = TryExtractFriendlyNameAsync();
                        if (!string.IsNullOrEmpty(f))
                            Util.Post(() => FriendlyName = f);
                    }, "FN async extractor for " + Argument).Start();

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

        /// <summary>
        /// Returns true when instance is not folder and has some Argument.
        /// So, this property doesn't actually guarantee this instance points 
        /// to some real file, or even has proper syntax in Argument.
        /// </summary>
        public bool IsFile
        {
            get { return Argument != null && !IsFolder; }
        }
        /// <summary>
        /// Returns true if this instance points to some file which ends with ".lnk", case insensitive.
        /// True does not guarantee that this file actually exists on disk.
        /// </summary>
        public bool IsLink
        {
            get { return IsFile && Argument.EndsWith(".lnk", StringComparison.InvariantCultureIgnoreCase); }
        }
        /// <summary>
        /// Returns l-cased cached target of the link for this instance if it is Link;
        /// returns null otherwise. 
        /// </summary>
        public string ResolvedLink
        {
            get
            {
                if(IsLink && _resolvedLink == null)
                    _resolvedLink = Util.ResolveLink(PowerItemTree.ResolveItem(this).FileName).ToLowerInvariant();
                return _resolvedLink;
            }
        }
        /// <summary>
        /// Returns true if this instance is Library or shell namespace("::{guid}...") ptr,
        /// or if SpecialFolderId was explicitly set to Power8Class.
        /// </summary>
        public bool IsSpecialObject
        {
            get { return IsLibrary 
                        || (!string.IsNullOrEmpty(Argument) && Argument.StartsWith("::{")) 
                        || SpecialFolderId == API.Csidl.POWER8CLASS; }
        }
        /// <summary>
        /// Returns true if this instance points to the file with ".library-ms" extension,
        /// AND this instance was explicitly switched to Folder mode.
        /// </summary>
        public bool IsLibrary
        {
            get { return IsFolder && 
                  !string.IsNullOrEmpty(Argument) && 
                  Argument.EndsWith(".library-ms", StringComparison.InvariantCultureIgnoreCase); }
        }
        /// <summary>
        /// Returns false when this instance references the Vista-style guid-based Control panel flow item,
        /// launchable via Rundll or "Control /name" only.
        /// Also returns false for Power8Class-items, since they have same virtualization degree as
        /// mentioned flow items, in particular, they're totally virtual from the FS perspective.
        /// </summary>
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
        /// <summary>
        /// Returns true when this instance points to the element of Control panel. 
        /// To achieve this, CONTROLS special folder id is used, also the item must not be the folder
        /// and have parent.
        /// </summary>
        public bool IsControlPanelChildItem
        {
            get { return !IsFolder && Parent != null && Parent.SpecialFolderId == API.Csidl.CONTROLS; }
        }
        /// <summary>
        /// Returns true if this instance references the folder under User or Common Start menu.
        /// </summary>
        public bool IsFolderUnderStartMenu
        {
            get
            {
                return IsFolder &&
                       PowerItemTree.StartMenuRoot.Count > 0 &&
                       Root == PowerItemTree.StartMenuRoot[0];
            }
        }
        /// <summary>
        /// Returns true if this instance has Mfu as Root
        /// </summary>
        public bool IsMfuChild
        {
            get { return Parent == MfuList.MfuSearchRoot; }
        }

        #endregion

        #region Implementations and overrides
        
        /// <summary>
        /// Compares this PowerItem to other one. Since this function is widely used
        /// in different sortings, comparision is done based on eveluated FriendlyName
        /// Use Equals() to determine if items are REALLY similar
        /// </summary>
        /// <param name="other">The PowerItem which this one is compared to.</param>
        /// <returns>0 if items are equal, 1 if this one is bigger, and -1 otherwise</returns>
        public int CompareTo(PowerItem other)
        {
            return String.CompareOrdinal(FriendlyName, other.FriendlyName);
        }
        /// <summary>
        /// Used in some places where incorrect bindings are used :)
        /// returns Friendly name
        /// </summary>
        public override string ToString()
        {
            return FriendlyName;
        }
        /// <summary>
        /// Gets item hash code based on Friendly name, Argument and IsFolder
        /// </summary>
        public override int GetHashCode()
        {
            return (FriendlyName + IsFolder + Argument).GetHashCode();
        }
        /// <summary> Compares two PowerItems based on hash codes. </summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns>True if obj is PowerItem and HashCodes equal</returns>
        public override bool Equals(object obj)
        {
            return obj is PowerItem && obj.GetHashCode() == GetHashCode();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Fires PropertyChanged event passing property name as argument
        /// </summary>
        /// <param name="property">Name of the Property that had changed</param>
        public void OnPropertyChanged(string property)
        {   
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(property));
        }

        #endregion

        #region Methods
        //Run
        /// <summary>
        /// Runs the object this PowerItem is bound to. Depending on what are the Argument
        /// and other properties of this instance, an application may be run, folder (or
        /// special system window) may be opened, Control panel element may be launched,
        /// Power8 window may be displayed, and so on... Calls InvokeVerb(null).
        /// </summary>
        public void Invoke()
        {
            InvokeVerb(null);
        }
        /// <summary>
        /// When not null value is passed as argument, tries to perform the command
        /// related to the argument passed via the system Verb engine. Works for
        /// non-folders.<br/>
        /// If RunAsAdmin verb is passed and this PowerItem instance is Folder, flag 
        /// is converted to boolean true instructing ResolveItem() that, if available, 
        /// Common start menu folder should be used instead of user one. This is not 
        /// used for non-folder items and won't work for folders not under Start menu,
        /// even there's corresponding CSIDLs.<br/>
        /// If null value is passed, then it simply runs the PowerItem. Consult Invoke()
        /// xml-doc on how this can be performed.
        /// </summary>
        /// <param name="verb">One from <code>API.SEVerbs</code> constants, or null/empty 
        /// string, which means "no command"</param>
        public void InvokeVerb(string verb)
        {
            //Shell namespaces and Power8 classes
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
            //All the other stuff: FS-resolvable
            var psi = PowerItemTree.ResolveItem(this, IsFolder && verb == API.SEVerbs.RunAsAdmin);
            if (!string.IsNullOrEmpty(verb) && IsFile)  //pass verb
                psi.Verb = verb;
            if (psi.Arguments.StartsWith("\\\\"))       //network items may be only directly launched
                psi.UseShellExecute = false;
            try
            {
                Process.Start(psi);
            }
            catch (Win32Exception w32E) //Any exception will be handled really out of here, but we
            {                           //shall report proper error
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
        /// <summary>
        /// Clears the Icon andFriendly name. As this class implements INotifyPropChnaged,
        /// this results in total 5 events: Icon null, Icon null (async extractor started),
        ///  FriendlyName null, FriendlyName available, Icon available
        /// </summary>
        public void Update()
        {
            Icon = null;
            FriendlyName = null;
        }
        //Sort
        /// <summary>
        /// Recousively calls SortDescription on all children, then
        /// sorts owh children list putting Folders first, then 
        /// non-folders. Consult CompareTo() for details.
        /// Though method doesn't check for <code>AllowAsyncFriendlyName</code>,
        /// when that property is true and this method is called, a high 
        /// possibility of race conditions occur.
        /// </summary>
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
        /// <summary>
        /// Checks if this instance matches with the provided search query.
        /// Tries to match item by Camel content and by raw strings.
        /// Both Camel and Raw strings are cached lowercase, so query must also
        /// be lowercase to find something.
        /// See xml-doc for MatchCamelCheck() and MatchRawCheck() for details.
        /// </summary>
        /// <param name="query">Search query</param>
        /// <returns>Boolean indicating if this item matches the query or not.</returns>
        public bool Match(string query)
        {
            return MatchCamelCheck(query) || MatchRawCheck(query);
        }
        /// <summary>
        /// Tries to match search query to this item's special Camel string.
        /// Camel string is built from sources available by FillCamels(),
        /// see xml-doc of it for the algorythm. Works for Links or EXE references
        /// (Friendly name, Argument, Resolved link are used), and for 
        /// Control panel items (only FriendlyName). See Match() xml-doc for 
        /// details on parameter and return value.
        /// </summary>
        private bool MatchCamelCheck(string query)
        {
            if (_camels == null)
            {
                var b = new StringBuilder();
                if(IsLink || (Argument != null && Argument.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase)))
                {
                    foreach (var s in new[] {FriendlyName, Argument, ResolvedLink}.Where(s => !string.IsNullOrEmpty(s)))
                        FillCamels(s, b);
                }
                else if (IsControlPanelChildItem && !string.IsNullOrEmpty(FriendlyName))
                {
                    FillCamels(FriendlyName, b);
                }
                _camels = b.ToString().ToLowerInvariant();
            }
            return _camels.Contains(query);
        }
        /// <summary>
        /// Tries to match search query to this item's special string containing of 
        /// concatenated Friendly name, Argument and Resolved link. See Match() xml-doc for 
        /// details on parameter and return value.
        /// </summary>
        private bool MatchRawCheck(string query)
        {
            if (_raws == null)
                _raws = (string.Empty + Argument + FriendlyName + ResolvedLink).ToLowerInvariant();
            return _raws.Contains(query);
        }
        /// <summary>
        /// Appends the _camels builder with new data from string passed according to the algorythm:
        /// for each char, it is added if a separator or punctuation char was before, and it 
        /// is not such char itself, or else if it is number or UPPERCASE.
        /// </summary>
        /// <param name="s">String for analysis to be added to the camels string</param>
        /// <param name="sBuilder">The stringBuilder used to build Cames string </param>
        private static void FillCamels(string s, StringBuilder sBuilder)
        {
            bool lastDelim = false;
            foreach (var cch in s)
            {
                if ((lastDelim && !(char.IsSeparator(cch) || char.IsPunctuation(cch)))
                     || char.IsUpper(cch) 
                     || char.IsNumber(cch))
                {
                    sBuilder.Append(cch);
                    lastDelim = false;
                }
                else if (char.IsSeparator(cch) || char.IsPunctuation(cch))
                {
                    lastDelim = true;
                }
            }
        }
        /// <summary>
        /// This is the part of FriendlyName getter. Executes heavy part of FriendlyName evaluation either
        /// synchronously or in async way, depending on how it's called.
        /// </summary>
        /// <returns>The PI's partially evaluated FriendlyName, which can be also empty string or even null.
        /// Assign the value returned to the FriendlyName. When use this method asynchronously, always Post()
        /// or Send() the assignment.</returns>
        private string TryExtractFriendlyNameAsync()
        {
            string fName = null;
            if (ResourceIdString != null) //Return based on resId
            {
                fName = Util.ResolveStringResource(ResourceIdString);
                if (!string.IsNullOrEmpty(fName))
                    return fName;
                ResourceIdString = null; //Operation failed somewhere, resourceId is invalid
            }

            if (SpecialFolderId != API.Csidl.INVALID) //Resolve for a special folder, if any
            {
                if (SpecialFolderId == API.Csidl.POWER8JLITEM) //For jump list item <IShellLink>
                {
                    if (Argument.StartsWith("/n,::")) //in part., for explorer shell NSs
                        fName = Util.ResolveLongPathOrDisplayName(Argument.Substring(3));
                    if (string.IsNullOrEmpty(fName)) //Otherwise, set display name to link target
                        fName = Argument;
                    if (!string.IsNullOrEmpty(fName) && fName.Length > 60)
                    { //finally, if it is too long, cut it from the middle
                        fName = fName.Substring(0, 28) +
                                        "…" +
                                        fName.Substring(fName.Length - 28, 28);
                    }
                }
                else //for all the other special folders
                {
                    fName = Util.ResolveSpecialFolderName(SpecialFolderId);
                }
                if (!string.IsNullOrEmpty(fName))
                    return fName;
            }

            //If no SpecialFolderId or resolving failed
            if (Parent == null) //main menu
            {//this basically should never happen since the moment when desktop.ini parsing is implemented
                return  Resources.Str_AllPrograms;
            }

            //For Recent list...
            if (IsMfuChild //so it must have ARGUMENT...
                && (IsLink || Argument.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase)))
            {//...that is either link or exe (UserAssist doesn't return others but who knows)
                var container = PowerItemTree.SearchStartMenuItemSyncFast(Argument); //yeah, "fast"... :(
                if (container != null)
                    fName = container.FriendlyName;

                if (string.IsNullOrEmpty(fName)/*(still)*/ && !IsLink)
                {//get file version info table and extract data from there. Costly but provides valuable results.
                    var ver = FileVersionInfo.GetVersionInfo(PowerItemTree.GetResolvedArgument(this));
                    if (!string.IsNullOrWhiteSpace(ver.FileDescription)) //try description, if not fallback to...
                        fName = ver.FileDescription;
                    else if (!string.IsNullOrWhiteSpace(ver.ProductName)) //...Product. This is specifically for...
                        fName = ver.ProductName;                  //...NFS.Run and the kind of.
                }
            }
            return fName;
        }
        #endregion
    }
}
