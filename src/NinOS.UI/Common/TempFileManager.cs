using System;
using System.IO;
using System.Threading.Tasks;

namespace NinOS.UI.Common
{
    public static class TempFileManager
    {
        private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "NinOS");

        static TempFileManager()
        {
            try
            {
                if (!Directory.Exists(TempDir))
                {
                    Directory.CreateDirectory(TempDir);
                }
            }
            catch
            {
                // Ignorar error al crear directorio estático
            }
        }

        public static string GetTempFilePath(string prefix = "preview", string extension = ".pdf")
        {
            if (!extension.StartsWith("."))
                extension = "." + extension;

            if (!Directory.Exists(TempDir))
            {
                Directory.CreateDirectory(TempDir);
            }

            string fileName = $"{prefix}_{Guid.NewGuid():N}{extension}";
            return Path.Combine(TempDir, fileName);
        }

        public static void TryDelete(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Puede estar en uso momentáneamente; se limpiará al reiniciar
            }
        }

        public static void CleanupOldFilesAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(TempDir)) return;

                    var cutoff = DateTime.Now.AddHours(-12);
                    var files = Directory.GetFiles(TempDir);
                    foreach (var file in files)
                    {
                        try
                        {
                            var info = new FileInfo(file);
                            if (info.LastWriteTime < cutoff)
                            {
                                info.Delete();
                            }
                        }
                        catch
                        {
                            // Ignorar archivos en uso
                        }
                    }
                }
                catch
                {
                    // Ignorar errores generales de limpieza
                }
            });
        }
    }
}
