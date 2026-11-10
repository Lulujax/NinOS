using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            string cleanPeriod = (periodLabel ?? string.Empty).Trim().ToUpperInvariant();

            // Agrupación por vendedora para crear una hoja por cada una (réplica exacta del PDF)
            var groups = rows
                .GroupBy(r => string.IsNullOrWhiteSpace(r.seller_name) ? "SIN VENDEDOR" : r.seller_name.Trim())
                .OrderBy(g => g.Key)
                .ToList();

            var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // =========================================================================
            // HOJAS INDIVIDUALES POR VENDEDORA (Una hoja por cada vendedora, igual que el PDF)
            // =========================================================================
            foreach (var g in groups)
            {
                string rawName = g.Key.Trim();
                string sheetName = MakeSafeSheetName(rawName);
                if (usedSheetNames.Contains(sheetName))
                {
                    sheetName = MakeSafeSheetName($"{rawName} 2");
                }
                usedSheetNames.Add(sheetName);

                var ws = wb.Worksheets.Add(sheetName);
                var sRows = g.ToList();

                // 1. ENCABEZADO SUPERIOR: REPORTE DE COMISIONES (Izquierda) | PERÍODO (Derecha)
                // Sin ningún "Filtro:", solo el período (ej. AÑO 2026 o OCTUBRE 2026)
                var titleLeft = ws.Range(1, 1, 1, 6);
                titleLeft.Merge();
                titleLeft.Value = "REPORTE DE COMISIONES";
                titleLeft.Style.Font.Bold = true;
                titleLeft.Style.Font.FontSize = 13;
                titleLeft.Style.Font.FontColor = XLColor.White;
                titleLeft.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
                titleLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                titleLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var titleRight = ws.Range(1, 7, 1, 9);
                titleRight.Merge();
                titleRight.Value = cleanPeriod;
                titleRight.Style.Font.Bold = true;
                titleRight.Style.Font.FontSize = 11;
                titleRight.Style.Font.FontColor = XLColor.White;
                titleRight.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
                titleRight.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                titleRight.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Row(1).Height = 28;

                ws.Row(2).Height = 6;

                // Fila 3: VENDEDORA
                ws.Cell(3, 1).Value = "VENDEDORA:";
                ws.Cell(3, 1).Style.Font.Bold = true;
                ws.Cell(3, 1).Style.Font.FontSize = 9.5;
                ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#555555");
                ws.Cell(3, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var sellerRange = ws.Range(3, 2, 3, 5);
                sellerRange.Merge();
                sellerRange.Value = g.Key.ToUpperInvariant();
                sellerRange.Style.Font.Bold = true;
                sellerRange.Style.Font.FontSize = 11;
                sellerRange.Style.Font.FontColor = XLColor.FromHtml("#1B3A2D");
                sellerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                ws.Range(3, 1, 3, 9).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                ws.Range(3, 1, 3, 9).Style.Border.BottomBorderColor = XLColor.FromHtml("#B0BEC5");
                ws.Row(3).Height = 22;

                ws.Row(4).Height = 8;

                // 2. ENCABEZADOS DE LA TABLA (Sin ningún signo $)
                int headerRow = 5;
                string[] headers = new[]
                {
                    "NRO", "CLIENTE", "MONTO FACTURADO", "FECHA DE PAGO",
                    "MONTO PAGADO", "PRONTO PAGO", "COMISION 10%", "TOTAL COMISION", "PAGO COMISION"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(headerRow, i + 1);
                    cell.Value = headers[i];
                    ExcelFlatExporter.StyleHeaderCell(cell, "#1B3A2D");
                }
                ws.Row(headerRow).Height = 24;

                // 3. DETALLE DE NOTAS DE LA VENDEDORA
                int rowIdx = headerRow + 1;
                bool alt = false;
                foreach (var r in sRows)
                {
                    var rowRange = ws.Range(rowIdx, 1, rowIdx, 9);
                    if (alt)
                    {
                        rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAF9");
                    }
                    rowRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    rowRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#E0E0E0");

                    // Col 1: NRO (Centrado)
                    var nroCell = ws.Cell(rowIdx, 1);
                    nroCell.SetValue(r.note_number);
                    nroCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    nroCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 2: CLIENTE (Alineado a la izquierda)
                    var cliCell = ws.Cell(rowIdx, 2);
                    cliCell.SetValue(r.customer_name);
                    cliCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    cliCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 3: MONTO FACTURADO (Centrado, número sin $)
                    var factCell = ws.Cell(rowIdx, 3);
                    factCell.SetValue(r.invoiced_amount);
                    factCell.Style.NumberFormat.Format = "#,##0.00";
                    factCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    factCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 4: FECHA DE PAGO (Centrado)
                    var fpCell = ws.Cell(rowIdx, 4);
                    fpCell.SetValue(string.IsNullOrWhiteSpace(r.note_payments_text) ? "-" : r.note_payments_text);
                    fpCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    fpCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 5: MONTO PAGADO (Centrado, número sin $)
                    var pagCell = ws.Cell(rowIdx, 5);
                    pagCell.SetValue(r.paid_amount);
                    pagCell.Style.NumberFormat.Format = "#,##0.00";
                    pagCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    pagCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 6: PRONTO PAGO (Centrado)
                    var ppCell = ws.Cell(rowIdx, 6);
                    ppCell.SetValue(string.IsNullOrWhiteSpace(r.early_payment_text) ? "-" : r.early_payment_text);
                    ppCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ppCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 7: COMISION 10% (Centrado, número sin $)
                    var com10Cell = ws.Cell(rowIdx, 7);
                    com10Cell.SetValue(r.amount_usd);
                    com10Cell.Style.NumberFormat.Format = "#,##0.00";
                    com10Cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    com10Cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 8: TOTAL COMISION (Centrado, número sin $)
                    var totCell = ws.Cell(rowIdx, 8);
                    totCell.SetValue(r.amount_usd);
                    totCell.Style.NumberFormat.Format = "#,##0.00";
                    totCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    totCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 9: PAGO COMISION (Centrado)
                    var pagComCell = ws.Cell(rowIdx, 9);
                    pagComCell.SetValue(string.IsNullOrWhiteSpace(r.commission_paid_date_text) ? "-" : r.commission_paid_date_text);
                    pagComCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    pagComCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    ws.Row(rowIdx).Height = 20;
                    rowIdx++;
                    alt = !alt;
                }

                int lastDataRow = rowIdx - 1;

                // 4. TABLA DE TOTALES DE LA VENDEDORA (Al pie a la derecha, igual que el PDF)
                rowIdx++; // Espacio
                ws.Row(rowIdx).Height = 10;
                rowIdx++;

                decimal sFacturado = sRows.Sum(x => x.invoiced_amount);
                decimal sPagado = sRows.Sum(x => x.paid_amount);
                decimal sComision = sRows.Sum(x => x.amount_usd);
                decimal sComPagada = sRows.Where(x => x.remaining_amount_usd <= 0.005m).Sum(x => x.amount_usd);
                decimal sComPendiente = sRows.Where(x => x.remaining_amount_usd > 0.005m).Sum(x => x.remaining_amount_usd);

                void AddSellerTotalLine(int row, string label, decimal value, string? hexText = null, bool isBold = true)
                {
                    var lblRange = ws.Range(row, 6, row, 7);
                    lblRange.Merge();
                    lblRange.Value = label;
                    lblRange.Style.Font.Bold = isBold;
                    lblRange.Style.Font.FontSize = 9;
                    lblRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    lblRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    var valCell = ws.Cell(row, 8);
                    valCell.SetValue(value);
                    valCell.Style.Font.Bold = isBold;
                    valCell.Style.Font.FontSize = 9.5;
                    valCell.Style.NumberFormat.Format = "#,##0.00";
                    valCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    valCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    if (!string.IsNullOrWhiteSpace(hexText))
                    {
                        valCell.Style.Font.FontColor = XLColor.FromHtml(hexText);
                        lblRange.Style.Font.FontColor = XLColor.FromHtml(hexText);
                    }

                    var fullRange = ws.Range(row, 6, row, 8);
                    fullRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    fullRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CFD8DC");
                    ws.Row(row).Height = 20;
                }

                AddSellerTotalLine(rowIdx++, "TOTAL FACTURADO:", sFacturado, null, true);
                AddSellerTotalLine(rowIdx++, "TOTAL PAGADO CLIENTES:", sPagado, null, true);
                AddSellerTotalLine(rowIdx++, "TOTAL COMISION:", sComision, null, true);
                AddSellerTotalLine(rowIdx++, "COMISION PAGADA:", sComPagada, "#2E7D32", true);
                AddSellerTotalLine(rowIdx++, "COMISION PENDIENTE:", sComPendiente, sComPendiente > 0.005m ? "#C62828" : "#757575", true);

                // Ajuste y expansión generosa de columnas en la hoja de la vendedora
                ws.Columns().AdjustToContents();
                ws.Column(1).Width = Math.Max(ws.Column(1).Width + 5, 12);  // NRO
                ws.Column(2).Width = Math.Max(ws.Column(2).Width + 8, 34);  // CLIENTE
                ws.Column(3).Width = Math.Max(ws.Column(3).Width + 6, 20);  // MONTO FACTURADO
                ws.Column(4).Width = Math.Max(ws.Column(4).Width + 6, 18);  // FECHA DE PAGO
                ws.Column(5).Width = Math.Max(ws.Column(5).Width + 6, 18);  // MONTO PAGADO
                ws.Column(6).Width = Math.Max(ws.Column(6).Width + 5, 16);  // PRONTO PAGO
                ws.Column(7).Width = Math.Max(ws.Column(7).Width + 6, 18);  // COMISION 10%
                ws.Column(8).Width = Math.Max(ws.Column(8).Width + 6, 18);  // TOTAL COMISION
                ws.Column(9).Width = Math.Max(ws.Column(9).Width + 6, 18);  // PAGO COMISION

                ws.SheetView.FreezeRows(headerRow);
                ws.Range(headerRow, 1, lastDataRow, 9).SetAutoFilter();
            }

            // =========================================================================
            // ÚLTIMA HOJA: RESUMEN GENERAL (Consolidado de todo el año o mes seleccionado)
            // =========================================================================
            var wsSummary = wb.Worksheets.Add("Resumen General");

            // Título principal del Resumen
            var sumTitleLeft = wsSummary.Range(1, 1, 1, 5);
            sumTitleLeft.Merge();
            sumTitleLeft.Value = "RESUMEN GENERAL DE COMISIONES";
            sumTitleLeft.Style.Font.Bold = true;
            sumTitleLeft.Style.Font.FontSize = 13;
            sumTitleLeft.Style.Font.FontColor = XLColor.White;
            sumTitleLeft.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
            sumTitleLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            sumTitleLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var sumTitleRight = wsSummary.Range(1, 6, 1, 7);
            sumTitleRight.Merge();
            sumTitleRight.Value = cleanPeriod;
            sumTitleRight.Style.Font.Bold = true;
            sumTitleRight.Style.Font.FontSize = 11;
            sumTitleRight.Style.Font.FontColor = XLColor.White;
            sumTitleRight.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
            sumTitleRight.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            sumTitleRight.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            wsSummary.Row(1).Height = 28;

            wsSummary.Row(2).Height = 6;

            // Subtítulo descriptivo
            var subTitle = wsSummary.Range(3, 1, 3, 7);
            subTitle.Merge();
            subTitle.Value = "Consolidado de todas las vendedoras del período.";
            subTitle.Style.Font.Italic = true;
            subTitle.Style.Font.FontSize = 9.5;
            subTitle.Style.Font.FontColor = XLColor.FromHtml("#555555");
            subTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            subTitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            wsSummary.Row(3).Height = 20;

            wsSummary.Row(4).Height = 8;

            // 1. BLOQUE DE TOTALES DEL PERÍODO (Tarjeta ejecutiva)
            decimal grand_facturado = rows.Sum(r => r.invoiced_amount);
            decimal grand_pagado = rows.Sum(r => r.paid_amount);
            decimal grand_comision = rows.Sum(r => r.amount_usd);
            decimal grand_pagada = rows.Where(r => r.remaining_amount_usd <= 0.005m).Sum(r => r.amount_usd);
            decimal grand_pendiente = rows.Where(r => r.remaining_amount_usd > 0.005m).Sum(r => r.remaining_amount_usd);
            int grand_notas = rows.Count;

            void WriteGrandSummaryRow(int row, string label, decimal value, string? hexText = null, string? hexBg = null)
            {
                var lblRange = wsSummary.Range(row, 1, row, 3);
                lblRange.Merge();
                lblRange.Value = label;
                lblRange.Style.Font.Bold = true;
                lblRange.Style.Font.FontSize = 9.5;
                lblRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var valCell = wsSummary.Cell(row, 4);
                valCell.SetValue(value);
                valCell.Style.Font.Bold = true;
                valCell.Style.Font.FontSize = 10;
                valCell.Style.NumberFormat.Format = "#,##0.00";
                valCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                valCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                if (!string.IsNullOrWhiteSpace(hexText))
                {
                    valCell.Style.Font.FontColor = XLColor.FromHtml(hexText);
                }

                if (!string.IsNullOrWhiteSpace(hexBg))
                {
                    var fullRow = wsSummary.Range(row, 1, row, 4);
                    fullRow.Style.Fill.BackgroundColor = XLColor.FromHtml(hexBg);
                }

                var fullRange = wsSummary.Range(row, 1, row, 4);
                fullRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                fullRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CFD8DC");
                wsSummary.Row(row).Height = 20;
            }

            WriteGrandSummaryRow(5, "TOTAL FACTURADO:", grand_facturado);
            WriteGrandSummaryRow(6, "TOTAL PAGADO CLIENTES:", grand_pagado);
            WriteGrandSummaryRow(7, "TOTAL COMISION:", grand_comision);
            WriteGrandSummaryRow(8, "COMISION PAGADA:", grand_pagada, "#1B5E20", "#E8F5E9");
            WriteGrandSummaryRow(9, "COMISION PENDIENTE:", grand_pendiente, "#B71C1C", "#FFEBEE");

            wsSummary.Row(10).Height = 12;

            // 2. TABLA DESGLOSADA POR VENDEDORA (Idéntica a la del PDF)
            int sumTableTitleRow = 11;
            var sumTableTitle = wsSummary.Range(sumTableTitleRow, 1, sumTableTitleRow, 7);
            sumTableTitle.Merge();
            sumTableTitle.Value = "CONSOLIDADO POR VENDEDORA";
            sumTableTitle.Style.Font.Bold = true;
            sumTableTitle.Style.Font.FontSize = 11;
            sumTableTitle.Style.Font.FontColor = XLColor.FromHtml("#1B3A2D");
            sumTableTitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            wsSummary.Row(sumTableTitleRow).Height = 22;

            int sumHeaderRow = 12;
            string[] sumHeaders = new[]
            {
                "VENDEDORA", "NOTAS", "FACTURADO", "PAGADO",
                "COMISION", "PAGADA", "PENDIENTE"
            };

            for (int k = 0; k < sumHeaders.Length; k++)
            {
                var cell = wsSummary.Cell(sumHeaderRow, k + 1);
                cell.Value = sumHeaders[k];
                ExcelFlatExporter.StyleHeaderCell(cell, "#1B3A2D");
            }
            wsSummary.Row(sumHeaderRow).Height = 24;

            int sumRowIdx = sumHeaderRow + 1;
            bool sumAlt = false;

            foreach (var g in groups)
            {
                var sRows = g.ToList();
                int sNotas = sRows.Count;
                decimal sFact = sRows.Sum(r => r.invoiced_amount);
                decimal sPag = sRows.Sum(r => r.paid_amount);
                decimal sCom = sRows.Sum(r => r.amount_usd);
                decimal sPagada = sRows.Where(r => r.remaining_amount_usd <= 0.005m).Sum(r => r.amount_usd);
                decimal sPend = sRows.Where(r => r.remaining_amount_usd > 0.005m).Sum(r => r.remaining_amount_usd);

                var sRange = wsSummary.Range(sumRowIdx, 1, sumRowIdx, 7);
                if (sumAlt)
                {
                    sRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAF9");
                }
                sRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                sRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#E0E0E0");

                // Vendedora (Izquierda)
                var vCell = wsSummary.Cell(sumRowIdx, 1);
                vCell.SetValue(g.Key);
                vCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                vCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                vCell.Style.Font.Bold = true;

                // Notas (Centrado)
                var nCell = wsSummary.Cell(sumRowIdx, 2);
                nCell.SetValue(sNotas);
                nCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                nCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Facturado (Centrado, número sin $)
                var fCell = wsSummary.Cell(sumRowIdx, 3);
                fCell.SetValue(sFact);
                fCell.Style.NumberFormat.Format = "#,##0.00";
                fCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                fCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Pagado (Centrado, número sin $)
                var pCell = wsSummary.Cell(sumRowIdx, 4);
                pCell.SetValue(sPag);
                pCell.Style.NumberFormat.Format = "#,##0.00";
                pCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                pCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Comisión (Centrado, número sin $)
                var cCell = wsSummary.Cell(sumRowIdx, 5);
                cCell.SetValue(sCom);
                cCell.Style.NumberFormat.Format = "#,##0.00";
                cCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Pagada (Centrado, número sin $)
                var cpCell = wsSummary.Cell(sumRowIdx, 6);
                cpCell.SetValue(sPagada);
                cpCell.Style.NumberFormat.Format = "#,##0.00";
                cpCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cpCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cpCell.Style.Font.FontColor = XLColor.FromHtml("#2E7D32");

                // Pendiente (Centrado, número sin $)
                var cpendCell = wsSummary.Cell(sumRowIdx, 7);
                cpendCell.SetValue(sPend);
                cpendCell.Style.NumberFormat.Format = "#,##0.00";
                cpendCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cpendCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                if (sPend > 0.005m)
                {
                    cpendCell.Style.Font.FontColor = XLColor.FromHtml("#C62828");
                }

                wsSummary.Row(sumRowIdx).Height = 20;
                sumRowIdx++;
                sumAlt = !sumAlt;
            }

            int lastSumDataRow = sumRowIdx - 1;

            // Fila de TOTAL GENERAL en el Resumen
            int sumGrandTotalRow = sumRowIdx;
            var totGenLabel = wsSummary.Cell(sumGrandTotalRow, 1);
            totGenLabel.SetValue("TOTAL GENERAL");
            totGenLabel.Style.Font.Bold = true;
            totGenLabel.Style.Font.FontSize = 10;
            totGenLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            totGenLabel.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            wsSummary.Cell(sumGrandTotalRow, 2).FormulaA1 = $"SUM(B{sumHeaderRow + 1}:B{lastSumDataRow})";
            wsSummary.Cell(sumGrandTotalRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsSummary.Cell(sumGrandTotalRow, 2).Style.Font.Bold = true;

            wsSummary.Cell(sumGrandTotalRow, 3).FormulaA1 = $"SUM(C{sumHeaderRow + 1}:C{lastSumDataRow})";
            wsSummary.Cell(sumGrandTotalRow, 3).Style.NumberFormat.Format = "#,##0.00";
            wsSummary.Cell(sumGrandTotalRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsSummary.Cell(sumGrandTotalRow, 3).Style.Font.Bold = true;

            wsSummary.Cell(sumGrandTotalRow, 4).FormulaA1 = $"SUM(D{sumHeaderRow + 1}:D{lastSumDataRow})";
            wsSummary.Cell(sumGrandTotalRow, 4).Style.NumberFormat.Format = "#,##0.00";
            wsSummary.Cell(sumGrandTotalRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsSummary.Cell(sumGrandTotalRow, 4).Style.Font.Bold = true;

            wsSummary.Cell(sumGrandTotalRow, 5).FormulaA1 = $"SUM(E{sumHeaderRow + 1}:E{lastSumDataRow})";
            wsSummary.Cell(sumGrandTotalRow, 5).Style.NumberFormat.Format = "#,##0.00";
            wsSummary.Cell(sumGrandTotalRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsSummary.Cell(sumGrandTotalRow, 5).Style.Font.Bold = true;

            wsSummary.Cell(sumGrandTotalRow, 6).FormulaA1 = $"SUM(F{sumHeaderRow + 1}:F{lastSumDataRow})";
            wsSummary.Cell(sumGrandTotalRow, 6).Style.NumberFormat.Format = "#,##0.00";
            wsSummary.Cell(sumGrandTotalRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsSummary.Cell(sumGrandTotalRow, 6).Style.Font.Bold = true;

            wsSummary.Cell(sumGrandTotalRow, 7).FormulaA1 = $"SUM(G{sumHeaderRow + 1}:G{lastSumDataRow})";
            wsSummary.Cell(sumGrandTotalRow, 7).Style.NumberFormat.Format = "#,##0.00";
            wsSummary.Cell(sumGrandTotalRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsSummary.Cell(sumGrandTotalRow, 7).Style.Font.Bold = true;

            ExcelFlatExporter.StyleTotalRow(wsSummary.Range(sumGrandTotalRow, 1, sumGrandTotalRow, 7), "#E8F5E9");
            wsSummary.Row(sumGrandTotalRow).Height = 22;

            // Ajuste y expansión generosa de columnas en Resumen General
            wsSummary.Columns().AdjustToContents();
            wsSummary.Column(1).Width = Math.Max(wsSummary.Column(1).Width + 6, 28);  // VENDEDORA
            wsSummary.Column(2).Width = Math.Max(wsSummary.Column(2).Width + 5, 14);  // NOTAS
            wsSummary.Column(3).Width = Math.Max(wsSummary.Column(3).Width + 6, 22);  // FACTURADO
            wsSummary.Column(4).Width = Math.Max(wsSummary.Column(4).Width + 6, 20);  // PAGADO
            wsSummary.Column(5).Width = Math.Max(wsSummary.Column(5).Width + 6, 20);  // COMISION
            wsSummary.Column(6).Width = Math.Max(wsSummary.Column(6).Width + 6, 20);  // PAGADA
            wsSummary.Column(7).Width = Math.Max(wsSummary.Column(7).Width + 6, 22);  // PENDIENTE

            // Guardar con diálogo
            string scopeTag = string.IsNullOrWhiteSpace(sellerScope) ? "Todas" : sellerScope.Trim().Replace(" ", "_");
            string periodTag = periodLabel.Trim().Replace(" ", "_");
            string defaultName = $"Reporte_Comisiones_{periodTag}_{scopeTag}.xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar reporte de comisiones en Excel");
        }

        private static string MakeSafeSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Vendedora";
            string clean = name.Replace("\\", "").Replace("/", "").Replace("?", "").Replace("*", "")
                               .Replace("[", "").Replace("]", "").Replace(":", "").Trim();
            if (clean.Length > 28) clean = clean.Substring(0, 28);
            return string.IsNullOrWhiteSpace(clean) ? "Vendedora" : clean;
        }
    }
}
