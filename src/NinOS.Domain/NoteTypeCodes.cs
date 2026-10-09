using System;

namespace NinOS.Domain
{
    public static class NoteTypeCodes
    {
        public const string General = "GEN";
        public const string ProVenta = "MAR";
        public const string Promocion = "PRM";
        public const string PromocionProVenta = "PVP";
        public const string Volumen = "VOL";
        public const string VolumenProVenta = "VOLMAR";

        /// <summary>
        /// Codigos de la familia Pro Venta. Se usa en consultas de EF Core, donde el
        /// helper <see cref="is_pro_venta"/> no se puede traducir a SQL.
        /// </summary>
        public static readonly string[] pro_venta_codes = { ProVenta, PromocionProVenta, VolumenProVenta };

        /// <summary>
        /// Familia Pro Venta: las zonas Pro Venta trabajan con MAR, PVP y VOLMAR.
        /// </summary>
        public static bool is_pro_venta(string? code)
        {
            return string.Equals(code, ProVenta, StringComparison.OrdinalIgnoreCase)
                || string.Equals(code, PromocionProVenta, StringComparison.OrdinalIgnoreCase)
                || string.Equals(code, VolumenProVenta, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tipos que manejan descuento por volumen (VOL / VOLMAR). Los demas lo tienen
        /// en cero y ni siquiera muestran el campo.
        /// </summary>
        public static bool es_volumen(string? code)
        {
            return string.Equals(code, Volumen, StringComparison.OrdinalIgnoreCase)
                || string.Equals(code, VolumenProVenta, StringComparison.OrdinalIgnoreCase);
        }
    }
}