

namespace LastMileHealth.Converters
{
    using System;
    using System.Collections;
    using System.Windows;
    using System.Windows.Data;

    public class EmptyCollectionToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Value for inverting convertation result.
        /// </summary>
        private const string InvertValue = "invert";

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility result = Visibility.Collapsed;

            if (value != null)
            {
                if (value is ICollection)
                {
                    if ((value as ICollection).Count != 0)
                    {
                        result = Visibility.Visible;
                    }
                }
                else
                {
                    if ((int)value != 0)
                    {
                        result = Visibility.Visible;
                    }
                }
            }

            if (parameter != null && parameter.ToString() == InvertValue)
            {
                result = result == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
