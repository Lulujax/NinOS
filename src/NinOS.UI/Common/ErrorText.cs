using System;
using System.Linq;

namespace NinOS.UI.Common
{
    /// <summary>
    /// Convierte mensajes de excepcion (tecnicos/ingles de base de datos)
    /// en texto amigable para el usuario final.
    /// </summary>
    public static class ErrorText
    {
        public static string Get(Exception? exception)
        {
            string message = exception?.Message ?? string.Empty;
            string text = message.ToLowerInvariant();

            if (text.Contains("correlativo_duplicado"))
                return "Ya existe una nota con ese correlativo. Verifique el número e intente de nuevo.";

            if (text.Contains("unique constraint failed") ||
                text.Contains("duplicate key") ||
                text.Contains("uniqueconstraint"))
                return "Ya existe un registro con esos datos (código, número o referencia repetidos). Verifique e intente de nuevo.";

            if (text.Contains("foreign key") ||
                text.Contains("reference constraint") ||
                text.Contains("conflicted with"))
                return "No se puede completar la operación porque existe información relacionada con este registro.";

            if (text.Contains("sqlite error") ||
                text.Contains("sqliteexception") ||
                text.StartsWith("sqlite"))
                return "Ocurrió un error de base de datos. Revise los datos e intente de nuevo.";

            if (text.Contains(" with id ") ||
                text.Contains(" no encontrado") ||
                text.Contains(" no encontrada"))
                return "No se encontró el registro solicitado.";

            // Mensajes que ya vienen redactados de forma legible para el usuario.
            if (text.Contains("stock insuficiente") ||
                text.Contains("inventario insuficiente") ||
                text.Contains("no se puede") ||
                text.Contains("no puede") ||
                text.Contains("ya existe") ||
                text.Contains("ya fue") ||
                text.Contains("ya está") ||
                text.Contains("ya es") ||
                text.Contains("no existe") ||
                text.Contains("no hay") ||
                text.Contains("no tiene") ||
                text.Contains("no pertenece") ||
                text.Contains("no se encontro") ||
                text.Contains("no va anclada") ||
                text.Contains("no encontrado") ||
                text.Contains("no encontrada") ||
                text.Contains("requiere una nota") ||
                text.Contains("es demasiado") ||
                text.Contains("es obligatoria") ||
                text.Contains("es invalida") ||
                text.Contains("debe ") ||
                text.Contains("deben ser") ||
                text.Contains("tienes que") ||
                text.Contains("esta repetido") ||
                text.Contains("hay un renglon") ||
                text.Contains("supera lo entregado") ||
                text.Contains("se gestionan en el modulo") ||
                text.Contains("no es") ||
                text.Contains("revise") ||
                text.Contains("verifique"))
                return Capitalize(message);

            return "Ocurrió un error inesperado. Intente de nuevo más tarde.";
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }
    }
}