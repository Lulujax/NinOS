using System;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using NinOS.Domain.ViewModels;

namespace NinOS.UI.Common
{
    public static class CommissionExcelExporter
    {
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        public static void export(commission_receipt_dto receipt)
        {
            if (receipt == null || receipt.rows.Count == 0) return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Comprobante Comisión");

            // 1. ENCABEZADO INSTITUCIONAL ("de quién es", sin ningún símbolo $)
            // Fila 1: Título principal
            var titleRange = ws.Range(1, 1, 1, 9);
            titleRange.Merge();
            titleRange.Value = "COMPROBANTE DE PAGO DE COMISIÓN";
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 12;
            titleRange.Style.Font.FontColor = XLColor.White;
            titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 26;

            // Fila 2: Espacio
            ws.Row(2).Height = 5;

            // Fila 3: Bloque de metadatos (Vendedora, Referencia, Fecha)
            string seller = (receipt.seller_name ?? string.Empty).Trim().ToUpperInvariant();

            ws.Cell(3, 1).Value = "VENDEDORA:";
            ws.Cell(3, 1).Style.Font.Bold = true;
            ws.Cell(3, 1).Style.Font.FontSize = 8;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#555555");
            ws.Cell(3, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var sellerRange = ws.Range(3, 2, 3, 4);
            sellerRange.Merge();
            sellerRange.Value = seller;
            sellerRange.Style.Font.Bold = true;
            sellerRange.Style.Font.FontSize = 11;
            sellerRange.Style.Font.FontColor = XLColor.FromHtml("#1B3A2D");
            sellerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Cell(3, 5).Value = "REFERENCIA:";
            ws.Cell(3, 5).Style.Font.Bold = true;
            ws.Cell(3, 5).Style.Font.FontSize = 8;
            ws.Cell(3, 5).Style.Font.FontColor = XLColor.FromHtml("#555555");
            ws.Cell(3, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(3, 5).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Cell(3, 6).Value = string.IsNullOrWhiteSpace(receipt.reference_number) ? "-" : receipt.reference_number;
            ws.Cell(3, 6).Style.Font.Bold = true;
            ws.Cell(3, 6).Style.Font.FontSize = 10;
            ws.Cell(3, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(3, 6).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Cell(3, 8).Value = "FECHA:";
            ws.Cell(3, 8).Style.Font.Bold = true;
            ws.Cell(3, 8).Style.Font.FontSize = 8;
            ws.Cell(3, 8).Style.Font.FontColor = XLColor.FromHtml("#555555");
            ws.Cell(3, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(3, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Cell(3, 9).Value = receipt.payment_date.ToString("dd/MM/yyyy");
            ws.Cell(3, 9).Style.Font.Bold = true;
            ws.Cell(3, 9).Style.Font.FontSize = 10;
            ws.Cell(3, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(3, 9).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Range(3, 1, 3, 9).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ws.Range(3, 1, 3, 9).Style.Border.BottomBorderColor = XLColor.Black;
            ws.Row(3).Height = 20;

            // Fila 4: Espacio
            ws.Row(4).Height = 6;

            // 2. ENCABEZADOS DE LA TABLA (Titulos en 7, sin ningún signo $)
            int headerRow = 5;
            string[] headers = new[]
            {
                "NRO", "CLIENTE", "MONTO FACTURADO", "FECHA DE PAGO",
                "MONTO PAGADO", "PRONTO PAGO", "COMISION", "TOTAL COMISION", "PAGO COMISION"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                ExcelFlatExporter.StyleHeaderCell(cell, "#1B3A2D");
                cell.Style.Font.FontSize = 7;
            }
            ws.Row(headerRow).Height = 22;

            // 3. DETALLE DE NOTAS (Datos en 11)
            int rowIdx = headerRow + 1;
            bool alt = false;
            foreach (var r in receipt.rows)
            {
                var rowRange = ws.Range(rowIdx, 1, rowIdx, 9);
                if (alt)
                {
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAF9");
                }

                // Col 1: NRO (Centrado)
                var nroCell = ws.Cell(rowIdx, 1);
                nroCell.SetValue(r.note_number);
                nroCell.Style.Font.FontSize = 11;
                nroCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                nroCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 2: CLIENTE (Alineado a la izquierda, con WrapText)
                var cliCell = ws.Cell(rowIdx, 2);
                cliCell.SetValue(r.customer_name);
                cliCell.Style.Font.FontSize = 11;
                cliCell.Style.Alignment.WrapText = true;
                cliCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                cliCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 3: MONTO FACTURADO (Centrado, número sin $)
                var factCell = ws.Cell(rowIdx, 3);
                factCell.SetValue(r.invoiced_amount);
                factCell.Style.Font.FontSize = 11;
                factCell.Style.NumberFormat.Format = "#,##0.00";
                factCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                factCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 4: FECHA DE PAGO (Centrado, con Enter / WrapText para pagos parciales/abonos)
                var fpCell = ws.Cell(rowIdx, 4);
                string pDateText = !string.IsNullOrWhiteSpace(r.note_payments_text)
                    ? r.note_payments_text
                    : (r.payment_date?.ToString("dd/MM/yyyy") ?? "-");
                pDateText = pDateText.Replace("\r\n", "\n").Replace("\r", "\n").Replace("$", string.Empty);
                fpCell.SetValue(pDateText);
                fpCell.Style.Font.FontSize = 11;
                fpCell.Style.Alignment.WrapText = true;
                fpCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                fpCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 5: MONTO PAGADO (Centrado, número sin $)
                var pagCell = ws.Cell(rowIdx, 5);
                pagCell.SetValue(r.paid_amount);
                pagCell.Style.Font.FontSize = 11;
                pagCell.Style.NumberFormat.Format = "#,##0.00";
                pagCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                pagCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 6: PRONTO PAGO (Centrado)
                var ppCell = ws.Cell(rowIdx, 6);
                string ppText = r.early_payment_discount_pct > 0
                    ? $"SI ({r.early_payment_discount_pct:0.##}%)"
                    : (r.early_payment_discount > 0 ? "SI" : "NO");
                ppCell.SetValue(ppText);
                ppCell.Style.Font.FontSize = 11;
                ppCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ppCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 7: COMISION (Centrado, número sin $)
                var com10Cell = ws.Cell(rowIdx, 7);
                com10Cell.SetValue(r.commission_amount);
                com10Cell.Style.Font.FontSize = 11;
                com10Cell.Style.NumberFormat.Format = "#,##0.00";
                com10Cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                com10Cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 8: TOTAL COMISION (Centrado, número sin $)
                var totCell = ws.Cell(rowIdx, 8);
                totCell.SetValue(r.commission_amount);
                totCell.Style.Font.FontSize = 11;
                totCell.Style.NumberFormat.Format = "#,##0.00";
                totCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                totCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Col 9: PAGO COMISION (Centrado)
                var pagComCell = ws.Cell(rowIdx, 9);
                string commPaidDateText = !string.IsNullOrWhiteSpace(r.commission_paid_date_dmon)
                    ? r.commission_paid_date_dmon
                    : (r.commission_paid_date?.ToString("dd/MM/yyyy") ?? "-");
                pagComCell.SetValue(commPaidDateText);
                pagComCell.Style.Font.FontSize = 11;
                pagComCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                pagComCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Ajuste de altura dinámica según cantidad de líneas en FECHA DE PAGO o CLIENTE largo
                int pDateLines = Math.Max(1, pDateText.Split('\n').Length);
                string cName = r.customer_name ?? string.Empty;
                int nameLines = Math.Max(1, (int)Math.Ceiling((double)cName.Length / 19));
                int lineCount = Math.Max(pDateLines, nameLines);
                ws.Row(rowIdx).Height = Math.Max(20, lineCount * 14.5);

                rowIdx++;
                alt = !alt;
            }

            int lastDataRow = rowIdx - 1;

            // BORDES COMPLETOS EN NEGRO PARA TODA LA TABLA DE DETALLE
            var mainTableRange = ws.Range(headerRow, 1, lastDataRow, 9);
            mainTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            mainTableRange.Style.Border.InsideBorderColor = XLColor.Black;
            mainTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            mainTableRange.Style.Border.OutsideBorderColor = XLColor.Black;

            // 4. TABLA DE TOTALES Y LIQUIDACIÓN BANCARIA
            decimal sub_total = receipt.rows.Sum(x => x.commission_amount);

            rowIdx++; // Fila vacía de separación
            ws.Row(rowIdx).Height = 8;
            rowIdx++;

            // Fila: SUB TOTAL
            var subTotalLabel = ws.Range(rowIdx, 5, rowIdx, 7);
            subTotalLabel.Merge();
            subTotalLabel.Value = "SUB TOTAL";
            subTotalLabel.Style.Font.Bold = true;
            subTotalLabel.Style.Font.FontSize = 7;
            subTotalLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            subTotalLabel.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var subTotalVal = ws.Cell(rowIdx, 8);
            subTotalVal.SetValue(sub_total);
            subTotalVal.Style.Font.Bold = true;
            subTotalVal.Style.Font.FontSize = 11;
            subTotalVal.Style.NumberFormat.Format = "#,##0.00";
            subTotalVal.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            subTotalVal.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var subTotalRange = ws.Range(rowIdx, 5, rowIdx, 8);
            subTotalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            subTotalRange.Style.Border.OutsideBorderColor = XLColor.Black;
            subTotalRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            subTotalRange.Style.Border.InsideBorderColor = XLColor.Black;
            ws.Row(rowIdx).Height = 20;
            rowIdx++;

            // Fila: COMISION PAGADA
            var comPagLabel = ws.Range(rowIdx, 5, rowIdx, 7);
            comPagLabel.Merge();
            comPagLabel.Value = "COMISION PAGADA";
            comPagLabel.Style.Font.Bold = true;
            comPagLabel.Style.Font.FontSize = 7;
            comPagLabel.Style.Font.FontColor = XLColor.FromHtml("#1B3A2D");
            comPagLabel.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
            comPagLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            comPagLabel.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var comPagVal = ws.Cell(rowIdx, 8);
            comPagVal.SetValue(sub_total);
            comPagVal.Style.Font.Bold = true;
            comPagVal.Style.Font.FontSize = 11;
            comPagVal.Style.Font.FontColor = XLColor.FromHtml("#1B3A2D");
            comPagVal.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
            comPagVal.Style.NumberFormat.Format = "#,##0.00";
            comPagVal.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            comPagVal.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var comPagRange = ws.Range(rowIdx, 5, rowIdx, 8);
            comPagRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            comPagRange.Style.Border.OutsideBorderColor = XLColor.Black;
            comPagRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            comPagRange.Style.Border.InsideBorderColor = XLColor.Black;
            ws.Row(rowIdx).Height = 22;
            rowIdx++;

            // Fila de separación
            ws.Row(rowIdx).Height = 5;
            rowIdx++;

            // TABLA BANCARIA
            // Columnas: E: BANCO | F: NRO REF | G: MONTO BS | H: MONTO $ | I: FECHA
            int bankHeaderRow = rowIdx;
            string[] bankHeaders = new[] { "BANCO", "NRO REF", "MONTO BS", "MONTO $", "FECHA" };
            for (int k = 0; k < bankHeaders.Length; k++)
            {
                var bCell = ws.Cell(bankHeaderRow, 5 + k);
                bCell.Value = bankHeaders[k];
                ExcelFlatExporter.StyleHeaderCell(bCell, "#1B3A2D");
                bCell.Style.Font.FontSize = 7;
            }
            ws.Row(bankHeaderRow).Height = 20;
            rowIdx++;

            // Fila de datos bancarios (en 11)
            int bankDataRow = rowIdx;
            var bNameCell = ws.Cell(bankDataRow, 5);
            bNameCell.SetValue(string.IsNullOrWhiteSpace(receipt.bank_name) ? "-" : receipt.bank_name);
            bNameCell.Style.Font.FontSize = 11;
            bNameCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            bNameCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var bRefCell = ws.Cell(bankDataRow, 6);
            bRefCell.SetValue(string.IsNullOrWhiteSpace(receipt.reference_number) ? "-" : receipt.reference_number);
            bRefCell.Style.Font.FontSize = 11;
            bRefCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            bRefCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var bsCell = ws.Cell(bankDataRow, 7);
            bsCell.SetValue(receipt.total_bs);
            bsCell.Style.Font.FontSize = 11;
            bsCell.Style.NumberFormat.Format = "#,##0.00";
            bsCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            bsCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var usdCell = ws.Cell(bankDataRow, 8);
            usdCell.SetValue(receipt.total_usd);
            usdCell.Style.Font.FontSize = 11;
            usdCell.Style.NumberFormat.Format = "#,##0.00";
            usdCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            usdCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            string payDateDMon = receipt.payment_date.ToString("dd-MMM", Ve).Replace(".", "").ToLowerInvariant();
            var bDateCell = ws.Cell(bankDataRow, 9);
            bDateCell.SetValue(payDateDMon);
            bDateCell.Style.Font.FontSize = 11;
            bDateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            bDateCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Row(bankDataRow).Height = 20;
            rowIdx++;

            // Fila: TOTAL COMISION (BANCO y NRO REF combinados) | MONTO BS | MONTO $
            int bankTotalRow = rowIdx;
            var totComBankLabel = ws.Range(bankTotalRow, 5, bankTotalRow, 6);
            totComBankLabel.Merge();
            totComBankLabel.Value = "TOTAL COMISION";
            totComBankLabel.Style.Font.Bold = true;
            totComBankLabel.Style.Font.FontSize = 7;
            totComBankLabel.Style.Font.FontColor = XLColor.White;
            totComBankLabel.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A2D");
            totComBankLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            totComBankLabel.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var totBsCell = ws.Cell(bankTotalRow, 7);
            totBsCell.SetValue(receipt.total_bs);
            totBsCell.Style.Font.Bold = true;
            totBsCell.Style.Font.FontSize = 11;
            totBsCell.Style.NumberFormat.Format = "#,##0.00";
            totBsCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            totBsCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            totBsCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");

            var totUsdCell = ws.Cell(bankTotalRow, 8);
            totUsdCell.SetValue(receipt.total_usd);
            totUsdCell.Style.Font.Bold = true;
            totUsdCell.Style.Font.FontSize = 11;
            totUsdCell.Style.NumberFormat.Format = "#,##0.00";
            totUsdCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            totUsdCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            totUsdCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");

            var emptyDateCell = ws.Cell(bankTotalRow, 9);
            emptyDateCell.Value = "";
            emptyDateCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
            ws.Row(bankTotalRow).Height = 22;

            // BORDES COMPLETOS EN NEGRO PARA LA TABLA BANCARIA
            var bankTableRange = ws.Range(bankHeaderRow, 5, bankTotalRow, 9);
            bankTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            bankTableRange.Style.Border.OutsideBorderColor = XLColor.Black;
            bankTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            bankTableRange.Style.Border.InsideBorderColor = XLColor.Black;

            // 5. ANCHO COMPACTO DE COLUMNAS PARA QUE QUEPA AL 100% EN PANTALLA 1280x720 (TOTAL = 102)
            ws.Column(1).Width = 7;   // NRO
            ws.Column(2).Width = 22;  // CLIENTE
            ws.Column(3).Width = 11;  // MONTO FACTURADO
            ws.Column(4).Width = 13;  // FECHA DE PAGO
            ws.Column(5).Width = 11;  // MONTO PAGADO / BANCO
            ws.Column(6).Width = 8;   // PRONTO PAGO / NRO REF
            ws.Column(7).Width = 10;  // COMISION 10% / MONTO BS
            ws.Column(8).Width = 10;  // TOTAL COMISION / MONTO $
            ws.Column(9).Width = 10;  // PAGO COMISION / FECHA

            // Autofilter solo sobre la tabla de detalle
            ws.Range(headerRow, 1, lastDataRow, 9).SetAutoFilter();

            string defaultName = Path.GetFileNameWithoutExtension(CommissionPdfGenerator.get_default_file_name(receipt)) + ".xlsx";
            ExcelFlatExporter.SaveWithDialog(wb, defaultName, "Guardar comprobante de comisión en Excel");
        }
    }
}
