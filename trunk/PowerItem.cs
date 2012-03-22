using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace Power8
{
    public class PowerItem
    {
        public string Executable { get; set; }
        public string Argument { get; set; }
        public BitmapImage Icon { get; set; }
        public PowerItem Parent { get; set; }
        public bool IsFolder { get; set; }

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
            return Path.GetFileNameWithoutExtension(Argument);
        }
    }
}
