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
            var ws = wb.Worksheets.Add("TodosLosMovimientos");

            string[] headers = new[]
            {
                "MARCA", "CODIGO", "PRODUCTO", "FECHA", "DOCUMENTO", "MOTIVO",
                "CLIENTE", "VENDEDOR", "MOVIMIENTO", "UNIDADES", "PRECIO ($)",
                "SUBTOTAL ($)", "SALDO STOCK", "ESTADO"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, i + 1), ExcelFlatExporter.DarkBlueHeader);
            }

            int rowIdx = 2;
            var groups = model.ProductGroups ?? new System.Collections.Generic.List<ProductMovementGroup>();
            foreach (var grp in groups.OrderBy(g => g.Category).ThenBy(g => g.ProductCode))
            {
                foreach (var r in grp.Rows)
                {
                    int c = 1;
                    ws.Cell(rowIdx, c++).SetValue(grp.Category);
                    ws.Cell(rowIdx, c++).SetValue(grp.ProductCode);
                    ws.Cell(rowIdx, c++).SetValue(grp.ProductName);
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

                    var balanceCell = ws.Cell(rowIdx, c++);
                    balanceCell.SetValue(r.SaldoUnidades);
                    balanceCell.Style.NumberFormat.Format = "#,##0";

                    ws.Cell(rowIdx, c++).SetValue(r.Estado);

                    rowIdx++;
                }
            }

            ExcelFlatExporter.FormatTable(ws, headerRow: 1);

            string defaultName = $"KARDEX_MOVIMIENTOS_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar todos los movimientos en Excel");
        }
    }
}
