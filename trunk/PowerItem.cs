using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Power8
{
    public class PowerItem
    {
        public string Executable { get; set; }
        public string Argument { get; set; }
        public PowerItem Parent { get; set; }
        public bool IsFolder { get; set; }


        private ImageSource _icon;
        public ImageSource Icon
        {
            get
            {
                if (_icon == null && Argument != null)
                {
                    var img = new BitmapImage();
                    var stream = new MemoryStream();
                    var psi = PowerItemTree.ResolveItem(this);
                    API.GetIconForFile(IsFolder ? psi.Arguments : psi.FileName, API.Shgfi.SHGFI_SMALLICON).ToBitmap().Save(stream, ImageFormat.Png);
                    img.BeginInit();
                    img.StreamSource = stream;
                    img.CreateOptions = BitmapCreateOptions.None;
                    img.CacheOption = BitmapCacheOption.Default;
                    img.EndInit();
                    _icon = img;
                }
                return _icon;
            }
            set { _icon = value; }
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
            return string.IsNullOrEmpty(Argument) ? "All Programs" : Path.GetFileNameWithoutExtension(Argument);
        }

        public void Invoke()
        {
            Process.Start(PowerItemTree.ResolveItem(this));
        }
    }
}
