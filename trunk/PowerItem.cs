using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Power8.Properties;

namespace Power8
{
    public class PowerItem : INotifyPropertyChanged
    {
        public string Executable { get; set; }
        public string Argument { get; set; }
        public PowerItem Parent { get; set; }
        public bool IsFolder { get; set; }


        private ImageManager.ImageContainer _icon;
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


        private readonly ObservableCollection<PowerItem> _items = new ObservableCollection<PowerItem>(); 
        public ObservableCollection<PowerItem> Items
        {
            get { return _items; }
        }

        public string FriendlyName
        {
            get { return ToString(); }
        }

        public override string ToString()
        {
            if(string.IsNullOrEmpty(Argument))
                return "All Programs";
            return Path.GetFileNameWithoutExtension(Argument) ?? "";
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


        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string property)
        {   
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(property));
        }
    }
}
