using System;
using System.Collections.Generic;
using System.Linq;
using NinOS.Domain;
using NinOS.Domain.ViewModels;

namespace NinOS.UI.Common
{
    public static class ProVentaDetailPdfGenerator
    {
        public static void generate(pro_venta_relation_row row, List<pro_venta_weekly_row> notes, List<payment_dto> payments)
        {
            // Mismo criterio que el servicio: las anuladas se imprimen en la tabla pero no suman.
            // Si se sumaran aqui, el PDF de una relacion imprimiria un TOTAL que no cuadra con el
            // que muestra la pantalla.
            var vigentes = notes.Where(r => !r.esta_anulada).ToList();
            var anuladas = notes.Where(r => r.esta_anulada).ToList();

            var week_dto = new pro_venta_weekly_dto
            {
                id_relacion = row.id_relacion,
                relation_number = row.relation_number,
                week_start = row.week_start,
                week_end = row.week_end,
                city = "MARACAY",
                rows = notes,
                total_amount = Money.round(vigentes.Sum(r => r.amount)),
                total_commission_luis = Money.round(vigentes.Sum(r => r.commission_luis)),
                total_gastos_25 = Money.round(vigentes.Sum(r => r.gastos_25)),
                total_gastos_15 = Money.round(vigentes.Sum(r => r.gastos_15)),
                annulled_amount = Money.round(anuladas.Sum(r => r.amount)),
                annulled_count = anuladas.Count
            };

            decimal nota_por_pagar = Money.round(week_dto.total_amount - week_dto.total_gastos_25 - week_dto.total_gastos_15);
            ProVentaPdfGenerator.generate(week_dto, nota_por_pagar, payments, row);
        }
    }
}