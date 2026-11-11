using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace NinOS.UI.Common
{
    public static class CurrentInventoryExcelExporter
    {
        public static void export(CurrentInventoryPdfModel model)
        {
            if (model == null) return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Inventario Actual");

            var items = (model.Items ?? new System.Collections.Generic.List<CurrentInventoryPdfItem>())
                .OrderBy(x => string.IsNullOrWhiteSpace(x.Category) ? "OTROS" : x.Category.Trim().ToUpperInvariant())
                .ThenBy(x => x.ProductCode)
                .ToList();

            // 1. BANNER INSTITUCIONAL SUPERIOR (Fila 1)
            var titleLeft = ws.Range(1, 1, 1, 4);
            titleLeft.Merge();
            titleLeft.Value = "NINOS - INVENTARIO ACTUAL";
            titleLeft.Style.Font.Bold = true;
            titleLeft.Style.Font.FontSize = 12;
            titleLeft.Style.Font.FontColor = XLColor.White;
            titleLeft.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D62B5");
            titleLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            titleLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            titleLeft.Style.Alignment.Indent = 1;

            var titleRight = ws.Range(1, 5, 1, 6);
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

            StyleCard(ws.Range(3, 1, 3, 2), "TOTAL PRODUCTOS", model.TotalProducts.ToString("N0"));
            StyleCard(ws.Range(3, 3, 3, 4), "TOTAL UNIDADES", model.TotalUnits.ToString("N0"));
            StyleCard(ws.Range(3, 5, 3, 6), "VALORIZACIÓN TOTAL", $"$ {model.TotalValuationUsd:N2}", "#2E7D32");
            ws.Row(3).Height = 24;

            // Fila 4: Separador
            ws.Row(4).Height = 5;

            // 3. ENCABEZADOS DE LA TABLA (Fila 5)
            int headerRow = 5;
            string[] headers = new[]
            {
                "MARCA", "CÓDIGO", "DESCRIPCIÓN DEL PRODUCTO", "STOCK ACTUAL", "PRECIO ($)", "VALOR TOTAL ($)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 9.5;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D62B5");
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.Black;
            }
            ws.Row(headerRow).Height = 25;

            // 4. DETALLE DE PRODUCTOS (Fila 6 en adelante)
            int rowIdx = 6;
            bool alt = false;

            foreach (var item in items)
            {
                var rowRange = ws.Range(rowIdx, 1, rowIdx, headers.Length);

                // Col 1: MARCA
                var catCell = ws.Cell(rowIdx, 1);
                catCell.SetValue(item.Category);
                catCell.Style.Font.FontSize = 10;
                catCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                catCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 2: CÓDIGO
                var codeCell = ws.Cell(rowIdx, 2);
                codeCell.SetValue(item.ProductCode);
                codeCell.Style.Font.Bold = true;
                codeCell.Style.Font.FontSize = 10;
                codeCell.Style.Font.FontColor = XLColor.FromHtml("#1565C0");
                codeCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                codeCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 3: PRODUCTO / DESCRIPCIÓN (Con WrapText para nunca truncar)
                var nameCell = ws.Cell(rowIdx, 3);
                nameCell.SetValue(item.Name);
                nameCell.Style.Font.FontSize = 10;
                nameCell.Style.Alignment.WrapText = true;
                nameCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                nameCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 4: STOCK ACTUAL
                var qtyCell = ws.Cell(rowIdx, 4);
                qtyCell.SetValue(item.StockQuantity);
                qtyCell.Style.Font.Bold = true;
                qtyCell.Style.Font.FontSize = 10;
                qtyCell.Style.NumberFormat.Format = "#,##0";
                qtyCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                qtyCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 5: PRECIO ($)
                var priceCell = ws.Cell(rowIdx, 5);
                priceCell.SetValue(item.UnitPriceUsd);
                priceCell.Style.Font.FontSize = 10;
                priceCell.Style.NumberFormat.Format = "#,##0.00";
                priceCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                priceCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 6: VALOR TOTAL ($)
                var totalCell = ws.Cell(rowIdx, 6);
                totalCell.SetValue(item.TotalValueUsd);
                totalCell.Style.Font.Bold = true;
                totalCell.Style.Font.FontSize = 10;
                totalCell.Style.NumberFormat.Format = "#,##0.00";
                totalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                totalCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Color de fondo y alertas
                if (item.IsNegativeStock)
                {
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE");
                    qtyCell.Style.Font.FontColor = XLColor.FromHtml("#C62828");
                }
                else if (item.StockQuantity == 0)
                {
                    if (alt) rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                    qtyCell.Style.Font.FontColor = XLColor.FromHtml("#E65100");
                }
                else if (alt)
                {
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }

                // Bordes finos negros en cada celda
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorderColor = XLColor.Black;
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.OutsideBorderColor = XLColor.Black;

                // Altura dinámica según longitud del nombre del producto
                string pName = item.Name ?? string.Empty;
                int lines = Math.Max(1, (int)Math.Ceiling((double)pName.Length / 38));
                ws.Row(rowIdx).Height = Math.Max(20, lines * 14.5);

                rowIdx++;
                alt = !alt;
            }

            int lastDataRow = rowIdx - 1;

            // 5. FILA DE TOTALES GENERALES
            if (items.Count > 0)
            {
                var labelRange = ws.Range(rowIdx, 1, rowIdx, 3);
                labelRange.Merge();
                labelRange.Value = $"TOTAL GENERAL ({items.Count} PRODUCTOS)";
                labelRange.Style.Font.Bold = true;
                labelRange.Style.Font.FontSize = 10;
                labelRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                labelRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var totQtyCell = ws.Cell(rowIdx, 4);
                totQtyCell.SetValue(items.Sum(x => x.StockQuantity));
                totQtyCell.Style.Font.Bold = true;
                totQtyCell.Style.Font.FontSize = 10.5;
                totQtyCell.Style.NumberFormat.Format = "#,##0";
                totQtyCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                totQtyCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                ws.Cell(rowIdx, 5).SetValue("");

                var totValCell = ws.Cell(rowIdx, 6);
                totValCell.SetValue(items.Sum(x => x.TotalValueUsd));
                totValCell.Style.Font.Bold = true;
                totValCell.Style.Font.FontSize = 10.5;
                totValCell.Style.NumberFormat.Format = "#,##0.00";
                totValCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                totValCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

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

            // 6. ANCHOS DEFINIDOS EXACTOS PARA QUE QUEPA AL 100% EN PANTALLA (TOTAL = 114)
            ws.Column(1).Width = 15;  // MARCA
            ws.Column(2).Width = 13;  // CÓDIGO
            ws.Column(3).Width = 42;  // PRODUCTO (Espacio amplio para nombre)
            ws.Column(4).Width = 14;  // STOCK ACTUAL (Espacio para número y botón de filtro)
            ws.Column(5).Width = 14;  // PRECIO ($) (Espacio para precio y botón de filtro)
            ws.Column(6).Width = 16;  // VALOR TOTAL ($) (Espacio para total y botón de filtro)

            // Autofilter en encabezados (SIN inmovilizar filas / NO freeze)
            if (lastDataRow >= headerRow)
            {
                ws.Range(headerRow, 1, lastDataRow, headers.Length).SetAutoFilter();
            }
            ws.ShowGridLines = true;

            string defaultName = $"INVENTARIO_ACTUAL_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar inventario actual en Excel");
        }
    }
}
