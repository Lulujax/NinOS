using System;

namespace NinOS.Domain
{
    public readonly record struct resultado_edicion(bool aceptada, string motivo)
    {
        public static resultado_edicion Aceptar() => new resultado_edicion(true, string.Empty);

        public static resultado_edicion Rechazar(string motivo) => new resultado_edicion(false, motivo);
    }

    public static class pro_venta_note_edit
    {
        public const decimal DESCUENTO_MINIMO = 0m;
        public const decimal DESCUENTO_MAXIMO = 100m;

        public static decimal monto_desde_descuento(decimal monto_original, decimal descuento_porcentaje)
        {
            validar_monto_original(monto_original);
            validar_descuento(descuento_porcentaje);

            decimal monto = Money.round(monto_original * (1m - descuento_porcentaje / 100m));
            validar_resultado(monto_original, monto);
            return monto;
        }

        public static decimal descuento_desde_monto(decimal monto_original, decimal monto_a_cobrar)
        {
            validar_monto_original(monto_original);
            validar_monto_a_cobrar(monto_original, monto_a_cobrar);

            decimal descuento = Money.round((1m - monto_a_cobrar / monto_original) * 100m);
            validar_descuento(descuento);
            return descuento;
        }

        public static resultado_edicion evaluar_edicion(
            string estado_nota,
            bool relacion_pagada,
            decimal monto_original,
            decimal monto_actual,
            decimal monto_esperado,
            decimal monto_a_cobrar)
        {
            string estado = (estado_nota ?? string.Empty).Trim();
            if (string.Equals(estado, "Anulada", StringComparison.OrdinalIgnoreCase)
                || string.Equals(estado, "Devuelta", StringComparison.OrdinalIgnoreCase))
                return resultado_edicion.Rechazar("La nota está anulada o devuelta y no se puede editar.");

            if (relacion_pagada)
                return resultado_edicion.Rechazar("La relación ya está pagada: no se pueden editar sus notas.");

            if (Money.round(monto_actual) != Money.round(monto_esperado))
                return resultado_edicion.Rechazar("La nota cambió desde que se abrió la edición. Se recargarán los datos.");

            if (monto_a_cobrar < 0m || monto_a_cobrar > monto_original)
                return resultado_edicion.Rechazar(
                    "El monto a cobrar debe estar entre 0,00 y " + texto_monto_maximo(monto_original) + ".");

            return resultado_edicion.Aceptar();
        }

        public static string texto_monto_maximo(decimal monto_original)
            => Money.round(monto_original).ToString("N2", new System.Globalization.CultureInfo("es-VE"));

        private static void validar_monto_original(decimal monto_original)
        {
            if (monto_original <= 0m)
                throw new ArgumentException("El monto original de la nota debe ser mayor que cero.", nameof(monto_original));
        }

        private static void validar_descuento(decimal descuento_porcentaje)
        {
            if (descuento_porcentaje < DESCUENTO_MINIMO || descuento_porcentaje > DESCUENTO_MAXIMO)
                throw new ArgumentOutOfRangeException(nameof(descuento_porcentaje),
                    "El descuento debe estar entre 0 y 100.");
        }

        private static void validar_monto_a_cobrar(decimal monto_original, decimal monto_a_cobrar)
        {
            if (monto_a_cobrar < 0m)
                throw new ArgumentOutOfRangeException(nameof(monto_a_cobrar),
                    "El monto a cobrar no puede ser negativo.");
            if (monto_a_cobrar > monto_original)
                throw new ArgumentOutOfRangeException(nameof(monto_a_cobrar),
                    "El monto a cobrar no puede ser mayor que el monto original ("
                    + texto_monto_maximo(monto_original) + ").");
        }

        private static void validar_resultado(decimal monto_original, decimal monto)
        {
            if (monto < 0m || monto > monto_original)
                throw new InvalidOperationException("El monto calculado queda fuera del rango permitido.");
        }
    }
}
