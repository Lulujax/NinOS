using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using NinOS.Domain.ViewModels;

namespace NinOS.UI.Common
{
    public static class ProVentaRelationsSummaryExcelExporter
    {
        public static void export(
            string reportTitle,
            string monthLabel,
            List<pro_venta_relation_row>? pendingRows,
            List<pro_venta_relation_row>? paidRows)
        {
            var pending = pendingRows ?? new List<pro_venta_relation_row>();
            var paid = paidRows ?? new List<pro_venta_relation_row>();

            if (pending.Count == 0 && paid.Count == 0) return;

            using var wb = new XLWorkbook();

            if (pending.Count > 0)
            {
                var ws = wb.Worksheets.Add("Relaciones Pendientes");
                PopulateSheet(ws, pending, "PENDIENTES");
            }

            if (paid.Count > 0)
            {
                var ws = wb.Worksheets.Add("Relaciones Pagadas");
                PopulateSheet(ws, paid, "PAGADAS");
            }

            string defaultName = Path.GetFileNameWithoutExtension(ProVentaRelationsSummaryPdfGenerator.get_default_file_name(reportTitle, monthLabel)) + ".xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar reporte de relaciones Pro Venta en Excel");
        }

        private static void PopulateSheet(IXLWorksheet ws, List<pro_venta_relation_row> rows, string titleTag)
        {
            string[] headers = new[]
            {
                "RELACION NRO", "SEMANA INICIO", "SEMANA FIN",
                "MONTO POR PAGAR ($)", "ABONADO ($)", "SALDO PENDIENTE ($)", "ESTADO"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, i + 1), ExcelFlatExporter.DarkBlueHeader);
            }

            int rowIdx = 2;
            foreach (var r in rows.OrderBy(x => x.relation_number))
            {
                int c = 1;
                ws.Cell(rowIdx, c++).SetValue(r.relation_label);
                ws.Cell(rowIdx, c++).SetValue(r.week_start.ToString("dd/MM/yyyy"));
                ws.Cell(rowIdx, c++).SetValue(r.week_end.ToString("dd/MM/yyyy"));

                var amtCell = ws.Cell(rowIdx, c++);
                amtCell.SetValue(r.amount);
                amtCell.Style.NumberFormat.Format = "#,##0.00";

                var paidCell = ws.Cell(rowIdx, c++);
                paidCell.SetValue(r.paid_amount_usd);
                paidCell.Style.NumberFormat.Format = "#,##0.00";

                var balCell = ws.Cell(rowIdx, c++);
                balCell.SetValue(r.balance_due_usd);
                balCell.Style.NumberFormat.Format = "#,##0.00";

                ws.Cell(rowIdx, c++).SetValue(r.status);

                rowIdx++;
            }

            ws.Cell(rowIdx, 1).SetValue($"TOTAL {titleTag}");
            ws.Cell(rowIdx, 4).SetValue(rows.Sum(x => x.amount));
            ws.Cell(rowIdx, 4).Style.NumberFormat.Format = "#,##0.00";

            ws.Cell(rowIdx, 5).SetValue(rows.Sum(x => x.paid_amount_usd));
            ws.Cell(rowIdx, 5).Style.NumberFormat.Format = "#,##0.00";

            ws.Cell(rowIdx, 6).SetValue(rows.Sum(x => x.balance_due_usd));
            ws.Cell(rowIdx, 6).Style.NumberFormat.Format = "#,##0.00";

            ExcelFlatExporter.StyleTotalRow(ws.Range(rowIdx, 1, rowIdx, headers.Length), ExcelFlatExporter.BlueSoftBg);
            ExcelFlatExporter.FormatTable(ws, headerRow: 1);
        }
    }
}
