using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using NinOS.Domain.ViewModels;

namespace NinOS.UI.Common
{
    public static class ProVentaWeeklyExcelExporter
    {
        public static void export(
            pro_venta_weekly_dto dto,
            decimal nota_por_pagar,
            List<payment_dto>? payments = null,
            pro_venta_relation_row? relation_info = null)
        {
            if (dto == null) return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add($"Relacion {dto.relation_number}");

            string[] headers = new[]
            {
                "NRO NOTA", "CLIENTE", "MONTO NOTA ($)", "COMISION LUIS 10% ($)",
                "GASTOS OPER 25% ($)", "GASTOS ADM 15% ($)", "FECHA PAGO CLIENTE", "FORMA DE PAGO"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, i + 1), ExcelFlatExporter.DarkBlueHeader);
            }

            int rowIdx = 2;
            foreach (var r in dto.rows)
            {
                int c = 1;
                ws.Cell(rowIdx, c++).SetValue(r.note_number);
                ws.Cell(rowIdx, c++).SetValue(r.customer_name);

                var amtCell = ws.Cell(rowIdx, c++);
                amtCell.SetValue(r.amount);
                amtCell.Style.NumberFormat.Format = "#,##0.00";

                var commCell = ws.Cell(rowIdx, c++);
                commCell.SetValue(r.commission_luis);
                commCell.Style.NumberFormat.Format = "#,##0.00";

                var g25Cell = ws.Cell(rowIdx, c++);
                g25Cell.SetValue(r.gastos_25);
                g25Cell.Style.NumberFormat.Format = "#,##0.00";

                var g15Cell = ws.Cell(rowIdx, c++);
                g15Cell.SetValue(r.gastos_15);
                g15Cell.Style.NumberFormat.Format = "#,##0.00";

                ws.Cell(rowIdx, c++).SetValue(r.payment_date_display);
                ws.Cell(rowIdx, c++).SetValue(r.payment_method_display);

                if (r.esta_anulada)
                {
                    ws.Range(rowIdx, 1, rowIdx, headers.Length).Style.Font.FontColor = XLColor.Red;
                }

                rowIdx++;
            }

            // Fila de totales
            ws.Cell(rowIdx, 1).SetValue("TOTAL");
            ws.Cell(rowIdx, 3).SetValue(dto.total_amount);
            ws.Cell(rowIdx, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(rowIdx, 4).SetValue(dto.total_commission_luis);
            ws.Cell(rowIdx, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(rowIdx, 5).SetValue(dto.total_gastos_25);
            ws.Cell(rowIdx, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(rowIdx, 6).SetValue(dto.total_gastos_15);
            ws.Cell(rowIdx, 6).Style.NumberFormat.Format = "#,##0.00";

            ExcelFlatExporter.StyleTotalRow(ws.Range(rowIdx, 1, rowIdx, headers.Length), ExcelFlatExporter.BlueSoftBg);
            rowIdx += 2;

            // Bloque de Liquidación
            ws.Cell(rowIdx, 1).SetValue("LIQUIDACION").Style.Font.Bold = true;
            rowIdx++;

            ws.Cell(rowIdx, 1).SetValue("TOTAL COBRADO (NOTAS MENOS COMISION LUIS G)");
            ws.Cell(rowIdx, 3).SetValue(dto.total_amount - dto.total_commission_luis);
            ws.Cell(rowIdx, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Range(rowIdx, 1, rowIdx, 3).Style.Fill.BackgroundColor = XLColor.FromHtml(ExcelFlatExporter.BlueSoftBg);
            rowIdx++;

            ws.Cell(rowIdx, 1).SetValue("NOTA DE ENTREGA POR PAGAR A PRO VENTA");
            ws.Cell(rowIdx, 3).SetValue(nota_por_pagar);
            ws.Cell(rowIdx, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Range(rowIdx, 1, rowIdx, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2A8");
            rowIdx++;

            ws.Cell(rowIdx, 1).SetValue("DIFERENCIAL");
            ws.Cell(rowIdx, 3).SetValue((dto.total_amount - dto.total_commission_luis) - nota_por_pagar);
            ws.Cell(rowIdx, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Range(rowIdx, 1, rowIdx, 3).Style.Fill.BackgroundColor = XLColor.FromHtml(ExcelFlatExporter.BlueSoftBg);

            ExcelFlatExporter.FormatTable(ws, headerRow: 1);

            // Hoja de Pagos si hay
            if (payments != null && payments.Count > 0)
            {
                var wsPay = wb.Worksheets.Add("Pagos Registrados");
                string[] hPay = new[] { "FECHA", "MONTO ($)", "OBSERVACIONES" };
                for (int i = 0; i < hPay.Length; i++)
                {
                    wsPay.Cell(1, i + 1).Value = hPay[i];
                    ExcelFlatExporter.StyleHeaderCell(wsPay.Cell(1, i + 1), ExcelFlatExporter.DarkBlueHeader);
                }
                int rp = 2;
                foreach (var p in payments)
                {
                    wsPay.Cell(rp, 1).SetValue(p.payment_date.ToString("dd/MM/yyyy"));
                    var cellM = wsPay.Cell(rp, 2);
                    cellM.SetValue(p.amount_usd);
                    cellM.Style.NumberFormat.Format = "#,##0.00";
                    wsPay.Cell(rp, 3).SetValue(p.notes);
                    rp++;
                }
                wsPay.Cell(rp, 1).SetValue("TOTAL PAGADO");
                wsPay.Cell(rp, 2).SetValue(payments.Where(p => !p.es_anulacion).Sum(x => x.amount_usd));
                wsPay.Cell(rp, 2).Style.NumberFormat.Format = "#,##0.00";
                ExcelFlatExporter.StyleTotalRow(wsPay.Range(rp, 1, rp, 3), ExcelFlatExporter.BlueSoftBg);
                ExcelFlatExporter.FormatTable(wsPay, headerRow: 1);
            }

            string defaultName = Path.GetFileNameWithoutExtension(ProVentaPdfGenerator.get_default_file_name(dto)) + ".xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar relación Pro Venta en Excel");
        }
    }
}
