using System;
using System.Collections.Generic;
using System.Linq;

namespace NinOS.Infrastructure.Common
{
    /// <summary>
    /// Serie con rollover: el numero completo se trata como (prefijo numerico * 1000 + sufijo).
    /// "3200_001" -> 3200001; "3200_999" -> 3200999; el siguiente es 3201000 -> "3201_000".
    /// </summary>
    public static class SeriesCalculator
    {
        public static long ParseFullNumber(string? series_number)
        {
            if (string.IsNullOrWhiteSpace(series_number)) return 0;

            string[] parts = series_number.Split('_');
            if (parts.Length != 2) return 0;
            if (!long.TryParse(parts[0], out long prefix)) return 0;
            if (!long.TryParse(parts[1], out long suffix)) return 0;

            return prefix * 1000 + suffix;
        }

        /// <summary>
        /// Parsea el correlativo de una nota para ordenamiento numérico de más antigua a más nueva.
        /// Soporta series estándar "3200_001", con prefijos "NC-3200_001" o números directos "123".
        /// </summary>
        public static long ParseCorrelative(string? correlative)
        {
            if (string.IsNullOrWhiteSpace(correlative)) return 0;

            string trimmed = correlative.Trim();
            string[] parts = trimmed.Split('_');
            if (parts.Length == 2)
            {
                string prefix_digits = new string(parts[0].Where(char.IsDigit).ToArray());
                string suffix_digits = new string(parts[1].Where(char.IsDigit).ToArray());

                if (long.TryParse(prefix_digits, out long prefix) && long.TryParse(suffix_digits, out long suffix))
                {
                    return prefix * 1000 + suffix;
                }
            }

            string all_digits = new string(trimmed.Where(char.IsDigit).ToArray());
            if (long.TryParse(all_digits, out long direct))
            {
                return direct;
            }

            return 0;
        }

        public static IOrderedEnumerable<T> OrderByCorrelative<T>(this IEnumerable<T> source, Func<T, string?> correlativeSelector)
        {
            return source
                .OrderBy(item => ParseCorrelative(correlativeSelector(item)))
                .ThenBy(item => correlativeSelector(item) ?? string.Empty);
        }

        public static string FormatNumber(long full_number)
        {
            return $"{full_number / 1000}_{full_number % 1000:D3}";
        }

        public static long Seed(string? seller_code)
        {
            if (string.IsNullOrWhiteSpace(seller_code)) return 0;
            if (!long.TryParse(seller_code, out long prefix)) return 0;
            return prefix * 1000;
        }

        public static long GetMax(IEnumerable<string> existing_numbers, long seed)
        {
            long max = Math.Max(seed, 0);
            foreach (string number in existing_numbers)
            {
                long value = ParseFullNumber(number);
                if (value > max) max = value;
            }
            return max;
        }

        public static string GetNext(IEnumerable<string> existing_numbers, string? seller_code)
        {
            long seed = Seed(seller_code);
            return FormatNumber(GetMax(existing_numbers, seed) + 1);
        }
    }
}