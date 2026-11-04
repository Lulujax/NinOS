using System;
using System.IO;
using ClosedXML.Excel;
using NinOS.Domain.ViewModels;

namespace NinOS.UI.Common
{
    public static class NoteExcelExporter
    {
        public static void export(note_print_dto note)
        {
            if (note == null) return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Nota");

            string[] headers = new[]
            {
                "NRO NOTA", "DOCUMENTO", "FECHA", "CLIENTE", "RIF", "VENDEDOR",
                "CODIGO", "PRODUCTO", "CANTIDAD", "PRECIO ($)", "DESCUENTO ($)", "TOTAL ($)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, i + 1));
            }

            int rowIdx = 2;
            foreach (var d in note.details)
            {
                int c = 1;
                ws.Cell(rowIdx, c++).SetValue(note.note_number);
                ws.Cell(rowIdx, c++).SetValue(note.document_label);
                ws.Cell(rowIdx, c++).SetValue(note.creation_date.ToString("dd/MM/yyyy"));
                ws.Cell(rowIdx, c++).SetValue(note.customer_business_name);
                ws.Cell(rowIdx, c++).SetValue(note.customer_rif);
                ws.Cell(rowIdx, c++).SetValue(note.seller_name);

                ws.Cell(rowIdx, c++).SetValue(d.code);
                ws.Cell(rowIdx, c++).SetValue(d.name);

                var qCell = ws.Cell(rowIdx, c++);
                qCell.SetValue(d.quantity);
                qCell.Style.NumberFormat.Format = "#,##0";

                var pCell = ws.Cell(rowIdx, c++);
                pCell.SetValue(d.unit_price_usd);
                pCell.Style.NumberFormat.Format = "#,##0.00";

                var discCell = ws.Cell(rowIdx, c++);
                discCell.SetValue(d.discount_usd);
                discCell.Style.NumberFormat.Format = "#,##0.00";

                var totCell = ws.Cell(rowIdx, c++);
                totCell.SetValue(d.subtotal_usd);
                totCell.Style.NumberFormat.Format = "#,##0.00";

                rowIdx++;
            }

            ws.Cell(rowIdx, 1).SetValue("TOTAL NOTA ($)");
            ws.Cell(rowIdx, 12).SetValue(note.total_amount_usd);
            ws.Cell(rowIdx, 12).Style.NumberFormat.Format = "#,##0.00";
            ExcelFlatExporter.StyleTotalRow(ws.Range(rowIdx, 1, rowIdx, headers.Length));

            ExcelFlatExporter.FormatTable(ws, headerRow: 1);

            bool is_credit = string.Equals(note.document_label, "NOTA DE CREDITO", StringComparison.OrdinalIgnoreCase);
            string defaultName = is_credit ? $"NotaCredito_{note.note_number}.xlsx" : $"NotaEntrega_{note.note_number}.xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar nota en Excel");
        }

        public static void export(note_print_dto original, note_print_dto copy)
        {
            if (original == null && copy == null) return;
            if (original == null) { export(copy!); return; }
            if (copy == null) { export(original); return; }

            using var wb = new XLWorkbook();
            populate_note_sheet(wb.Worksheets.Add("Original"), original);
            populate_note_sheet(wb.Worksheets.Add("Copia"), copy);

            string defaultName = $"Notas_{original.note_number}_{copy.note_number}.xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar notas en Excel");
        }

        private static void populate_note_sheet(IXLWorksheet ws, note_print_dto note)
        {
            string[] headers = new[]
            {
                "NRO NOTA", "DOCUMENTO", "FECHA", "CLIENTE", "RIF", "VENDEDOR",
                "CODIGO", "PRODUCTO", "CANTIDAD", "PRECIO ($)", "DESCUENTO ($)", "TOTAL ($)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ExcelFlatExporter.StyleHeaderCell(ws.Cell(1, i + 1));
            }

            int rowIdx = 2;
            foreach (var d in note.details)
            {
                int c = 1;
                ws.Cell(rowIdx, c++).SetValue(note.note_number);
                ws.Cell(rowIdx, c++).SetValue(note.document_label);
                ws.Cell(rowIdx, c++).SetValue(note.creation_date.ToString("dd/MM/yyyy"));
                ws.Cell(rowIdx, c++).SetValue(note.customer_business_name);
                ws.Cell(rowIdx, c++).SetValue(note.customer_rif);
                ws.Cell(rowIdx, c++).SetValue(note.seller_name);

                ws.Cell(rowIdx, c++).SetValue(d.code);
                ws.Cell(rowIdx, c++).SetValue(d.name);

                var qCell = ws.Cell(rowIdx, c++);
                qCell.SetValue(d.quantity);
                qCell.Style.NumberFormat.Format = "#,##0";

                var pCell = ws.Cell(rowIdx, c++);
                pCell.SetValue(d.unit_price_usd);
                pCell.Style.NumberFormat.Format = "#,##0.00";

                var discCell = ws.Cell(rowIdx, c++);
                discCell.SetValue(d.discount_usd);
                discCell.Style.NumberFormat.Format = "#,##0.00";

                var totCell = ws.Cell(rowIdx, c++);
                totCell.SetValue(d.subtotal_usd);
                totCell.Style.NumberFormat.Format = "#,##0.00";

                rowIdx++;
            }

            ws.Cell(rowIdx, 1).SetValue("TOTAL NOTA ($)");
            ws.Cell(rowIdx, 12).SetValue(note.total_amount_usd);
            ws.Cell(rowIdx, 12).Style.NumberFormat.Format = "#,##0.00";
            ExcelFlatExporter.StyleTotalRow(ws.Range(rowIdx, 1, rowIdx, headers.Length));

            ExcelFlatExporter.FormatTable(ws, headerRow: 1);
        }
    }
}
