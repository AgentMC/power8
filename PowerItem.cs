using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Power8
{
    public class PowerItem : INotifyPropertyChanged
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
                if (_icon == null && Argument != null)
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
                if (string.IsNullOrEmpty(Argument))
                    return "All Programs";
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
            var psi = PowerItemTree.ResolveItem(this, IsFolder && verb == API.SEIVerbs.SEV_RunAsAdmin);
            if (!string.IsNullOrEmpty(verb) && IsFile)
                psi.Verb = verb;
            try
            {
                Process.Start(psi);
            }
            catch (Exception)
            {
                psi.Verb = null;
                Process.Start(psi);
                throw new InvalidProgramException("Unable to start specified object as Administrator. Process launched with regular user rights.");
            }
        }

        public void Update()
        {
            Icon = null;
            FriendlyName = null;
        }



        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string property)
        {   
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(property));
        }
    }
}
