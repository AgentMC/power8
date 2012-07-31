using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

#if DEBUG
using System.Diagnostics;
#endif

namespace Power8.Converters
{
    [ValueConversion(typeof(double), typeof(Thickness))]
    [TypeConverter(typeof(double))]
    class MenuWidthToMarginConverter : IValueConverter
    {
        private static readonly Thickness Classic = new Thickness(-14, 2, 0, 2), 
                                          Regular = new Thickness(-30, 0, 0, 0), 
                                          W8RPMenuHack = new Thickness(-42, 0, 0, 0);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dbl = (int) SystemParameters.MenuWidth;
            if (dbl == 18 || //HighContrast on 8
                (Util.OsIs.EightOrMore && !API.DwmIsCompositionEnabled()))
            {
                return Classic;
            }
            if (dbl == 19) //Aero/7 basic/XP theme
            {
                return Util.OsIs.EightRpOrMore ? W8RPMenuHack : Regular;
            }
#if DEBUG
            Debug.WriteLine("dbl="+dbl);
#endif
            return new Thickness(0, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
