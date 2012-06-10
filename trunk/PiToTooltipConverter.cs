using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using Power8.Properties;
#if DEBUG
using System.Diagnostics;
#endif

namespace Power8
{
    [ValueConversion(typeof(PowerItem), typeof(string))]
    [TypeConverter(typeof(PowerItem))]
    class PiToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var pi = value as PowerItem;
            if (pi == null)
                return Resources.Err_CantGetTooltip;
            if (!pi.IsNotPureControlPanelFlowItem)
                return Resources.Str_CplElement;
            return pi.IsSpecialObject
                    ? (pi.IsLibrary 
                        ? pi.FriendlyName + Resources.Str_Library 
                        : Util.GetOpenCommandForClass(pi.Argument).Item1)
                    : PowerItemTree.GetResolvedArgument(pi);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
