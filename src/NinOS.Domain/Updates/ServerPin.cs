using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NinOS.Domain.Updates
{
    public static class ServerPin
    {
        public const string CURRENT_PIN = "CaNxkh8HXtRJlcTxYcBEgRJVNkru0ZNZHZz9NikSz1c=";
        public const string BACKUP_PIN = "qKpO03hTCONttn2uu9wYrVOieVa1Q54acDbsFLMHOo4=";

        private const string CURL_PREFIX = "sha256//";
        private const int SHA256_BYTES = 32;
        private const int SHA256_HEX_LENGTH = 64;

        public static IReadOnlyList<string> default_pins => new[] { CURRENT_PIN, BACKUP_PIN };

        public static string? normalize(string? raw_pin)
        {
            if (string.IsNullOrWhiteSpace(raw_pin)) return null;

            string cleaned = string.Concat(raw_pin.Where(character => !char.IsWhiteSpace(character)));
            if (cleaned.StartsWith(CURL_PREFIX, StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(CURL_PREFIX.Length);

            string hex_candidate = cleaned.Replace(":", string.Empty).Replace("-", string.Empty);
            if (hex_candidate.Length == SHA256_HEX_LENGTH && is_hex(hex_candidate))
                return Convert.ToBase64String(Convert.FromHexString(hex_candidate));

            try
            {
                byte[] decoded = Convert.FromBase64String(cleaned);
                return decoded.Length == SHA256_BYTES ? Convert.ToBase64String(decoded) : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public static string compute_pin(X509Certificate2 certificate)
        {
            if (certificate == null) throw new ArgumentNullException(nameof(certificate));

            byte[] public_key_info = certificate.PublicKey.ExportSubjectPublicKeyInfo();
            string pin = Convert.ToBase64String(SHA256.HashData(public_key_info));

            if (normalize(pin) == null) throw new InvalidOperationException("La huella calculada no tiene un formato SHA-256 valido.");
            return pin;
        }

        public static bool is_allowed(string? candidate_pin, IEnumerable<string> allowed_pins)
        {
            if (allowed_pins == null) throw new ArgumentNullException(nameof(allowed_pins));

            string? candidate = normalize(candidate_pin);
            bool has_allowed_pin = false;
            bool matches = false;

            foreach (string allowed_pin in allowed_pins)
            {
                string? allowed = normalize(allowed_pin);
                if (allowed == null) throw new ArgumentException("Una de las huellas permitidas no es un SHA-256 valido.", nameof(allowed_pins));

                has_allowed_pin = true;
                if (candidate != null && string.Equals(candidate, allowed, StringComparison.Ordinal)) matches = true;
            }

            if (!has_allowed_pin) throw new ArgumentException("Debe haber al menos una huella permitida.", nameof(allowed_pins));
            return matches;
        }

        public static bool matches_default(X509Certificate2? certificate)
        {
            if (certificate == null) return false;
            return is_allowed(compute_pin(certificate), default_pins);
        }

        private static bool is_hex(string value)
        {
            foreach (char character in value)
            {
                bool is_digit = character >= '0' && character <= '9';
                bool is_lower = character >= 'a' && character <= 'f';
                bool is_upper = character >= 'A' && character <= 'F';
                if (!is_digit && !is_lower && !is_upper) return false;
            }
            return true;
        }
    }
}
