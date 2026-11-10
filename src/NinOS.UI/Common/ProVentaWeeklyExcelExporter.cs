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

            // 1. BANNER DE ENCABEZADO (Idéntico a la Página 1 del PDF)
            ws.Cell(1, 1).SetValue($"RELACION NRO {dto.relation_number}");
            ws.Range(1, 1, 1, 8).Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1565C0");
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(2, 1).SetValue($"{dto.week_start:dd/MM} AL {dto.week_end:dd/MM} _ FACTURAS POR COBRAR {dto.city}");
            ws.Range(2, 1, 2, 8).Merge();
            ws.Cell(2, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Style.Font.FontSize = 10;
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // 2. ENCABEZADOS DE LA TABLA
            string[] headers = new[]
            {
                "NRO NOTA CLIENTE", "CLIENTE", "MONTO NOTA ($)", "COMISION LUIS 10% ($)",
                "GASTOS OPER 25% ($)", "GASTOS ADM 15% ($)", "FECHA PAGO CLIENTE", "FORMA DE PAGO"
            };

            int headerRow = 4;
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(headerRow, i + 1).Value = headers[i];
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(headerRow, i + 1), ExcelFlatExporter.DarkBlueHeader);
            }

            int rowIdx = 5;
            foreach (var r in dto.rows)
            {
                int c = 1;
                string noteNum = r.esta_anulada ? $"{r.note_number} [ANULADA]" : r.note_number;
                ws.Cell(rowIdx, c++).SetValue(noteNum);
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
                    ws.Range(rowIdx, 1, rowIdx, headers.Length).Style.Font.FontColor = XLColor.FromHtml("#C62828");
                    ws.Range(rowIdx, 1, rowIdx, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE");
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
            rowIdx++;

            // Leyenda de anuladas si las hay
            if (dto.has_annulled)
            {
                rowIdx++;
                ws.Cell(rowIdx, 1).SetValue($"Las notas marcadas en rojo están ANULADAS y no se incluyen en los totales ni en la liquidación. Anuladas: {dto.annulled_count} nota(s) por {dto.annulled_amount:N2} USD.");
                ws.Range(rowIdx, 1, rowIdx, 8).Merge();
                ws.Range(rowIdx, 1, rowIdx, 8).Style.Font.FontSize = 9;
                ws.Range(rowIdx, 1, rowIdx, 8).Style.Font.FontColor = XLColor.FromHtml("#C62828");
                ws.Range(rowIdx, 1, rowIdx, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE");
                ws.Range(rowIdx, 1, rowIdx, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(rowIdx, 1, rowIdx, 8).Style.Border.OutsideBorderColor = XLColor.FromHtml("#C62828");
            }

            rowIdx += 2;

            // Bloque de Liquidación (idéntico al cuadro del PDF)
            ws.Cell(rowIdx, 1).SetValue("LIQUIDACION");
            ws.Cell(rowIdx, 1).Style.Font.Bold = true;
            ws.Cell(rowIdx, 1).Style.Font.FontSize = 11;
            ws.Cell(rowIdx, 1).Style.Font.FontColor = XLColor.FromHtml("#1565C0");
            rowIdx++;

            decimal total_cobrado = dto.total_amount - dto.total_commission_luis;
            decimal diferencial = total_cobrado - nota_por_pagar;

            ws.Cell(rowIdx, 1).SetValue("TOTAL COBRADO (NOTAS MENOS COMISION LUIS G)");
            ws.Range(rowIdx, 1, rowIdx, 2).Merge();
            ws.Cell(rowIdx, 1).Style.Font.Bold = true;
            ws.Cell(rowIdx, 3).SetValue(total_cobrado);
            ws.Cell(rowIdx, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(rowIdx, 3).Style.Font.Bold = true;
            ws.Range(rowIdx, 1, rowIdx, 3).Style.Fill.BackgroundColor = XLColor.FromHtml(ExcelFlatExporter.BlueSoftBg);
            ws.Range(rowIdx, 1, rowIdx, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rowIdx++;

            ws.Cell(rowIdx, 1).SetValue("NOTA DE ENTREGA POR PAGAR A PRO VENTA");
            ws.Range(rowIdx, 1, rowIdx, 2).Merge();
            ws.Cell(rowIdx, 1).Style.Font.Bold = true;
            ws.Cell(rowIdx, 3).SetValue(nota_por_pagar);
            ws.Cell(rowIdx, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(rowIdx, 3).Style.Font.Bold = true;
            ws.Range(rowIdx, 1, rowIdx, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2A8");
            ws.Range(rowIdx, 1, rowIdx, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rowIdx++;

            ws.Cell(rowIdx, 1).SetValue("DIFERENCIAL");
            ws.Range(rowIdx, 1, rowIdx, 2).Merge();
            ws.Cell(rowIdx, 1).Style.Font.Bold = true;
            ws.Cell(rowIdx, 3).SetValue(diferencial);
            ws.Cell(rowIdx, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(rowIdx, 3).Style.Font.Bold = true;
            ws.Range(rowIdx, 1, rowIdx, 3).Style.Fill.BackgroundColor = XLColor.FromHtml(ExcelFlatExporter.BlueSoftBg);
            ws.Range(rowIdx, 1, rowIdx, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ExcelFlatExporter.FormatTable(ws, headerRow: headerRow);

            // 3. HOJA 2: RESUMEN Y PAGOS (Réplica de la Página 2 del PDF)
            var wsDetail = wb.Worksheets.Add("Resumen y Pagos");

            wsDetail.Cell(1, 1).SetValue($"RELACION NRO {dto.relation_number} — RESUMEN Y PAGOS");
            wsDetail.Range(1, 1, 1, 4).Merge();
            wsDetail.Cell(1, 1).Style.Font.Bold = true;
            wsDetail.Cell(1, 1).Style.Font.FontSize = 13;
            wsDetail.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1565C0");
            wsDetail.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            wsDetail.Cell(3, 1).SetValue("RESUMEN DE LA RELACION");
            wsDetail.Cell(3, 1).Style.Font.Bold = true;
            wsDetail.Cell(3, 1).Style.Font.FontSize = 10;
            wsDetail.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#1565C0");

            string[] hNotes = new[] { "NRO NOTA", "CLIENTE", "MONTO ($)" };
            for (int i = 0; i < hNotes.Length; i++)
            {
                wsDetail.Cell(4, i + 1).Value = hNotes[i];
                ExcelFlatExporter.StyleHeaderCell(wsDetail.Cell(4, i + 1), ExcelFlatExporter.DarkBlueHeader);
            }

            int rn = 5;
            foreach (var n in dto.rows)
            {
                string noteLabel = n.esta_anulada ? $"{n.note_number} [ANULADA]" : n.note_number;
                wsDetail.Cell(rn, 1).SetValue(noteLabel);
                wsDetail.Cell(rn, 2).SetValue(n.customer_name);
                var cM = wsDetail.Cell(rn, 3);
                cM.SetValue(n.amount);
                cM.Style.NumberFormat.Format = "#,##0.00";

                if (n.esta_anulada)
                {
                    wsDetail.Range(rn, 1, rn, 3).Style.Font.FontColor = XLColor.FromHtml("#C62828");
                    wsDetail.Range(rn, 1, rn, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE");
                }
                rn++;
            }

            decimal total_notes = dto.rows.Where(r => !r.esta_anulada).Sum(r => r.amount);
            wsDetail.Cell(rn, 1).SetValue("TOTAL");
            wsDetail.Cell(rn, 1).Style.Font.Bold = true;
            wsDetail.Cell(rn, 3).SetValue(total_notes);
            wsDetail.Cell(rn, 3).Style.NumberFormat.Format = "#,##0.00";
            wsDetail.Cell(rn, 3).Style.Font.Bold = true;
            ExcelFlatExporter.StyleTotalRow(wsDetail.Range(rn, 1, rn, 3), ExcelFlatExporter.BlueSoftBg);
            rn += 2;

            // Tabla 2: Registro de Pagos
            wsDetail.Cell(rn, 1).SetValue("REGISTRO DE PAGOS");
            wsDetail.Cell(rn, 1).Style.Font.Bold = true;
            wsDetail.Cell(rn, 1).Style.Font.FontSize = 10;
            wsDetail.Cell(rn, 1).Style.Font.FontColor = XLColor.FromHtml("#1565C0");
            rn++;

            string[] hPay = new[] { "FECHA DEL PAGO", "MONTO USD", "SUBTOTAL USD", "OBSERVACION" };
            int payHeaderRow = rn;
            for (int i = 0; i < hPay.Length; i++)
            {
                wsDetail.Cell(payHeaderRow, i + 1).Value = hPay[i];
                ExcelFlatExporter.StyleHeaderCell(wsDetail.Cell(payHeaderRow, i + 1), ExcelFlatExporter.DarkBlueHeader);
            }
            rn++;

            decimal total_paid = 0m;
            decimal running = 0m;
            if (payments != null && payments.Count > 0)
            {
                foreach (var p in payments)
                {
                    wsDetail.Cell(rn, 1).SetValue(p.payment_date.ToString("dd/MM/yyyy"));
                    var mCell = wsDetail.Cell(rn, 2);
                    mCell.SetValue(p.amount_usd);
                    mCell.Style.NumberFormat.Format = "#,##0.00";
                    mCell.Style.Font.Bold = true;
                    if (p.es_negativo) mCell.Style.Font.FontColor = XLColor.FromHtml("#D32F2F");
                    else mCell.Style.Font.FontColor = XLColor.FromHtml("#2E7D32");

                    if (!p.es_anulacion)
                    {
                        running += p.amount_usd;
                        total_paid += p.amount_usd;
                    }
                    decimal remaining = total_notes - running;
                    if (remaining <= 0.005m) remaining = 0m;

                    var subCell = wsDetail.Cell(rn, 3);
                    subCell.SetValue(remaining);
                    subCell.Style.NumberFormat.Format = "#,##0.00";
                    subCell.Style.Font.Bold = true;
                    subCell.Style.Font.FontColor = XLColor.FromHtml("#1565C0");

                    wsDetail.Cell(rn, 4).SetValue(p.notes);
                    rn++;
                }
            }

            wsDetail.Cell(rn, 1).SetValue("TOTAL PAGADO");
            wsDetail.Cell(rn, 1).Style.Font.Bold = true;
            wsDetail.Cell(rn, 2).SetValue(total_paid);
            wsDetail.Cell(rn, 2).Style.NumberFormat.Format = "#,##0.00";
            wsDetail.Cell(rn, 2).Style.Font.Bold = true;
            ExcelFlatExporter.StyleTotalRow(wsDetail.Range(rn, 1, rn, 4), ExcelFlatExporter.BlueSoftBg);
            rn += 2;

            // Bloque Resumen Financiero de la Relación (idéntico a página 2 PDF)
            decimal balance = total_notes - total_paid;
            if (balance <= 0.005m) balance = 0m;
            string status = relation_info?.status ?? (balance <= 0.005m ? "Pagada" : "Pendiente");

            wsDetail.Cell(rn, 1).SetValue("MONTO TOTAL DE NOTAS:");
            wsDetail.Cell(rn, 2).SetValue(total_notes);
            wsDetail.Cell(rn, 2).Style.NumberFormat.Format = "#,##0.00";
            wsDetail.Cell(rn, 2).Style.Font.Bold = true;
            rn++;

            wsDetail.Cell(rn, 1).SetValue("TOTAL ABONADO:");
            wsDetail.Cell(rn, 2).SetValue(total_paid);
            wsDetail.Cell(rn, 2).Style.NumberFormat.Format = "#,##0.00";
            wsDetail.Cell(rn, 2).Style.Font.Bold = true;
            wsDetail.Cell(rn, 2).Style.Font.FontColor = XLColor.FromHtml("#2E7D32");
            rn++;

            wsDetail.Cell(rn, 1).SetValue("SALDO PENDIENTE:");
            wsDetail.Cell(rn, 2).SetValue(balance);
            wsDetail.Cell(rn, 2).Style.NumberFormat.Format = "#,##0.00";
            wsDetail.Cell(rn, 2).Style.Font.Bold = true;
            wsDetail.Cell(rn, 2).Style.Font.FontColor = balance > 0 ? XLColor.FromHtml("#C62828") : XLColor.FromHtml("#2E7D32");
            rn++;

            wsDetail.Cell(rn, 1).SetValue("ESTADO:");
            wsDetail.Cell(rn, 2).SetValue(status);
            wsDetail.Cell(rn, 2).Style.Font.Bold = true;
            wsDetail.Cell(rn, 2).Style.Font.FontColor = status == "Pagada" ? XLColor.FromHtml("#2E7D32") : XLColor.FromHtml("#E65100");

            ExcelFlatExporter.FormatTable(wsDetail, headerRow: 4);

            string defaultName = Path.GetFileNameWithoutExtension(ProVentaPdfGenerator.get_default_file_name(dto)) + ".xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar relación Pro Venta en Excel");
        }
    }
}
