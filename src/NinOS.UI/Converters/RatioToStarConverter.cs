using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NinOS.UI.Converters
{
    // Convierte una proporcion 0..1 en un GridLength en unidades Star para dibujar barras
    // horizontales con un Grid de dos columnas, sin depender de medir el control en codigo.
    public class RatioToStarConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double ratio = 0d;

            if (value is double d) ratio = d;
            else if (value is decimal m) ratio = (double)m;
            else if (value is float f) ratio = f;
            else if (value is int i) ratio = i;
            else if (value != null && double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
                ratio = parsed;

            if (ratio <= 0d) return new GridLength(0d, GridUnitType.Pixel);
            if (ratio > 1d) ratio = 1d;

            return new GridLength(ratio, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
