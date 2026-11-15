using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Common
{
    /// <summary>
    /// Clave del Panel Admin.
    ///
    /// La clave NO esta en el codigo: vive solo en admin_security.json, guardada como hash
    /// (irreversible) junto al ejecutable, y ese archivo no va a Git. Si el archivo falta o
    /// esta dañado, el panel queda cerrado: antes se regeneraba con una clave por defecto que
    /// estaba escrita en el codigo del repositorio publico, asi que cualquiera podia entrar.
    ///
    /// Para crear la clave la primera vez: NinOS.UI.exe --set-admin-password
    /// </summary>
    public static class AdminSecurity
    {
        private const int KeyIterations = 200_000;
        private const int MinPasswordLength = 6;
        private const int MaxAttempts = 5;

        private static readonly string config_path =
            Path.Combine(AppContext.BaseDirectory, "admin_security.json");

        private static int _failed_attempts;
        private static DateTime _unlock_at = DateTime.MinValue;

        public static string ConfigPath => config_path;

        public static int MinimumPasswordLength => MinPasswordLength;

        /// <summary>True si ya existe una clave guardada y legible.</summary>
        public static bool is_configured()
        {
            (byte[] salt, byte[] hash, int _) = load_config();
            return salt.Length > 0 && hash.Length > 0;
        }

        public static bool verify(string password)
        {
            if (DateTime.Now < _unlock_at) return false;
            if (string.IsNullOrEmpty(password)) return false;

            (byte[] salt, byte[] hash, int iterations) = load_config();
            if (salt.Length == 0 || hash.Length == 0)
            {
                AppLog.Warn("Panel Admin sin clave configurada: acceso bloqueado");
                return false;
            }

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

            AppLog.Error("Intento fallido de acceso al Panel Admin");
            return false;
        }

        /// <summary>
        /// Guarda la clave nueva. Devuelve false si no se pudo guardar, para que quien llama
        /// no diga "listo" cuando en realidad el archivo no se escribio.
        /// </summary>
        public static bool change_password(string new_password)
        {
            if (string.IsNullOrEmpty(new_password) || new_password.Length < MinPasswordLength)
            {
                AppLog.Warn($"Clave del Panel Admin rechazada: minimo {MinPasswordLength} caracteres");
                return false;
            }

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
                writer.Flush();

                _failed_attempts = 0;
                _unlock_at = DateTime.MinValue;
                AppLog.Info("Clave del Panel Admin actualizada");
                return true;
            }
            catch (Exception ex)
            {
                AppLog.Error("No se pudo guardar la clave del Panel Admin", ex);
                return false;
            }
        }

        public static int remaining_delay_seconds()
        {
            int remaining = (int)Math.Ceiling((_unlock_at - DateTime.Now).TotalSeconds);
            return remaining > 0 ? remaining : 0;
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

                AppLog.Warn("admin_security.json sin los campos esperados");
            }
            catch (Exception ex)
            {
                // No se regenera con una clave por defecto: si el archivo esta dañado,
                // el panel queda cerrado hasta que se vuelva a crear a proposito.
                AppLog.Error("admin_security.json dañado o ilegible", ex);
            }

            return (Array.Empty<byte>(), Array.Empty<byte>(), KeyIterations);
        }
    }
}
