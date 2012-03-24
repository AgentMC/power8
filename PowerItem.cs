using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Power8
{
    public class PowerItem : INotifyPropertyChanged
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
            Process.Start(PowerItemTree.ResolveItem(this));
        }

        public void Update()
        {
            Icon = null;
        }

        public Double MinWidth
        {
            get { return Parent == null ? 300 : 0; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string property)
        {   
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(property));
        }
    }

    [TypeConverter]
    public class ImageConverter : IValueConverter 
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new Image { Source = value as ImageSource, Width=16, Height = 16};
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Image) value).Source;
        }
    }


}
