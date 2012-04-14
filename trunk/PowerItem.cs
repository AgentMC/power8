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
        private string _friendlyName;
        private bool _expanding;

        public string Argument { get; set; }
        public PowerItem Parent { get; set; }
        public bool IsFolder { get; set; }
        public string ResourceIdString { get; set; }
        public bool AutoExpand { get; set; }
        public bool NonCachedIcon { get; set; }


        public ImageManager.ImageContainer Icon
        {
            get
            {
                if (_icon == null && Argument != null)
                    _icon = ImageManager.GetImageContainer(this, API.Shgfi.SHGFI_SMALLICON);
                return _icon;
            }
            set
            {
                _icon = value;
                OnPropertyChanged("Icon");
            }
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

        public string FriendlyName
        {
            get
            {
                if(_friendlyName != null)
                    return _friendlyName;
                if (ResourceIdString != null)
                {
                    _friendlyName = Util.ResolveResource(ResourceIdString);
                    if (_friendlyName != null)
                        return _friendlyName;
                    ResourceIdString = null; //Operation failed somewhere, resourceId is invalid
                }
                if (string.IsNullOrEmpty(Argument))
                    return "All Programs";
                var path = IsLink ? Path.GetFileNameWithoutExtension(Argument) : Path.GetFileName(Argument);
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
            get { return IsFile && Argument.EndsWith(".lnk"); }
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
