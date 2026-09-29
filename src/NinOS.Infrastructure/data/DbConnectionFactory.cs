using System;
using System.IO;
using System.Text.Json;

namespace NinOS.Infrastructure.Data
{
    public static class DbConnectionFactory
    {
        // Keepalive evita que el VPS corte las conexiones inactivas del pool y que
        // la siguiente consulta falle con "connection closed" aunque haya internet.
        public const string DefaultConnectionString =
            "Host=82.39.109.158;Port=5432;Database=ninos_db;Username=ninos_admin;Password=1234;" +
            "Timeout=15;Command Timeout=45;Keepalive=30;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=30;";

        public static string GetConnectionString()
        {
            // 1. Variable de entorno (mayor prioridad)
            string? configured = Environment.GetEnvironmentVariable("NINOS_DB_CONNECTION");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            // 2. Archivo appsettings.json junto al ejecutable
            string settings_path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(settings_path))
            {
                try
                {
                    string json = File.ReadAllText(settings_path);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs_section))
                    {
                        if (cs_section.TryGetProperty("NinOSDb", out var conn_elem))
                        {
                            string? cs = conn_elem.GetString();
                            if (!string.IsNullOrWhiteSpace(cs)) return cs;
                        }
                        if (cs_section.TryGetProperty("DefaultConnection", out var default_elem))
                        {
                            string? cs = default_elem.GetString();
                            if (!string.IsNullOrWhiteSpace(cs)) return cs;
                        }
                    }
                }
                catch
                {
                    // Si el archivo JSON tiene formato no valido, se usa el valor predeterminado
                }
            }

            // 3. Cadena por defecto (VPS)
            return DefaultConnectionString;
        }
    }
}
