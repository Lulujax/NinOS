using System;

namespace NinOS.Domain
{
    public readonly record struct liquidacion_vesupronta(
        decimal total_general,
        decimal veinticinco_por_ciento_lista,
        decimal total_general_menos_veinticinco,
        decimal quince_por_ciento_lista,
        decimal total_a_pagar_pronta,
        decimal diferencial);

    public static class vesupronta_note_rules
    {
        public const int MAXIMO_PRODUCTOS = 20;

        public static void validar_cantidad_productos(string? note_type_code, int cantidad_renglones)
        {
            if (!NoteTypeCodes.is_vesupronta(note_type_code)) return;
            if (cantidad_renglones < 1)
                throw new InvalidOperationException("La nota no tiene productos.");
            if (cantidad_renglones > MAXIMO_PRODUCTOS)
                throw new InvalidOperationException($"Una nota de Vesupronta admite como máximo {MAXIMO_PRODUCTOS} productos.");
        }

        public static liquidacion_vesupronta calcular_liquidacion(decimal total_general)
        {
            if (total_general < 0m)
                throw new ArgumentOutOfRangeException(nameof(total_general), "El total general no puede ser negativo.");

            decimal total = redondear(total_general);
            decimal veinticinco = redondear(total * 0.25m);
            decimal menos_veinticinco = total - veinticinco;
            decimal quince = redondear(menos_veinticinco * 0.15m);
            decimal a_pagar = menos_veinticinco - quince;
            decimal diferencial = total - a_pagar;

            return new liquidacion_vesupronta(total, veinticinco, menos_veinticinco, quince, a_pagar, diferencial);
        }

        private static decimal redondear(decimal valor)
            => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }
}
