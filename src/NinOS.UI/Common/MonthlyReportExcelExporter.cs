using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Common;

namespace NinOS.UI.Common
{
    public static class MonthlyReportExcelExporter
    {
        public static void export(monthly_report_dto report)
        {
            if (report == null) return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Reporte");

            int col = 1;
            ws.Cell(1, col).Value = "CORRELATIVO";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++));

            ws.Cell(1, col).Value = "FECHA";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++));

            if (report.show_dispatch_date_column)
            {
                ws.Cell(1, col).Value = "DESPACHO";
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++));
            }

            ws.Cell(1, col).Value = "CLIENTE";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++));

            ws.Cell(1, col).Value = "VENDEDOR";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++));

            ws.Cell(1, col).Value = "ZONA";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++));

            ws.Cell(1, col).Value = "MONTO ($)";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++));

            if (report.show_paid_balance_summary)
            {
                ws.Cell(1, col).Value = "ABONADO ($)";
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++));

                ws.Cell(1, col).Value = "SALDO ($)";
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++));
            }

            ws.Cell(1, col).Value = "ESTADO";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++));

            int totalCols = col - 1;
            int rowIdx = 2;

            var rows = report.rows ?? new System.Collections.Generic.List<monthly_report_row_dto>();
            var srows = rows
                .OrderBy(r => report.ordenar_zombies_primero && r.is_customer_ghost ? 0 : 1)
                .ThenBy(r => r.date)
                .ThenBy(r => SeriesCalculator.ParseCorrelative(r.document_number))
                .ThenBy(r => r.document_number ?? string.Empty)
                .ToList();

            foreach (var r in srows)
            {
                int c = 1;
                ws.Cell(rowIdx, c++).SetValue(r.document_number);
                ws.Cell(rowIdx, c++).SetValue(r.date.ToString("dd/MM/yyyy"));
                if (report.show_dispatch_date_column)
                {
                    ws.Cell(rowIdx, c++).SetValue(r.dispatch_date.HasValue ? r.dispatch_date.Value.ToString("dd/MM/yyyy") : "-");
                }
                ws.Cell(rowIdx, c++).SetValue(r.customer_name);
                ws.Cell(rowIdx, c++).SetValue(r.seller_name);
                ws.Cell(rowIdx, c++).SetValue(r.zone_name);

                var amountCell = ws.Cell(rowIdx, c++);
                amountCell.SetValue(r.amount_usd);
                amountCell.Style.NumberFormat.Format = "#,##0.00";

                if (report.show_paid_balance_summary)
                {
                    var paidCell = ws.Cell(rowIdx, c++);
                    paidCell.SetValue(r.paid_amount_usd);
                    paidCell.Style.NumberFormat.Format = "#,##0.00";

                    var balCell = ws.Cell(rowIdx, c++);
                    balCell.SetValue(r.balance_due_usd);
                    balCell.Style.NumberFormat.Format = "#,##0.00";
                }

                ws.Cell(rowIdx, c++).SetValue(r.status);

                if (string.Equals(r.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase))
                {
                    ws.Range(rowIdx, 1, rowIdx, totalCols).Style.Font.FontColor = XLColor.Red;
                }

                rowIdx++;
            }

            // Fila de totales
            var validRows = srows.Where(r => !string.Equals(r.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase) && !string.Equals(r.status?.Trim(), "Devuelta", StringComparison.OrdinalIgnoreCase)).ToList();
            if (validRows.Count > 0)
            {
                ws.Cell(rowIdx, 1).SetValue("TOTAL VIGENTE");
                int colAmount = report.show_dispatch_date_column ? 7 : 6;
                ws.Cell(rowIdx, colAmount).SetValue(validRows.Sum(x => x.amount_usd));
                ws.Cell(rowIdx, colAmount).Style.NumberFormat.Format = "#,##0.00";

                if (report.show_paid_balance_summary)
                {
                    ws.Cell(rowIdx, colAmount + 1).SetValue(validRows.Sum(x => x.paid_amount_usd));
                    ws.Cell(rowIdx, colAmount + 1).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(rowIdx, colAmount + 2).SetValue(validRows.Sum(x => x.balance_due_usd));
                    ws.Cell(rowIdx, colAmount + 2).Style.NumberFormat.Format = "#,##0.00";
                }

                ExcelFlatExporter.StyleTotalRow(ws.Range(rowIdx, 1, rowIdx, totalCols));
            }

            ExcelFlatExporter.FormatTable(ws, headerRow: 1);

            string defaultName = Path.GetFileNameWithoutExtension(MonthlyReportPdfGenerator.get_default_file_name(report)) + ".xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar reporte de ventas en Excel");
        }
    }
}
