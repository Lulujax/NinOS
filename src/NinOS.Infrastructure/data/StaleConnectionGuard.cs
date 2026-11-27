using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using NinOS.Infrastructure.Logging;

namespace NinOS.Infrastructure.Data
{
    public sealed class StaleConnectionGuard : DbConnectionInterceptor
    {
        private static long _last_activity_ticks;

        public override async Task ConnectionOpenedAsync(
            DbConnection connection,
            ConnectionEndEventData event_data,
            CancellationToken cancellation_token = default)
        {
            ArgumentNullException.ThrowIfNull(connection);

            if (should_ping())
            {
                await ensure_alive_async(connection, cancellation_token);
            }

            mark_activity();
        }

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData event_data)
        {
            ArgumentNullException.ThrowIfNull(connection);

            if (should_ping())
            {
                ensure_alive(connection);
            }

            mark_activity();
        }

        public override Task ConnectionClosedAsync(DbConnection connection, ConnectionEndEventData event_data)
        {
            mark_activity();
            return Task.CompletedTask;
        }

        public override void ConnectionClosed(DbConnection connection, ConnectionEndEventData event_data)
        {
            mark_activity();
        }

        private static bool should_ping()
        {
            long ticks = Interlocked.Read(ref _last_activity_ticks);
            DateTime? last_activity = ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
            return ConnectionFreshnessPolicy.needs_ping(last_activity, DateTime.UtcNow, ConnectionFreshnessPolicy.DEFAULT_IDLE_THRESHOLD);
        }

        private static void mark_activity()
        {
            Interlocked.Exchange(ref _last_activity_ticks, DateTime.UtcNow.Ticks);
        }

        private static async Task ensure_alive_async(DbConnection connection, CancellationToken cancellation_token)
        {
            try
            {
                await ping_async(connection, cancellation_token);
                return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                AppLog.Warn($"La conexion guardada estaba caida ({exception.GetType().Name}); abriendo una nueva.");
            }

            discard_pool(connection);
            await connection.CloseAsync();
            await connection.OpenAsync(cancellation_token);
            await ping_async(connection, cancellation_token);
            AppLog.Info("Conexion nueva abierta despues de descartar la caida.");
        }

        private static void ensure_alive(DbConnection connection)
        {
            try
            {
                ping(connection);
                return;
            }
            catch (Exception exception)
            {
                AppLog.Warn($"La conexion guardada estaba caida ({exception.GetType().Name}); abriendo una nueva.");
            }

            discard_pool(connection);
            connection.Close();
            connection.Open();
            ping(connection);
            AppLog.Info("Conexion nueva abierta despues de descartar la caida.");
        }

        private static async Task ping_async(DbConnection connection, CancellationToken cancellation_token)
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;
            await command.ExecuteScalarAsync(cancellation_token);
        }

        private static void ping(DbConnection connection)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;
            command.ExecuteScalar();
        }

        private static void discard_pool(DbConnection connection)
        {
            if (connection is NpgsqlConnection npgsql_connection)
            {
                NpgsqlConnection.ClearPool(npgsql_connection);
            }
        }
    }
}
