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
        public const string Vesupronta = "VES";
        public const string PromocionVesupronta = "VESPRM";
        public const string VolumenVesupronta = "VESVOL";

        public static readonly string[] pro_venta_codes = { ProVenta, PromocionProVenta, VolumenProVenta };

        public static bool is_pro_venta(string? code)
        {
            return string.Equals(code, ProVenta, StringComparison.OrdinalIgnoreCase)
                || string.Equals(code, PromocionProVenta, StringComparison.OrdinalIgnoreCase)
                || string.Equals(code, VolumenProVenta, StringComparison.OrdinalIgnoreCase);
        }

        public static bool es_volumen(string? code)
        {
            return string.Equals(code, Volumen, StringComparison.OrdinalIgnoreCase)
                || string.Equals(code, VolumenProVenta, StringComparison.OrdinalIgnoreCase)
                || string.Equals(code, VolumenVesupronta, StringComparison.OrdinalIgnoreCase);
        }
    }
}
