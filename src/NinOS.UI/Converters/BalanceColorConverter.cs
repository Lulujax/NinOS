using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NinOS.UI.Converters
{
    public class BalanceColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal balance)
            {
                if (balance == 0)
                    return new SolidColorBrush(Colors.Green);
                else if (balance > 0 && balance <= 100)
                    return new SolidColorBrush(Colors.Orange);
                else
                    return new SolidColorBrush(Colors.Red);
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}