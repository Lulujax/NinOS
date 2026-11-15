using System;

namespace NinOS.Domain
{
    public class note_discount_result
    {
        public decimal condition_amount { get; }
        public decimal volume_amount { get; }
        public decimal total_amount_usd { get; }

        public note_discount_result(decimal condition_amount, decimal volume_amount, decimal total_amount_usd)
        {
            this.condition_amount = condition_amount;
            this.volume_amount = volume_amount;
            this.total_amount_usd = total_amount_usd;
        }
    }

    public static class note_discount_calculator
    {
        public static note_discount_result calculate(decimal base_amount, decimal condition_percentage, decimal volume_percentage)
        {
            if (base_amount < 0)
                throw new ArgumentException("El monto base no puede ser negativo.", nameof(base_amount));
            if (condition_percentage < 0 || condition_percentage > 100)
                throw new ArgumentException("El descuento de condicion debe estar entre 0 y 100.", nameof(condition_percentage));
            if (volume_percentage < 0 || volume_percentage > 100)
                throw new ArgumentException("El descuento de volumen debe estar entre 0 y 100.", nameof(volume_percentage));

            decimal condition_amount = Money.round(base_amount * condition_percentage / 100m);
            decimal volume_amount = Money.round(base_amount * volume_percentage / 100m);
            decimal total_amount_usd = Money.round(base_amount - condition_amount - volume_amount);

            return new note_discount_result(condition_amount, volume_amount, total_amount_usd);
        }
    }
}
