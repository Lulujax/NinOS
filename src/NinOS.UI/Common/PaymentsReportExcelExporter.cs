using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using NinOS.Domain.ViewModels;

namespace NinOS.UI.Common
{
    public static class PaymentsReportExcelExporter
    {
        public static void export(string month, List<payment_dto> payments, string groupMode = "Por Vendedor")
        {
            if (payments == null) return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Pagos");

            string[] headers = new[]
            {
                "FECHA", "NOTA", "CLIENTE", "VENDEDOR", "ZONA",
                "BANCO", "NRO REF", "METODO", "MONTO ($)", "MONTO BS", "TASA (BS/$)", "OBSERVACIONES"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, i + 1));
            }

            int rowIdx = 2;
            foreach (var p in payments.OrderBy(x => x.payment_date).ThenBy(x => x.note_number))
            {
                int c = 1;
                ws.Cell(rowIdx, c++).SetValue(p.payment_date.ToString("dd/MM/yyyy"));
                ws.Cell(rowIdx, c++).SetValue(p.note_number);
                ws.Cell(rowIdx, c++).SetValue(p.customer_name);
                ws.Cell(rowIdx, c++).SetValue(p.seller_name);
                ws.Cell(rowIdx, c++).SetValue(p.zone_name);
                ws.Cell(rowIdx, c++).SetValue(p.bank_name);
                ws.Cell(rowIdx, c++).SetValue(p.reference_number);
                ws.Cell(rowIdx, c++).SetValue(p.payment_type);

                var usdCell = ws.Cell(rowIdx, c++);
                usdCell.SetValue(p.amount_usd);
                usdCell.Style.NumberFormat.Format = "#,##0.00";

                var bsCell = ws.Cell(rowIdx, c++);
                bsCell.SetValue(p.amount_bs);
                bsCell.Style.NumberFormat.Format = "#,##0.00";

                var rateCell = ws.Cell(rowIdx, c++);
                if (p.exchange_rate.HasValue)
                {
                    rateCell.SetValue(p.exchange_rate.Value);
                    rateCell.Style.NumberFormat.Format = "#,##0.00";
                }
                else
                {
                    rateCell.SetValue("-");
                }

                ws.Cell(rowIdx, c++).SetValue(p.notes);

                if (p.es_anulacion)
                {
                    ws.Range(rowIdx, 1, rowIdx, headers.Length).Style.Font.FontColor = XLColor.Red;
                }

                rowIdx++;
            }

            // Fila de totales
            var reales = payments.Where(p => !p.es_anulacion).ToList();
            if (reales.Count > 0)
            {
                ws.Cell(rowIdx, 1).SetValue("TOTAL REAL COBRADO");
                ws.Cell(rowIdx, 9).SetValue(reales.Sum(x => x.amount_usd));
                ws.Cell(rowIdx, 9).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(rowIdx, 10).SetValue(reales.Sum(x => x.amount_bs));
                ws.Cell(rowIdx, 10).Style.NumberFormat.Format = "#,##0.00";

                ExcelFlatExporter.StyleTotalRow(ws.Range(rowIdx, 1, rowIdx, headers.Length));
            }

            ExcelFlatExporter.FormatTable(ws, headerRow: 1);

            string defaultName = Path.GetFileNameWithoutExtension(PaymentsReportPdfGenerator.get_default_file_name(month, groupMode)) + ".xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar reporte de pagos en Excel");
        }
    }
}
