using System;
using System.IO;
using ClosedXML.Excel;

namespace NinOS.UI.Common
{
    public static class InventoryHistoryExcelExporter
    {
        public static void export(InventoryHistoryPdfModel model)
        {
            if (model == null) return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("HistorialMovimientos");

            int col = 1;
            ws.Cell(1, col).Value = "CODIGO";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++), ExcelFlatExporter.DarkBlueHeader);

            ws.Cell(1, col).Value = model.ItemLabel.ToUpperInvariant();
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++), ExcelFlatExporter.DarkBlueHeader);

            ws.Cell(1, col).Value = "FECHA";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++), ExcelFlatExporter.DarkBlueHeader);

            ws.Cell(1, col).Value = "DOCUMENTO";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++), ExcelFlatExporter.DarkBlueHeader);

            ws.Cell(1, col).Value = "MOTIVO";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++), ExcelFlatExporter.DarkBlueHeader);

            ws.Cell(1, col).Value = "CLIENTE";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++), ExcelFlatExporter.DarkBlueHeader);

            ws.Cell(1, col).Value = "VENDEDOR";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++), ExcelFlatExporter.DarkBlueHeader);

            ws.Cell(1, col).Value = "MOVIMIENTO";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++), ExcelFlatExporter.DarkBlueHeader);

            ws.Cell(1, col).Value = "UNIDADES";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++), ExcelFlatExporter.DarkBlueHeader);

            ws.Cell(1, col).Value = "PRECIO ($)";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++), ExcelFlatExporter.DarkBlueHeader);

            ws.Cell(1, col).Value = "SUBTOTAL ($)";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++), ExcelFlatExporter.DarkBlueHeader);

            if (model.MostrarSaldos)
            {
                ws.Cell(1, col).Value = "SALDO STOCK";
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++), ExcelFlatExporter.DarkBlueHeader);
            }

            ws.Cell(1, col).Value = "ESTADO";
            ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, col++), ExcelFlatExporter.DarkBlueHeader);

            int totalCols = col - 1;
            int rowIdx = 2;

            var rows = model.Rows ?? new System.Collections.Generic.List<InventoryHistoryPdfRow>();
            foreach (var r in rows)
            {
                int c = 1;
                ws.Cell(rowIdx, c++).SetValue(model.ItemCode);
                ws.Cell(rowIdx, c++).SetValue(model.ItemName);
                ws.Cell(rowIdx, c++).SetValue(r.FechaLocal.ToString("dd/MM/yyyy HH:mm"));
                ws.Cell(rowIdx, c++).SetValue(string.IsNullOrWhiteSpace(r.NoteNumber) ? r.Documento : r.NoteNumber);
                ws.Cell(rowIdx, c++).SetValue(r.Motivo);
                ws.Cell(rowIdx, c++).SetValue(r.Cliente);
                ws.Cell(rowIdx, c++).SetValue(r.Vendedor);
                ws.Cell(rowIdx, c++).SetValue(r.Movimiento);

                var unitsCell = ws.Cell(rowIdx, c++);
                unitsCell.SetValue(r.Unidades);
                unitsCell.Style.NumberFormat.Format = "#,##0";
                if (r.EsEntrada) unitsCell.Style.Font.FontColor = XLColor.Green;
                else unitsCell.Style.Font.FontColor = XLColor.Red;

                var priceCell = ws.Cell(rowIdx, c++);
                priceCell.SetValue(r.PrecioUsd);
                priceCell.Style.NumberFormat.Format = "#,##0.00";

                var subtotalCell = ws.Cell(rowIdx, c++);
                subtotalCell.SetValue(r.SubtotalUsd);
                subtotalCell.Style.NumberFormat.Format = "#,##0.00";

                if (model.MostrarSaldos)
                {
                    var balanceCell = ws.Cell(rowIdx, c++);
                    balanceCell.SetValue(r.SaldoUnidades);
                    balanceCell.Style.NumberFormat.Format = "#,##0";
                }

                ws.Cell(rowIdx, c++).SetValue(r.Estado);

                rowIdx++;
            }

            ExcelFlatExporter.FormatTable(ws, headerRow: 1);

            string safe_code = string.IsNullOrWhiteSpace(model.ItemCode) ? "INVENTARIO" : model.ItemCode.Replace(" ", "_");
            string defaultName = $"HISTORIAL_{safe_code}_{DateTime.Now:yyyyMMdd}.xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar historial de movimientos en Excel");
        }
    }
}
