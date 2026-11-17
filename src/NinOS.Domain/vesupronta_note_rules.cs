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
        public const string LEYENDA_RECEPCION = "Certifico haber recibido total y materialmente la mercancia indicada en esta nota de despacho";

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
