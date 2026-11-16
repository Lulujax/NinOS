using System;
using System.Security.Cryptography;

namespace NinOS.Domain
{
    public static class AdminPasswordHasher
    {
        public const int MinimumLength = 12;
        public const int Iterations = 200_000;

        private const int SaltLength = 16;
        private const int HashLength = 32;
        private const int MinimumSaltBytes = 8;

        public static bool is_long_enough(string? password)
        {
            return !string.IsNullOrEmpty(password) && password.Length >= MinimumLength;
        }

        public static admin_key create(string password, DateTime updated_at_utc)
        {
            if (!is_long_enough(password))
                throw new ArgumentException($"La clave del Panel Admin debe tener al menos {MinimumLength} caracteres.", nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltLength);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashLength);

            return new admin_key
            {
                id_key = admin_key.SingleRowId,
                password_salt = Convert.ToBase64String(salt),
                password_hash = Convert.ToBase64String(hash),
                iterations = Iterations,
                updated_at_utc = updated_at_utc
            };
        }

        public static bool matches(string? password, admin_key? stored)
        {
            if (string.IsNullOrEmpty(password) || stored == null) return false;
            if (!try_decode(stored, out byte[] salt, out byte[] expected_hash)) return false;

            byte[] candidate = Rfc2898DeriveBytes.Pbkdf2(password, salt, stored.iterations, HashAlgorithmName.SHA256, expected_hash.Length);
            return CryptographicOperations.FixedTimeEquals(candidate, expected_hash);
        }

        private static bool try_decode(admin_key stored, out byte[] salt, out byte[] hash)
        {
            salt = Array.Empty<byte>();
            hash = Array.Empty<byte>();

            if (stored.iterations < 1) return false;

            try
            {
                salt = Convert.FromBase64String(stored.password_salt);
                hash = Convert.FromBase64String(stored.password_hash);
            }
            catch (FormatException)
            {
                return false;
            }

            return salt.Length >= MinimumSaltBytes && hash.Length > 0;
        }
    }
}
