using System;
using System.IO;

namespace NinOS.Infrastructure.Logging
{
    /// <summary>
    /// Registra toda la actividad de la aplicacion en un archivo .log
    /// ubicado junto al ejecutable.
    /// </summary>
    public static class AppLog
    {
        private static readonly object _sync = new object();
        private static string? _file_path;

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

        public static void Info(string message) => Write("INFO", message, null);

        public static void Warn(string message) => Write("WARN", message, null);

        public static void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

        private static void Write(string level, string message, Exception? exception)
        {
            try
            {
                lock (_sync)
                {
                    string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

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