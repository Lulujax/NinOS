using System;
using System.Globalization;
using System.Linq;

namespace NinOS.UI.Common
{
    public static class SearchText
    {
        private static readonly CultureInfo EsVe = CultureInfo.GetCultureInfo("es-VE");

        public static string money(decimal value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture).ToLowerInvariant()
                + " " + value.ToString("0.##", EsVe).ToLowerInvariant()
                + " " + value.ToString("N2", EsVe).ToLowerInvariant();
        }

        public static string pct(decimal? value)
        {
            return value.HasValue ? pct(value.Value) : string.Empty;
        }

        public static string pct(decimal value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture).ToLowerInvariant() + "%";
        }

        public static string date(DateTime? value)
        {
            return value.HasValue ? date(value.Value) : string.Empty;
        }

        public static string date(DateTime value)
        {
            return value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                + " " + value.ToString("MM/yyyy", CultureInfo.InvariantCulture)
                + " " + value.ToString("yyyy", CultureInfo.InvariantCulture)
                + " " + value.ToString("MMMM yyyy", EsVe).ToLowerInvariant();
        }

        public static string combine(params string?[] parts)
        {
            return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p))).ToLowerInvariant();
        }
    }
}