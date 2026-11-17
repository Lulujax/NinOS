using System;
using System.Collections.Generic;
using System.Linq;

namespace NinOS.Domain
{
    public readonly record struct movimiento_con_fecha(DateTime instante_utc, decimal monto);

    public static class relation_balance_at
    {
        public static resumen_relacion calcular(
            DateTime fecha_corte_local,
            IReadOnlyList<movimiento_con_fecha> notas,
            IReadOnlyList<movimiento_con_fecha> abonos,
            bool es_heredada,
            decimal saldo_heredado)
        {
            ArgumentNullException.ThrowIfNull(notas);
            ArgumentNullException.ThrowIfNull(abonos);
            if (fecha_corte_local == default)
                throw new ArgumentException("La fecha de corte no puede estar vacía.", nameof(fecha_corte_local));

            DateTime dia_corte = fecha_corte_local.Date;

            IReadOnlyList<decimal> montos_notas = notas
                .Where(n => hasta_el_corte(n.instante_utc, dia_corte))
                .Select(n => n.monto)
                .ToList();

            IReadOnlyList<decimal> montos_abonos = abonos
                .Where(a => hasta_el_corte(a.instante_utc, dia_corte))
                .Select(a => a.monto)
                .ToList();

            return relation_amounts.calcular(montos_notas, montos_abonos, es_heredada, saldo_heredado);
        }

        private static bool hasta_el_corte(DateTime instante_utc, DateTime dia_corte)
            => AppTimeZone.to_local(instante_utc).Date <= dia_corte;
    }
}
