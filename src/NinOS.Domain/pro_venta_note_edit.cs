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

            return Money.round(monto_original * (1m - descuento_porcentaje / 100m));
        }

        public static resultado_edicion evaluar_monto(
            string estado_nota,
            bool relacion_pagada,
            decimal monto_actual,
            decimal monto_esperado,
            decimal monto_nuevo)
        {
            resultado_edicion base_resultado = evaluar_base(estado_nota, relacion_pagada, monto_actual, monto_esperado);
            if (!base_resultado.aceptada) return base_resultado;

            if (monto_nuevo < 0m)
                return resultado_edicion.Rechazar("El monto a cobrar no puede ser negativo.");

            return resultado_edicion.Aceptar();
        }

        public static resultado_edicion evaluar_descuento(
            string estado_nota,
            bool relacion_pagada,
            decimal monto_actual,
            decimal monto_esperado,
            decimal descuento_nuevo)
        {
            resultado_edicion base_resultado = evaluar_base(estado_nota, relacion_pagada, monto_actual, monto_esperado);
            if (!base_resultado.aceptada) return base_resultado;

            if (descuento_nuevo < DESCUENTO_MINIMO || descuento_nuevo > DESCUENTO_MAXIMO)
                return resultado_edicion.Rechazar("El descuento debe estar entre 0 y 100.");

            return resultado_edicion.Aceptar();
        }

        private static resultado_edicion evaluar_base(
            string estado_nota,
            bool relacion_pagada,
            decimal monto_actual,
            decimal monto_esperado)
        {
            string estado = (estado_nota ?? string.Empty).Trim();
            if (string.Equals(estado, "Anulada", StringComparison.OrdinalIgnoreCase)
                || string.Equals(estado, "Devuelta", StringComparison.OrdinalIgnoreCase))
                return resultado_edicion.Rechazar("La nota está anulada o devuelta y no se puede editar.");

            if (relacion_pagada)
                return resultado_edicion.Rechazar("La relación ya está pagada: no se pueden editar sus notas.");

            if (Money.round(monto_actual) != Money.round(monto_esperado))
                return resultado_edicion.Rechazar("La nota cambió desde que se abrió la edición. Se recargarán los datos.");

            return resultado_edicion.Aceptar();
        }

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
    }
}
