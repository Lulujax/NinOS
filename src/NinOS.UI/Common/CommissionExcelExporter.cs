using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using NinOS.Domain.ViewModels;

namespace NinOS.UI.Common
{
    public static class CommissionExcelExporter
    {
        public static void export(commission_receipt_dto receipt)
        {
            if (receipt == null || receipt.rows.Count == 0) return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Comisiones");

            string[] headers = new[]
            {
                "NRO NOTA", "CLIENTE", "MONTO FACTURADO ($)", "FECHA DE PAGO",
                "MONTO PAGADO ($)", "PRONTO PAGO", "COMISION 10% ($)", "TOTAL COMISION ($)", "FECHA PAGO COMISION"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, i + 1));
            }

            int rowIdx = 2;
            foreach (var r in receipt.rows)
            {
                int c = 1;
                ws.Cell(rowIdx, c++).SetValue(r.note_number);
                ws.Cell(rowIdx, c++).SetValue(r.customer_name);

                var factCell = ws.Cell(rowIdx, c++);
                factCell.SetValue(r.invoiced_amount);
                factCell.Style.NumberFormat.Format = "#,##0.00";

                ws.Cell(rowIdx, c++).SetValue(r.payment_date?.ToString("dd/MM/yyyy") ?? "-");

                var paidCell = ws.Cell(rowIdx, c++);
                paidCell.SetValue(r.paid_amount);
                paidCell.Style.NumberFormat.Format = "#,##0.00";

                ws.Cell(rowIdx, c++).SetValue(r.early_payment_discount_pct > 0 ? $"SI ({r.early_payment_discount_pct:0.##}%)" : (r.early_payment_discount > 0 ? "SI" : "NO"));

                var comm10Cell = ws.Cell(rowIdx, c++);
                comm10Cell.SetValue(r.commission_10);
                comm10Cell.Style.NumberFormat.Format = "#,##0.00";

                var totCommCell = ws.Cell(rowIdx, c++);
                totCommCell.SetValue(r.commission_10);
                totCommCell.Style.NumberFormat.Format = "#,##0.00";

                ws.Cell(rowIdx, c++).SetValue(r.commission_paid_date?.ToString("dd/MM/yyyy") ?? "-");

                rowIdx++;
            }

            ws.Cell(rowIdx, 1).SetValue("TOTAL COMISION");
            ws.Cell(rowIdx, 8).SetValue(receipt.rows.Sum(x => x.commission_10));
            ws.Cell(rowIdx, 8).Style.NumberFormat.Format = "#,##0.00";
            ExcelFlatExporter.StyleTotalRow(ws.Range(rowIdx, 1, rowIdx, headers.Length));

            ExcelFlatExporter.FormatTable(ws, headerRow: 1);

            string defaultName = Path.GetFileNameWithoutExtension(CommissionPdfGenerator.get_default_file_name(receipt)) + ".xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar comprobante de comisión en Excel");
        }
    }
}
