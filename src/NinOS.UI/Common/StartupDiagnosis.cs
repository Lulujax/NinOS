using System;
using System.Net.Sockets;
using System.Reflection;
using NinOS.Infrastructure.Data;

namespace NinOS.UI.Common
{
    /// <summary>Por que no pudo arrancar el sistema.</summary>
    public enum startup_failure_cause
    {
        sin_configuracion,
        sin_conexion,
        credenciales,
        base_inexistente,
        permisos,
        migracion,
        desconocida
    }

    public sealed class startup_diagnosis
    {
        public startup_failure_cause cause { get; init; }
        public string mensaje { get; init; } = string.Empty;

        /// <summary>True cuando el usuario puede resolverlo solo (internet) y reintentar sirve.</summary>
        public bool vale_la_pena_reintentar => cause == startup_failure_cause.sin_conexion;
    }

    /// <summary>
    /// Traduce el error del arranque a una causa y un mensaje entendible.
    ///
    /// Antes la pantalla de carga decia siempre "Verifique su conexion a internet", aunque
    /// el problema fuera la contrasena, la base inexistente o una migracion fallida. Eso
    /// hacia perder tiempo buscando en el lugar equivocado.
    ///
    /// Es una funcion pura: no toca la base ni la pantalla, asi que se puede probar sola.
    /// </summary>
    public static class StartupDiagnosis
    {
        // Codigos de error de PostgreSQL (SQLSTATE).
        private const string ClaveIncorrecta = "28P01";
        private const string AutorizacionInvalida = "28000";
        private const string BaseInexistente = "3D000";
        private const string SinPermisos = "42501";

        public static startup_diagnosis describe(Exception? exception)
        {
            if (exception == null)
            {
                return new startup_diagnosis
                {
                    cause = startup_failure_cause.desconocida,
                    mensaje = "No se pudo iniciar el sistema y no quedo registrado el motivo. Avise al administrador."
                };
            }

            foreach (Exception actual in cadena_de_errores(exception))
            {
                startup_failure_cause? encontrada = clasificar(actual);
                if (encontrada.HasValue) return construir(encontrada.Value, exception);
            }

            return construir(startup_failure_cause.desconocida, exception);
        }

        private static startup_failure_cause? clasificar(Exception exception)
        {
            if (es_falta_de_configuracion(exception)) return startup_failure_cause.sin_configuracion;

            string? estado = codigo_de_postgres(exception);
            if (estado == ClaveIncorrecta || estado == AutorizacionInvalida) return startup_failure_cause.credenciales;
            if (estado == BaseInexistente) return startup_failure_cause.base_inexistente;
            if (estado == SinPermisos) return startup_failure_cause.permisos;

            if (exception is SocketException || exception is TimeoutException) return startup_failure_cause.sin_conexion;

            if (es_de_migracion(exception)) return startup_failure_cause.migracion;

            return null;
        }

        private static bool es_falta_de_configuracion(Exception exception)
        {
            return exception is InvalidOperationException
                && string.Equals(exception.Message, DbConnectionFactory.MissingConfigurationMessage, StringComparison.Ordinal);
        }

        private static bool es_de_migracion(Exception exception)
        {
            string mensaje = exception.Message ?? string.Empty;

            return mensaje.Contains("__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase)
                || mensaje.Contains("migration", StringComparison.OrdinalIgnoreCase)
                || mensaje.Contains("migracion", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Lee el SQLSTATE de un error de PostgreSQL sin depender del tipo concreto de Npgsql,
        /// para que esta clase no arrastre la libreria de base de datos.
        /// </summary>
        private static string? codigo_de_postgres(Exception exception)
        {
            PropertyInfo? propiedad = exception.GetType().GetProperty("SqlState");
            return propiedad?.GetValue(exception) as string;
        }

        private static System.Collections.Generic.IEnumerable<Exception> cadena_de_errores(Exception exception)
        {
            Exception? actual = exception;
            int vueltas = 0;

            while (actual != null && vueltas < 20)
            {
                yield return actual;

                if (actual is AggregateException agrupado)
                {
                    foreach (Exception interno in agrupado.InnerExceptions)
                    {
                        foreach (Exception anidado in cadena_de_errores(interno))
                        {
                            yield return anidado;
                        }
                    }
                    yield break;
                }

                actual = actual.InnerException;
                vueltas++;
            }
        }

        private static startup_diagnosis construir(startup_failure_cause cause, Exception exception)
        {
            string mensaje = cause switch
            {
                startup_failure_cause.sin_configuracion =>
                    "NinOS no tiene configurada la conexion con el sistema. Avise al administrador.",

                startup_failure_cause.sin_conexion =>
                    "No se pudo conectar con el sistema. Puede que no haya internet o que la conexion este lenta o inestable. Revise su internet, espere unos segundos y vuelva a intentar.",

                startup_failure_cause.credenciales =>
                    "No se pudo acceder al sistema con los datos configurados. Avise al administrador.",

                startup_failure_cause.base_inexistente =>
                    "No se encontro la informacion del sistema en el servidor. Avise al administrador.",

                startup_failure_cause.permisos =>
                    "Este equipo no tiene permisos suficientes para usar el sistema. Avise al administrador.",

                startup_failure_cause.migracion =>
                    "No se pudo terminar de actualizar el sistema. Avise al administrador antes de seguir usandolo.",

                _ => "Ocurrio un error al iniciar el sistema. Avise al administrador del sistema."
            };

            return new startup_diagnosis { cause = cause, mensaje = mensaje };
        }
    }
}
