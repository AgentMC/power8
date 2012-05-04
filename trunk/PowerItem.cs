using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Power8.Properties;
using System.Linq;

namespace Power8
{
    public class PowerItem : INotifyPropertyChanged, IComparable<PowerItem>
    {
        private ImageManager.ImageContainer _icon;
        private readonly ObservableCollection<PowerItem> _items = new ObservableCollection<PowerItem>();
        private string _friendlyName, _resIdString;
        private bool _expanding, _hasLargeIcon;

        public string Argument { get; set; }
        public PowerItem Parent { get; set; }
        public bool IsFolder { get; set; }
        public bool AutoExpand { get; set; }
        public API.Csidl SpecialFolderId { get; set; }


        public PowerItem()
        {
            SpecialFolderId = API.Csidl.INVALID;
        }
    

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
                _icon = value;
                OnPropertyChanged("Icon");
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

        private bool _nonCachedIcon;
        public bool NonCachedIcon
        {
            get { return IsLibrary || _nonCachedIcon; } 
            set { _nonCachedIcon = value; }
        }




        public ObservableCollection<PowerItem> Items
        {
            get
            {
                if (_items.Count == 0 && AutoExpand && !_expanding)
                {
                    _expanding = true;
                    PowerItemTree.ScanFolder(this, string.Empty, false);
                }
                return _items;
            }
        }

        public bool IsAutoExpandPending
        {
            get { return AutoExpand && !_expanding; }
        }



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
                    _friendlyName = Util.ResolveSpecialFolderName(SpecialFolderId);
                    if (_friendlyName != null)
                        return _friendlyName;
                }
                if (string.IsNullOrEmpty(Argument))
                    return Resources.AllPrograms;
                var path = IsLink || IsLibrary ? Path.GetFileNameWithoutExtension(Argument) : Path.GetFileName(Argument);
                return string.IsNullOrEmpty(path) ? Argument : path;
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

        public bool IsSpecialObject
        {
            get { return IsLibrary || (!string.IsNullOrEmpty(Argument) && 
                                       Argument.StartsWith("::{")); }
        }

        public bool IsLibrary
        {
            get { return IsFolder && 
                  !string.IsNullOrEmpty(Argument) && 
                  Argument.EndsWith(".library-ms", StringComparison.InvariantCultureIgnoreCase); }
        }

        public bool IsNotControlPanelFlowItem
        {
            get
            {
                return Parent == null || Parent.SpecialFolderId != API.Csidl.CONTROLS ||
                       Argument.EndsWith(".cpl", StringComparison.InvariantCultureIgnoreCase);
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

        public void Invoke()
        {
            InvokeVerb(null);
        }

        public void InvokeVerb(string verb)
        {
            if (string.IsNullOrEmpty(Argument) && SpecialFolderId != API.Csidl.INVALID)
            {
                //API.IShellFolder isf, isf2;
                //var res = API.SHGetDesktopFolder(out isf);
                //var pidl = IntPtr.Zero;
                //res = API.SHGetSpecialFolderLocation(IntPtr.Zero, SpecialFolderId, ref pidl);
                //var iidSf = new Guid(API.IID_IShellFolder);
                //IntPtr isf2Ptr, isvPtr, hWnd;
                //res = isf.BindToObject(pidl, IntPtr.Zero, ref iidSf, out isf2Ptr);
                //isf2 = (API.IShellFolder) Marshal.GetObjectForIUnknown(isf2Ptr);
                //var iidSv = new Guid(API.IID_IShellView);
                //res = isf2.CreateViewObject(IntPtr.Zero, ref iidSv, out isvPtr);
                //var isv = (API.IShellView) Marshal.GetObjectForIUnknown(isvPtr);

                //var prov = (API.IServiceProvider) isv;
                //API.IShellBrowser browser;
                //var sidGuid = new Guid(API.SID_STopLevelBrowser);
                //var browserGuid = new Guid(API.IID_IShellBrowser);
                //res = prov.QueryService(ref browserGuid, ref browserGuid, out browser);
                //var settings = new API.FOLDERSETTINGS
                //                   {vFlags = API.FOLDERFLAGS.FWF_NONE, viewMode = API.FOLDERVIEWMODE.FVM_AUTO};
                //var r = new API.RECT();
                //res = isv.CreateViewWindow(isv, ref settings, browser, ref r, out hWnd);
                //if (hWnd == IntPtr.Zero)
                //    res = browser.GetWindow(out hWnd);
                //API.ShowWindow(hWnd, API.SWCommands.SHOW);

                //Fuck it...
                return;
            }
            var psi = PowerItemTree.ResolveItem(this, IsFolder && verb == API.SEIVerbs.SEV_RunAsAdmin);
            if (!string.IsNullOrEmpty(verb) && IsFile)
                psi.Verb = verb;
            try
            {
                Process.Start(psi);
            }
            catch (Win32Exception w32E)
            {
                if (w32E.ErrorCode == -2147467259)
                {
                    psi.Verb = null;
                    Process.Start(psi);
                    throw new InvalidProgramException(Resources.StartAsAdminFailed);
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



        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string property)
        {   
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(property));
        }
    }
}
