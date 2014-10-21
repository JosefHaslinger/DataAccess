﻿using System;
using System.Globalization;

namespace JPB.DataAccess.ModelsAnotations
{
    /// <summary>
    /// Converts values from DB to C# and back
    /// </summary>
    public interface IValueConverter
    {
        /// <summary>
        /// Converts a value from a DB to a C# object 
        /// </summary>
        /// <param name="value">Object from the DB</param>
        /// <param name="targetType">Type of Property to convert to</param>
        /// <param name="parameter">given Params</param>
        /// <param name="culture">Current Culture</param>
        /// <returns>C# object that is of type of property</returns>
        object Convert(object value, Type targetType, object parameter, CultureInfo culture);
        object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
    }
}