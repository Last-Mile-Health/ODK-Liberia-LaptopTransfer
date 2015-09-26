using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LastMileHealth.Converters
{
    public class BoolToVisibilityValueConverter : IValueConverter
    {
        /// <summary>
        /// Value for inverting convertation result.
        /// </summary>
        private const string InvertValue = "invert";

        /// <summary>
        /// Converts boolean value to System.Windows.Visibility.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var result = Visibility.Collapsed;
            if (value != null)
            {
                var flag = (bool)value;
                if (parameter != null && parameter.ToString() == InvertValue)
                {
                    flag = !flag;
                }

                if (flag)
                {
                    result = Visibility.Visible;
                }
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
