
namespace LastMileHealth.Converters
{
    using System;
    using System.Windows.Data;

    /// <summary>
    /// Converts for invertation boolean value.
    /// </summary>
    public class BooleanToInvertBooleanValueConverter : IValueConverter
    {
        /// <summary>
        /// Invertation boolean value.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="parameter">Additional parameters.</param>
        /// <param name="culture">Current culture info.</param>
        /// <returns>boolean opacity value.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="parameter">Additional parameters.</param>
        /// <param name="culture">Current culture info.</param>
        /// <returns>Boolean value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

