using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using ClosedXML.Excel;
using Microsoft.Win32;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Common
{
    public static class PriceListExcelGenerator
    {
        private static readonly System.Text.UTF8Encoding Utf8Bom = new System.Text.UTF8Encoding(true);

        private static readonly Dictionary<string, (string hex, string softHex)> BrandStyles =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "DEFILE",          ("#1B3A2D", "#E8F0E4") },
            { "BIOLINE",         ("#00695C", "#E0F2F1") },
            { "REMBRANDT",       ("#6A1B9A", "#F3E5F5") },
            { "ESTILISTA",       ("#E65100", "#FFF3E0") },
            { "DEPIL CLEAR",     ("#AD1457", "#FCE4EC") },
            { "KEDAM",           ("#4E342E", "#EFEBE9") },
            { "OLEOS",           ("#1565C0", "#E3F2FD") },
            { "AMAZONIA SECRET", ("#2E7D32", "#E8F5E9") },
            { "CUTIQUE",         ("#C62828", "#FFEBEE") }
        };

        private static (string hex, string softHex) GetBrandHex(string brand)
        {
            if (BrandStyles.TryGetValue(brand, out var s)) return s;
            return ("#37474F", "#ECEFF1");
        }

        private static string SanitizeSheetName(string name, int index)
        {
            if (string.IsNullOrWhiteSpace(name)) return $"Lista {index}";
            var invalid = new[] { '\\', '/', '?', '*', ':', '[', ']' };
            string clean = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
            if (string.IsNullOrWhiteSpace(clean)) return $"Lista {index}";
            if (clean.Length > 28) clean = clean.Substring(0, 28);
            return clean;
        }

        public static void generate(List<price_list_item> items, List<price_list_column> columns)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Guardar lista de precios",
                    Filter = "Libro de Excel (*.xlsx)|*.xlsx|Archivo CSV (*.csv)|*.csv",
                    FileName = $"Lista_Precios_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                };

                Window? owner = Application.Current?.MainWindow;
                bool? result = owner != null ? saveDialog.ShowDialog(owner) : saveDialog.ShowDialog();
                if (result != true) return;

                string ext = Path.GetExtension(saveDialog.FileName).ToLowerInvariant();
                if (ext == ".csv")
                {
                    generate_csv(saveDialog.FileName, items, columns);
                }
                else
                {
                    generate_xlsx(saveDialog.FileName, items, columns);
                }

                AppDialog.Show(
                    "Lista de precios exportada exitosamente.",
                    "Exportación completada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLog.Error("Error al generar archivo Excel de lista de precios", ex);
                AppDialog.Show(
                    $"No se pudo exportar el archivo de Excel:\n\n{ErrorText.Get(ex)}",
                    "Error al exportar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static decimal resolve_price(price_list_item item, price_list_column col)
        {
            if (col.is_original) return item.unit_price_usd;
            if (item.prices_by_key != null)
            {
                if (item.prices_by_key.TryGetValue(col.key, out decimal p)) return p;
                if (item.prices_by_key.TryGetValue(col.name, out decimal p2)) return p2;
            }
            return item.unit_price_usd;
        }

        private static void generate_xlsx(string filePath, List<price_list_item> items, List<price_list_column> columns)
        {
            if (columns == null || columns.Count == 0)
            {
                columns = new List<price_list_column>
                {
                    new price_list_column { key = "ORIGINAL", name = "Principal", is_original = true }
                };
            }

            var ordered = (items ?? new List<price_list_item>())
                .Where(i => !string.IsNullOrWhiteSpace(i.product_code))
                .OrderBy(i => string.IsNullOrWhiteSpace(i.brand) ? "OTROS" : i.brand.Trim().ToUpperInvariant())
                .ThenBy(i => i.product_code)
                .ToList();

            var groups = ordered
                .GroupBy(i => string.IsNullOrWhiteSpace(i.brand) ? "OTROS" : i.brand.Trim().ToUpperInvariant())
                .OrderBy(g => g.Key)
                .ToList();

            using var wb = new XLWorkbook();
            var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // =========================================================================
            // SI HAY MÁS DE UNA LISTA: PESTAÑA COMPARATIVA INICIAL
            // =========================================================================
            if (columns.Count > 1)
            {
                string compSheetName = "Comparativa";
                usedSheetNames.Add(compSheetName);
                var wsComp = wb.Worksheets.Add(compSheetName);

                int compTotalCols = 3 + columns.Count;

                // Banner superior
                var compTitleLeft = wsComp.Range(1, 1, 1, 3);
                compTitleLeft.Merge();
                compTitleLeft.Value = "NINOS - LISTA DE PRECIOS - COMPARATIVA GENERAL";
                compTitleLeft.Style.Font.Bold = true;
                compTitleLeft.Style.Font.FontSize = 12;
                compTitleLeft.Style.Font.FontColor = XLColor.White;
                compTitleLeft.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
                compTitleLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                compTitleLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                compTitleLeft.Style.Alignment.Indent = 1;

                var compTitleRight = wsComp.Range(1, 4, 1, compTotalCols);
                compTitleRight.Merge();
                compTitleRight.Value = $"ACTUALIZADO: {DateTime.Now:dd/MM/yyyy HH:mm}";
                compTitleRight.Style.Font.Bold = true;
                compTitleRight.Style.Font.FontSize = 9.5;
                compTitleRight.Style.Font.FontColor = XLColor.White;
                compTitleRight.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
                compTitleRight.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                compTitleRight.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                wsComp.Row(1).Height = 26;

                wsComp.Row(2).Height = 5;

                int cRow = 3;
                for (int gIdx = 0; gIdx < groups.Count; gIdx++)
                {
                    var group = groups[gIdx];
                    string brandName = group.Key;
                    var (brandHex, _) = GetBrandHex(brandName);
                    var groupItems = group.ToList();

                    // Banner de marca
                    var bRange = wsComp.Range(cRow, 1, cRow, compTotalCols);
                    bRange.Merge();
                    bRange.Value = $"  {brandName} ({groupItems.Count} PRODUCTOS)";
                    bRange.Style.Font.Bold = true;
                    bRange.Style.Font.FontSize = 10.5;
                    bRange.Style.Font.FontColor = XLColor.White;
                    bRange.Style.Fill.BackgroundColor = XLColor.FromHtml(brandHex);
                    bRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    bRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    wsComp.Row(cRow).Height = 22;
                    cRow++;

                    // Cabecera de la tabla comparativa
                    wsComp.Cell(cRow, 1).SetValue("MARCA");
                    wsComp.Cell(cRow, 2).SetValue("CÓDIGO");
                    wsComp.Cell(cRow, 3).SetValue("DESCRIPCIÓN DEL PRODUCTO");
                    for (int cIdx = 0; cIdx < columns.Count; cIdx++)
                    {
                        wsComp.Cell(cRow, 4 + cIdx).SetValue($"{columns[cIdx].name.ToUpperInvariant()} ($)");
                    }

                    var cHeaderRange = wsComp.Range(cRow, 1, cRow, compTotalCols);
                    cHeaderRange.Style.Font.Bold = true;
                    cHeaderRange.Style.Font.FontSize = 9.5;
                    cHeaderRange.Style.Font.FontColor = XLColor.FromHtml("#1B3A2D");
                    cHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8ECE9");
                    cHeaderRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cHeaderRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    cHeaderRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#D0D7DE");
                    cHeaderRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cHeaderRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D0D7DE");
                    wsComp.Cell(cRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    wsComp.Cell(cRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    wsComp.Cell(cRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    for (int cIdx = 0; cIdx < columns.Count; cIdx++)
                    {
                        wsComp.Cell(cRow, 4 + cIdx).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    wsComp.Row(cRow).Height = 22;
                    cRow++;

                    // Filas de productos
                    bool alt = false;
                    foreach (var item in groupItems)
                    {
                        var rowRange = wsComp.Range(cRow, 1, cRow, compTotalCols);

                        wsComp.Cell(cRow, 1).SetValue(brandName);
                        wsComp.Cell(cRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        wsComp.Cell(cRow, 2).SetValue(item.product_code);
                        wsComp.Cell(cRow, 2).Style.Font.Bold = true;
                        wsComp.Cell(cRow, 2).Style.Font.FontColor = XLColor.FromHtml("#1565C0");
                        wsComp.Cell(cRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        var nameCell = wsComp.Cell(cRow, 3);
                        nameCell.SetValue(item.name);
                        nameCell.Style.Alignment.WrapText = true;
                        nameCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                        for (int cIdx = 0; cIdx < columns.Count; cIdx++)
                        {
                            decimal price = resolve_price(item, columns[cIdx]);
                            var pCell = wsComp.Cell(cRow, 4 + cIdx);
                            pCell.SetValue(price);
                            pCell.Style.Font.Bold = true;
                            pCell.Style.NumberFormat.Format = "#,##0.00";
                            pCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        }

                        if (alt) rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                        rowRange.Style.Font.FontSize = 10;
                        rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        rowRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#D0D7DE");
                        rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        rowRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D0D7DE");

                        int lines = Math.Max(1, (int)Math.Ceiling((double)(item.name?.Length ?? 0) / 44));
                        wsComp.Row(cRow).Height = Math.Max(20, lines * 14.5);

                        cRow++;
                        alt = !alt;
                    }

                    if (gIdx < groups.Count - 1)
                    {
                        wsComp.Row(cRow).Height = 8;
                        cRow++;
                    }
                }

                wsComp.Column(1).Width = 14; // MARCA
                wsComp.Column(2).Width = 13; // CÓDIGO
                wsComp.Column(3).Width = 46; // PRODUCTO
                for (int cIdx = 0; cIdx < columns.Count; cIdx++)
                {
                    wsComp.Column(4 + cIdx).Width = 15;
                }
                wsComp.SheetView.FreezeRows(2);
                wsComp.ShowGridLines = true;
            }

            // =========================================================================
            // PESTAÑAS INDIVIDUALES (Una pestaña por cada lista seleccionada)
            // =========================================================================
            for (int sheetIndex = 0; sheetIndex < columns.Count; sheetIndex++)
            {
                var col = columns[sheetIndex];
                string listTitle = col.is_original || string.Equals(col.name, "Principal", StringComparison.OrdinalIgnoreCase)
                    ? "PRINCIPAL"
                    : col.name.Trim().ToUpperInvariant();

                string sName = SanitizeSheetName(col.name, sheetIndex + 1);
                if (usedSheetNames.Contains(sName)) sName = $"{sName} ({sheetIndex + 1})";
                usedSheetNames.Add(sName);

                var ws = wb.Worksheets.Add(sName);

                // 1. Banner Superior
                var titleLeft = ws.Range(1, 1, 1, 2);
                titleLeft.Merge();
                titleLeft.Value = $"NINOS - LISTA DE PRECIOS - {listTitle}";
                titleLeft.Style.Font.Bold = true;
                titleLeft.Style.Font.FontSize = 12;
                titleLeft.Style.Font.FontColor = XLColor.White;
                titleLeft.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
                titleLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                titleLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                titleLeft.Style.Alignment.Indent = 1;

                var titleRight = ws.Cell(1, 3);
                titleRight.Value = $"ACTUALIZADO: {DateTime.Now:dd/MM/yyyy HH:mm}";
                titleRight.Style.Font.Bold = true;
                titleRight.Style.Font.FontSize = 9.5;
                titleRight.Style.Font.FontColor = XLColor.White;
                titleRight.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
                titleRight.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                titleRight.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Row(1).Height = 26;

                ws.Row(2).Height = 5;

                int rowIdx = 3;

                for (int gIdx = 0; gIdx < groups.Count; gIdx++)
                {
                    var group = groups[gIdx];
                    string brandName = group.Key;
                    var (brandHex, _) = GetBrandHex(brandName);
                    var groupItems = group.ToList();

                    // Banner de Marca con color corporativo
                    var brandBanner = ws.Range(rowIdx, 1, rowIdx, 3);
                    brandBanner.Merge();
                    brandBanner.Value = $"  {brandName} ({groupItems.Count} PRODUCTOS)";
                    brandBanner.Style.Font.Bold = true;
                    brandBanner.Style.Font.FontSize = 10.5;
                    brandBanner.Style.Font.FontColor = XLColor.White;
                    brandBanner.Style.Fill.BackgroundColor = XLColor.FromHtml(brandHex);
                    brandBanner.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    brandBanner.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Row(rowIdx).Height = 22;
                    rowIdx++;

                    // Cabecera de la tabla
                    ws.Cell(rowIdx, 1).SetValue("CÓDIGO");
                    ws.Cell(rowIdx, 2).SetValue("DESCRIPCIÓN DEL PRODUCTO");
                    ws.Cell(rowIdx, 3).SetValue("PRECIO ($)");

                    var tableHeaderRange = ws.Range(rowIdx, 1, rowIdx, 3);
                    tableHeaderRange.Style.Font.Bold = true;
                    tableHeaderRange.Style.Font.FontSize = 9.5;
                    tableHeaderRange.Style.Font.FontColor = XLColor.FromHtml("#1B3A2D");
                    tableHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8ECE9");
                    tableHeaderRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    tableHeaderRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    tableHeaderRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#D0D7DE");
                    tableHeaderRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    tableHeaderRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D0D7DE");
                    ws.Cell(rowIdx, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(rowIdx, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    ws.Cell(rowIdx, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Row(rowIdx).Height = 22;
                    rowIdx++;

                    // Filas de productos con efecto cebra y WrapText garantizado
                    bool alt = false;
                    foreach (var item in groupItems)
                    {
                        var rowRange = ws.Range(rowIdx, 1, rowIdx, 3);

                        // Col 1: CÓDIGO
                        var codeCell = ws.Cell(rowIdx, 1);
                        codeCell.SetValue(item.product_code);
                        codeCell.Style.Font.Bold = true;
                        codeCell.Style.Font.FontSize = 10;
                        codeCell.Style.Font.FontColor = XLColor.FromHtml("#1565C0");
                        codeCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        codeCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        // Col 2: DESCRIPCIÓN DEL PRODUCTO (Con WrapText para no cortarse)
                        var nameCell = ws.Cell(rowIdx, 2);
                        nameCell.SetValue(item.name);
                        nameCell.Style.Font.FontSize = 10;
                        nameCell.Style.Alignment.WrapText = true;
                        nameCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        nameCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        // Col 3: PRECIO ($)
                        decimal price = resolve_price(item, col);
                        var priceCell = ws.Cell(rowIdx, 3);
                        priceCell.SetValue(price);
                        priceCell.Style.Font.Bold = true;
                        priceCell.Style.Font.FontSize = 10;
                        priceCell.Style.NumberFormat.Format = "#,##0.00";
                        priceCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        priceCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        if (alt) rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                        rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        rowRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#D0D7DE");
                        rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        rowRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D0D7DE");

                        // Altura dinámica: se ajusta para que todo el texto sea visible
                        int lines = Math.Max(1, (int)Math.Ceiling((double)(item.name?.Length ?? 0) / 50));
                        ws.Row(rowIdx).Height = Math.Max(20, lines * 14.5);

                        rowIdx++;
                        alt = !alt;
                    }

                    // Espaciador entre marcas
                    if (gIdx < groups.Count - 1)
                    {
                        ws.Row(rowIdx).Height = 8;
                        rowIdx++;
                    }
                }

                // Anchos compactos que caben 100% en pantalla sin scroll horizontal (TOTAL = 82)
                ws.Column(1).Width = 14;  // CÓDIGO
                ws.Column(2).Width = 52;  // DESCRIPCIÓN (Espacio amplio para nombre completo)
                ws.Column(3).Width = 16;  // PRECIO ($)

                ws.SheetView.FreezeRows(2);
                ws.ShowGridLines = true;
            }

            wb.SaveAs(filePath);
        }

        private static void generate_csv(string filePath, List<price_list_item> items, List<price_list_column> columns)
        {
            var lines = new List<string>();
            lines.Add("sep=;");

            var headers = new List<string> { "CÓDIGO", "PRODUCTO", "CATEGORÍA" };
            foreach (var col in columns)
            {
                if (columns.Count == 1 || col.is_original || string.Equals(col.name, "Principal", StringComparison.OrdinalIgnoreCase))
                {
                    headers.Add("PRECIO");
                }
                else
                {
                    headers.Add(col.name.Trim().ToUpperInvariant());
                }
            }
            lines.Add(string.Join(";", headers.Select(escape_csv)));

            var ordered = items
                .Where(i => !string.IsNullOrWhiteSpace(i.product_code))
                .OrderBy(i => i.brand)
                .ThenBy(i => i.product_code);

            foreach (var item in ordered)
            {
                var row = new List<string>
                {
                    escape_csv(item.product_code),
                    escape_csv(item.name),
                    escape_csv(item.brand)
                };

                foreach (var col in columns)
                {
                    decimal price = resolve_price(item, col);
                    row.Add(price.ToString("N2", CultureInfo.InvariantCulture));
                }

                lines.Add(string.Join(";", row));
            }

            File.WriteAllLines(filePath, lines, Utf8Bom);
        }

        private static string escape_csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(";") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}
