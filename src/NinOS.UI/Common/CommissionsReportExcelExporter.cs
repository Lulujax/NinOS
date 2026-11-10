using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Common
{
    public static class CommissionsReportExcelExporter
    {
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

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
            // HOJAS INDIVIDUALES POR VENDEDORA (Una hoja por cada vendedora, réplica del PDF)
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
                titleLeft.Style.Font.FontSize = 12;
                titleLeft.Style.Font.FontColor = XLColor.White;
                titleLeft.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
                titleLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                titleLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var titleRight = ws.Range(1, 7, 1, 9);
                titleRight.Merge();
                titleRight.Value = cleanPeriod;
                titleRight.Style.Font.Bold = true;
                titleRight.Style.Font.FontSize = 10;
                titleRight.Style.Font.FontColor = XLColor.White;
                titleRight.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
                titleRight.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                titleRight.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Row(1).Height = 26;

                ws.Row(2).Height = 5;

                // Fila 3: VENDEDORA
                ws.Cell(3, 1).Value = "VENDEDORA:";
                ws.Cell(3, 1).Style.Font.Bold = true;
                ws.Cell(3, 1).Style.Font.FontSize = 8;
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
                ws.Range(3, 1, 3, 9).Style.Border.BottomBorderColor = XLColor.Black;
                ws.Row(3).Height = 20;

                ws.Row(4).Height = 6;

                // 2. ENCABEZADOS DE LA TABLA (Titulos en 7, sin ningún signo $)
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
                    cell.Style.Font.FontSize = 7;
                }
                ws.Row(headerRow).Height = 22;

                // 3. DETALLE DE NOTAS DE LA VENDEDORA (Datos en 11)
                int rowIdx = headerRow + 1;
                bool alt = false;
                foreach (var r in sRows)
                {
                    var rowRange = ws.Range(rowIdx, 1, rowIdx, 9);
                    if (alt)
                    {
                        rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAF9");
                    }

                    // Col 1: NRO (Centrado)
                    var nroCell = ws.Cell(rowIdx, 1);
                    nroCell.SetValue(r.note_number);
                    nroCell.Style.Font.FontSize = 11;
                    nroCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    nroCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 2: CLIENTE (Alineado a la izquierda, con WrapText)
                    var cliCell = ws.Cell(rowIdx, 2);
                    cliCell.SetValue(r.customer_name);
                    cliCell.Style.Font.FontSize = 11;
                    cliCell.Style.Alignment.WrapText = true;
                    cliCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    cliCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 3: MONTO FACTURADO (Centrado, número sin $)
                    var factCell = ws.Cell(rowIdx, 3);
                    factCell.SetValue(r.invoiced_amount);
                    factCell.Style.Font.FontSize = 11;
                    factCell.Style.NumberFormat.Format = "#,##0.00";
                    factCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    factCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 4: FECHA DE PAGO (Centrado, con Enter / WrapText para pagos parciales/abonos)
                    var fpCell = ws.Cell(rowIdx, 4);
                    string pDateText = !string.IsNullOrWhiteSpace(r.note_payments_text)
                        ? r.note_payments_text
                        : (r.note_last_payment_date?.ToString("dd/MM/yyyy") ?? "-");
                    pDateText = pDateText.Replace("\r\n", "\n").Replace("\r", "\n").Replace("$", string.Empty);
                    fpCell.SetValue(pDateText);
                    fpCell.Style.Font.FontSize = 11;
                    fpCell.Style.Alignment.WrapText = true;
                    fpCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    fpCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 5: MONTO PAGADO (Centrado, número sin $)
                    var pagCell = ws.Cell(rowIdx, 5);
                    pagCell.SetValue(r.paid_amount);
                    pagCell.Style.Font.FontSize = 11;
                    pagCell.Style.NumberFormat.Format = "#,##0.00";
                    pagCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    pagCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 6: PRONTO PAGO (Centrado)
                    var ppCell = ws.Cell(rowIdx, 6);
                    ppCell.SetValue(string.IsNullOrWhiteSpace(r.early_payment_text) ? "-" : r.early_payment_text);
                    ppCell.Style.Font.FontSize = 11;
                    ppCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ppCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 7: COMISION 10% (Centrado, número sin $)
                    var com10Cell = ws.Cell(rowIdx, 7);
                    com10Cell.SetValue(r.amount_usd);
                    com10Cell.Style.Font.FontSize = 11;
                    com10Cell.Style.NumberFormat.Format = "#,##0.00";
                    com10Cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    com10Cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 8: TOTAL COMISION (Centrado, número sin $)
                    var totCell = ws.Cell(rowIdx, 8);
                    totCell.SetValue(r.amount_usd);
                    totCell.Style.Font.FontSize = 11;
                    totCell.Style.NumberFormat.Format = "#,##0.00";
                    totCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    totCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 9: PAGO COMISION (Centrado)
                    var pagComCell = ws.Cell(rowIdx, 9);
                    pagComCell.SetValue(string.IsNullOrWhiteSpace(r.commission_paid_date_text) ? "-" : r.commission_paid_date_text);
                    pagComCell.Style.Font.FontSize = 11;
                    pagComCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    pagComCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Ajuste de altura dinámica según cantidad de líneas en FECHA DE PAGO o CLIENTE largo
                    int pDateLines = Math.Max(1, pDateText.Split('\n').Length);
                    string cName = r.customer_name ?? string.Empty;
                    int nameLines = Math.Max(1, (int)Math.Ceiling((double)cName.Length / 19));
                    int lineCount = Math.Max(pDateLines, nameLines);
                    ws.Row(rowIdx).Height = Math.Max(20, lineCount * 14.5);

                    rowIdx++;
                    alt = !alt;
                }

                int lastDataRow = rowIdx - 1;

                // BORDES COMPLETOS EN NEGRO PARA TODA LA TABLA DE NOTAS
                var notesTableRange = ws.Range(headerRow, 1, lastDataRow, 9);
                notesTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                notesTableRange.Style.Border.InsideBorderColor = XLColor.Black;
                notesTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                notesTableRange.Style.Border.OutsideBorderColor = XLColor.Black;

                // 4. TABLA DE TOTALES Y LIQUIDACIÓN BANCARIA DE LA VENDEDORA
                // (Idéntica a la vista previa del comprobante individual)
                decimal sub_total = sRows.Sum(x => x.amount_usd);
                decimal comision_pagada = sRows.Sum(x => x.paid_amount_usd > 0 ? x.paid_amount_usd : (x.is_paid || x.remaining_amount_usd <= 0.005m ? x.amount_usd : 0m));

                var paid_payments = sRows
                    .Where(r => r.paid_amount_usd > 0 || !string.IsNullOrWhiteSpace(r.reference_number) || !string.IsNullOrWhiteSpace(r.bank_name) || r.amount_bs > 0)
                    .GroupBy(r => new
                    {
                        Bank = string.IsNullOrWhiteSpace(r.bank_name) ? "-" : r.bank_name.Trim(),
                        Ref = string.IsNullOrWhiteSpace(r.reference_number) ? "-" : r.reference_number.Trim(),
                        Date = !string.IsNullOrWhiteSpace(r.commission_paid_date_text)
                            ? r.commission_paid_date_text
                            : (r.commission_paid_date != null ? r.commission_paid_date.Value.ToString("dd-MMM", Ve).Replace(".", "").ToLowerInvariant() : "-")
                    })
                    .Select(group => new
                    {
                        Bank = group.Key.Bank,
                        Ref = group.Key.Ref,
                        Date = group.Key.Date,
                        AmountBs = group.Sum(x => x.amount_bs),
                        AmountUsd = group.Sum(x => x.paid_amount_usd > 0 ? x.paid_amount_usd : x.amount_usd)
                    })
                    .ToList();

                if (paid_payments.Count == 0)
                {
                    paid_payments.Add(new
                    {
                        Bank = "-",
                        Ref = "-",
                        Date = "-",
                        AmountBs = 0m,
                        AmountUsd = comision_pagada
                    });
                }

                decimal total_pay_bs = paid_payments.Sum(p => p.AmountBs);
                decimal total_pay_usd = paid_payments.Sum(p => p.AmountUsd);
                if (total_pay_usd <= 0 && comision_pagada > 0) total_pay_usd = comision_pagada;

                rowIdx++; // Fila vacía de separación
                ws.Row(rowIdx).Height = 8;
                rowIdx++;

                // Fila: SUB TOTAL
                var subTotalLabel = ws.Range(rowIdx, 5, rowIdx, 7);
                subTotalLabel.Merge();
                subTotalLabel.Value = "SUB TOTAL";
                subTotalLabel.Style.Font.Bold = true;
                subTotalLabel.Style.Font.FontSize = 7;
                subTotalLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                subTotalLabel.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var subTotalVal = ws.Cell(rowIdx, 8);
                subTotalVal.SetValue(sub_total);
                subTotalVal.Style.Font.Bold = true;
                subTotalVal.Style.Font.FontSize = 11;
                subTotalVal.Style.NumberFormat.Format = "#,##0.00";
                subTotalVal.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                subTotalVal.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var subTotalRange = ws.Range(rowIdx, 5, rowIdx, 8);
                subTotalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                subTotalRange.Style.Border.OutsideBorderColor = XLColor.Black;
                subTotalRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                subTotalRange.Style.Border.InsideBorderColor = XLColor.Black;
                ws.Row(rowIdx).Height = 20;
                rowIdx++;

                // Fila: COMISION PAGADA
                var comPagLabel = ws.Range(rowIdx, 5, rowIdx, 7);
                comPagLabel.Merge();
                comPagLabel.Value = "COMISION PAGADA";
                comPagLabel.Style.Font.Bold = true;
                comPagLabel.Style.Font.FontSize = 7;
                comPagLabel.Style.Font.FontColor = XLColor.FromHtml("#1B3A2D");
                comPagLabel.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
                comPagLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                comPagLabel.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var comPagVal = ws.Cell(rowIdx, 8);
                comPagVal.SetValue(comision_pagada);
                comPagVal.Style.Font.Bold = true;
                comPagVal.Style.Font.FontSize = 11;
                comPagVal.Style.Font.FontColor = XLColor.FromHtml("#1B3A2D");
                comPagVal.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
                comPagVal.Style.NumberFormat.Format = "#,##0.00";
                comPagVal.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                comPagVal.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var comPagRange = ws.Range(rowIdx, 5, rowIdx, 8);
                comPagRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                comPagRange.Style.Border.OutsideBorderColor = XLColor.Black;
                comPagRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                comPagRange.Style.Border.InsideBorderColor = XLColor.Black;
                ws.Row(rowIdx).Height = 22;
                rowIdx++;

                // Fila de separación
                ws.Row(rowIdx).Height = 5;
                rowIdx++;

                // TABLA BANCARIA
                // Columnas: E: BANCO | F: NRO REF | G: MONTO BS | H: MONTO $ | I: FECHA
                int bankHeaderRow = rowIdx;
                string[] bankHeaders = new[] { "BANCO", "NRO REF", "MONTO BS", "MONTO $", "FECHA" };
                for (int k = 0; k < bankHeaders.Length; k++)
                {
                    var bCell = ws.Cell(bankHeaderRow, 5 + k);
                    bCell.Value = bankHeaders[k];
                    ExcelFlatExporter.StyleHeaderCell(bCell, "#1B3A2D");
                    bCell.Style.Font.FontSize = 7;
                }
                ws.Row(bankHeaderRow).Height = 20;
                rowIdx++;

                // Filas de datos bancarios (en 11)
                foreach (var pay in paid_payments)
                {
                    int bRow = rowIdx;
                    var bNameCell = ws.Cell(bRow, 5);
                    bNameCell.SetValue(pay.Bank);
                    bNameCell.Style.Font.FontSize = 11;
                    bNameCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    bNameCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    var bRefCell = ws.Cell(bRow, 6);
                    bRefCell.SetValue(pay.Ref);
                    bRefCell.Style.Font.FontSize = 11;
                    bRefCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    bRefCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    var bsCell = ws.Cell(bRow, 7);
                    bsCell.SetValue(pay.AmountBs);
                    bsCell.Style.Font.FontSize = 11;
                    bsCell.Style.NumberFormat.Format = "#,##0.00";
                    bsCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    bsCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    var usdCell = ws.Cell(bRow, 8);
                    usdCell.SetValue(pay.AmountUsd);
                    usdCell.Style.Font.FontSize = 11;
                    usdCell.Style.NumberFormat.Format = "#,##0.00";
                    usdCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    usdCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    var bDateCell = ws.Cell(bRow, 9);
                    bDateCell.SetValue(pay.Date);
                    bDateCell.Style.Font.FontSize = 11;
                    bDateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    bDateCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    ws.Row(bRow).Height = 20;
                    rowIdx++;
                }

                // Fila: TOTAL COMISION (BANCO y NRO REF combinados) | MONTO BS | MONTO $
                int bankTotalRow = rowIdx;
                var totComBankLabel = ws.Range(bankTotalRow, 5, bankTotalRow, 6);
                totComBankLabel.Merge();
                totComBankLabel.Value = "TOTAL COMISION";
                totComBankLabel.Style.Font.Bold = true;
                totComBankLabel.Style.Font.FontSize = 7;
                totComBankLabel.Style.Font.FontColor = XLColor.White;
                totComBankLabel.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
                totComBankLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                totComBankLabel.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var totBsCell = ws.Cell(bankTotalRow, 7);
                totBsCell.SetValue(total_pay_bs);
                totBsCell.Style.Font.Bold = true;
                totBsCell.Style.Font.FontSize = 11;
                totBsCell.Style.NumberFormat.Format = "#,##0.00";
                totBsCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                totBsCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                totBsCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");

                var totUsdCell = ws.Cell(bankTotalRow, 8);
                totUsdCell.SetValue(total_pay_usd);
                totUsdCell.Style.Font.Bold = true;
                totUsdCell.Style.Font.FontSize = 11;
                totUsdCell.Style.NumberFormat.Format = "#,##0.00";
                totUsdCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                totUsdCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                totUsdCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");

                var emptyDateCell = ws.Cell(bankTotalRow, 9);
                emptyDateCell.Value = "";
                emptyDateCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
                ws.Row(bankTotalRow).Height = 22;

                // BORDES COMPLETOS EN NEGRO PARA LA TABLA BANCARIA
                var bankTableRange = ws.Range(bankHeaderRow, 5, bankTotalRow, 9);
                bankTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                bankTableRange.Style.Border.OutsideBorderColor = XLColor.Black;
                bankTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                bankTableRange.Style.Border.InsideBorderColor = XLColor.Black;

                // 5. ANCHO COMPACTO DE COLUMNAS PARA QUE QUEPA AL 100% EN PANTALLA 1280x720 (TOTAL = 102)
                ws.Column(1).Width = 7;   // NRO
                ws.Column(2).Width = 22;  // CLIENTE
                ws.Column(3).Width = 11;  // MONTO FACTURADO
                ws.Column(4).Width = 13;  // FECHA DE PAGO
                ws.Column(5).Width = 11;  // MONTO PAGADO / BANCO
                ws.Column(6).Width = 8;   // PRONTO PAGO / NRO REF
                ws.Column(7).Width = 10;  // COMISION 10% / MONTO BS
                ws.Column(8).Width = 10;  // TOTAL COMISION / MONTO $
                ws.Column(9).Width = 10;  // PAGO COMISION / FECHA

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
            sumTitleLeft.Style.Font.FontSize = 12;
            sumTitleLeft.Style.Font.FontColor = XLColor.White;
            sumTitleLeft.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
            sumTitleLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            sumTitleLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var sumTitleRight = wsSummary.Range(1, 6, 1, 7);
            sumTitleRight.Merge();
            sumTitleRight.Value = cleanPeriod;
            sumTitleRight.Style.Font.Bold = true;
            sumTitleRight.Style.Font.FontSize = 10;
            sumTitleRight.Style.Font.FontColor = XLColor.White;
            sumTitleRight.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
            sumTitleRight.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            sumTitleRight.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            wsSummary.Row(1).Height = 26;

            wsSummary.Row(2).Height = 5;

            // Subtítulo descriptivo
            var subTitle = wsSummary.Range(3, 1, 3, 7);
            subTitle.Merge();
            subTitle.Value = "Consolidado de todas las vendedoras del período.";
            subTitle.Style.Font.Italic = true;
            subTitle.Style.Font.FontSize = 9;
            subTitle.Style.Font.FontColor = XLColor.FromHtml("#555555");
            subTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            subTitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            wsSummary.Row(3).Height = 20;

            wsSummary.Row(4).Height = 6;

            // TABLA DESGLOSADA POR VENDEDORA DIRECTAMENTE (Sin el bloque superior individual)
            int sumHeaderRow = 5;
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
                cell.Style.Font.FontSize = 7;
            }
            wsSummary.Row(sumHeaderRow).Height = 22;

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

                // Vendedora (Izquierda, FontSize 11)
                var vCell = wsSummary.Cell(sumRowIdx, 1);
                vCell.SetValue(g.Key);
                vCell.Style.Font.FontSize = 11;
                vCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                vCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                vCell.Style.Font.Bold = true;

                // Notas (Centrado, FontSize 11)
                var nCell = wsSummary.Cell(sumRowIdx, 2);
                nCell.SetValue(sNotas);
                nCell.Style.Font.FontSize = 11;
                nCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                nCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Facturado (Centrado, número sin $, FontSize 11)
                var fCell = wsSummary.Cell(sumRowIdx, 3);
                fCell.SetValue(sFact);
                fCell.Style.Font.FontSize = 11;
                fCell.Style.NumberFormat.Format = "#,##0.00";
                fCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                fCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Pagado (Centrado, número sin $, FontSize 11)
                var pCell = wsSummary.Cell(sumRowIdx, 4);
                pCell.SetValue(sPag);
                pCell.Style.Font.FontSize = 11;
                pCell.Style.NumberFormat.Format = "#,##0.00";
                pCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                pCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Comisión (Centrado, número sin $, FontSize 11)
                var cCell = wsSummary.Cell(sumRowIdx, 5);
                cCell.SetValue(sCom);
                cCell.Style.Font.FontSize = 11;
                cCell.Style.NumberFormat.Format = "#,##0.00";
                cCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Pagada (Centrado, número sin $, FontSize 11)
                var cpCell = wsSummary.Cell(sumRowIdx, 6);
                cpCell.SetValue(sPagada);
                cpCell.Style.Font.FontSize = 11;
                cpCell.Style.NumberFormat.Format = "#,##0.00";
                cpCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cpCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cpCell.Style.Font.FontColor = XLColor.FromHtml("#2E7D32");

                // Pendiente (Centrado, número sin $, FontSize 11)
                var cpendCell = wsSummary.Cell(sumRowIdx, 7);
                cpendCell.SetValue(sPend);
                cpendCell.Style.Font.FontSize = 11;
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
            wsSummary.Cell(sumGrandTotalRow, 2).Style.Font.FontSize = 11;
            wsSummary.Cell(sumGrandTotalRow, 2).Style.Font.Bold = true;

            wsSummary.Cell(sumGrandTotalRow, 3).FormulaA1 = $"SUM(C{sumHeaderRow + 1}:C{lastSumDataRow})";
            wsSummary.Cell(sumGrandTotalRow, 3).Style.NumberFormat.Format = "#,##0.00";
            wsSummary.Cell(sumGrandTotalRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsSummary.Cell(sumGrandTotalRow, 3).Style.Font.FontSize = 11;
            wsSummary.Cell(sumGrandTotalRow, 3).Style.Font.Bold = true;

            wsSummary.Cell(sumGrandTotalRow, 4).FormulaA1 = $"SUM(D{sumHeaderRow + 1}:D{lastSumDataRow})";
            wsSummary.Cell(sumGrandTotalRow, 4).Style.NumberFormat.Format = "#,##0.00";
            wsSummary.Cell(sumGrandTotalRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsSummary.Cell(sumGrandTotalRow, 4).Style.Font.FontSize = 11;
            wsSummary.Cell(sumGrandTotalRow, 4).Style.Font.Bold = true;

            wsSummary.Cell(sumGrandTotalRow, 5).FormulaA1 = $"SUM(E{sumHeaderRow + 1}:E{lastSumDataRow})";
            wsSummary.Cell(sumGrandTotalRow, 5).Style.NumberFormat.Format = "#,##0.00";
            wsSummary.Cell(sumGrandTotalRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsSummary.Cell(sumGrandTotalRow, 5).Style.Font.FontSize = 11;
            wsSummary.Cell(sumGrandTotalRow, 5).Style.Font.Bold = true;

            wsSummary.Cell(sumGrandTotalRow, 6).FormulaA1 = $"SUM(F{sumHeaderRow + 1}:F{lastSumDataRow})";
            wsSummary.Cell(sumGrandTotalRow, 6).Style.NumberFormat.Format = "#,##0.00";
            wsSummary.Cell(sumGrandTotalRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsSummary.Cell(sumGrandTotalRow, 6).Style.Font.FontSize = 11;
            wsSummary.Cell(sumGrandTotalRow, 6).Style.Font.Bold = true;

            wsSummary.Cell(sumGrandTotalRow, 7).FormulaA1 = $"SUM(G{sumHeaderRow + 1}:G{lastSumDataRow})";
            wsSummary.Cell(sumGrandTotalRow, 7).Style.NumberFormat.Format = "#,##0.00";
            wsSummary.Cell(sumGrandTotalRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsSummary.Cell(sumGrandTotalRow, 7).Style.Font.FontSize = 11;
            wsSummary.Cell(sumGrandTotalRow, 7).Style.Font.Bold = true;

            ExcelFlatExporter.StyleTotalRow(wsSummary.Range(sumGrandTotalRow, 1, sumGrandTotalRow, 7), "#E8F5E9");
            wsSummary.Row(sumGrandTotalRow).Height = 22;

            // BORDES COMPLETOS EN NEGRO PARA LA TABLA DEL RESUMEN
            var sumTableRange = wsSummary.Range(sumHeaderRow, 1, sumGrandTotalRow, 7);
            sumTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            sumTableRange.Style.Border.OutsideBorderColor = XLColor.Black;
            sumTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            sumTableRange.Style.Border.InsideBorderColor = XLColor.Black;

            // Ancho compacto de columnas en Resumen General para pantalla 1280x720 (TOTAL = 100)
            wsSummary.Column(1).Width = 22;  // VENDEDORA
            wsSummary.Column(2).Width = 8;   // NOTAS
            wsSummary.Column(3).Width = 14;  // FACTURADO
            wsSummary.Column(4).Width = 14;  // PAGADO
            wsSummary.Column(5).Width = 14;  // COMISION
            wsSummary.Column(6).Width = 14;  // PAGADA
            wsSummary.Column(7).Width = 14;  // PENDIENTE

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
