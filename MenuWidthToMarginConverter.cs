using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
#if DEBUG
using System.Diagnostics;
#endif

namespace Power8
{
    [ValueConversion(typeof(double), typeof(Thickness))]
    [TypeConverter(typeof(double))]
    class MenuWidthToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int dbl = (int) SystemParameters.MenuWidth;
            if (dbl == 19) //Aero/7
            {
                if(Environment.OSVersion.Version >= new Version(6,2,0,8400))
                    return new Thickness(-42, 0, 0, 0);
                return new Thickness(-30, 0, 0, 0);
            }
            if (dbl == 18) //Classic/7
            { 
                return new Thickness(-14, 2, 0, 2); 
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
