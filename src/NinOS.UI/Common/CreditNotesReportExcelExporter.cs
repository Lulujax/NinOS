using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using NinOS.Domain.ViewModels;

namespace NinOS.UI.Common
{
    public static class CreditNotesReportExcelExporter
    {
        public static void export(credit_note_report_dto report)
        {
            if (report == null) return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("NotasDeCredito");

            string[] headers = new[]
            {
                "FECHA", "CATEGORIA", "NRO NC", "NOTA ENTREGA", "CLIENTE",
                "VENDEDOR", "ZONA", "MONTO ($)", "ESTADO"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, i + 1));
            }

            int rowIdx = 2;
            var rows = report.rows ?? new System.Collections.Generic.List<credit_note_report_row_dto>();
            foreach (var r in rows.OrderBy(x => x.creation_date).ThenBy(x => x.note_number))
            {
                int c = 1;
                ws.Cell(rowIdx, c++).SetValue(r.fecha_display);
                ws.Cell(rowIdx, c++).SetValue(r.categoria_display);
                ws.Cell(rowIdx, c++).SetValue(r.note_number);
                ws.Cell(rowIdx, c++).SetValue(r.entrega_display);
                ws.Cell(rowIdx, c++).SetValue(r.customer_name);
                ws.Cell(rowIdx, c++).SetValue(r.seller_name);
                ws.Cell(rowIdx, c++).SetValue(r.zone_name);

                var amountCell = ws.Cell(rowIdx, c++);
                amountCell.SetValue(r.total_amount_usd);
                amountCell.Style.NumberFormat.Format = "#,##0.00";

                ws.Cell(rowIdx, c++).SetValue(r.status);

                if (string.Equals(r.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase))
                {
                    ws.Range(rowIdx, 1, rowIdx, headers.Length).Style.Font.FontColor = XLColor.Red;
                }

                rowIdx++;
            }

            var valid = rows.Where(r => !string.Equals(r.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase)).ToList();
            if (valid.Count > 0)
            {
                ws.Cell(rowIdx, 1).SetValue("TOTAL NC");
                ws.Cell(rowIdx, 8).SetValue(valid.Sum(x => x.total_amount_usd));
                ws.Cell(rowIdx, 8).Style.NumberFormat.Format = "#,##0.00";

                ExcelFlatExporter.StyleTotalRow(ws.Range(rowIdx, 1, rowIdx, headers.Length));
            }

            ExcelFlatExporter.FormatTable(ws, headerRow: 1);

            string defaultName = Path.GetFileNameWithoutExtension(CreditNotesReportPdfGenerator.get_default_file_name(report)) + ".xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar reporte de notas de crédito en Excel");
        }
    }
}
