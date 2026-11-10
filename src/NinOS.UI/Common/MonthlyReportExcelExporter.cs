using System;
using System.Collections.Generic;
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
            string cleanPeriod = (report.month ?? string.Empty).Trim().ToUpperInvariant();
            string mode = string.IsNullOrWhiteSpace(report.group_mode) ? "Por Vendedor" : report.group_mode;
            bool isByZone = mode == "Por Zona";
            string groupBadge = isByZone ? "ZONA" : "VENDEDOR";

            var rows = report.rows ?? new List<monthly_report_row_dto>();

            // Agrupación de filas (Por Vendedor o Por Zona, réplica del PDF)
            var groups = isByZone
                ? rows.GroupBy(r => string.IsNullOrWhiteSpace(r.zone_name) ? "SIN ZONA" : r.zone_name.Trim()).OrderBy(g => g.Key).ToList()
                : rows.GroupBy(r => string.IsNullOrWhiteSpace(r.seller_name) ? "SIN VENDEDOR" : r.seller_name.Trim()).OrderBy(g => g.Key).ToList();

            var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (groups.Count == 0)
            {
                var wsEmpty = wb.Worksheets.Add("Reporte");
                wsEmpty.Cell(1, 1).SetValue(report.empty_text);
                wsEmpty.Cell(1, 1).Style.Font.Italic = true;
                wsEmpty.Cell(1, 1).Style.Font.FontSize = 11;
                string emptyName = Path.GetFileNameWithoutExtension(MonthlyReportPdfGenerator.get_default_file_name(report)) + ".xlsx";
                ExcelFlatExporter.SaveWithDialog(wb, emptyName, "Guardar reporte en Excel");
                return;
            }

            // =========================================================================
            // HOJAS INDIVIDUALES POR GRUPO (Una hoja por cada Vendedora o Zona)
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

                // Armar lista de columnas según las opciones activas del reporte
                var headers = new List<string> { "CORRELATIVO", "FECHA" };
                if (report.show_dispatch_date_column) headers.Add("DESPACHO");
                headers.Add("CLIENTE");
                headers.Add(isByZone ? "VENDEDOR" : "ZONA");
                headers.Add("MONTO");
                if (report.show_paid_balance_summary)
                {
                    headers.Add("ABONADO");
                    headers.Add("SALDO");
                }
                headers.Add("ESTADO");

                int totalCols = headers.Count;

                // 1. ENCABEZADO SUPERIOR INSTITUCIONAL
                var titleLeft = ws.Range(1, 1, 1, Math.Max(1, totalCols - 2));
                titleLeft.Merge();
                titleLeft.Value = report.title;
                titleLeft.Style.Font.Bold = true;
                titleLeft.Style.Font.FontSize = 12;
                titleLeft.Style.Font.FontColor = XLColor.White;
                titleLeft.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
                titleLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                titleLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var titleRight = ws.Range(1, Math.Max(2, totalCols - 1), 1, totalCols);
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

                // Fila 3: Metadato del Grupo (VENDEDOR o ZONA)
                ws.Cell(3, 1).Value = $"{groupBadge}:";
                ws.Cell(3, 1).Style.Font.Bold = true;
                ws.Cell(3, 1).Style.Font.FontSize = 8;
                ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#555555");
                ws.Cell(3, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var groupRange = ws.Range(3, 2, 3, Math.Min(5, totalCols));
                groupRange.Merge();
                groupRange.Value = g.Key.ToUpperInvariant();
                groupRange.Style.Font.Bold = true;
                groupRange.Style.Font.FontSize = 11;
                groupRange.Style.Font.FontColor = XLColor.FromHtml("#1B3A2D");
                groupRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                ws.Range(3, 1, 3, totalCols).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                ws.Range(3, 1, 3, totalCols).Style.Border.BottomBorderColor = XLColor.Black;
                ws.Row(3).Height = 20;

                ws.Row(4).Height = 6;

                // 2. ENCABEZADOS DE LA TABLA (Titulos en tamaño 7)
                int headerRow = 5;
                for (int i = 0; i < headers.Count; i++)
                {
                    var cell = ws.Cell(headerRow, i + 1);
                    cell.Value = headers[i];
                    ExcelFlatExporter.StyleHeaderCell(cell, "#1B3A2D");
                    cell.Style.Font.FontSize = 7;
                }
                ws.Row(headerRow).Height = 22;

                // 3. DETALLE DE NOTAS (Datos en tamaño 11)
                var srows = g
                    .OrderBy(r => report.ordenar_zombies_primero && r.is_customer_ghost ? 0 : 1)
                    .ThenBy(r => r.date)
                    .ThenBy(r => SeriesCalculator.ParseCorrelative(r.document_number))
                    .ThenBy(r => r.document_number ?? string.Empty)
                    .ToList();

                int rowIdx = headerRow + 1;
                bool alt = false;

                foreach (var r in srows)
                {
                    bool is_annulled = string.Equals(r.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase);
                    var rowRange = ws.Range(rowIdx, 1, rowIdx, totalCols);
                    if (is_annulled)
                    {
                        rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE");
                    }
                    else if (alt)
                    {
                        rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAF9");
                    }

                    int colIdx = 1;

                    // Correlativo (Centrado, 11)
                    var numCell = ws.Cell(rowIdx, colIdx++);
                    numCell.SetValue(r.document_number);
                    numCell.Style.Font.FontSize = 11;
                    numCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    numCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Fecha (Centrado, 11)
                    var dtCell = ws.Cell(rowIdx, colIdx++);
                    dtCell.SetValue(r.fecha_display);
                    dtCell.Style.Font.FontSize = 11;
                    dtCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    dtCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Despacho opcional (Centrado, 11)
                    if (report.show_dispatch_date_column)
                    {
                        var despCell = ws.Cell(rowIdx, colIdx++);
                        despCell.SetValue(r.despacho_display);
                        despCell.Style.Font.FontSize = 11;
                        despCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        despCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    }

                    // Cliente (Izquierda, 11, con ajuste de línea si es largo)
                    var cliCell = ws.Cell(rowIdx, colIdx++);
                    cliCell.SetValue(r.customer_name);
                    cliCell.Style.Font.FontSize = 11;
                    cliCell.Style.Alignment.WrapText = true;
                    cliCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    cliCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Zona o Vendedor secundario (Izquierda, 11)
                    string secondary = isByZone ? r.seller_name : r.zone_name;
                    var secCell = ws.Cell(rowIdx, colIdx++);
                    secCell.SetValue(secondary);
                    secCell.Style.Font.FontSize = 11;
                    secCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    secCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Monto (Centrado, número sin $, 11)
                    var mCell = ws.Cell(rowIdx, colIdx++);
                    mCell.SetValue(r.amount_usd);
                    mCell.Style.Font.FontSize = 11;
                    mCell.Style.NumberFormat.Format = "#,##0.00";
                    mCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    mCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Abonado y Saldo si aplica a CxC (Centrado, número sin $, 11)
                    if (report.show_paid_balance_summary)
                    {
                        var pCell = ws.Cell(rowIdx, colIdx++);
                        pCell.SetValue(r.paid_amount_usd);
                        pCell.Style.Font.FontSize = 11;
                        pCell.Style.NumberFormat.Format = "#,##0.00";
                        pCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        pCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        var bCell = ws.Cell(rowIdx, colIdx++);
                        bCell.SetValue(r.balance_due_usd);
                        bCell.Style.Font.FontSize = 11;
                        bCell.Style.NumberFormat.Format = "#,##0.00";
                        bCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        bCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        if (r.balance_due_usd > 0.005m && !is_annulled)
                        {
                            bCell.Style.Font.FontColor = XLColor.FromHtml("#C62828");
                            bCell.Style.Font.Bold = true;
                        }
                    }

                    // Estado (Centrado, negrita, 11)
                    var stCell = ws.Cell(rowIdx, colIdx++);
                    stCell.SetValue(r.status);
                    stCell.Style.Font.FontSize = 11;
                    stCell.Style.Font.Bold = true;
                    stCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    stCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    if (is_annulled) stCell.Style.Font.FontColor = XLColor.FromHtml("#C62828");
                    else if (string.Equals(r.status?.Trim(), "Devuelta", StringComparison.OrdinalIgnoreCase)) stCell.Style.Font.FontColor = XLColor.FromHtml("#7B1FA2");
                    else if (string.Equals(r.status?.Trim(), "Pendiente", StringComparison.OrdinalIgnoreCase)) stCell.Style.Font.FontColor = XLColor.FromHtml("#E65100");
                    else if (string.Equals(r.status?.Trim(), "Pagada", StringComparison.OrdinalIgnoreCase)) stCell.Style.Font.FontColor = XLColor.FromHtml("#2E7D32");

                    // Ajuste de altura dinámica si el nombre del cliente se desborda y continúa en la línea de abajo
                    int clientColWidth = report.show_dispatch_date_column ? 22 : 24;
                    string clientName = r.customer_name ?? string.Empty;
                    int charsPerLine = Math.Max(15, clientColWidth - 3);
                    int nameLines = Math.Max(1, (int)Math.Ceiling((double)clientName.Length / charsPerLine));

                    string secondaryText = isByZone ? r.seller_name : r.zone_name;
                    int secColWidth = isByZone ? 14 : 15;
                    int secCharsPerLine = Math.Max(12, secColWidth - 3);
                    int secLines = Math.Max(1, (int)Math.Ceiling((double)(secondaryText ?? string.Empty).Length / secCharsPerLine));

                    int rowLines = Math.Max(nameLines, secLines);
                    ws.Row(rowIdx).Height = Math.Max(20, rowLines * 15);

                    rowIdx++;
                    alt = !alt;
                }

                int lastDataRow = rowIdx - 1;

                // BORDES COMPLETOS EN NEGRO PARA TODA LA TABLA DE NOTAS
                var mainTableRange = ws.Range(headerRow, 1, lastDataRow, totalCols);
                mainTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                mainTableRange.Style.Border.InsideBorderColor = XLColor.Black;
                mainTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                mainTableRange.Style.Border.OutsideBorderColor = XLColor.Black;

                // 4. TABLA DE TOTALES Y LIQUIDACIÓN AL PIE
                rowIdx++; // Separación
                ws.Row(rowIdx).Height = 8;
                rowIdx++;

                var valid_srows = srows.Where(x => !es_anulada(x) && !es_devuelta(x)).ToList();
                decimal group_total = valid_srows.Sum(x => x.amount_usd);
                decimal group_paid = valid_srows.Sum(x => x.paid_amount_usd);
                decimal group_balance = valid_srows.Sum(x => x.balance_due_usd);

                var annulled_rows = srows.Where(x => es_anulada(x)).ToList();
                int annulled_count = annulled_rows.Count;
                decimal annulled_amount = annulled_rows.Sum(x => x.amount_usd);

                var returned_rows = srows.Where(x => es_devuelta(x)).ToList();
                int returned_count = returned_rows.Count;
                decimal returned_amount = returned_rows.Sum(x => x.amount_usd);

                int boxStartCol = Math.Max(1, totalCols - 3);
                int boxLabelEndCol = totalCols - 1;
                int boxValCol = totalCols;

                void AddTotalBoxLine(int row, string label, decimal val, string? hexText = null, string? hexBg = null, bool bold = true)
                {
                    var lblRange = ws.Range(row, boxStartCol, row, boxLabelEndCol);
                    lblRange.Merge();
                    lblRange.Value = label;
                    lblRange.Style.Font.Bold = bold;
                    lblRange.Style.Font.FontSize = 8;
                    lblRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    lblRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    var valCell = ws.Cell(row, boxValCol);
                    valCell.SetValue(val);
                    valCell.Style.Font.Bold = bold;
                    valCell.Style.Font.FontSize = 11;
                    valCell.Style.NumberFormat.Format = "#,##0.00";
                    valCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    valCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    if (!string.IsNullOrWhiteSpace(hexText))
                    {
                        valCell.Style.Font.FontColor = XLColor.FromHtml(hexText);
                        lblRange.Style.Font.FontColor = XLColor.FromHtml(hexText);
                    }

                    if (!string.IsNullOrWhiteSpace(hexBg))
                    {
                        ws.Range(row, boxStartCol, row, boxValCol).Style.Fill.BackgroundColor = XLColor.FromHtml(hexBg);
                    }

                    var rowRange = ws.Range(row, boxStartCol, row, boxValCol);
                    rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    rowRange.Style.Border.OutsideBorderColor = XLColor.Black;
                    rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    rowRange.Style.Border.InsideBorderColor = XLColor.Black;
                    ws.Row(row).Height = 20;
                }

                int boxStartRow = rowIdx;
                AddTotalBoxLine(rowIdx++, report.show_paid_balance_summary ? "SUBTOTAL FACTURADO" : "SUBTOTAL", group_total);

                if (report.show_paid_balance_summary)
                {
                    AddTotalBoxLine(rowIdx++, "SALDO YA COBRADO", group_paid, "#1B5E20", "#E8F5E9");
                    AddTotalBoxLine(rowIdx++, "SALDO POR COBRAR", group_balance, "#B71C1C", "#FFEBEE");
                }

                if (annulled_count > 0)
                {
                    AddTotalBoxLine(rowIdx++, $"ANULADAS ({annulled_count})", annulled_amount, "#C62828", "#FFEBEE");
                }

                if (returned_count > 0)
                {
                    AddTotalBoxLine(rowIdx++, $"DEVUELTAS ({returned_count})", returned_amount, "#7B1FA2", "#F3E5F5");
                }

                // 5. ANCHO COMPACTO DE COLUMNAS PARA PANTALLA 1280x720 (~102-106 UNIDADES)
                if (report.show_paid_balance_summary)
                {
                    if (report.show_dispatch_date_column)
                    {
                        ws.Column(1).Width = 11;  // CORRELATIVO
                        ws.Column(2).Width = 10;  // FECHA
                        ws.Column(3).Width = 10;  // DESPACHO
                        ws.Column(4).Width = 22;  // CLIENTE
                        ws.Column(5).Width = 14;  // ZONA / VEND
                        ws.Column(6).Width = 11;  // MONTO
                        ws.Column(7).Width = 11;  // ABONADO
                        ws.Column(8).Width = 11;  // SALDO
                        ws.Column(9).Width = 9;   // ESTADO
                    }
                    else
                    {
                        ws.Column(1).Width = 12;  // CORRELATIVO
                        ws.Column(2).Width = 11;  // FECHA
                        ws.Column(3).Width = 24;  // CLIENTE
                        ws.Column(4).Width = 15;  // ZONA / VEND
                        ws.Column(5).Width = 11;  // MONTO
                        ws.Column(6).Width = 11;  // ABONADO
                        ws.Column(7).Width = 11;  // SALDO
                        ws.Column(8).Width = 9;   // ESTADO
                    }
                }
                else
                {
                    ws.Column(1).Width = 14;  // CORRELATIVO
                    ws.Column(2).Width = 12;  // FECHA
                    ws.Column(3).Width = 30;  // CLIENTE
                    ws.Column(4).Width = 18;  // ZONA / VEND
                    ws.Column(5).Width = 14;  // MONTO
                    ws.Column(6).Width = 12;  // ESTADO
                }

                // Autofilter sin FreezeRows
                ws.Range(headerRow, 1, lastDataRow, totalCols).SetAutoFilter();
            }

            // =========================================================================
            // ÚLTIMA HOJA: RESUMEN GENERAL (Consolidado de todo el período)
            // =========================================================================
            var wsSummary = wb.Worksheets.Add("Resumen General");

            string summaryTitle = report.show_paid_balance_summary
                ? "RESUMEN GENERAL DE CUENTAS POR COBRAR"
                : "RESUMEN GENERAL DE VENTAS";

            var sumHeaders = report.show_paid_balance_summary
                ? new List<string> { groupBadge, "NOTAS", "FACTURADO", "ABONADO", "SALDO", "PENDIENTES" }
                : new List<string> { groupBadge, "NOTAS", "FACTURADO", "VALIDAS" };

            int sumCols = sumHeaders.Count;

            // Título principal del Resumen
            var sumTitleLeft = wsSummary.Range(1, 1, 1, Math.Max(1, sumCols - 2));
            sumTitleLeft.Merge();
            sumTitleLeft.Value = summaryTitle;
            sumTitleLeft.Style.Font.Bold = true;
            sumTitleLeft.Style.Font.FontSize = 12;
            sumTitleLeft.Style.Font.FontColor = XLColor.White;
            sumTitleLeft.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
            sumTitleLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            sumTitleLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var sumTitleRight = wsSummary.Range(1, Math.Max(2, sumCols - 1), 1, sumCols);
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
            var subTitle = wsSummary.Range(3, 1, 3, sumCols);
            subTitle.Merge();
            subTitle.Value = $"Consolidado de todas las {groupBadge.ToLowerInvariant()}s del período.";
            subTitle.Style.Font.Italic = true;
            subTitle.Style.Font.FontSize = 9;
            subTitle.Style.Font.FontColor = XLColor.FromHtml("#555555");
            subTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            subTitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            wsSummary.Row(3).Height = 20;

            wsSummary.Row(4).Height = 6;

            // TABLA DESGLOSADA DIRECTAMENTE (Sin tarjeta individual superior)
            int sumHeaderRow = 5;
            for (int k = 0; k < sumHeaders.Count; k++)
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
                var gRows = g.ToList();
                int sNotas = gRows.Count;
                var valid = gRows.Where(r => !es_anulada(r) && !es_devuelta(r)).ToList();
                decimal sFact = valid.Sum(r => r.amount_usd);
                decimal sPag = valid.Sum(r => r.paid_amount_usd);
                decimal sSaldo = valid.Sum(r => r.balance_due_usd);
                int sPend = valid.Count(r => string.Equals(r.status?.Trim(), "Pendiente", StringComparison.OrdinalIgnoreCase));

                var sRange = wsSummary.Range(sumRowIdx, 1, sumRowIdx, sumCols);
                if (sumAlt)
                {
                    sRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAF9");
                }

                // Grupo (Izquierda, 11)
                var gCell = wsSummary.Cell(sumRowIdx, 1);
                gCell.SetValue(g.Key);
                gCell.Style.Font.FontSize = 11;
                gCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                gCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                gCell.Style.Font.Bold = true;

                // Notas (Centrado, 11)
                var nCell = wsSummary.Cell(sumRowIdx, 2);
                nCell.SetValue(sNotas);
                nCell.Style.Font.FontSize = 11;
                nCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                nCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Facturado (Centrado, número sin $, 11)
                var fCell = wsSummary.Cell(sumRowIdx, 3);
                fCell.SetValue(sFact);
                fCell.Style.Font.FontSize = 11;
                fCell.Style.NumberFormat.Format = "#,##0.00";
                fCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                fCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                if (report.show_paid_balance_summary)
                {
                    // Abonado (Centrado, número sin $, 11)
                    var pCell = wsSummary.Cell(sumRowIdx, 4);
                    pCell.SetValue(sPag);
                    pCell.Style.Font.FontSize = 11;
                    pCell.Style.NumberFormat.Format = "#,##0.00";
                    pCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    pCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    pCell.Style.Font.FontColor = XLColor.FromHtml("#2E7D32");

                    // Saldo (Centrado, número sin $, 11)
                    var bCell = wsSummary.Cell(sumRowIdx, 5);
                    bCell.SetValue(sSaldo);
                    bCell.Style.Font.FontSize = 11;
                    bCell.Style.NumberFormat.Format = "#,##0.00";
                    bCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    bCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    if (sSaldo > 0.005m)
                    {
                        bCell.Style.Font.FontColor = XLColor.FromHtml("#C62828");
                        bCell.Style.Font.Bold = true;
                    }

                    // Pendientes (Centrado, 11)
                    var pendCell = wsSummary.Cell(sumRowIdx, 6);
                    pendCell.SetValue(sPend);
                    pendCell.Style.Font.FontSize = 11;
                    pendCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    pendCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                else
                {
                    var valCell = wsSummary.Cell(sumRowIdx, 4);
                    valCell.SetValue(valid.Count);
                    valCell.Style.Font.FontSize = 11;
                    valCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    valCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
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

            if (report.show_paid_balance_summary)
            {
                wsSummary.Cell(sumGrandTotalRow, 4).FormulaA1 = $"SUM(D{sumHeaderRow + 1}:D{lastSumDataRow})";
                wsSummary.Cell(sumGrandTotalRow, 4).Style.NumberFormat.Format = "#,##0.00";
                wsSummary.Cell(sumGrandTotalRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsSummary.Cell(sumGrandTotalRow, 4).Style.Font.FontSize = 11;
                wsSummary.Cell(sumGrandTotalRow, 4).Style.Font.Bold = true;
                wsSummary.Cell(sumGrandTotalRow, 4).Style.Font.FontColor = XLColor.FromHtml("#2E7D32");

                wsSummary.Cell(sumGrandTotalRow, 5).FormulaA1 = $"SUM(E{sumHeaderRow + 1}:E{lastSumDataRow})";
                wsSummary.Cell(sumGrandTotalRow, 5).Style.NumberFormat.Format = "#,##0.00";
                wsSummary.Cell(sumGrandTotalRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsSummary.Cell(sumGrandTotalRow, 5).Style.Font.FontSize = 11;
                wsSummary.Cell(sumGrandTotalRow, 5).Style.Font.Bold = true;
                wsSummary.Cell(sumGrandTotalRow, 5).Style.Font.FontColor = XLColor.FromHtml("#C62828");

                wsSummary.Cell(sumGrandTotalRow, 6).FormulaA1 = $"SUM(F{sumHeaderRow + 1}:F{lastSumDataRow})";
                wsSummary.Cell(sumGrandTotalRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsSummary.Cell(sumGrandTotalRow, 6).Style.Font.FontSize = 11;
                wsSummary.Cell(sumGrandTotalRow, 6).Style.Font.Bold = true;
            }
            else
            {
                wsSummary.Cell(sumGrandTotalRow, 4).FormulaA1 = $"SUM(D{sumHeaderRow + 1}:D{lastSumDataRow})";
                wsSummary.Cell(sumGrandTotalRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsSummary.Cell(sumGrandTotalRow, 4).Style.Font.FontSize = 11;
                wsSummary.Cell(sumGrandTotalRow, 4).Style.Font.Bold = true;
            }

            ExcelFlatExporter.StyleTotalRow(wsSummary.Range(sumGrandTotalRow, 1, sumGrandTotalRow, sumCols), "#E8F5E9");
            wsSummary.Row(sumGrandTotalRow).Height = 22;

            // BORDES COMPLETOS EN NEGRO PARA LA TABLA DEL RESUMEN
            var sumTableRange = wsSummary.Range(sumHeaderRow, 1, sumGrandTotalRow, sumCols);
            sumTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            sumTableRange.Style.Border.OutsideBorderColor = XLColor.Black;
            sumTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            sumTableRange.Style.Border.InsideBorderColor = XLColor.Black;

            // Ancho compacto de columnas en Resumen General
            if (report.show_paid_balance_summary)
            {
                wsSummary.Column(1).Width = 24;  // VENDEDORA / ZONA
                wsSummary.Column(2).Width = 10;  // NOTAS
                wsSummary.Column(3).Width = 16;  // FACTURADO
                wsSummary.Column(4).Width = 16;  // ABONADO
                wsSummary.Column(5).Width = 16;  // SALDO
                wsSummary.Column(6).Width = 12;  // PENDIENTES
            }
            else
            {
                wsSummary.Column(1).Width = 26;  // VENDEDORA / ZONA
                wsSummary.Column(2).Width = 12;  // NOTAS
                wsSummary.Column(3).Width = 20;  // FACTURADO
                wsSummary.Column(4).Width = 14;  // VALIDAS
            }

            string defaultName = Path.GetFileNameWithoutExtension(MonthlyReportPdfGenerator.get_default_file_name(report)) + ".xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar reporte en Excel");
        }

        private static string MakeSafeSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Grupo";
            string clean = name.Replace("\\", "").Replace("/", "").Replace("?", "").Replace("*", "")
                               .Replace("[", "").Replace("]", "").Replace(":", "").Trim();
            if (clean.Length > 28) clean = clean.Substring(0, 28);
            return string.IsNullOrWhiteSpace(clean) ? "Grupo" : clean;
        }

        private static bool es_anulada(monthly_report_row_dto r)
            => string.Equals(r.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase);

        private static bool es_devuelta(monthly_report_row_dto r)
            => string.Equals(r.status?.Trim(), "Devuelta", StringComparison.OrdinalIgnoreCase);
    }
}
