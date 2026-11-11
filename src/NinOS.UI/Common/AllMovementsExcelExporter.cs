using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace NinOS.UI.Common
{
    public static class AllMovementsExcelExporter
    {
        public static void export(AllMovementsPdfModel model)
        {
            if (model == null) return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Todos los Movimientos");

            var groups = model.ProductGroups ?? new System.Collections.Generic.List<ProductMovementGroup>();

            // Calculamos saldos acumulados retroactivos para cada grupo de producto
            foreach (var grp in groups)
            {
                calcular_saldos(grp);
            }

            // 1. BANNER INSTITUCIONAL SUPERIOR (Fila 1)
            var titleLeft = ws.Range(1, 1, 1, 9);
            titleLeft.Merge();
            titleLeft.Value = "NINOS - TODOS LOS MOVIMIENTOS DE INVENTARIO";
            titleLeft.Style.Font.Bold = true;
            titleLeft.Style.Font.FontSize = 12;
            titleLeft.Style.Font.FontColor = XLColor.White;
            titleLeft.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D62B5");
            titleLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            titleLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            titleLeft.Style.Alignment.Indent = 1;

            var titleRight = ws.Range(1, 10, 1, 15);
            titleRight.Merge();
            titleRight.Value = $"EMISIÓN: {model.EmissionDate:dd/MM/yyyy HH:mm}";
            titleRight.Style.Font.Bold = true;
            titleRight.Style.Font.FontSize = 10;
            titleRight.Style.Font.FontColor = XLColor.White;
            titleRight.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D62B5");
            titleRight.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            titleRight.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 26;

            // Fila 2: Separador
            ws.Row(2).Height = 4;

            // 2. BLOQUE DE TARJETAS KPI (Fila 3)
            void StyleCard(IXLRange range, string label, string value, string valueColorHex = "#0D62B5")
            {
                range.Merge();
                range.Value = $"{label}: {value}";
                range.Style.Font.Bold = true;
                range.Style.Font.FontSize = 9.5;
                range.Style.Font.FontColor = XLColor.FromHtml(valueColorHex);
                range.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                range.Style.Border.OutsideBorderColor = XLColor.Black;
            }

            StyleCard(ws.Range(3, 1, 3, 3), "PRODUCTOS CON MOVIMIENTO", model.TotalProducts.ToString("N0"));
            StyleCard(ws.Range(3, 4, 3, 7), "REGISTROS TOTALES", $"{model.TotalMovements:N0} movs");
            StyleCard(ws.Range(3, 8, 3, 11), "TOTAL ENTRADAS", $"+{model.TotalEntradas:N0} uds", "#2E7D32");
            StyleCard(ws.Range(3, 12, 3, 15), "TOTAL SALIDAS", $"-{model.TotalSalidas:N0} uds", "#C62828");
            ws.Row(3).Height = 24;

            // Fila 4: Separador
            ws.Row(4).Height = 5;

            // 3. ENCABEZADOS DE LA TABLA (Fila 5)
            int headerRow = 5;
            string[] headers = new[]
            {
                "MARCA", "CÓDIGO", "PRODUCTO", "NOTA", "FECHA", "DOCUMENTO", "MOTIVO",
                "CLIENTE", "VENDEDOR", "MOVIMIENTO", "UNIDADES", "PRECIO ($)",
                "SUBTOTAL ($)", "SALDO STOCK", "ESTADO"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 8.5;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D62B5");
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.Black;
            }
            ws.Row(headerRow).Height = 25;

            // 4. DETALLE DE MOVIMIENTOS (Fila 6 en adelante)
            int rowIdx = 6;
            bool alt = false;
            decimal totalSubtotalGeneral = 0m;

            var orderedGroups = groups
                .OrderBy(g => string.IsNullOrWhiteSpace(g.Category) ? "OTROS" : g.Category.Trim().ToUpperInvariant())
                .ThenBy(g => g.ProductCode)
                .ToList();

            foreach (var grp in orderedGroups)
            {
                foreach (var r in grp.Rows)
                {
                    totalSubtotalGeneral += r.SubtotalUsd;
                    var rowRange = ws.Range(rowIdx, 1, rowIdx, headers.Length);
                    bool isAnulada = string.Equals(r.Estado?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase);

                    int c = 1;
                    // Col 1: MARCA
                    var mCell = ws.Cell(rowIdx, c++);
                    mCell.SetValue(grp.Category);
                    mCell.Style.Font.FontSize = 9.5;
                    mCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    mCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 2: CÓDIGO
                    var cdCell = ws.Cell(rowIdx, c++);
                    cdCell.SetValue(grp.ProductCode);
                    cdCell.Style.Font.Bold = true;
                    cdCell.Style.Font.FontSize = 9.5;
                    cdCell.Style.Font.FontColor = XLColor.FromHtml("#1565C0");
                    cdCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cdCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 3: PRODUCTO
                    var pCell = ws.Cell(rowIdx, c++);
                    pCell.SetValue(grp.ProductName);
                    pCell.Style.Font.FontSize = 9.5;
                    pCell.Style.Alignment.WrapText = true;
                    pCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    pCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 4: NOTA (Número de nota)
                    var notaCell = ws.Cell(rowIdx, c++);
                    notaCell.SetValue(string.IsNullOrWhiteSpace(r.NoteNumber) ? "-" : r.NoteNumber);
                    notaCell.Style.Font.Bold = !string.IsNullOrWhiteSpace(r.NoteNumber) && r.NoteNumber != "-";
                    notaCell.Style.Font.FontSize = 9.5;
                    notaCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    notaCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 5: FECHA (DateTime nativo para permitir filtro jerárquico Enero..Diciembre en Excel)
                    var fCell = ws.Cell(rowIdx, c++);
                    fCell.SetValue(r.FechaLocal);
                    fCell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                    fCell.Style.Font.FontSize = 9.5;
                    fCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    fCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 6: DOCUMENTO
                    var docCell = ws.Cell(rowIdx, c++);
                    docCell.SetValue(string.IsNullOrWhiteSpace(r.Documento) ? "-" : r.Documento);
                    docCell.Style.Font.FontSize = 9.5;
                    docCell.Style.Alignment.WrapText = true;
                    docCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    docCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 7: MOTIVO
                    var motCell = ws.Cell(rowIdx, c++);
                    motCell.SetValue(r.Motivo);
                    motCell.Style.Font.FontSize = 9.5;
                    motCell.Style.Alignment.WrapText = true;
                    motCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    motCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 8: CLIENTE
                    var cliCell = ws.Cell(rowIdx, c++);
                    cliCell.SetValue(r.Cliente);
                    cliCell.Style.Font.FontSize = 9.5;
                    cliCell.Style.Alignment.WrapText = true;
                    cliCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    cliCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 9: VENDEDOR
                    var venCell = ws.Cell(rowIdx, c++);
                    venCell.SetValue(r.Vendedor);
                    venCell.Style.Font.FontSize = 9.5;
                    venCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    venCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 10: MOVIMIENTO (ENTRADA / SALIDA)
                    var movCell = ws.Cell(rowIdx, c++);
                    movCell.SetValue(r.Movimiento);
                    movCell.Style.Font.Bold = true;
                    movCell.Style.Font.FontSize = 9.5;
                    movCell.Style.Font.FontColor = r.EsEntrada ? XLColor.FromHtml("#2E7D32") : XLColor.FromHtml("#C62828");
                    movCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    movCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 11: UNIDADES
                    var unitsCell = ws.Cell(rowIdx, c++);
                    unitsCell.SetValue(r.Unidades);
                    unitsCell.Style.Font.Bold = true;
                    unitsCell.Style.Font.FontSize = 9.5;
                    unitsCell.Style.NumberFormat.Format = "#,##0";
                    unitsCell.Style.Font.FontColor = r.EsEntrada ? XLColor.FromHtml("#2E7D32") : XLColor.FromHtml("#C62828");
                    unitsCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    unitsCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 12: PRECIO ($)
                    var priceCell = ws.Cell(rowIdx, c++);
                    priceCell.SetValue(r.PrecioUsd);
                    priceCell.Style.Font.FontSize = 9.5;
                    priceCell.Style.NumberFormat.Format = "#,##0.00";
                    priceCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    priceCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 13: SUBTOTAL ($)
                    var subtotalCell = ws.Cell(rowIdx, c++);
                    subtotalCell.SetValue(r.SubtotalUsd);
                    subtotalCell.Style.Font.Bold = true;
                    subtotalCell.Style.Font.FontSize = 9.5;
                    subtotalCell.Style.NumberFormat.Format = "#,##0.00";
                    subtotalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    subtotalCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 14: SALDO STOCK
                    var balanceCell = ws.Cell(rowIdx, c++);
                    balanceCell.SetValue(r.SaldoUnidades);
                    balanceCell.Style.Font.Bold = true;
                    balanceCell.Style.Font.FontSize = 9.5;
                    balanceCell.Style.Font.FontColor = XLColor.FromHtml("#0D62B5");
                    balanceCell.Style.NumberFormat.Format = "#,##0";
                    balanceCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    balanceCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Col 15: ESTADO
                    var stCell = ws.Cell(rowIdx, c++);
                    stCell.SetValue(r.Estado);
                    stCell.Style.Font.Bold = true;
                    stCell.Style.Font.FontSize = 9.5;
                    stCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    stCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    if (isAnulada) stCell.Style.Font.FontColor = XLColor.FromHtml("#C62828");

                    // Colores de fondo de fila
                    if (isAnulada)
                    {
                        rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE");
                    }
                    else if (alt)
                    {
                        rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                    }

                    // Bordes de celda en negro
                    rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    rowRange.Style.Border.InsideBorderColor = XLColor.Black;
                    rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    rowRange.Style.Border.OutsideBorderColor = XLColor.Black;

                    // Altura dinámica
                    int pLines = Math.Max(1, (int)Math.Ceiling((double)(grp.ProductName?.Length ?? 0) / 32));
                    int cLines = Math.Max(1, (int)Math.Ceiling((double)(r.Cliente?.Length ?? 0) / 25));
                    int mLines = Math.Max(1, (int)Math.Ceiling((double)(r.Motivo?.Length ?? 0) / 20));
                    int maxLines = Math.Max(pLines, Math.Max(cLines, mLines));
                    ws.Row(rowIdx).Height = Math.Max(20, maxLines * 14.5);

                    rowIdx++;
                    alt = !alt;
                }
            }

            int lastDataRow = rowIdx - 1;

            // 5. FILA DE TOTALES GENERALES (Al final de la tabla)
            if (model.TotalMovements > 0)
            {
                var totLabelRange = ws.Range(rowIdx, 1, rowIdx, 10);
                totLabelRange.Merge();
                totLabelRange.Value = $"TOTALES GENERALES ({model.TotalMovements} MOVIMIENTOS)";
                totLabelRange.Style.Font.Bold = true;
                totLabelRange.Style.Font.FontSize = 10;
                totLabelRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                totLabelRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Total de unidades (neto: entradas - salidas)
                var totUnitsCell = ws.Cell(rowIdx, 11);
                totUnitsCell.SetValue(model.TotalEntradas - model.TotalSalidas);
                totUnitsCell.Style.Font.Bold = true;
                totUnitsCell.Style.Font.FontSize = 10;
                totUnitsCell.Style.NumberFormat.Format = "#,##0";
                totUnitsCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                totUnitsCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                ws.Cell(rowIdx, 12).SetValue("");

                // Total Subtotal ($)
                var totSubCell = ws.Cell(rowIdx, 13);
                totSubCell.SetValue(totalSubtotalGeneral);
                totSubCell.Style.Font.Bold = true;
                totSubCell.Style.Font.FontSize = 10;
                totSubCell.Style.NumberFormat.Format = "#,##0.00";
                totSubCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                totSubCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                ws.Cell(rowIdx, 14).SetValue("");
                ws.Cell(rowIdx, 15).SetValue("");

                var totalRange = ws.Range(rowIdx, 1, rowIdx, headers.Length);
                totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD");
                totalRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                totalRange.Style.Border.InsideBorderColor = XLColor.Black;
                totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                totalRange.Style.Border.OutsideBorderColor = XLColor.Black;
                totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                totalRange.Style.Border.TopBorderColor = XLColor.Black;
                totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
                totalRange.Style.Border.BottomBorderColor = XLColor.Black;
                ws.Row(rowIdx).Height = 25;
            }

            // 6. ANCHOS DE COLUMNAS DEFINIDOS AMPLIOS Y CONFORTABLES (SIN DESBORDAMIENTO)
            ws.Column(1).Width = 14;   // MARCA
            ws.Column(2).Width = 14;   // CÓDIGO
            ws.Column(3).Width = 35;   // PRODUCTO
            ws.Column(4).Width = 14;   // NOTA
            ws.Column(5).Width = 20;   // FECHA (20 para fecha nativa sin desborde ###)
            ws.Column(6).Width = 18;   // DOCUMENTO
            ws.Column(7).Width = 22;   // MOTIVO
            ws.Column(8).Width = 28;   // CLIENTE
            ws.Column(9).Width = 16;   // VENDEDOR
            ws.Column(10).Width = 14;  // MOVIMIENTO
            ws.Column(11).Width = 13;  // UNIDADES
            ws.Column(12).Width = 14;  // PRECIO ($)
            ws.Column(13).Width = 16;  // SUBTOTAL ($)
            ws.Column(14).Width = 15;  // SALDO STOCK
            ws.Column(15).Width = 14;  // ESTADO

            // Filtro automático (SIN inmovilizar filas / NO freeze)
            if (lastDataRow >= headerRow)
            {
                ws.Range(headerRow, 1, lastDataRow, headers.Length).SetAutoFilter();
            }
            ws.ShowGridLines = true;

            string defaultName = $"TODOS_LOS_MOVIMIENTOS_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar todos los movimientos en Excel");
        }

        private static void calcular_saldos(ProductMovementGroup group)
        {
            if (group.Rows == null) return;

            int saldo = group.CurrentStock;
            foreach (var row in group.Rows)
            {
                row.SaldoUnidades = saldo;
                saldo -= row.SignedUnits;
            }
        }
    }
}
