using System;

namespace NinOS.Domain
{
    public static class RelationBalance
    {
        public static decimal raw_balance(decimal amount, decimal paid_amount_usd)
        {
            if (amount < 0)
                throw new ArgumentException("El monto de la relacion no puede ser negativo.", nameof(amount));
            if (paid_amount_usd < 0)
                throw new ArgumentException("Lo abonado no puede ser negativo.", nameof(paid_amount_usd));

            return Money.round(amount - paid_amount_usd);
        }

        public static decimal balance_due(decimal amount, decimal paid_amount_usd)
        {
            decimal balance = raw_balance(amount, paid_amount_usd);
            return balance < 0 ? 0 : balance;
        }
    }
}
