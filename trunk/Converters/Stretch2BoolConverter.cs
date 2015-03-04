using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Power8.Converters
{
    /// <summary>
    /// This class is used to switch UseLayoutRounding on Image of MainButton 
    /// to True when "No scale" chosen by user in app settings. This makes picture sharper
    /// but makes no sense when applied to scaled image.
    /// </summary>    
    [ValueConversion(typeof(Stretch), typeof(bool))]
    [TypeConverter(typeof(Stretch))]
    class Stretch2BoolConverter : IValueConverter
    {
        /// <summary>
        /// Returns true when Stretch.None is passed.
        /// </summary>
        /// <param name="value">System.Windows.Media.Stretch of image chosen.</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Stretch) value) == Stretch.None;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
