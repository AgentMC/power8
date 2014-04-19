using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Power8.Converters
{
    /// <summary>
    /// This converter returns !String.IsNullOrWhiteSpace(of value passed)
    /// Used to dynamically enabled/disable UI elements in case they're logically tied to 
    /// some textual field
    /// </summary>
    [ValueConversion(typeof(String), typeof(Boolean))]
    [TypeConverter(typeof(String))]
    class TextToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !String.IsNullOrWhiteSpace((string)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
