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
        private static readonly XLColor Accent = XLColor.FromHtml("#1565C0");
        private static readonly XLColor SoftAccent = XLColor.FromHtml("#E3F2FD");
        private static readonly XLColor LightBorder = XLColor.FromHtml("#B0BEC5");
        private static readonly XLColor GreenColor = XLColor.FromHtml("#2E7D32");
        private static readonly XLColor RedColor = XLColor.FromHtml("#C62828");
        private static readonly XLColor OrangeColor = XLColor.FromHtml("#E65100");

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
            string sheetName = (pending.Count > 0 && paid.Count > 0)
                ? "Consolidado Relaciones"
                : (pending.Count > 0 ? "Relaciones Pendientes" : "Relaciones Pagadas");
            var ws = wb.Worksheets.Add(sheetName);

            // 1. BANNER DE ENCABEZADO (Idéntico al PDF de ProVentaRelationsSummaryPdfGenerator)
            ws.Cell(1, 1).SetValue("REPORTE DE RELACIONES PRO VENTA - MARACAY");
            ws.Range(1, 1, 1, 3).Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 13;
            ws.Cell(1, 1).Style.Font.FontColor = Accent;

            ws.Cell(1, 4).SetValue($"EMISION: {DateTime.Now:dd/MM/yyyy HH:mm}");
            ws.Range(1, 4, 1, 5).Merge();
            ws.Cell(1, 4).Style.Font.Bold = true;
            ws.Cell(1, 4).Style.Font.FontSize = 9;
            ws.Cell(1, 4).Style.Font.FontColor = XLColor.FromHtml("#555555");
            ws.Cell(1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(2, 1).SetValue(reportTitle.ToUpperInvariant());
            ws.Range(2, 1, 2, 3).Merge();
            ws.Cell(2, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Style.Font.FontSize = 10;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#333333");

            string displayPeriod = (string.IsNullOrWhiteSpace(monthLabel) || string.Equals(monthLabel.Trim(), "Todas las relaciones", StringComparison.OrdinalIgnoreCase) || string.Equals(monthLabel.Trim(), "Todas", StringComparison.OrdinalIgnoreCase))
                ? "TODAS"
                : monthLabel.ToUpperInvariant();
            ws.Cell(2, 4).SetValue(displayPeriod);
            ws.Range(2, 4, 2, 5).Merge();
            ws.Cell(2, 4).Style.Font.Bold = true;
            ws.Cell(2, 4).Style.Font.FontSize = 9;
            ws.Cell(2, 4).Style.Font.FontColor = Accent;
            ws.Cell(2, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Línea divisora azul
            var dividerRange = ws.Range(3, 1, 3, 5);
            dividerRange.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            dividerRange.Style.Border.BottomBorderColor = Accent;

            int currentRow = 5;

            // 2. SECCIÓN PENDIENTES
            if (pending.Count > 0)
            {
                currentRow = RenderSection(ws, currentRow, "RELACIONES POR COBRAR - PENDIENTES", pending, isPending: true);
            }

            // 3. SECCIÓN PAGADAS
            if (paid.Count > 0)
            {
                if (pending.Count > 0) currentRow += 2;
                currentRow = RenderSection(ws, currentRow, "RELACIONES POR COBRAR - PAGADAS", paid, isPending: false);
            }

            // 4. GRAN TOTAL / RESUMEN CONSOLIDADO (Tarjeta idéntica al PDF)
            if (pending.Count > 0 || paid.Count > 0)
            {
                currentRow += 2;
                decimal totalMontoPagar = pending.Sum(r => r.amount) + paid.Sum(r => r.amount);
                decimal totalAbonado = pending.Sum(r => r.paid_amount_usd) + paid.Sum(r => r.paid_amount_usd);
                decimal totalSaldo = pending.Sum(r => r.balance_due_usd) + paid.Sum(r => r.balance_due_usd);

                string boxTitle = (pending.Count > 0 && paid.Count > 0)
                    ? "CONSOLIDADO GENERAL DE RELACIONES"
                    : (pending.Count > 0 ? "TOTAL RELACIONES PENDIENTES" : "TOTAL RELACIONES PAGADAS");

                int boxStartRow = currentRow;
                int colStart = 2; // Columnas B a E
                int colEnd = 5;

                // Título de la tarjeta
                ws.Cell(currentRow, colStart).SetValue(boxTitle);
                ws.Range(currentRow, colStart, currentRow, colEnd).Merge();
                ws.Cell(currentRow, colStart).Style.Font.Bold = true;
                ws.Cell(currentRow, colStart).Style.Font.FontSize = 10;
                ws.Cell(currentRow, colStart).Style.Font.FontColor = Accent;
                ws.Cell(currentRow, colStart).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(currentRow, colStart).Style.Fill.BackgroundColor = SoftAccent;
                currentRow++;

                // Fila 1: TOTAL MONTO POR PAGAR
                ws.Cell(currentRow, colStart).SetValue("TOTAL MONTO POR PAGAR:");
                ws.Range(currentRow, colStart, currentRow, colEnd - 1).Merge();
                ws.Cell(currentRow, colStart).Style.Font.Bold = true;
                ws.Cell(currentRow, colStart).Style.Font.FontSize = 9.5;
                var mCell = ws.Cell(currentRow, colEnd);
                mCell.SetValue(totalMontoPagar);
                mCell.Style.Font.Bold = true;
                mCell.Style.Font.FontSize = 9.5;
                mCell.Style.NumberFormat.Format = "$#,##0.00";
                mCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Range(currentRow, colStart, currentRow, colEnd).Style.Fill.BackgroundColor = SoftAccent;
                currentRow++;

                // Fila 2: TOTAL COBRADO / ABONADO
                ws.Cell(currentRow, colStart).SetValue("TOTAL COBRADO / ABONADO:");
                ws.Range(currentRow, colStart, currentRow, colEnd - 1).Merge();
                ws.Cell(currentRow, colStart).Style.Font.Bold = true;
                ws.Cell(currentRow, colStart).Style.Font.FontSize = 9.5;
                ws.Cell(currentRow, colStart).Style.Font.FontColor = GreenColor;
                var aCell = ws.Cell(currentRow, colEnd);
                aCell.SetValue(totalAbonado);
                aCell.Style.Font.Bold = true;
                aCell.Style.Font.FontSize = 9.5;
                aCell.Style.Font.FontColor = GreenColor;
                aCell.Style.NumberFormat.Format = "$#,##0.00";
                aCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Range(currentRow, colStart, currentRow, colEnd).Style.Fill.BackgroundColor = SoftAccent;
                currentRow++;

                // Fila 3: SALDO PENDIENTE TOTAL
                ws.Cell(currentRow, colStart).SetValue("SALDO PENDIENTE TOTAL:");
                ws.Range(currentRow, colStart, currentRow, colEnd - 1).Merge();
                ws.Cell(currentRow, colStart).Style.Font.Bold = true;
                ws.Cell(currentRow, colStart).Style.Font.FontSize = 10;
                ws.Cell(currentRow, colStart).Style.Font.FontColor = RedColor;
                var sCell = ws.Cell(currentRow, colEnd);
                sCell.SetValue(totalSaldo);
                sCell.Style.Font.Bold = true;
                sCell.Style.Font.FontSize = 10.5;
                sCell.Style.Font.FontColor = RedColor;
                sCell.Style.NumberFormat.Format = "$#,##0.00";
                sCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Range(currentRow, colStart, currentRow, colEnd).Style.Fill.BackgroundColor = SoftAccent;

                // Borde exterior de la tarjeta
                var boxRange = ws.Range(boxStartRow, colStart, currentRow, colEnd);
                boxRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                boxRange.Style.Border.OutsideBorderColor = Accent;
            }

            // Anchos de columna optimizados
            ws.Column(1).Width = 32; // RELACION NRO
            ws.Column(2).Width = 24; // MONTO POR PAGAR ($)
            ws.Column(3).Width = 22; // ABONADO ($)
            ws.Column(4).Width = 24; // SALDO PENDIENTE ($)
            ws.Column(5).Width = 18; // ESTADO

            string defaultName = Path.GetFileNameWithoutExtension(ProVentaRelationsSummaryPdfGenerator.get_default_file_name(reportTitle, monthLabel)) + ".xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar reporte de relaciones Pro Venta en Excel");
        }

        private static int RenderSection(IXLWorksheet ws, int startRow, string sectionTitle, List<pro_venta_relation_row> rows, bool isPending)
        {
            // Encabezado de la sección
            ws.Cell(startRow, 1).SetValue(sectionTitle);
            ws.Range(startRow, 1, startRow, 3).Merge();
            ws.Cell(startRow, 1).Style.Font.Bold = true;
            ws.Cell(startRow, 1).Style.Font.FontSize = 10.5;
            ws.Cell(startRow, 1).Style.Font.FontColor = Accent;

            ws.Cell(startRow, 5).SetValue($"{rows.Count} relación(es)");
            ws.Cell(startRow, 5).Style.Font.Bold = true;
            ws.Cell(startRow, 5).Style.Font.FontSize = 8.5;
            ws.Cell(startRow, 5).Style.Font.FontColor = XLColor.FromHtml("#555555");
            ws.Cell(startRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            startRow++;

            // Encabezados de la tabla
            string[] headers = new[]
            {
                "RELACION NRO", "MONTO POR PAGAR ($)", "ABONADO ($)", "SALDO PENDIENTE ($)", "ESTADO"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(startRow, i + 1);
                cell.SetValue(headers[i]);
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 9;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = Accent;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.Horizontal = (i >= 1 && i <= 3) ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Center;
            }
            startRow++;

            // Filas de datos
            bool alternate = false;
            foreach (var r in rows.OrderBy(x => x.relation_number))
            {
                var bg = alternate ? SoftAccent : XLColor.White;
                alternate = !alternate;

                // Col 1: Relación Nro
                var c1 = ws.Cell(startRow, 1);
                c1.SetValue(r.relation_label);
                c1.Style.Font.Bold = true;
                c1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Col 2: Monto por pagar
                var c2 = ws.Cell(startRow, 2);
                c2.SetValue(r.amount);
                c2.Style.NumberFormat.Format = "$#,##0.00";
                c2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Col 3: Abonado
                var c3 = ws.Cell(startRow, 3);
                c3.SetValue(r.paid_amount_usd);
                c3.Style.Font.Bold = true;
                c3.Style.Font.FontColor = GreenColor;
                c3.Style.NumberFormat.Format = "$#,##0.00";
                c3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Col 4: Saldo pendiente
                var c4 = ws.Cell(startRow, 4);
                c4.SetValue(r.balance_due_usd);
                c4.Style.Font.Bold = true;
                c4.Style.Font.FontColor = r.balance_due_usd > 0 ? RedColor : GreenColor;
                c4.Style.NumberFormat.Format = "$#,##0.00";
                c4.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Col 5: Estado
                var c5 = ws.Cell(startRow, 5);
                c5.SetValue(r.status);
                c5.Style.Font.Bold = true;
                c5.Style.Font.FontColor = isPending ? OrangeColor : GreenColor;
                c5.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var rowRange = ws.Range(startRow, 1, startRow, headers.Length);
                rowRange.Style.Fill.BackgroundColor = bg;
                rowRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.BottomBorderColor = LightBorder;

                startRow++;
            }

            // Fila de totales de sección
            ws.Cell(startRow, 1).SetValue("TOTAL");
            ws.Cell(startRow, 1).Style.Font.Bold = true;
            ws.Cell(startRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var t2 = ws.Cell(startRow, 2);
            t2.SetValue(rows.Sum(x => x.amount));
            t2.Style.Font.Bold = true;
            t2.Style.NumberFormat.Format = "$#,##0.00";
            t2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var t3 = ws.Cell(startRow, 3);
            t3.SetValue(rows.Sum(x => x.paid_amount_usd));
            t3.Style.Font.Bold = true;
            t3.Style.Font.FontColor = GreenColor;
            t3.Style.NumberFormat.Format = "$#,##0.00";
            t3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var t4 = ws.Cell(startRow, 4);
            t4.SetValue(rows.Sum(x => x.balance_due_usd));
            t4.Style.Font.Bold = true;
            t4.Style.Font.FontColor = isPending ? RedColor : GreenColor;
            t4.Style.NumberFormat.Format = "$#,##0.00";
            t4.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(startRow, 5).SetValue(string.Empty);

            var totalRowRange = ws.Range(startRow, 1, startRow, headers.Length);
            totalRowRange.Style.Fill.BackgroundColor = SoftAccent;
            totalRowRange.Style.Border.TopBorder = XLBorderStyleValues.Medium;
            totalRowRange.Style.Border.TopBorderColor = Accent;
            totalRowRange.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            totalRowRange.Style.Border.BottomBorderColor = Accent;

            return startRow + 1;
        }
    }
}
