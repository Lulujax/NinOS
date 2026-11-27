using System;
using System.Collections.Generic;
using System.IO;
using NinOS.Infrastructure.Logging;

namespace NinOS.Infrastructure.Data
{
    public enum LegacyCopyResult
    {
        SameFolder,
        NothingToCopy,
        AlreadyInDataFolder,
        Copied,
        Failed
    }

    public static class LocalDataFolder
    {
        public const string VELOPACK_CURRENT_FOLDER_NAME = "current";
        public const string VELOPACK_UPDATER_FILE_NAME = "Update.exe";
        public const string BACKUPS_FOLDER_NAME = "Backups";

        public static readonly IReadOnlyList<string> LEGACY_FILE_NAMES = new[]
        {
            "appsettings.json",
            "backup_config.json"
        };

        private static readonly object _sync = new object();
        private static string? _data_directory;

        public static string ProgramDirectory => AppContext.BaseDirectory;

        public static string DataDirectory
        {
            get
            {
                lock (_sync)
                {
                    if (_data_directory == null)
                    {
                        _data_directory = resolve_data_directory(ProgramDirectory, File.Exists);
                    }
                    return _data_directory;
                }
            }
        }

        public static bool IsInstalledApp =>
            !string.Equals(Path.TrimEndingDirectorySeparator(ProgramDirectory), DataDirectory, StringComparison.OrdinalIgnoreCase);

        public static string file_path(string file_name)
        {
            if (string.IsNullOrWhiteSpace(file_name))
            {
                throw new ArgumentException("El nombre del archivo no puede estar vacio.", nameof(file_name));
            }
            if (Path.IsPathRooted(file_name) || file_name.IndexOfAny(new[] { '\\', '/' }) >= 0)
            {
                throw new ArgumentException("Se espera solo el nombre del archivo, sin carpetas.", nameof(file_name));
            }
            return Path.Combine(DataDirectory, file_name);
        }

        public static string resolve_data_directory(string program_directory, Func<string, bool> file_exists)
        {
            if (string.IsNullOrWhiteSpace(program_directory))
            {
                throw new ArgumentException("La carpeta del programa no puede estar vacia.", nameof(program_directory));
            }
            if (file_exists == null)
            {
                throw new ArgumentNullException(nameof(file_exists));
            }

            string trimmed = Path.TrimEndingDirectorySeparator(program_directory);
            string folder_name = Path.GetFileName(trimmed);
            string? parent = Path.GetDirectoryName(trimmed);

            bool is_velopack_install =
                string.Equals(folder_name, VELOPACK_CURRENT_FOLDER_NAME, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(parent)
                && file_exists(Path.Combine(parent, VELOPACK_UPDATER_FILE_NAME));

            string result = is_velopack_install ? parent! : trimmed;

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException("No se pudo determinar la carpeta de datos de la app.");
            }
            return result;
        }

        public static void migrate_legacy_data()
        {
            string program_directory = Path.TrimEndingDirectorySeparator(ProgramDirectory);
            string data_directory = DataDirectory;

            if (string.Equals(program_directory, data_directory, StringComparison.OrdinalIgnoreCase))
            {
                AppLog.Info("Carpeta de datos: es la misma del programa, no hay nada que mudar");
                return;
            }

            AppLog.Info("Carpeta de datos: instalacion de Velopack, se usan los datos fuera de current");

            foreach (string file_name in LEGACY_FILE_NAMES)
            {
                LegacyCopyResult result = copy_legacy_file(program_directory, data_directory, file_name);
                AppLog.Info($"Carpeta de datos: {file_name} -> {result}");
            }

            int copied_backups = copy_legacy_backups(
                Path.Combine(program_directory, BACKUPS_FOLDER_NAME),
                Path.Combine(data_directory, BACKUPS_FOLDER_NAME));
            AppLog.Info($"Carpeta de datos: respaldos locales copiados = {copied_backups}");
        }

        public static LegacyCopyResult copy_legacy_file(string source_directory, string target_directory, string file_name)
        {
            if (string.IsNullOrWhiteSpace(source_directory)) throw new ArgumentException("Falta la carpeta de origen.", nameof(source_directory));
            if (string.IsNullOrWhiteSpace(target_directory)) throw new ArgumentException("Falta la carpeta de destino.", nameof(target_directory));
            if (string.IsNullOrWhiteSpace(file_name)) throw new ArgumentException("Falta el nombre del archivo.", nameof(file_name));

            string source_path = Path.Combine(source_directory, file_name);
            string target_path = Path.Combine(target_directory, file_name);

            if (string.Equals(Path.GetFullPath(source_path), Path.GetFullPath(target_path), StringComparison.OrdinalIgnoreCase))
            {
                return LegacyCopyResult.SameFolder;
            }
            if (!File.Exists(source_path))
            {
                return LegacyCopyResult.NothingToCopy;
            }
            if (File.Exists(target_path))
            {
                return LegacyCopyResult.AlreadyInDataFolder;
            }

            try
            {
                Directory.CreateDirectory(target_directory);
                File.Copy(source_path, target_path, overwrite: false);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                AppLog.Error($"Carpeta de datos: no se pudo copiar {file_name} ({exception.GetType().Name})");
                return LegacyCopyResult.Failed;
            }

            if (!File.Exists(target_path))
            {
                throw new InvalidOperationException($"La copia de {file_name} no quedo en la carpeta de datos.");
            }
            return LegacyCopyResult.Copied;
        }

        public static int copy_legacy_backups(string source_backups, string target_backups)
        {
            if (string.IsNullOrWhiteSpace(source_backups)) throw new ArgumentException("Falta la carpeta de origen.", nameof(source_backups));
            if (string.IsNullOrWhiteSpace(target_backups)) throw new ArgumentException("Falta la carpeta de destino.", nameof(target_backups));

            if (!Directory.Exists(source_backups))
            {
                return 0;
            }

            int copied = 0;
            string[] backup_files;
            try
            {
                backup_files = Directory.GetFiles(source_backups);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                AppLog.Error($"Carpeta de datos: no se pudo leer la carpeta de respaldos vieja ({exception.GetType().Name})");
                return 0;
            }

            foreach (string backup_file in backup_files)
            {
                LegacyCopyResult result = copy_legacy_file(source_backups, target_backups, Path.GetFileName(backup_file));
                if (result == LegacyCopyResult.Copied)
                {
                    copied++;
                }
            }
            return copied;
        }
    }
}
