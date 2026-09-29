using System;
using System.Text;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Common
{
    /// <summary>
    /// Convierte errores tecnicos (de red, base de datos o del sistema) en textos
    /// que un usuario sin conocimientos de informatica pueda entender y resolver.
    /// Ademas deja constancia en el archivo de log, porque todo error que se le
    /// muestra al usuario tiene que quedar registrado para poder investigarlo.
    /// </summary>
    public static class ErrorText
    {
        public static string Get(Exception? exception, string? contexto = null)
        {
            if (exception == null)
            {
                AppLog.Error("Error mostrado al usuario" + describe(contexto) +
                             " sin detalle (excepcion nula).");
                return "Ocurrió un error inesperado. Inténtalo de nuevo.";
            }

            AppLog.Error("Error mostrado al usuario" + describe(contexto) +
                         " -> " + exception.GetType().Name + ": " + exception.Message,
                         exception);

            string original = exception.Message ?? string.Empty;
            string chain = build_chain_text(exception);

            // ---------- 1. Conexion e internet ----------
            if (matches(chain,
                "no such host",
                "name or service not known",
                "hostbynameresolve",
                "could not resolve host",
                "name resolution",
                "dns"))
                return "No hay conexión a internet o el servidor no está disponible.\nRevisa tu conexión (cable o wifi) e inténtalo de nuevo.";

            if (matches(chain,
                "connection refused",
                "failed to connect",
                "unable to connect",
                "could not connect",
                "no se pudo establecer",
                "error establishing a connection",
                "connection reset",
                "connection terminated",
                "connection was closed",
                "broken pipe",
                "no connection could be made",
                "servidor no está disponible"))
                return "No se pudo conectar con el servidor de la base de datos.\nRevisa tu conexión a internet e inténtalo de nuevo.";

            if (matches(chain,
                "timeout",
                "timed out",
                "tiempo de espera",
                "execution timeout"))
                return "El servidor tardó demasiado en responder.\nRevisa tu conexión a internet e inténtalo de nuevo.";

            if (matches(chain,
                "password authentication failed",
                "authentication failed",
                "pg_hba",
                "role \"",
                "no password supplied",
                "sslpassword"))
                return "No se pudo iniciar sesión en la base de datos.\nVerifica que el internet esté bien conectado e inténtalo de nuevo.";

            if (chain.Contains("database") && (chain.Contains("does not exist") || chain.Contains("no existe")))
                return "No se encontró la base de datos del sistema.\nAvisa al administrador.";

            if (matches(chain, "too many clients", "too many connections"))
                return "El servidor está ocupado en este momento.\nEspera un momento e inténtalo de nuevo.";

            if (matches(chain,
                "certificate",
                "ssl connection",
                "could not load ssl",
                "truststore"))
                return "No se pudo establecer una conexión segura con el servidor.\nAvisa al administrador.";

            if (matches(chain,
                "read-only file system",
                "server closed the connection unexpectedly",
                "terminating connection",
                "database system is",
                "out of memory"))
                return "El servidor se desconectó y no pudo completar la operación.\nEspera un momento e inténtalo de nuevo.";

            // ---------- 2. Datos repetidos o relacionados ----------
            if (chain.Contains("correlativo_duplicado") || chain.Contains("correlativo duplicado"))
                return "Ya existe una nota con ese correlativo. Revisa el número e inténtalo de nuevo.";

            if (matches(chain,
                "unique constraint failed",
                "duplicate key",
                "uniqueconstraint",
                "unique_violation",
                "23505"))
                return "Ya existe un registro con esos datos (código, número o referencia repetidos).\nVerifica e inténtalo de nuevo.";

            if (matches(chain,
                "foreign key",
                "reference constraint",
                "conflicted with",
                "23503"))
                return "No se puede completar la operación porque este registro ya tiene información asociada.";

            if (matches(chain,
                "23502",
                "null value in column",
                "not null constraint"))
                return "Faltó completar un dato obligatorio. Revisa el formulario e inténtalo de nuevo.";

            if (matches(chain,
                "22001",
                "value too long",
                "character varying",
                "character(1)",
                "string_data_right_truncation"))
                return "Alguno de los datos escritos es demasiado largo para el campo.\nEscríbelo más corto e inténtalo de nuevo.";

            if (matches(chain,
                "22p02",
                "invalid input syntax",
                "could not parse",
                "is not a valid",
                "format exception",
                "not in a correct format",
                "input string"))
                return "Revisa los datos escritos: alguno no tiene el formato correcto.\nPor ejemplo, las cantidades y los precios deben ser números.";

            if (matches(chain,
                "value was either too large",
                "does not fall within the expected range",
                "out of range",
                "overflow",
                "22003",
                "argumentexception"))
                return "Alguno de los valores escritos está fuera del rango permitido.\nRevisa cantidades, precios o saldos e inténtalo de nuevo.";

            if (matches(chain,
                "divide by zero",
                "división por cero",
                "divison por cero"))
                return "No se pudo hacer el cálculo porque falta un valor.\nRevisa los datos e inténtalo de nuevo.";

            // ---------- 3. Informacion no encontrada en el sistema ----------
            if (matches(chain,
                "sequence contains no matching element",
                "no element matches the predicate",
                "sequence contains no elements"))
                return "No se encontró la información necesaria para completar la operación.\nVuelve a cargarlo e inténtalo de nuevo.";

            if (matches(chain,
                "object reference not set",
                "nullreferenceexception"))
                return "Ocurrió un problema al procesar la información.\nInténtalo de nuevo.";

            if (matches(chain,
                " with id ",
                " no encontrado",
                " no encontrada"))
                return "No se encontró el registro solicitado.";

            // ---------- 4. Archivos, carpetas e impresoras ----------
            if (matches(chain,
                "being used by another process",
                "the process cannot access the file",
                "used by another",
                "archivo está siendo utilizado",
                "access denied on closing"))
                return "El archivo está abierto en otro programa.\nCiérralo e inténtalo de nuevo.";

            if (matches(chain,
                "unauthorizedaccess",
                "access to the path",
                "permission denied",
                "no se tiene permiso"))
                return "No se tiene permiso para usar esa carpeta o archivo.\nElige otra ubicación, por ejemplo el Escritorio.";

            if (matches(chain,
                "no such file or directory",
                "could not find file",
                "directorio no encontrado",
                "no se encontró el archivo"))
                return "No se encontró el archivo o la carpeta indicada.";

            if (matches(chain,
                "printer",
                "impresora",
                "no printers are installed"))
                return "No se encontró ninguna impresora.\nRevisa las impresoras instaladas en Windows e inténtalo de nuevo.";

            // ---------- 5. Mensajes que ya estan redactados para el usuario ----------
            if (matches(chain,
                "stock insuficiente",
                "inventario insuficiente",
                "no se puede",
                "no puede",
                "ya existe",
                "ya fue",
                "ya está",
                "ya es",
                "no existe",
                "no hay",
                "no tiene",
                "no pertenece",
                "no se encontro",
                "no va anclada",
                "no encontrado",
                "no encontrada",
                "requiere una nota",
                "es demasiado",
                "es obligatoria",
                "es invalida",
                "debe ",
                "deben ser",
                "tienes que",
                "esta repetido",
                "hay un renglon",
                "supera lo entregado",
                "se gestionan en el modulo",
                "no es",
                "revise",
                "verifique"))
                return Capitalize(original);

            // ---------- 6. Ultimo recurso ----------
            if (matches(chain,
                "npgsql",
                "postgresexception",
                "dbupdateexception",
                "dbcontext",
                "entityframework",
                "sqlite",
                "base de datos"))
                return "No se pudo completar la operación por un problema con la base de datos.\nInténtalo de nuevo y, si sigue igual, avisa al administrador.";

            if (matches(chain, "operationcanceled", "operation was canceled", "tarea cancelada"))
                return "La operación fue cancelada.";

            if (matches(chain, "objectdisposed", "disposed object"))
                return "Ocurrió un error inesperado al actualizar los datos.\nInténtalo de nuevo.";

            return "Ocurrió un error inesperado y la operación no se completó.\nInténtalo de nuevo y, si el problema continúa, avisa al administrador.";
        }

        private static string describe(string? contexto)
        {
            return string.IsNullOrWhiteSpace(contexto) ? string.Empty : " [" + contexto + "]";
        }

        private static bool matches(string chain, params string[] needles)
        {
            foreach (string needle in needles)
            {
                if (chain.Contains(needle)) return true;
            }
            return false;
        }

        private static string build_chain_text(Exception? exception)
        {
            var text = new StringBuilder();
            Exception? current = exception;
            int depth = 0;

            while (current != null && depth < 8)
            {
                text.Append(current.GetType().Name).Append(' ');
                text.Append(current.Message).Append(' ');

                if (current is AggregateException aggregate && aggregate.InnerExceptions.Count > 0)
                {
                    foreach (Exception inner in aggregate.InnerExceptions)
                    {
                        text.Append(inner.GetType().Name).Append(' ');
                        text.Append(inner.Message).Append(' ');
                    }
                }

                current = current.InnerException;
                depth++;
            }

            return text.ToString().ToLowerInvariant();
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }
    }
}
