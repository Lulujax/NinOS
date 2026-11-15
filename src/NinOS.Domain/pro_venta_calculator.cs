namespace NinOS.Domain
{
    /// <summary>
    /// Porcentajes de Pro Venta. Un solo lugar para que pantalla, reporte semanal, detalle de
    /// relacion, PDF y Excel no puedan discrepar entre si.
    ///
    /// Los tres se calculan POR NOTA sobre el monto original: el monto SIN descuento aplicado.
    /// Antes el reporte semanal usaba el total ya descontado (ej: 292,90 en vez de 325,44) y
    /// los gastos administrativos salian del total completo en vez de lo que queda tras los
    /// operativos.
    /// </summary>
    public static class pro_venta_calculator
    {
        public const string Ciudad = "MARACAY";

        public const decimal PorcentajeComision = 0.10m;
        public const decimal PorcentajeGastosOperativos = 0.25m;
        public const decimal PorcentajeGastosAdministrativos = 0.15m;

        public static decimal comision(decimal monto_original)
            => Money.round(sanear(monto_original) * PorcentajeComision);

        public static decimal gastos_operativos(decimal monto_original)
            => Money.round(sanear(monto_original) * PorcentajeGastosOperativos);

        /// <summary>15% de lo que queda del monto original despues de los gastos operativos.</summary>
        public static decimal gastos_administrativos(decimal monto_original)
        {
            decimal limpio = sanear(monto_original);
            decimal resto = limpio - gastos_operativos(limpio);
            return Money.round(resto * PorcentajeGastosAdministrativos);
        }

        /// <summary>Un monto negativo no tiene sentido en una nota: se trata como cero.</summary>
        private static decimal sanear(decimal monto) => monto < 0m ? 0m : monto;
    }
}
