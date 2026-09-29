using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Common
{
    public class backup_config
    {
        public bool auto_enabled { get; set; }
        public string auto_time { get; set; } = "22:00";
        public int retention_count { get; set; } = 10;
        public string? last_run_date { get; set; }
    }

    public static class DatabaseBackup
    {
        public static string BackupsFolder =>
            Path.Combine(AppContext.BaseDirectory, "Backups");

        private static string config_path => Path.Combine(AppContext.BaseDirectory, "backup_config.json");

        public static backup_config load_config()
        {
            try
            {
                if (File.Exists(config_path))
                {
                    return JsonSerializer.Deserialize<backup_config>(File.ReadAllText(config_path)) ?? new backup_config();
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("No se pudo leer backup_config.json", ex);
            }
            return new backup_config();
        }

        public static void save_config(backup_config config)
        {
            File.WriteAllText(config_path, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static string? find_pg_tool(string tool_name = "pg_dump.exe")
        {
            string? from_env = Environment.GetEnvironmentVariable("NINOS_PG_DUMP");
            if (!string.IsNullOrWhiteSpace(from_env) && File.Exists(from_env)) return from_env;

            foreach (string root in pg_install_roots())
            {
                foreach (string version_dir in Directory.GetDirectories(root).OrderByDescending(d => d))
                {
                    string candidate = Path.Combine(version_dir, "bin", tool_name);
                    if (File.Exists(candidate)) return candidate;
                }
            }

            try
            {
                string path_var = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                foreach (string dir in path_var.Split(Path.PathSeparator))
                {
                    if (string.IsNullOrWhiteSpace(dir)) continue;
                    string candidate = Path.Combine(dir.Trim(), tool_name);
                    if (File.Exists(candidate)) return candidate;
                }
            }
            catch
            {
                // PATH no disponible: se devuelve null
            }
            return null;
        }

        public static string? find_pg_dump() => find_pg_tool("pg_dump.exe");

        private static IEnumerable<string> pg_install_roots()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "PostgreSQL"),
                @"C:\Program Files\PostgreSQL"
            };

            return candidates
                .Where(root => !string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static (string host, int port, string database, string user, string password) read_connection_parts()
        {
            string host = "localhost";
            int port = 5432;
            string database = "";
            string user = "";
            string password = "";

            foreach (string pair in DbConnectionFactory.GetConnectionString().Split(';'))
            {
                string[] kv = pair.Split(new[] { '=' }, 2);
                if (kv.Length != 2) continue;

                string key = kv[0].Trim().ToLowerInvariant();
                string value = kv[1].Trim();

                switch (key)
                {
                    case "host": host = value; break;
                    case "port": int.TryParse(value, out port); break;
                    case "database": database = value; break;
                    case "username":
                    case "user id": user = value; break;
                    case "password": password = value; break;
                }
            }

            return (host, port, database, user, password);
        }

        private static int run_tool(string tool_path, List<string> args, string password, out string output)
        {
            var start_info = new ProcessStartInfo(tool_path)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            foreach (string arg in args) start_info.ArgumentList.Add(arg);
            start_info.Environment["PGPASSWORD"] = password;

            using Process? process = Process.Start(start_info);
            if (process == null)
            {
                output = "No se pudo iniciar el proceso.";
                return -1;
            }

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            output = (stdout + Environment.NewLine + stderr).Trim();
            return process.ExitCode;
        }

        public static async Task<string> create_backup_async(string file_path, CancellationToken token = default)
        {
            var parts = read_connection_parts();
            if (string.IsNullOrWhiteSpace(parts.database) || string.IsNullOrWhiteSpace(parts.user))
                return "No se pudo leer la configuracion de conexion a la base de datos.";

            string? pg_dump = find_pg_dump();
            if (pg_dump == null)
                return "No se encontro pg_dump.exe. Indica la ruta en la variable de entorno NINOS_PG_DUMP.";

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file_path))!);
            }
            catch (Exception ex)
            {
                return $"No se pudo crear la carpeta de destino: {ex.Message}";
            }

            var args = new List<string>
            {
                "-h", parts.host,
                "-p", parts.port.ToString(),
                "-U", parts.user,
                "-d", parts.database,
                "--format", "custom",
                "--compress", "5",
                "--no-owner",
                "--no-privileges",
                "-f", file_path
            };

            AppLog.Info($"Backup iniciado: {file_path}");

            string output = string.Empty;
            int code = await Task.Run(
                () => run_tool(pg_dump, args, parts.password, out output),
                token);

            if (code != 0)
            {
                AppLog.Error($"Backup fallo (codigo {code}): {output}");
                return $"pg_dump devolvio el codigo {code}.\n{output}";
            }

            AppLog.Info($"Backup completado: {file_path}");
            return $"Backup generado correctamente en:\n{file_path}";
        }

        public static async Task<string> restore_backup_async(string file_path, CancellationToken token = default)
        {
            if (!File.Exists(file_path))
                return "El archivo de backup no existe.";

            var parts = read_connection_parts();
            string? pg_restore = find_pg_tool("pg_restore.exe");
            if (string.IsNullOrWhiteSpace(pg_restore) || !File.Exists(pg_restore))
                return "No se encontro pg_restore.exe en el equipo.";

            var args = new List<string>
            {
                "-h", parts.host,
                "-p", parts.port.ToString(),
                "-U", parts.user,
                "-d", parts.database,
                "--clean",
                "--if-exists",
                "--no-owner",
                "--no-privileges",
                file_path
            };

            AppLog.Info($"Restauracion iniciada desde: {file_path}");

            string output = string.Empty;
            int code = await Task.Run(
                () => run_tool(pg_restore, args, parts.password, out output),
                token);

            // pg_restore devuelve avisos no fatales en codigos 1-2 cuando la BD destino ya existe
            if (code > 2)
            {
                AppLog.Error($"Restauracion fallo (codigo {code}): {output}");
                return $"pg_restore devolvio el codigo {code}.\n{output}";
            }

            AppLog.Info($"Restauracion completada desde: {file_path}");
            return "Base de datos restaurada correctamente. Reinicia la aplicacion para que los cambios se vean reflejados.";
        }

        public static string default_backup_path() =>
            Path.Combine(BackupsFolder, $"ninos_backup_{DateTime.Now:yyyy-MM-dd_HHmmss}.dump");

        public static int prune_old_backups()
        {
            backup_config config = load_config();
            if (config.retention_count <= 0) return 0;

            try
            {
                if (!Directory.Exists(BackupsFolder)) return 0;

                FileInfo[] files = new DirectoryInfo(BackupsFolder)
                    .GetFiles("ninos_backup_*.dump")
                    .OrderByDescending(f => f.CreationTime)
                    .ToArray();

                int removed = 0;
                foreach (FileInfo old in files.Skip(config.retention_count))
                {
                    try
                    {
                        old.Delete();
                        removed++;
                    }
                    catch
                    {
                        // si el archivo esta en uso, se conserva
                    }
                }
                return removed;
            }
            catch (Exception ex)
            {
                AppLog.Error("No se pudieron limpiar los backups antiguos", ex);
                return 0;
            }
        }

        public static void register_auto_backup(DispatcherTimer timer)
        {
            timer.Interval = TimeSpan.FromSeconds(30);
            timer.Tick += async (s, e) =>
            {
                backup_config current = load_config();
                if (!current.auto_enabled) return;
                if (current.last_run_date == DateTime.Now.ToString("yyyy-MM-dd")) return;
                if (!TimeSpan.TryParse(current.auto_time, out TimeSpan target)) return;

                DateTime now = DateTime.Now;
                if (now.TimeOfDay < target || now.TimeOfDay >= target.Add(TimeSpan.FromMinutes(1))) return;

                timer.Stop();

                string result = await create_backup_async(default_backup_path());
                AppLog.Info($"Backup automatico: {result.Replace(Environment.NewLine, " ")}");

                // Solo se marca el dia como hecho si el backup se genero bien, para reintentar si falla.
                if (result.StartsWith("Backup generado"))
                {
                    current.last_run_date = now.ToString("yyyy-MM-dd");
                    save_config(current);
                    prune_old_backups();
                }

                timer.Start();
            };
            timer.Start();
        }
    }
}
