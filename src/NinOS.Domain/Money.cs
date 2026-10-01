using System;

namespace NinOS.Domain
{
    /// <summary>
    /// Redondeo de montos monetarios a 2 decimales.
    ///
    /// Se fuerza AwayFromZero en vez de usar el redondeo por defecto de .NET (ToEven, "de banco").
    /// En dinero 0.125 tiene que ser 0.13 y no 0.12: con ToEven un 0.125 sube y otro 0.125 baja,
    /// y los totales dejan de cuadrar con la suma de las filas. El caso es real y no teorico, porque
    /// un monto de 1.25 * 10% da 0.125 exacto.
    /// </summary>
    public static class Money
    {
        public const int Decimals = 2;

        public static decimal round(decimal value)
        {
            return Math.Round(value, Decimals, MidpointRounding.AwayFromZero);
        }
    }
}