using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Common
{
    public static class CustomersExcelExporter
    {
        public static void export(CustomersReportPdfModel model)
        {
            if (model == null) return;

            using var wb = new XLWorkbook();
            var customers = model.Customers ?? new List<CustomerRowDto>();

            // 1. Hoja Principal con todos los clientes seleccionados
            string mainSheetName = model.TotalZones <= 1 && !string.IsNullOrWhiteSpace(model.Scope) && model.Scope.StartsWith("Zona", StringComparison.OrdinalIgnoreCase)
                ? MakeSafeSheetName(model.Scope)
                : "Clientes";

            var wsMain = wb.Worksheets.Add(mainSheetName);
            BuildWorksheet(wsMain, model.Scope, model.SearchFilter, model.EmissionDate, customers);

            // 2. Si hay múltiples zonas, agregar hojas dedicadas por cada zona seleccionada
            if (model.TotalZones > 1)
            {
                var groupedByZone = customers
                    .GroupBy(c => string.IsNullOrWhiteSpace(c.ZonaName) || c.ZonaName == "-" ? "Sin Zona" : c.ZonaName.Trim())
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { mainSheetName };

                foreach (var grp in groupedByZone)
                {
                    string safeName = MakeSafeSheetName(grp.Key);
                    if (usedSheetNames.Contains(safeName))
                    {
                        safeName = MakeSafeSheetName($"{grp.Key} 2");
                    }
                    usedSheetNames.Add(safeName);

                    var wsZone = wb.Worksheets.Add(safeName);
                    BuildWorksheet(wsZone, $"Zona {grp.Key}", model.SearchFilter, model.EmissionDate, grp.ToList());
                }
            }

            string safeScope = SanitizeFileName(string.IsNullOrWhiteSpace(model.Scope) ? "GENERAL" : model.Scope);
            string defaultName = $"LISTADO_CLIENTES_{safeScope}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar listado de clientes en Excel");
        }

        private static void BuildWorksheet(
            IXLWorksheet ws,
            string scope,
            string? searchFilter,
            DateTime emissionDate,
            List<CustomerRowDto> items)
        {
            const int totalCols = 10;

            // Fila 1: Banner de Encabezado Superior (Fondo Verde NinOS #1B3A2D)
            var titleLeft = ws.Range(1, 1, 1, 7);
            titleLeft.Merge();
            titleLeft.Value = $"LISTADO DE CLIENTES - {scope.ToUpperInvariant()}";
            titleLeft.Style.Font.Bold = true;
            titleLeft.Style.Font.FontSize = 11.5;
            titleLeft.Style.Font.FontColor = XLColor.White;
            titleLeft.Style.Fill.BackgroundColor = XLColor.FromHtml(ExcelFlatExporter.DarkGreenHeader);
            titleLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            titleLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var titleRight = ws.Range(1, 8, 1, totalCols);
            titleRight.Merge();
            titleRight.Value = $"EMISIÓN: {emissionDate:dd/MM/yyyy HH:mm}";
            titleRight.Style.Font.Bold = true;
            titleRight.Style.Font.FontSize = 9.5;
            titleRight.Style.Font.FontColor = XLColor.White;
            titleRight.Style.Fill.BackgroundColor = XLColor.FromHtml(ExcelFlatExporter.DarkGreenHeader);
            titleRight.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            titleRight.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 25;

            int currentRow = 2;
            if (!string.IsNullOrWhiteSpace(searchFilter))
            {
                var filterRange = ws.Range(currentRow, 1, currentRow, totalCols);
                filterRange.Merge();
                filterRange.Value = $"Filtro de búsqueda aplicado: \"{searchFilter.Trim()}\"";
                filterRange.Style.Font.Italic = true;
                filterRange.Style.Font.FontSize = 8.5;
                filterRange.Style.Font.FontColor = XLColor.FromHtml("#555555");
                ws.Row(currentRow).Height = 16;
                currentRow++;
            }

            // Fila separadora delgada
            ws.Row(currentRow).Height = 5;
            currentRow++;

            // Fila de Encabezados de Columna
            int headerRow = currentRow;
            string[] headers = new[]
            {
                "N°", "CÓDIGO", "RAZÓN SOCIAL / CLIENTE", "RIF", "CONTACTO",
                "TELÉFONO", "DIRECCIÓN FISCAL", "DIRECCIÓN ENTREGA", "ZONA", "VENDEDOR"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                ExcelFlatExporter.StyleHeaderCell(cell, ExcelFlatExporter.DarkGreenHeader);
            }
            ws.Row(headerRow).Height = 23;
            currentRow++;

            // Filas de Datos
            int index = 1;
            bool alt = false;
            foreach (var cust in items)
            {
                int c = 1;

                // 1. N°
                var numCell = ws.Cell(currentRow, c++);
                numCell.SetValue(index);
                numCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                numCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                numCell.Style.Font.FontSize = 9.5;

                // 2. CÓDIGO
                var codeCell = ws.Cell(currentRow, c++);
                codeCell.SetValue(cust.CustomerCode ?? string.Empty);
                codeCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                codeCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                codeCell.Style.Font.Bold = true;
                codeCell.Style.Font.FontSize = 9.5;
                codeCell.Style.Font.FontColor = XLColor.FromHtml(ExcelFlatExporter.DarkGreenHeader);

                // 3. RAZÓN SOCIAL (Con Enter/WrapText para texto largo)
                var nameCell = ws.Cell(currentRow, c++);
                nameCell.SetValue(cust.BusinessName ?? string.Empty);
                nameCell.Style.Font.Bold = true;
                nameCell.Style.Font.FontSize = 9.5;
                nameCell.Style.Alignment.WrapText = true;
                nameCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                nameCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // 4. RIF
                var rifCell = ws.Cell(currentRow, c++);
                rifCell.SetValue(cust.Rif ?? string.Empty);
                rifCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rifCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                rifCell.Style.Font.FontSize = 9.0;

                // 5. CONTACTO (Con Enter/WrapText)
                var contactCell = ws.Cell(currentRow, c++);
                contactCell.SetValue(cust.ContactName ?? string.Empty);
                contactCell.Style.Alignment.WrapText = true;
                contactCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                contactCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                contactCell.Style.Font.FontSize = 9.0;

                // 6. TELÉFONO
                var phoneCell = ws.Cell(currentRow, c++);
                phoneCell.SetValue(cust.PhoneNumber ?? string.Empty);
                phoneCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                phoneCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                phoneCell.Style.Font.FontSize = 9.0;

                // 7. DIRECCIÓN FISCAL (Con Enter/WrapText)
                var fiscalCell = ws.Cell(currentRow, c++);
                fiscalCell.SetValue(cust.FiscalAddress ?? string.Empty);
                fiscalCell.Style.Alignment.WrapText = true;
                fiscalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                fiscalCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                fiscalCell.Style.Font.FontSize = 8.5;
                fiscalCell.Style.Font.FontColor = XLColor.FromHtml("#333333");

                // 8. DIRECCIÓN ENTREGA (Con Enter/WrapText)
                var delivCell = ws.Cell(currentRow, c++);
                delivCell.SetValue(cust.EffectiveDeliveryAddress ?? string.Empty);
                delivCell.Style.Alignment.WrapText = true;
                delivCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                delivCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                delivCell.Style.Font.FontSize = 8.5;
                delivCell.Style.Font.FontColor = XLColor.FromHtml("#333333");

                // 9. ZONA (Con Enter/WrapText)
                var zoneCell = ws.Cell(currentRow, c++);
                zoneCell.SetValue(cust.ZonaName ?? string.Empty);
                zoneCell.Style.Alignment.WrapText = true;
                zoneCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                zoneCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                zoneCell.Style.Font.FontSize = 9.0;

                // 10. VENDEDOR
                var sellerCell = ws.Cell(currentRow, c++);
                sellerCell.SetValue(cust.SellerName ?? string.Empty);
                sellerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sellerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sellerCell.Style.Font.FontSize = 9.0;

                // Fondo cebra y bordes sutiles
                var dataRowRange = ws.Range(currentRow, 1, currentRow, totalCols);
                if (alt)
                {
                    dataRowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }
                dataRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                dataRowRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E0E0E0");
                dataRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRowRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E0E0E0");

                // Altura dinámica de fila según texto para que el Enter / WrapText se aprecie con claridad
                int maxLines = 1;
                string name = cust.BusinessName ?? string.Empty;
                string fiscal = cust.FiscalAddress ?? string.Empty;
                string deliv = cust.EffectiveDeliveryAddress ?? string.Empty;
                string contact = cust.ContactName ?? string.Empty;

                maxLines = Math.Max(maxLines, (int)Math.Ceiling((double)name.Length / 23));
                maxLines = Math.Max(maxLines, (int)Math.Ceiling((double)fiscal.Length / 21));
                maxLines = Math.Max(maxLines, (int)Math.Ceiling((double)deliv.Length / 21));
                maxLines = Math.Max(maxLines, (int)Math.Ceiling((double)contact.Length / 16));

                ws.Row(currentRow).Height = Math.Max(20, Math.Min(70, maxLines * 15.0));

                currentRow++;
                index++;
                alt = !alt;
            }

            // Fila de Totales
            if (items.Count > 0)
            {
                ws.Cell(currentRow, 1).SetValue("TOTAL CLIENTES");
                ws.Cell(currentRow, 2).SetValue(items.Count);
                ws.Cell(currentRow, 2).Style.NumberFormat.Format = "#,##0";
                ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(currentRow, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                ExcelFlatExporter.StyleTotalRow(ws.Range(currentRow, 1, currentRow, totalCols), ExcelFlatExporter.AccentSoftBg);
                ws.Row(currentRow).Height = 22;
            }

            // ANCHOS FIJOS ESTANDARIZADOS PARA QUE QUEPA 100% EN PANTALLA (TOTAL = 147)
            ws.Column(1).Width = 5;      // N°
            ws.Column(2).Width = 9;      // CÓDIGO
            ws.Column(3).Width = 24;     // RAZÓN SOCIAL / CLIENTE (WrapText)
            ws.Column(4).Width = 12.5;   // RIF
            ws.Column(5).Width = 16;     // CONTACTO (WrapText)
            ws.Column(6).Width = 12.5;   // TELÉFONO
            ws.Column(7).Width = 22;     // DIRECCIÓN FISCAL (WrapText)
            ws.Column(8).Width = 22;     // DIRECCIÓN ENTREGA (WrapText)
            ws.Column(9).Width = 13;     // ZONA (WrapText)
            ws.Column(10).Width = 11;    // VENDEDOR

            // Configuración de visualización e impresión para caber al 100% sin scroll horizontal
            ws.SheetView.ZoomScale = 100;
            ws.SheetView.FreezeRows(headerRow);
            if (currentRow > headerRow)
            {
                ws.Range(headerRow, 1, currentRow - 1, totalCols).SetAutoFilter();
            }
            ws.ShowGridLines = true;
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.FitToPages(1, 0); // Ajustar a 1 página de ancho
            ws.PageSetup.PaperSize = XLPaperSize.LetterPaper;
        }

        private static string MakeSafeSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Clientes";
            string clean = name.Replace("\\", "").Replace("/", "").Replace("?", "").Replace("*", "")
                               .Replace("[", "").Replace("]", "").Replace(":", "").Trim();
            if (clean.Length > 28) clean = clean.Substring(0, 28);
            return string.IsNullOrWhiteSpace(clean) ? "Clientes" : clean;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "GENERAL";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Trim().Replace(' ', '_');
        }
    }
}
