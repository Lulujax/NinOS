using System;

namespace NinOS.Domain
{
    public static class liquidacion_pro_venta
    {
        public static decimal total_cobrado(decimal total_monto_original, decimal comision_luis)
        {
            validar_no_negativo(total_monto_original, nameof(total_monto_original));
            validar_no_negativo(comision_luis, nameof(comision_luis));
            return Money.round(total_monto_original - comision_luis);
        }

        public static decimal nota_por_pagar(decimal total_monto_original, decimal gastos_operativos, decimal gastos_administrativos)
        {
            validar_no_negativo(total_monto_original, nameof(total_monto_original));
            validar_no_negativo(gastos_operativos, nameof(gastos_operativos));
            validar_no_negativo(gastos_administrativos, nameof(gastos_administrativos));
            return Money.round(total_monto_original - gastos_operativos - gastos_administrativos);
        }

        public static decimal diferencial(decimal total_cobrado, decimal nota_por_pagar)
            => Money.round(total_cobrado - nota_por_pagar);

        private static void validar_no_negativo(decimal valor, string nombre)
        {
            if (valor < 0m)
                throw new ArgumentOutOfRangeException(nombre, "La liquidación no acepta montos negativos.");
        }
    }
}
