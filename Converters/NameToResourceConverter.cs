using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Power8.Converters
{
    /// <summary>
    /// There's no easy way to have localization in XAML. MS suggests to use additional proxy class 
    /// with a number of properties. I dislike this way, so let's just use a converter that is able
    /// to find localized content based on element's name (by default) or any other stuff.
    /// One way converter. Data: string (usually name of element). Returns (CR_+ data) resource.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    [TypeConverter(typeof(string))]
    class NameToResourceConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Properties.Resources.ResourceManager.GetString("CR_" + (string) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
