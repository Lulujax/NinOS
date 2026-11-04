using System;
using System.Collections.Generic;
using ClosedXML.Excel;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Common
{
    public static class CommissionsReportExcelExporter
    {
        public static void export(string periodLabel, string filter, string? sellerScope, List<commission_row_dto> rows)
        {
            if (rows == null || rows.Count == 0) return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Comisiones");

            string[] headers = new[]
            {
                "NRO NOTA", "CLIENTE", "VENDEDOR", "FECHA PAGO", "MONTO FACTURADO ($)",
                "MONTO PAGADO ($)", "PRONTO PAGO", "COMISION %", "TOTAL COMISION ($)",
                "PAGO COMISION ($)", "SALDO ($)", "ESTADO"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, i + 1));
            }

            int rowIdx = 2;
            foreach (var r in rows)
            {
                int c = 1;
                ws.Cell(rowIdx, c++).SetValue(r.note_number);
                ws.Cell(rowIdx, c++).SetValue(r.customer_name);
                ws.Cell(rowIdx, c++).SetValue(r.seller_name);
                ws.Cell(rowIdx, c++).SetValue(r.note_last_payment_date?.ToString("dd/MM/yyyy") ?? "-");

                var factCell = ws.Cell(rowIdx, c++);
                factCell.SetValue(r.invoiced_amount);
                factCell.Style.NumberFormat.Format = "#,##0.00";

                var pagCell = ws.Cell(rowIdx, c++);
                pagCell.SetValue(r.paid_amount);
                pagCell.Style.NumberFormat.Format = "#,##0.00";

                ws.Cell(rowIdx, c++).SetValue(r.early_payment_discount > 0 ? $"${r.early_payment_discount:N2}" : "-");
                ws.Cell(rowIdx, c++).SetValue(r.commission_percentage_text);

                var comCell = ws.Cell(rowIdx, c++);
                comCell.SetValue(r.amount_usd);
                comCell.Style.NumberFormat.Format = "#,##0.00";

                var pagComCell = ws.Cell(rowIdx, c++);
                pagComCell.SetValue(r.paid_amount_usd);
                pagComCell.Style.NumberFormat.Format = "#,##0.00";

                var salCell = ws.Cell(rowIdx, c++);
                salCell.SetValue(r.remaining_amount_usd);
                salCell.Style.NumberFormat.Format = "#,##0.00";

                ws.Cell(rowIdx, c++).SetValue(r.status);

                rowIdx++;
            }

            // Totales
            ws.Cell(rowIdx, 1).SetValue("TOTALES");
            ws.Cell(rowIdx, 5).FormulaA1 = $"SUM(E2:E{rowIdx - 1})";
            ws.Cell(rowIdx, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(rowIdx, 6).FormulaA1 = $"SUM(F2:F{rowIdx - 1})";
            ws.Cell(rowIdx, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(rowIdx, 9).FormulaA1 = $"SUM(I2:I{rowIdx - 1})";
            ws.Cell(rowIdx, 9).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(rowIdx, 10).FormulaA1 = $"SUM(J2:J{rowIdx - 1})";
            ws.Cell(rowIdx, 10).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(rowIdx, 11).FormulaA1 = $"SUM(K2:K{rowIdx - 1})";
            ws.Cell(rowIdx, 11).Style.NumberFormat.Format = "#,##0.00";
            ExcelFlatExporter.StyleTotalRow(ws.Range(rowIdx, 1, rowIdx, headers.Length));

            ExcelFlatExporter.FormatTable(ws, headerRow: 1);

            string defaultName = $"Reporte_Comisiones_{periodLabel.Replace(" ", "_")}.xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar reporte de comisiones en Excel");
        }
    }
}
