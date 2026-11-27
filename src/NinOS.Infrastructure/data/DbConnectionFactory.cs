using System;
using System.IO;
using System.Text.Json;
using Npgsql;
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
        public const string MissingConfigurationMessage =
            "NinOS no tiene configurada la conexion con el sistema, asi que no puede abrir.\n\n" +
            "Avise al administrador del sistema para que la configure.";

        public static string SettingsFilePath => LocalDataFolder.file_path(SettingsFileName);

        public static string GetConnectionString()
        {
            if (TryGetConnectionString(out string? connection_string, out string source))
            {
                AppLog.Info($"Conexion a la base tomada de {source}");
                return apply_resilience_settings(connection_string!);
            }

            AppLog.Error($"No hay configuracion de conexion: falta {SettingsFileName} en la carpeta de datos de NinOS");
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

        public const int KEEPALIVE_SECONDS = 30;

        public const int IDLE_LIFETIME_SECONDS = 30;

        private const int NPGSQL_DEFAULT_IDLE_LIFETIME_SECONDS = 300;

        public static string apply_resilience_settings(string connection_string)
        {
            if (string.IsNullOrWhiteSpace(connection_string))
            {
                throw new ArgumentException("La cadena de conexion no puede estar vacia.", nameof(connection_string));
            }

            NpgsqlConnectionStringBuilder builder;
            try
            {
                builder = new NpgsqlConnectionStringBuilder(connection_string);
            }
            catch (ArgumentException exception)
            {
                AppLog.Warn($"La cadena de conexion no se pudo interpretar para ajustarla ({exception.GetType().Name}); se usa tal cual.");
                return connection_string;
            }

            if (builder.KeepAlive == 0)
            {
                builder.KeepAlive = KEEPALIVE_SECONDS;
            }

            if (!builder.TcpKeepAlive)
            {
                builder.TcpKeepAlive = true;
            }

            if (builder.ConnectionIdleLifetime == NPGSQL_DEFAULT_IDLE_LIFETIME_SECONDS)
            {
                builder.ConnectionIdleLifetime = IDLE_LIFETIME_SECONDS;
            }

            string adjusted = builder.ConnectionString;

            if (new NpgsqlConnectionStringBuilder(adjusted).KeepAlive <= 0)
            {
                throw new InvalidOperationException("La cadena de conexion ajustada quedo sin keepalive.");
            }

            return adjusted;
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
