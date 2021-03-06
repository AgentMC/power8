﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Power8.Helpers;

namespace Power8.Converters
{
    /// <summary>
    /// Reimplement system menu style together with control template is too complex task.
    /// The data templates were the solution to get rid of Image generator.
    /// On the other hand, all data template stuff goes to Content property of MenuItem.
    /// So we need to shift data to the left to show Icon properly. Here comes the converter.
    /// One way converter. No data/parameters are considered. Returns Thickness shift for 
    /// current OS.
    /// </summary>
    [ValueConversion(typeof(double), typeof(Thickness))]
    [TypeConverter(typeof(double))]
    class MenuWidthToMarginConverter : IValueConverter
    {
        /// <summary>
        /// Predefined shift values. W8RP hack is because templates and styles are 
        /// treated in different way on W8.
        /// </summary>
        private static readonly Thickness Classic = new Thickness(-14, 2, 0, 2), 
                                          Regular = new Thickness(-30, 0, 0, 0), 
                                          W8RPMenuHack = new Thickness(-42, 0, 0, 0);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dbl = (int) SystemParameters.MenuWidth;
            if (dbl == 18 /*XP/7 Classic*/ || //following is the definition of HighContrast on 8
                (Util.OsIs.EightOrMore && !API.DwmIsCompositionEnabled()))
            {
                return Classic;
            }
            if (dbl == 19) //Aero/7 basic/XP theme
            {
                return Util.OsIs.EightRpOrMore ? W8RPMenuHack : Regular;
            }
            Log.Raw("dbl=" + dbl);
            return new Thickness(0, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
