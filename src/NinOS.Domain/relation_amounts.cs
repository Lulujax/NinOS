using System;
using System.Collections.Generic;
using System.Linq;

namespace NinOS.Domain
{
    public readonly record struct resumen_relacion(
        decimal monto_por_pagar,
        decimal abonado,
        decimal saldo,
        string estado,
        int notas_vigentes)
    {
        public bool esta_pagada => estado == ESTADO_PAGADA;

        public const string ESTADO_PAGADA = "Pagada";
        public const string ESTADO_PENDIENTE = "Pendiente";
    }

    public static class relation_amounts
    {
        private const decimal TOLERANCIA_SALDO = 0.005m;

        public static resumen_relacion calcular(
            IReadOnlyList<decimal> montos_notas_vigentes,
            IReadOnlyList<decimal> abonos,
            bool es_heredada,
            decimal saldo_heredado)
        {
            ArgumentNullException.ThrowIfNull(montos_notas_vigentes);
            ArgumentNullException.ThrowIfNull(abonos);
            validar_montos(montos_notas_vigentes);
            if (saldo_heredado < 0m)
                throw new ArgumentException("El saldo heredado no puede ser negativo.", nameof(saldo_heredado));

            decimal monto = es_heredada
                ? Money.round(saldo_heredado)
                : Money.round(montos_notas_vigentes.Sum());

            decimal abonado = Money.round(abonos.Where(a => a > 0m).Sum());
            decimal saldo_bruto = RelationBalance.raw_balance(monto, abonado);
            decimal saldo = saldo_bruto < 0m ? 0m : saldo_bruto;

            bool sin_notas_por_cobrar = !es_heredada && montos_notas_vigentes.Count == 0;
            bool pagada = sin_notas_por_cobrar || saldo <= TOLERANCIA_SALDO;

            string estado = pagada ? resumen_relacion.ESTADO_PAGADA : resumen_relacion.ESTADO_PENDIENTE;
            return new resumen_relacion(monto, abonado, saldo, estado, montos_notas_vigentes.Count);
        }

        private static void validar_montos(IReadOnlyList<decimal> montos_notas_vigentes)
        {
            if (montos_notas_vigentes.Any(m => m < 0m))
                throw new ArgumentException("El monto de una nota de la relacion no puede ser negativo.", nameof(montos_notas_vigentes));
        }
    }
}
