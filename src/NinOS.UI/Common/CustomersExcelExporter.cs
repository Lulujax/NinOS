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
                : "Maestro de Clientes";

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
            string defaultName = $"MAESTRO_CLIENTES_{safeScope}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar maestro de clientes en Excel");
        }

        private static void BuildWorksheet(
            IXLWorksheet ws,
            string scope,
            string? searchFilter,
            DateTime emissionDate,
            List<CustomerRowDto> items)
        {
            const int totalCols = 9;

            // Fila 1: Banner de Encabezado Superior (Fondo Verde NinOS #1B3A2D, bordes en negro)
            var titleLeft = ws.Range(1, 1, 1, 6);
            titleLeft.Merge();
            titleLeft.Value = $"MAESTRO DE CLIENTES - {scope.ToUpperInvariant()}";
            titleLeft.Style.Font.Bold = true;
            titleLeft.Style.Font.FontSize = 11.5;
            titleLeft.Style.Font.FontColor = XLColor.White;
            titleLeft.Style.Fill.BackgroundColor = XLColor.FromHtml(ExcelFlatExporter.DarkGreenHeader);
            titleLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            titleLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var titleRight = ws.Range(1, 7, 1, totalCols);
            titleRight.Merge();
            titleRight.Value = $"EMISIÓN: {emissionDate:dd/MM/yyyy HH:mm}";
            titleRight.Style.Font.Bold = true;
            titleRight.Style.Font.FontSize = 9.5;
            titleRight.Style.Font.FontColor = XLColor.White;
            titleRight.Style.Fill.BackgroundColor = XLColor.FromHtml(ExcelFlatExporter.DarkGreenHeader);
            titleRight.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            titleRight.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var titleFull = ws.Range(1, 1, 1, totalCols);
            titleFull.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            titleFull.Style.Border.OutsideBorderColor = XLColor.Black;
            ws.Row(1).Height = 25;

            int currentRow = 2;
            if (!string.IsNullOrWhiteSpace(searchFilter))
            {
                var filterRange = ws.Range(currentRow, 1, currentRow, totalCols);
                filterRange.Merge();
                filterRange.Value = $"Filtro de búsqueda aplicado: \"{searchFilter.Trim()}\"";
                filterRange.Style.Font.Italic = true;
                filterRange.Style.Font.FontSize = 8.5;
                filterRange.Style.Font.FontColor = XLColor.FromHtml("#333333");
                filterRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                filterRange.Style.Border.OutsideBorderColor = XLColor.Black;
                ws.Row(currentRow).Height = 16;
                currentRow++;
            }

            // Fila separadora delgada
            ws.Row(currentRow).Height = 5;
            currentRow++;

            // Fila de Encabezados de Columna (9 columnas, sin Vendedor)
            int headerRow = currentRow;
            string[] headers = new[]
            {
                "N°", "CÓDIGO", "RAZÓN SOCIAL / CLIENTE", "RIF", "CONTACTO",
                "TELÉFONO", "DIRECCIÓN FISCAL", "DIRECCIÓN ENTREGA", "ZONA"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Font.FontSize = 9.5;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml(ExcelFlatExporter.DarkGreenHeader);
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.Black;
                cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.InsideBorderColor = XLColor.Black;
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
                fiscalCell.Style.Font.FontColor = XLColor.FromHtml("#222222");

                // 8. DIRECCIÓN ENTREGA (Con Enter/WrapText)
                var delivCell = ws.Cell(currentRow, c++);
                delivCell.SetValue(cust.EffectiveDeliveryAddress ?? string.Empty);
                delivCell.Style.Alignment.WrapText = true;
                delivCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                delivCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                delivCell.Style.Font.FontSize = 8.5;
                delivCell.Style.Font.FontColor = XLColor.FromHtml("#222222");

                // 9. ZONA (Con Enter/WrapText)
                var zoneCell = ws.Cell(currentRow, c++);
                zoneCell.SetValue(cust.ZonaName ?? string.Empty);
                zoneCell.Style.Alignment.WrapText = true;
                zoneCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                zoneCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                zoneCell.Style.Font.FontSize = 9.0;

                // Fondo cebra y TODOS LOS BORDES EN NEGRO
                var dataRowRange = ws.Range(currentRow, 1, currentRow, totalCols);
                if (alt)
                {
                    dataRowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }
                dataRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                dataRowRange.Style.Border.InsideBorderColor = XLColor.Black;
                dataRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRowRange.Style.Border.OutsideBorderColor = XLColor.Black;

                // Altura dinámica de fila según texto para que el Enter / WrapText se aprecie con claridad
                int maxLines = 1;
                string name = cust.BusinessName ?? string.Empty;
                string fiscal = cust.FiscalAddress ?? string.Empty;
                string deliv = cust.EffectiveDeliveryAddress ?? string.Empty;
                string contact = cust.ContactName ?? string.Empty;

                maxLines = Math.Max(maxLines, (int)Math.Ceiling((double)name.Length / 25));
                maxLines = Math.Max(maxLines, (int)Math.Ceiling((double)fiscal.Length / 24));
                maxLines = Math.Max(maxLines, (int)Math.Ceiling((double)deliv.Length / 24));
                maxLines = Math.Max(maxLines, (int)Math.Ceiling((double)contact.Length / 16));

                ws.Row(currentRow).Height = Math.Max(20, Math.Min(70, maxLines * 15.0));

                currentRow++;
                index++;
                alt = !alt;
            }

            // Fila de Totales con bordes en negro
            if (items.Count > 0)
            {
                ws.Cell(currentRow, 1).SetValue("TOTAL CLIENTES");
                ws.Cell(currentRow, 2).SetValue(items.Count);
                ws.Cell(currentRow, 2).Style.NumberFormat.Format = "#,##0";
                ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(currentRow, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var totalRange = ws.Range(currentRow, 1, currentRow, totalCols);
                totalRange.Style.Font.Bold = true;
                totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml(ExcelFlatExporter.AccentSoftBg);
                totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                totalRange.Style.Border.TopBorderColor = XLColor.Black;
                totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
                totalRange.Style.Border.BottomBorderColor = XLColor.Black;
                totalRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                totalRange.Style.Border.InsideBorderColor = XLColor.Black;
                totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                totalRange.Style.Border.OutsideBorderColor = XLColor.Black;
                ws.Row(currentRow).Height = 22;
            }

            // ANCHOS FIJOS ESTANDARIZADOS PARA QUE QUEPA 100% EN PANTALLA (TOTAL = 150)
            ws.Column(1).Width = 5;      // N°
            ws.Column(2).Width = 10;     // CÓDIGO
            ws.Column(3).Width = 26;     // RAZÓN SOCIAL / CLIENTE (WrapText)
            ws.Column(4).Width = 13;     // RIF
            ws.Column(5).Width = 17;     // CONTACTO (WrapText)
            ws.Column(6).Width = 13;     // TELÉFONO
            ws.Column(7).Width = 26;     // DIRECCIÓN FISCAL (WrapText)
            ws.Column(8).Width = 26;     // DIRECCIÓN ENTREGA (WrapText)
            ws.Column(9).Width = 14;     // ZONA (WrapText)

            // Configuración de visualización e impresión para caber al 100% sin scroll horizontal (NO freeze)
            ws.SheetView.ZoomScale = 100;
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
            if (string.IsNullOrWhiteSpace(name)) return "Maestro";
            string clean = name.Replace("\\", "").Replace("/", "").Replace("?", "").Replace("*", "")
                               .Replace("[", "").Replace("]", "").Replace(":", "").Trim();
            if (clean.Length > 28) clean = clean.Substring(0, 28);
            return string.IsNullOrWhiteSpace(clean) ? "Maestro" : clean;
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
