using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Power8.Converters
{
    /// <summary>
    ///
    /// </summary>
    [ValueConversion(typeof(BitmapSource), typeof(Visibility))]
    [TypeConverter(typeof(BitmapSource))]
    class BitmapSource2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (((string)parameter) == "true")
                return value == null ? Visibility.Hidden : Visibility.Visible;
            return value == null ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
