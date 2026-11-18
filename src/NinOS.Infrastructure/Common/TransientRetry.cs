using System;
using System.Threading.Tasks;
using NinOS.Infrastructure.Logging;

namespace NinOS.Infrastructure.Common
{
    public static class TransientRetry
    {
        public static async Task<T> run_async<T>(Func<Task<T>> operation, string operation_name, int max_attempts)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (string.IsNullOrWhiteSpace(operation_name)) throw new ArgumentException("El nombre de la operacion no puede estar vacio.", nameof(operation_name));
            if (max_attempts < 1) throw new ArgumentOutOfRangeException(nameof(max_attempts), "Debe haber al menos un intento.");

            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception exception) when (attempt < max_attempts && is_transient(exception))
                {
                    AppLog.Warn($"{operation_name}: intento {attempt} de {max_attempts} fallo por la conexion ({exception.GetType().Name}). Se reintenta.");
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));
                }
            }
        }

        private static bool is_transient(Exception exception)
        {
            return exception is TimeoutException
                || exception is System.IO.IOException
                || exception is System.Net.Sockets.SocketException
                || exception is Npgsql.NpgsqlException;
        }
    }
}
