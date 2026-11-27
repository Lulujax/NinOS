using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Common
{
    public static class AdminElevation
    {
        public const int ERROR_CANCELLED_BY_USER = 1223;

        public const string RUNAS_VERB = "runas";

        public const string MissingPermissionsMessage =
            "NinOS necesita permisos de administrador para abrir.\n\n" +
            "Vuelva a abrir el sistema y responda «Sí» cuando Windows pregunte si permite que haga cambios.";

        public static bool is_running_as_admin()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static bool must_relaunch_elevated(bool is_admin, bool is_installed_app)
        {
            return is_installed_app && !is_admin;
        }

        public static string quote_argument(string argument)
        {
            if (argument == null) throw new ArgumentNullException(nameof(argument));
            if (argument.Length > 0 && argument.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
            {
                return argument;
            }

            StringBuilder quoted = new StringBuilder("\"");
            int pending_backslashes = 0;
            foreach (char character in argument)
            {
                if (character == '\\')
                {
                    pending_backslashes++;
                    continue;
                }
                if (character == '"')
                {
                    quoted.Append('\\', pending_backslashes * 2 + 1).Append('"');
                }
                else
                {
                    quoted.Append('\\', pending_backslashes).Append(character);
                }
                pending_backslashes = 0;
            }
            quoted.Append('\\', pending_backslashes * 2).Append('"');
            return quoted.ToString();
        }

        public static ProcessStartInfo build_elevated_start(string executable_path, string[] arguments)
        {
            if (string.IsNullOrWhiteSpace(executable_path))
            {
                throw new ArgumentException("Falta la ruta del ejecutable.", nameof(executable_path));
            }
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));

            ProcessStartInfo start_info = new ProcessStartInfo(executable_path)
            {
                UseShellExecute = true,
                Verb = RUNAS_VERB,
                Arguments = string.Join(" ", arguments.Select(quote_argument)),
                WorkingDirectory = Path.GetDirectoryName(executable_path) ?? string.Empty
            };

            if (!start_info.UseShellExecute || start_info.Verb != RUNAS_VERB)
            {
                throw new InvalidOperationException("El arranque elevado quedo mal armado.");
            }
            return start_info;
        }

        public static bool try_relaunch_elevated(string[] arguments)
        {
            string? executable_path = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executable_path) || !File.Exists(executable_path))
            {
                AppLog.Error("Permisos: no se encontro el ejecutable para reabrir con permisos de administrador");
                return false;
            }

            try
            {
                AppLog.Info($"Permisos: reabriendo con permisos de administrador ({arguments.Length} argumentos)");
                Process.Start(build_elevated_start(executable_path, arguments));
                return true;
            }
            catch (Win32Exception exception) when (exception.NativeErrorCode == ERROR_CANCELLED_BY_USER)
            {
                AppLog.Warn("Permisos: el usuario rechazo el aviso de permisos de administrador");
                return false;
            }
            catch (Win32Exception exception)
            {
                AppLog.Error($"Permisos: no se pudo reabrir con permisos de administrador (codigo {exception.NativeErrorCode})");
                return false;
            }
        }
    }
}
