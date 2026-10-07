using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NinOS.Domain
{
    // Estandar de codigos del inventario: PREFIJO DE MARCA + CORRELATIVO DE 5 DIGITOS.
    // Vive en el dominio (y no en la vista) para que el formulario, el servicio que arma el
    // siguiente codigo y la validacion hablen todos del mismo formato.
    public static class product_code_rules
    {
        // Prefijo de 3 letras por marca. Se inicializa con los predeterminados y se
        // sincroniza dinámicamente con la base de datos (tabla product_line).
        private static readonly ConcurrentDictionary<string, string> _prefixes =
            new ConcurrentDictionary<string, string>(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["DEFILE"] = "DEF",
                    ["OLEOS"] = "OLE",
                    ["REMBRANDT"] = "REM",
                    ["BIOLINE"] = "BIO",
                    ["AMAZONIA SECRET"] = "AMA",
                    ["KEDAM"] = "KED",
                    ["DEPIL CLEAR"] = "DEP",
                    ["ESTILISTA"] = "EST",
                    ["CUTIQUE"] = "CUT",
                    ["OTROS"] = "OTR"
                },
                StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyDictionary<string, string> CategoryPrefixes => _prefixes;

        public static void RegisterPrefix(string category, string prefix)
        {
            if (!string.IsNullOrWhiteSpace(category) && !string.IsNullOrWhiteSpace(prefix))
            {
                _prefixes[category.Trim().ToUpperInvariant()] = prefix.Trim().ToUpperInvariant();
            }
        }

        public const int DefaultCodeDigits = 5;

        // Sin este valor el constructor de product revienta con un ArgumentException vacio
        // que el usuario no entiende, asi que se devuelve una cadena vacia y el formulario
        // valida antes de guardar.
        public const string EmptyCode = "";

        public static bool TryGetPrefix(string? category, out string prefix)
        {
            prefix = string.Empty;

            if (string.IsNullOrWhiteSpace(category)) return false;
            return _prefixes.TryGetValue(category!, out prefix!);
        }

        public static int DigitsFor(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return DefaultCodeDigits;
            return prefix.EndsWith("-", StringComparison.Ordinal) ? 3 : DefaultCodeDigits;
        }

        public static string Format(string prefix, int number, int digits)
        {
            return prefix + number.ToString(new string('0', digits));
        }

        // Devuelve el correlativo del codigo, o null si el codigo no sigue el formato
        // de la marca indicada (prefijo correcto + exactamente los digitos esperados).
        public static int? TryParseNumber(string? code, string prefix)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            if (string.IsNullOrEmpty(prefix)) return null;
            if (!code!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;

            string suffix = code.Substring(prefix.Length);
            if (suffix.Length != DigitsFor(prefix)) return null;

            foreach (char c in suffix)
            {
                if (!char.IsDigit(c)) return null;
            }

            return int.TryParse(suffix, out int value) ? value : null;
        }

        public static bool IsValidFor(string? code, string category)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            if (!TryGetPrefix(category, out string prefix)) return false;
            return TryParseNumber(code, prefix) != null;
        }
    }
}
