using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Power8.Converters
{
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
