using System;

namespace NinOS.Domain
{
    public static class NoteTypeCodes
    {
        public const string General = "GEN";
        public const string ProVenta = "MAR";
        public const string Promocion = "PRM";
        public const string PromocionProVenta = "PVP";

        public static bool is_pro_venta(string? code)
        {
            return string.Equals(code, ProVenta, StringComparison.OrdinalIgnoreCase)
                || string.Equals(code, PromocionProVenta, StringComparison.OrdinalIgnoreCase);
        }
    }
}