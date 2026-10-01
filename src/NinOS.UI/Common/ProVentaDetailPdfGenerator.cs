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
            var week_dto = new pro_venta_weekly_dto
            {
                id_relacion = row.id_relacion,
                relation_number = row.relation_number,
                week_start = row.week_start,
                week_end = row.week_end,
                city = "MARACAY",
                rows = notes,
                total_amount = notes.Sum(r => r.amount),
                total_commission_luis = notes.Sum(r => r.commission_luis),
                total_gastos_25 = notes.Sum(r => r.gastos_25),
                total_gastos_15 = notes.Sum(r => r.gastos_15)
            };

            decimal nota_por_pagar = Money.round(week_dto.total_amount - week_dto.total_gastos_25 - week_dto.total_gastos_15);
            ProVentaPdfGenerator.generate(week_dto, nota_por_pagar, payments, row);
        }
    }
}