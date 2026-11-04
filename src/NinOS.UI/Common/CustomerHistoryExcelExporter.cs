using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace NinOS.UI.Common
{
    public static class CustomerHistoryExcelExporter
    {
        public static void export(CustomerHistoryPdfModel model)
        {
            if (model == null) return;

            using var wb = new XLWorkbook();

            // 1. Hoja Notas de Entrega
            var wsNotes = wb.Worksheets.Add("Notas de Entrega");
            string[] headersNotes = new[]
            {
                "EMISION", "DESPACHO", "N NOTA", "VENDEDOR", "TIPO", "TOTAL ($)", "ABONADO ($)", "SALDO ($)", "ESTADO"
            };
            for (int i = 0; i < headersNotes.Length; i++)
            {
                wsNotes.Cell(1, i + 1).Value = headersNotes[i];
                ExcelFlatExporter.StyleHeaderCell(wsNotes.Cell(1, i + 1), ExcelFlatExporter.DarkBlueHeader);
            }
            int rNote = 2;
            foreach (var n in model.DeliveryNotes)
            {
                int c = 1;
                wsNotes.Cell(rNote, c++).SetValue(n.creation_date.ToString("dd/MM/yyyy"));
                wsNotes.Cell(rNote, c++).SetValue(n.dispatch_date.HasValue ? n.dispatch_date.Value.ToString("dd/MM/yyyy") : "-");
                wsNotes.Cell(rNote, c++).SetValue(n.note_number);
                wsNotes.Cell(rNote, c++).SetValue(n.seller_name);
                wsNotes.Cell(rNote, c++).SetValue(n.note_type_name);

                var totalCell = wsNotes.Cell(rNote, c++);
                totalCell.SetValue(n.total_amount_usd);
                totalCell.Style.NumberFormat.Format = "#,##0.00";

                var paidCell = wsNotes.Cell(rNote, c++);
                paidCell.SetValue(n.paid_amount_usd);
                paidCell.Style.NumberFormat.Format = "#,##0.00";

                var balCell = wsNotes.Cell(rNote, c++);
                balCell.SetValue(n.balance_due_usd);
                balCell.Style.NumberFormat.Format = "#,##0.00";

                wsNotes.Cell(rNote, c++).SetValue(n.status);

                if (string.Equals(n.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase))
                {
                    wsNotes.Range(rNote, 1, rNote, headersNotes.Length).Style.Font.FontColor = XLColor.Red;
                }
                rNote++;
            }
            var validNotes = model.DeliveryNotes.Where(x => !string.Equals(x.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase)).ToList();
            if (validNotes.Count > 0)
            {
                wsNotes.Cell(rNote, 1).SetValue("TOTALES");
                wsNotes.Cell(rNote, 6).SetValue(validNotes.Sum(x => x.total_amount_usd));
                wsNotes.Cell(rNote, 6).Style.NumberFormat.Format = "#,##0.00";
                wsNotes.Cell(rNote, 7).SetValue(validNotes.Sum(x => x.paid_amount_usd));
                wsNotes.Cell(rNote, 7).Style.NumberFormat.Format = "#,##0.00";
                wsNotes.Cell(rNote, 8).SetValue(validNotes.Sum(x => x.balance_due_usd));
                wsNotes.Cell(rNote, 8).Style.NumberFormat.Format = "#,##0.00";
                ExcelFlatExporter.StyleTotalRow(wsNotes.Range(rNote, 1, rNote, headersNotes.Length), ExcelFlatExporter.BlueSoftBg);
            }
            ExcelFlatExporter.FormatTable(wsNotes, headerRow: 1);

            // 2. Hoja Notas de Crédito
            if (model.CreditNotes != null && model.CreditNotes.Count > 0)
            {
                var wsCn = wb.Worksheets.Add("Notas de Credito");
                string[] headersCn = new[] { "FECHA", "N NC", "NOTA ORIGEN", "CATEGORIA", "VENDEDOR", "TOTAL ($)", "ESTADO" };
                for (int i = 0; i < headersCn.Length; i++)
                {
                    wsCn.Cell(1, i + 1).Value = headersCn[i];
                    ExcelFlatExporter.StyleHeaderCell(wsCn.Cell(1, i + 1), "#6A1B9A");
                }
                int rCn = 2;
                foreach (var cn in model.CreditNotes)
                {
                    int c = 1;
                    wsCn.Cell(rCn, c++).SetValue(cn.creation_date.ToString("dd/MM/yyyy"));
                    wsCn.Cell(rCn, c++).SetValue(cn.note_number);
                    wsCn.Cell(rCn, c++).SetValue(cn.source_note_number);
                    wsCn.Cell(rCn, c++).SetValue(cn.category);
                    wsCn.Cell(rCn, c++).SetValue(cn.seller_name);

                    var tCell = wsCn.Cell(rCn, c++);
                    tCell.SetValue(cn.total_amount_usd);
                    tCell.Style.NumberFormat.Format = "#,##0.00";

                    wsCn.Cell(rCn, c++).SetValue(cn.status);
                    rCn++;
                }
                var validCns = model.CreditNotes.Where(x => !string.Equals(x.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase)).ToList();
                if (validCns.Count > 0)
                {
                    wsCn.Cell(rCn, 1).SetValue("TOTAL NC");
                    wsCn.Cell(rCn, 6).SetValue(validCns.Sum(x => x.total_amount_usd));
                    wsCn.Cell(rCn, 6).Style.NumberFormat.Format = "#,##0.00";
                    ExcelFlatExporter.StyleTotalRow(wsCn.Range(rCn, 1, rCn, headersCn.Length), "#F3E5F5");
                }
                ExcelFlatExporter.FormatTable(wsCn, headerRow: 1);
            }

            // 3. Hoja Pagos
            if (model.Payments != null && model.Payments.Count > 0)
            {
                var wsPay = wb.Worksheets.Add("Pagos y Abonos");
                string[] headersPay = new[] { "FECHA", "N NOTA", "METODO", "REFERENCIA", "MONTO ($)", "MONTO BS", "TASA (BS/$)", "OBSERVACIONES" };
                for (int i = 0; i < headersPay.Length; i++)
                {
                    wsPay.Cell(1, i + 1).Value = headersPay[i];
                    ExcelFlatExporter.StyleHeaderCell(wsPay.Cell(1, i + 1), "#2E7D32");
                }
                int rPay = 2;
                foreach (var p in model.Payments)
                {
                    int c = 1;
                    wsPay.Cell(rPay, c++).SetValue(p.payment_date.ToString("dd/MM/yyyy"));
                    wsPay.Cell(rPay, c++).SetValue(p.note_number);
                    wsPay.Cell(rPay, c++).SetValue(p.payment_type);
                    wsPay.Cell(rPay, c++).SetValue(p.reference_number);

                    var mCell = wsPay.Cell(rPay, c++);
                    mCell.SetValue(p.amount_usd);
                    mCell.Style.NumberFormat.Format = "#,##0.00";

                    var bsCell = wsPay.Cell(rPay, c++);
                    bsCell.SetValue(p.amount_bs);
                    bsCell.Style.NumberFormat.Format = "#,##0.00";

                    var rateCell = wsPay.Cell(rPay, c++);
                    if (p.exchange_rate.HasValue)
                    {
                        rateCell.SetValue(p.exchange_rate.Value);
                        rateCell.Style.NumberFormat.Format = "#,##0.00";
                    }
                    else
                    {
                        rateCell.SetValue("-");
                    }

                    wsPay.Cell(rPay, c++).SetValue(p.notes);
                    rPay++;
                }
                wsPay.Cell(rPay, 1).SetValue("TOTAL ABONADO");
                wsPay.Cell(rPay, 5).SetValue(model.Payments.Sum(x => x.amount_usd));
                wsPay.Cell(rPay, 5).Style.NumberFormat.Format = "#,##0.00";
                wsPay.Cell(rPay, 6).SetValue(model.Payments.Sum(x => x.amount_bs));
                wsPay.Cell(rPay, 6).Style.NumberFormat.Format = "#,##0.00";
                ExcelFlatExporter.StyleTotalRow(wsPay.Range(rPay, 1, rPay, headersPay.Length), "#E8F5E9");
                ExcelFlatExporter.FormatTable(wsPay, headerRow: 1);
            }

            // 4. Hoja Top Productos
            if (model.TopProducts != null && model.TopProducts.Count > 0)
            {
                var wsTop = wb.Worksheets.Add("Top Productos");
                string[] headersTop = new[] { "CODIGO", "PRODUCTO", "CANTIDAD", "ULTIMO PRECIO ($)", "TOTAL FACTURADO ($)", "ULTIMA COMPRA" };
                for (int i = 0; i < headersTop.Length; i++)
                {
                    wsTop.Cell(1, i + 1).Value = headersTop[i];
                    ExcelFlatExporter.StyleHeaderCell(wsTop.Cell(1, i + 1), "#E65100");
                }
                int rTop = 2;
                foreach (var prod in model.TopProducts)
                {
                    int c = 1;
                    wsTop.Cell(rTop, c++).SetValue(prod.ProductCode);
                    wsTop.Cell(rTop, c++).SetValue(prod.ProductName);

                    var qCell = wsTop.Cell(rTop, c++);
                    qCell.SetValue(prod.TotalQuantity);
                    qCell.Style.NumberFormat.Format = "#,##0";

                    var pCell = wsTop.Cell(rTop, c++);
                    pCell.SetValue(prod.LastPriceUsd);
                    pCell.Style.NumberFormat.Format = "#,##0.00";

                    var totCell = wsTop.Cell(rTop, c++);
                    totCell.SetValue(prod.TotalAmountUsd);
                    totCell.Style.NumberFormat.Format = "#,##0.00";

                    wsTop.Cell(rTop, c++).SetValue(prod.LastPurchaseDate != DateTime.MinValue ? prod.LastPurchaseDate.ToString("dd/MM/yyyy") : "-");
                    rTop++;
                }
                wsTop.Cell(rTop, 1).SetValue("TOTALES");
                wsTop.Cell(rTop, 3).SetValue(model.TopProducts.Sum(x => x.TotalQuantity));
                wsTop.Cell(rTop, 3).Style.NumberFormat.Format = "#,##0";
                wsTop.Cell(rTop, 5).SetValue(model.TopProducts.Sum(x => x.TotalAmountUsd));
                wsTop.Cell(rTop, 5).Style.NumberFormat.Format = "#,##0.00";
                ExcelFlatExporter.StyleTotalRow(wsTop.Range(rTop, 1, rTop, headersTop.Length), "#FFF3E0");
                ExcelFlatExporter.FormatTable(wsTop, headerRow: 1);
            }

            string defaultName = Path.GetFileNameWithoutExtension(CustomerHistoryPdfGenerator.get_default_file_name(model)) + ".xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar historial de cliente en Excel");
        }
    }
}
