using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Power8.Converters
{
    [ValueConversion(typeof(bool), typeof(int))]
    [TypeConverter(typeof(bool))]
    class BoolToGridRowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (bool) value; //true means window is above middle of the screen
            var p = bool.Parse(((string)parameter) ?? "false"); //true means start menu
            return v ^ p ? 2 : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
