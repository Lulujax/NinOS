using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace NinOS.UI.Common
{
    public static class InventoryHistoryExcelExporter
    {
        public static void export(InventoryHistoryPdfModel model)
        {
            if (model == null) return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Historial");

            var rows = model.Rows ?? new System.Collections.Generic.List<InventoryHistoryPdfRow>();

            int totalCols = model.MostrarSaldos ? 12 : 11;

            // 1. BANNER INSTITUCIONAL SUPERIOR (Fila 1)
            var titleLeft = ws.Range(1, 1, 1, Math.Max(1, totalCols - 3));
            titleLeft.Merge();
            string itemHeader = string.IsNullOrWhiteSpace(model.ItemCode)
                ? $"NINOS - HISTORIAL DE {model.ItemLabel}: {model.ItemName}"
                : $"NINOS - HISTORIAL DE {model.ItemLabel}: {model.ItemName} [{model.ItemCode}]";
            titleLeft.Value = itemHeader.ToUpperInvariant();
            titleLeft.Style.Font.Bold = true;
            titleLeft.Style.Font.FontSize = 11.5;
            titleLeft.Style.Font.FontColor = XLColor.White;
            titleLeft.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D62B5");
            titleLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            titleLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            titleLeft.Style.Alignment.Indent = 1;

            var titleRight = ws.Range(1, Math.Max(2, totalCols - 2), 1, totalCols);
            titleRight.Merge();
            titleRight.Value = $"EMISIÓN: {DateTime.Now:dd/MM/yyyy HH:mm}";
            titleRight.Style.Font.Bold = true;
            titleRight.Style.Font.FontSize = 9.5;
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
                range.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D0D7DE");
            }

            StyleCard(ws.Range(3, 1, 3, 3), "CATEGORÍA", string.IsNullOrWhiteSpace(model.Category) ? "OTROS" : model.Category);
            StyleCard(ws.Range(3, 4, 3, 6), "PRECIO ACTUAL", $"$ {model.UnitPriceUsd:N2}", "#2E7D32");
            if (model.MostrarSaldos)
            {
                StyleCard(ws.Range(3, 7, 3, 9), "STOCK ACTUAL", $"{model.CurrentStock:N0} uds", model.CurrentStock < 0 ? "#C62828" : "#E65100");
                StyleCard(ws.Range(3, 10, 3, 12), "REGISTROS", $"{rows.Count:N0} movs");
            }
            else
            {
                StyleCard(ws.Range(3, 7, 3, 11), "REGISTROS EN TOTAL", $"{rows.Count:N0} movs");
            }
            ws.Row(3).Height = 24;

            // Fila 4: Separador
            ws.Row(4).Height = 5;

            // 3. ENCABEZADOS DE LA TABLA (Fila 5)
            int headerRow = 5;
            string[] headers = model.MostrarSaldos
                ? new[] { "FECHA", "DOCUMENTO", "MOTIVO", "CLIENTE", "VENDEDOR", "MOVIMIENTO", "UNIDADES", "PRECIO ($)", "SUBTOTAL ($)", "SALDO STOCK", "ESTADO", "NOTA #" }
                : new[] { "FECHA", "DOCUMENTO", "MOTIVO", "CLIENTE", "VENDEDOR", "MOVIMIENTO", "UNIDADES", "PRECIO ($)", "SUBTOTAL ($)", "ESTADO", "NOTA #" };

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
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#90CAF9");
            }
            ws.Row(headerRow).Height = 25;

            // 4. DETALLE DE MOVIMIENTOS (Fila 6 en adelante)
            int rowIdx = 6;
            bool alt = false;
            decimal totalSubtotalGeneral = 0m;
            int totalEntradas = 0;
            int totalSalidas = 0;

            foreach (var r in rows)
            {
                totalSubtotalGeneral += r.SubtotalUsd;
                if (r.EsEntrada) totalEntradas += r.Unidades;
                else totalSalidas += r.Unidades;

                var rowRange = ws.Range(rowIdx, 1, rowIdx, headers.Length);
                bool isAnulada = string.Equals(r.Estado?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase);

                int c = 1;
                // Col 1: FECHA
                var fCell = ws.Cell(rowIdx, c++);
                fCell.SetValue(r.FechaLocal.ToString("dd/MM/yyyy HH:mm"));
                fCell.Style.Font.FontSize = 9.5;
                fCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                fCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 2: DOCUMENTO
                var docCell = ws.Cell(rowIdx, c++);
                docCell.SetValue(r.Documento);
                docCell.Style.Font.FontSize = 9.5;
                docCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                docCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 3: MOTIVO
                var motCell = ws.Cell(rowIdx, c++);
                motCell.SetValue(r.Motivo);
                motCell.Style.Font.FontSize = 9.5;
                motCell.Style.Alignment.WrapText = true;
                motCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                motCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 4: CLIENTE
                var cliCell = ws.Cell(rowIdx, c++);
                cliCell.SetValue(r.Cliente);
                cliCell.Style.Font.FontSize = 9.5;
                cliCell.Style.Alignment.WrapText = true;
                cliCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                cliCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 5: VENDEDOR
                var venCell = ws.Cell(rowIdx, c++);
                venCell.SetValue(r.Vendedor);
                venCell.Style.Font.FontSize = 9.5;
                venCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                venCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 6: MOVIMIENTO
                var movCell = ws.Cell(rowIdx, c++);
                movCell.SetValue(r.Movimiento);
                movCell.Style.Font.Bold = true;
                movCell.Style.Font.FontSize = 9.5;
                movCell.Style.Font.FontColor = r.EsEntrada ? XLColor.FromHtml("#2E7D32") : XLColor.FromHtml("#C62828");
                movCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                movCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 7: UNIDADES
                var unitsCell = ws.Cell(rowIdx, c++);
                unitsCell.SetValue(r.Unidades);
                unitsCell.Style.Font.Bold = true;
                unitsCell.Style.Font.FontSize = 9.5;
                unitsCell.Style.NumberFormat.Format = "#,##0";
                unitsCell.Style.Font.FontColor = r.EsEntrada ? XLColor.FromHtml("#2E7D32") : XLColor.FromHtml("#C62828");
                unitsCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                unitsCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 8: PRECIO ($)
                var priceCell = ws.Cell(rowIdx, c++);
                priceCell.SetValue(r.PrecioUsd);
                priceCell.Style.Font.FontSize = 9.5;
                priceCell.Style.NumberFormat.Format = "#,##0.00";
                priceCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                priceCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 9: SUBTOTAL ($)
                var subtotalCell = ws.Cell(rowIdx, c++);
                subtotalCell.SetValue(r.SubtotalUsd);
                subtotalCell.Style.Font.Bold = true;
                subtotalCell.Style.Font.FontSize = 9.5;
                subtotalCell.Style.NumberFormat.Format = "#,##0.00";
                subtotalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                subtotalCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 10: SALDO STOCK (si aplica)
                if (model.MostrarSaldos)
                {
                    var balanceCell = ws.Cell(rowIdx, c++);
                    balanceCell.SetValue(r.SaldoUnidades);
                    balanceCell.Style.Font.Bold = true;
                    balanceCell.Style.Font.FontSize = 9.5;
                    balanceCell.Style.Font.FontColor = XLColor.FromHtml("#0D62B5");
                    balanceCell.Style.NumberFormat.Format = "#,##0";
                    balanceCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    balanceCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                // Col 11: ESTADO
                var stCell = ws.Cell(rowIdx, c++);
                stCell.SetValue(r.Estado);
                stCell.Style.Font.Bold = true;
                stCell.Style.Font.FontSize = 9.5;
                stCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                stCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                if (isAnulada) stCell.Style.Font.FontColor = XLColor.FromHtml("#C62828");

                // Col 12: NOTA #
                var numCell = ws.Cell(rowIdx, c++);
                numCell.SetValue(string.IsNullOrWhiteSpace(r.NoteNumber) ? "-" : r.NoteNumber);
                numCell.Style.Font.FontSize = 9.5;
                numCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                numCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Colores de fondo de fila
                if (isAnulada)
                {
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE");
                }
                else if (alt)
                {
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }

                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#D0D7DE");
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D0D7DE");

                int cLines = Math.Max(1, (int)Math.Ceiling((double)(r.Cliente?.Length ?? 0) / 18));
                int mLines = Math.Max(1, (int)Math.Ceiling((double)(r.Motivo?.Length ?? 0) / 16));
                int maxLines = Math.Max(cLines, mLines);
                ws.Row(rowIdx).Height = Math.Max(20, maxLines * 14.5);

                rowIdx++;
                alt = !alt;
            }

            int lastDataRow = rowIdx - 1;

            // 5. FILA DE TOTALES GENERALES
            if (rows.Count > 0)
            {
                var totLabelRange = ws.Range(rowIdx, 1, rowIdx, 6);
                totLabelRange.Merge();
                totLabelRange.Value = $"TOTALES ({rows.Count} MOVIMIENTOS)";
                totLabelRange.Style.Font.Bold = true;
                totLabelRange.Style.Font.FontSize = 10;
                totLabelRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                totLabelRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var totUnitsCell = ws.Cell(rowIdx, 7);
                totUnitsCell.SetValue(totalEntradas - totalSalidas);
                totUnitsCell.Style.Font.Bold = true;
                totUnitsCell.Style.Font.FontSize = 10;
                totUnitsCell.Style.NumberFormat.Format = "#,##0";
                totUnitsCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                totUnitsCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                ws.Cell(rowIdx, 8).SetValue("");

                var totSubCell = ws.Cell(rowIdx, 9);
                totSubCell.SetValue(totalSubtotalGeneral);
                totSubCell.Style.Font.Bold = true;
                totSubCell.Style.Font.FontSize = 10;
                totSubCell.Style.NumberFormat.Format = "#,##0.00";
                totSubCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                totSubCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                for (int k = 10; k <= headers.Length; k++)
                {
                    ws.Cell(rowIdx, k).SetValue("");
                }

                var totalRange = ws.Range(rowIdx, 1, rowIdx, headers.Length);
                totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD");
                totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                totalRange.Style.Border.TopBorderColor = XLColor.FromHtml("#0D62B5");
                totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
                totalRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#0D62B5");
                ws.Row(rowIdx).Height = 25;
            }

            // 6. ANCHOS DE COLUMNAS DEFINIDOS PARA AJUSTARSE A PANTALLA (TOTAL = ~140)
            ws.Column(1).Width = 14;   // FECHA
            ws.Column(2).Width = 13;   // DOCUMENTO
            ws.Column(3).Width = 17;   // MOTIVO
            ws.Column(4).Width = 19;   // CLIENTE
            ws.Column(5).Width = 13;   // VENDEDOR
            ws.Column(6).Width = 11;   // MOVIMIENTO
            ws.Column(7).Width = 10;   // UNIDADES
            ws.Column(8).Width = 11;   // PRECIO ($)
            ws.Column(9).Width = 12;   // SUBTOTAL ($)
            if (model.MostrarSaldos)
            {
                ws.Column(10).Width = 11;  // SALDO STOCK
                ws.Column(11).Width = 11;  // ESTADO
                ws.Column(12).Width = 12;  // NOTA #
            }
            else
            {
                ws.Column(10).Width = 11;  // ESTADO
                ws.Column(11).Width = 12;  // NOTA #
            }

            if (lastDataRow >= headerRow)
            {
                ws.Range(headerRow, 1, lastDataRow, headers.Length).SetAutoFilter();
            }
            ws.SheetView.FreezeRows(headerRow);
            ws.ShowGridLines = true;

            string safe_code = string.IsNullOrWhiteSpace(model.ItemCode) ? "INVENTARIO" : model.ItemCode.Replace(" ", "_");
            string defaultName = $"HISTORIAL_{safe_code}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar historial de movimientos en Excel");
        }
    }
}
