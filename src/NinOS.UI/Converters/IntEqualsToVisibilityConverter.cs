using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NinOS.UI.Converters
{
    public class IntEqualsToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = false;
            string param = parameter as string ?? string.Empty;

            if (param.StartsWith("not:", StringComparison.OrdinalIgnoreCase))
            {
                invert = true;
                param = param.Substring(4);
            }

            bool equals = false;
            if (value is int int_value && int.TryParse(param, out int target))
            {
                equals = int_value == target;
            }

            bool visible = invert ? !equals : equals;
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}