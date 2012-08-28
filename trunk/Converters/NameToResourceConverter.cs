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
        /// <summary>
        /// Converts the control name given into a localized control text.
        /// </summary>
        /// <param name="value">String, the name of control. "CR_" is added to it and is searched in resources.</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>String to be put into control Content or Text or similar property</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Properties.Resources.ResourceManager.GetString("CR_" + (string) value);
        }

        /// <summary> Shortcut to be used from the code </summary>
        /// <param name="controlName">String, the name of control. "CR_" is added to it and is searched in resources.</param>
        /// <returns>String to be put into control Content or Text or similar property</returns>
        public string Convert(string controlName)
        {
            return (string) Convert(controlName, null, null, null);
        }

        /// <summary> Not implemented </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
