using System;
using System.IO;

namespace NinOS.Infrastructure.Logging
{
    /// <summary>
    /// Registra toda la actividad de la aplicacion en un archivo .log
    /// ubicado junto al ejecutable.
    /// </summary>
    public enum AppLogLevel
    {
        Info = 0,
        Warn = 1,
        Error = 2
    }

    public static class AppLog
    {
        public const string EnvironmentVariableName = "NINOS_LOG_LEVEL";

        private static readonly object _sync = new object();
        private static string? _file_path;
        private static AppLogLevel? _min_level;

        public static string FilePath
        {
            get
            {
                if (_file_path == null)
                {
                    _file_path = Path.Combine(AppContext.BaseDirectory, "ninos-ui.log");
                }
                return _file_path;
            }
        }

        public static AppLogLevel MinLevel
        {
            get
            {
                if (_min_level == null)
                {
                    string? raw = Environment.GetEnvironmentVariable(EnvironmentVariableName);
                    _min_level = Enum.TryParse<AppLogLevel>(raw, ignoreCase: true, out var parsed)
                        ? parsed
                        : AppLogLevel.Info;
                }
                return _min_level.Value;
            }
        }

        public static void Info(string message) => Write(AppLogLevel.Info, "INFO", message, null);

        public static void Warn(string message) => Write(AppLogLevel.Warn, "WARN", message, null);

        public static void Error(string message, Exception? exception = null) => Write(AppLogLevel.Error, "ERROR", message, exception);

        private static void Write(AppLogLevel level, string level_label, string message, Exception? exception)
        {
            if (level < MinLevel) return;

            try
            {
                lock (_sync)
                {
                    string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level_label}] {message}";

                    if (exception != null)
                    {
                        entry += Environment.NewLine + exception;
                    }

                    File.AppendAllText(FilePath, entry + Environment.NewLine);
                }
            }
            catch
            {
                // el logging nunca debe derribar la aplicacion
            }
        }
    }
}