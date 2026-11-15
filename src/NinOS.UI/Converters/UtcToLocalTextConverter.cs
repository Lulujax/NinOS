using System;
using System.Globalization;
using System.Windows.Data;
using NinOS.Domain;

namespace NinOS.UI.Converters
{
    public class UtcToLocalTextConverter : IValueConverter
    {
        public const string DefaultFormat = "dd/MM/yyyy";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? format = parameter as string;
            if (string.IsNullOrWhiteSpace(format)) format = DefaultFormat;

            return value switch
            {
                DateTime fecha => AppTimeZone.to_local_text(fecha, format),
                DateTimeOffset fecha_offset => AppTimeZone.to_local_text(fecha_offset.UtcDateTime, format),
                _ => string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("La fecha mostrada es de solo lectura.");
        }
    }
}
