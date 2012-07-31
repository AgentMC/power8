using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using Power8.Properties;

namespace Power8.Converters
{
    /// <summary>
    /// This converter tries to get tooltip fo PowerItem passed in various ways. In general,
    /// it is the resolved argument of PowerItem, but other options available as well.
    /// One-way converter. Data: PowerItem. Returns: the descriptive tooltip.
    /// 2nd option: when "pin" is passed as parameter, returns pinned/unpinned string
    /// for boolean passed as data item.
    /// </summary>
    [ValueConversion(typeof(PowerItem), typeof(string))]
    [TypeConverter(typeof(PowerItem))]
    class PiToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null && (string)parameter == "pin")
                return (bool?) value == true ? Resources.Str_Unpin : Resources.Str_Pin;
            var pi = value as PowerItem;
            if (pi == null)
                return Resources.Err_CantGetTooltip;
            if (!pi.IsNotPureControlPanelFlowItem)
                return Resources.Str_CplElement;
            if( pi.IsSpecialObject)
            {
                var cmd = Util.GetOpenCommandForClass(pi.Argument);
                if(cmd == null)
                    return pi.FriendlyName + Resources.Str_Library;
                return cmd.Item1;
            }
            try
            {
                return PowerItemTree.GetResolvedArgument(pi);
            }
            catch (IOException)
            {
                return Resources.Err_CantGetTooltip;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
