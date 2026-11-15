using System;
using System.IO;
using System.Text.Json;
using NinOS.Infrastructure.Logging;

namespace NinOS.Infrastructure.Data
{
    /// <summary>
    /// Entrega la cadena de conexion a PostgreSQL.
    ///
    /// La cadena sale de la variable de entorno NINOS_DB_CONNECTION o del appsettings.json
    /// que esta junto al ejecutable. Ninguno de los dos va a Git, asi que el repositorio
    /// publico no contiene credenciales. Si no hay ninguna de las dos, no se conecta a nada.
    /// </summary>
    public static class DbConnectionFactory
    {
        public const string EnvironmentVariableName = "NINOS_DB_CONNECTION";

        public const string SettingsFileName = "appsettings.json";

        // NUNCA agregar aqui una cadena de conexion por defecto. Antes habia una que apuntaba
        // a la base de PRODUCCION: cualquier PC sin configurar entraba a los datos reales del
        // cliente sin avisar, y la contraseña quedaba publicada en el repositorio.
        public static string MissingConfigurationMessage =>
            "NinOS no tiene configurada la conexion a la base de datos, asi que no puede abrir.\n\n" +
            "Se busco en estos dos lugares:\n" +
            $"   1. La variable de entorno {EnvironmentVariableName}\n" +
            $"   2. El archivo {SettingsFileName} junto al programa:\n" +
            $"      {SettingsFilePath}\n\n" +
            "Avise al administrador del sistema para que la configure.";

        public static string SettingsFilePath => Path.Combine(AppContext.BaseDirectory, SettingsFileName);

        public static string GetConnectionString()
        {
            if (TryGetConnectionString(out string? connection_string, out string source))
            {
                AppLog.Info($"Conexion a la base tomada de {source}");
                return connection_string!;
            }

            AppLog.Error("No hay configuracion de conexion a la base de datos");
            throw new InvalidOperationException(MissingConfigurationMessage);
        }

        /// <summary>
        /// Busca la cadena sin lanzar excepcion. Devuelve false si no hay ninguna configurada.
        /// El parametro source dice de donde salio, para dejarlo en el log sin exponer la cadena.
        /// </summary>
        public static bool TryGetConnectionString(out string? connection_string, out string source)
        {
            string? from_environment = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (!string.IsNullOrWhiteSpace(from_environment))
            {
                connection_string = from_environment.Trim();
                source = $"la variable de entorno {EnvironmentVariableName}";
                return true;
            }

            if (File.Exists(SettingsFilePath))
            {
                string? from_file = read_from_settings_file(SettingsFilePath);
                if (!string.IsNullOrWhiteSpace(from_file))
                {
                    connection_string = from_file.Trim();
                    source = SettingsFileName;
                    return true;
                }
            }

            connection_string = null;
            source = string.Empty;
            return false;
        }

        private static string? read_from_settings_file(string settings_path)
        {
            try
            {
                string json = File.ReadAllText(settings_path);
                using JsonDocument document = JsonDocument.Parse(json);

                if (!document.RootElement.TryGetProperty("ConnectionStrings", out JsonElement connection_strings))
                {
                    AppLog.Warn($"{SettingsFileName} no tiene la seccion ConnectionStrings");
                    return null;
                }

                if (connection_strings.TryGetProperty("NinOSDb", out JsonElement ninos_db))
                {
                    return ninos_db.GetString();
                }

                if (connection_strings.TryGetProperty("DefaultConnection", out JsonElement default_connection))
                {
                    return default_connection.GetString();
                }

                AppLog.Warn($"{SettingsFileName} no tiene NinOSDb ni DefaultConnection dentro de ConnectionStrings");
                return null;
            }
            catch (Exception exception)
            {
                // El error se registra en vez de ignorarse: sin esto, un archivo mal formado
                // parecia "sin configuracion" y nadie sabia que el problema era el JSON.
                AppLog.Error($"No se pudo leer {SettingsFileName}", exception);
                return null;
            }
        }
    }
}
