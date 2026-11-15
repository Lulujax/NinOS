using System;
using System.IO;
using System.Text.Json;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Updates
{
    public static class UpdateSettings
    {
        public const string EnvironmentVariableName = "NINOS_UPDATE_URL";
        public const string SettingsSection = "Updates";
        public const string SettingsKey = "BaseUrl";

        public static bool try_get_base_url(out string base_url, out string reason)
        {
            string? from_environment = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (!string.IsNullOrWhiteSpace(from_environment))
            {
                return try_parse_base_url(from_environment, out base_url, out reason);
            }

            string settings_path = DbConnectionFactory.SettingsFilePath;
            if (!File.Exists(settings_path))
            {
                base_url = string.Empty;
                reason = $"No hay variable {EnvironmentVariableName} ni archivo {DbConnectionFactory.SettingsFileName} con la seccion {SettingsSection}.";
                return false;
            }

            try
            {
                string? from_file = read_base_url_from_json(File.ReadAllText(settings_path));
                return try_parse_base_url(from_file, out base_url, out reason);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                AppLog.Warn($"Actualizaciones: no se pudo leer {DbConnectionFactory.SettingsFileName}: {exception.GetType().Name}");
                base_url = string.Empty;
                reason = $"No se pudo leer {DbConnectionFactory.SettingsFileName}.";
                return false;
            }
        }

        public static string? read_base_url_from_json(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty(SettingsSection, out JsonElement section)) return null;
                if (section.ValueKind != JsonValueKind.Object) return null;
                if (!section.TryGetProperty(SettingsKey, out JsonElement value)) return null;
                return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static bool try_parse_base_url(string? raw_url, out string base_url, out string reason)
        {
            base_url = string.Empty;

            if (string.IsNullOrWhiteSpace(raw_url))
            {
                reason = $"La direccion de actualizaciones ({SettingsSection}:{SettingsKey}) esta vacia.";
                return false;
            }

            if (!Uri.TryCreate(raw_url.Trim(), UriKind.Absolute, out Uri? uri))
            {
                reason = "La direccion de actualizaciones no es una direccion valida.";
                return false;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                reason = "La direccion de actualizaciones debe usar HTTPS.";
                return false;
            }

            string normalized = uri.ToString();
            base_url = normalized.EndsWith("/", StringComparison.Ordinal) ? normalized : normalized + "/";
            reason = string.Empty;
            return true;
        }
    }
}
