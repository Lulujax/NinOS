using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Common
{
    public static class AdminSecurity
    {
        public const string DefaultPassword = "070430";

        private const int KeyIterations = 200_000;
        private const int MaxAttempts = 5;

        private static readonly string config_path =
            Path.Combine(AppContext.BaseDirectory, "admin_security.json");

        private static int _failed_attempts;
        private static DateTime _unlock_at = DateTime.MinValue;

        public static bool verify(string password)
        {
            if (DateTime.Now < _unlock_at) return false;
            if (string.IsNullOrEmpty(password)) return false;

            (byte[] salt, byte[] hash, int iterations) = load_or_default();
            if (salt.Length == 0 || hash.Length == 0) return false;

            byte[] candidate = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterations, HashAlgorithmName.SHA256, hash.Length);

            bool ok = CryptographicOperations.FixedTimeEquals(candidate, hash);
            if (ok)
            {
                _failed_attempts = 0;
                _unlock_at = DateTime.MinValue;
                return true;
            }

            _failed_attempts++;
            if (_failed_attempts >= MaxAttempts)
                _unlock_at = DateTime.Now.AddSeconds(Math.Min(60, 5 * (_failed_attempts - MaxAttempts + 1)));

            AppLog.Error($"Intento fallido de acceso al Panel Admin");
            return false;
        }

        public static void change_password(string new_password)
        {
            if (string.IsNullOrEmpty(new_password)) return;

            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                new_password, salt, KeyIterations, HashAlgorithmName.SHA256, 32);

            try
            {
                using var stream = new FileStream(config_path, FileMode.Create, FileAccess.Write);
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
                writer.WriteStartObject();
                writer.WriteString("password_salt", Convert.ToBase64String(salt));
                writer.WriteString("password_hash", Convert.ToBase64String(hash));
                writer.WriteNumber("iterations", KeyIterations);
                writer.WriteString("updated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                writer.WriteEndObject();
            }
            catch (Exception ex)
            {
                AppLog.Error($"No se pudo guardar la clave admin: {ex.Message}");
            }
        }

        public static int remaining_delay_seconds()
        {
            int remaining = (int)Math.Ceiling((_unlock_at - DateTime.Now).TotalSeconds);
            return remaining > 0 ? remaining : 0;
        }

        private static (byte[] salt, byte[] hash, int iterations) load_or_default()
        {
            (byte[] salt, byte[] hash, int iterations) = load_config();
            if (salt.Length > 0 && hash.Length > 0) return (salt, hash, iterations);

            change_password(DefaultPassword);
            return load_config();
        }

        private static (byte[] salt, byte[] hash, int iterations) load_config()
        {
            try
            {
                if (!File.Exists(config_path)) return (Array.Empty<byte>(), Array.Empty<byte>(), KeyIterations);

                using var doc = JsonDocument.Parse(File.ReadAllText(config_path));
                if (doc.RootElement.TryGetProperty("password_salt", out JsonElement s)
                    && doc.RootElement.TryGetProperty("password_hash", out JsonElement h))
                {
                    byte[] salt = Convert.FromBase64String(s.GetString() ?? string.Empty);
                    byte[] hash = Convert.FromBase64String(h.GetString() ?? string.Empty);
                    int iterations = doc.RootElement.TryGetProperty("iterations", out JsonElement it)
                        ? it.GetInt32()
                        : KeyIterations;
                    return (salt, hash, iterations);
                }
            }
            catch
            {
                // archivo corrupto → regenerar con la clave por defecto
            }

            return (Array.Empty<byte>(), Array.Empty<byte>(), KeyIterations);
        }
    }
}