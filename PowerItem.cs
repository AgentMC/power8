﻿using System;
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
                {
                    _friendlyName = Resources.AllPrograms;
                }
                else
                {
                    var path = IsLink || IsLibrary ? Path.GetFileNameWithoutExtension(Argument) : Path.GetFileName(Argument);
                    _friendlyName = string.IsNullOrEmpty(path) ? Argument : path;
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
                return Parent == null 
                    || Parent.SpecialFolderId != API.Csidl.CONTROLS 
                    || (Argument != null 
                        && Argument.EndsWith(".cpl", StringComparison.InvariantCultureIgnoreCase));
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
                Util.DisplaySpecialFolder(SpecialFolderId);
                return;
            }
            var psi = PowerItemTree.ResolveItem(this, IsFolder && verb == API.SEIVerbs.SEV_RunAsAdmin);
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