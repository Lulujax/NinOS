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
            var ws = wb.Worksheets.Add("InventarioActual");

            string[] headers = new[]
            {
                "MARCA", "CODIGO", "PRODUCTO", "STOCK ACTUAL", "PRECIO ($)", "VALOR TOTAL ($)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, i + 1), ExcelFlatExporter.DarkBlueHeader);
            }

            int rowIdx = 2;
            var items = model.Items ?? new System.Collections.Generic.List<CurrentInventoryPdfItem>();
            foreach (var item in items.OrderBy(x => x.Category).ThenBy(x => x.ProductCode))
            {
                int c = 1;
                ws.Cell(rowIdx, c++).SetValue(item.Category);
                ws.Cell(rowIdx, c++).SetValue(item.ProductCode);
                ws.Cell(rowIdx, c++).SetValue(item.Name);

                var qtyCell = ws.Cell(rowIdx, c++);
                qtyCell.SetValue(item.StockQuantity);
                qtyCell.Style.NumberFormat.Format = "#,##0";

                var priceCell = ws.Cell(rowIdx, c++);
                priceCell.SetValue(item.UnitPriceUsd);
                priceCell.Style.NumberFormat.Format = "#,##0.00";

                var totalCell = ws.Cell(rowIdx, c++);
                totalCell.SetValue(item.TotalValueUsd);
                totalCell.Style.NumberFormat.Format = "#,##0.00";

                if (item.IsNegativeStock)
                {
                    ws.Range(rowIdx, 1, rowIdx, headers.Length).Style.Font.FontColor = XLColor.Red;
                }

                rowIdx++;
            }

            if (items.Count > 0)
            {
                ws.Cell(rowIdx, 1).SetValue("TOTALES");
                ws.Cell(rowIdx, 4).SetValue(items.Sum(x => x.StockQuantity));
                ws.Cell(rowIdx, 4).Style.NumberFormat.Format = "#,##0";

                ws.Cell(rowIdx, 6).SetValue(items.Sum(x => x.TotalValueUsd));
                ws.Cell(rowIdx, 6).Style.NumberFormat.Format = "#,##0.00";

                ExcelFlatExporter.StyleTotalRow(ws.Range(rowIdx, 1, rowIdx, headers.Length), ExcelFlatExporter.BlueSoftBg);
            }

            ExcelFlatExporter.FormatTable(ws, headerRow: 1);

            string defaultName = $"INVENTARIO_ACTUAL_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar inventario actual en Excel");
        }
    }
}
