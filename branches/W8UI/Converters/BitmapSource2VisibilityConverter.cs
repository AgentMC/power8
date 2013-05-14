using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Power8.Converters
{
    /// <summary>
    /// This class is used to switch visibility of Main Start Button grids On and Off.
    /// It behaves depending on the fact is the value null, and what parameter is passed.
    /// </summary>    
    [ValueConversion(typeof(BitmapSource), typeof(Visibility))]
    [TypeConverter(typeof(BitmapSource))]
    class BitmapSource2VisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts the fact of value existence and parameter into visibility
        /// </summary>
        /// <param name="value">Something or null. Treat null as false.</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">String, "true" or something else, e.g. "false". The "true"
        /// parameter preserves the conversion logic straightforward (needed only for one UI element)
        /// and other ones invert it.</param>
        /// <param name="culture">Not used</param>
        /// <returns>When using normal logic, returns <code>Visibility.Hidden</code> when value is null
        /// and <code>Visibility.Visible</code> otherwise. Inverts tre return pair for the inverted
        /// logic.</returns>
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
