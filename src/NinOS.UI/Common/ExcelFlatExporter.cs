using System;
using System.IO;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace NinOS.UI.Common
{
    public static class ExcelFlatExporter
    {
        public static readonly string DarkGreenHeader = "#1B3A2D";
        public static readonly string DarkBlueHeader = "#1565C0";
        public static readonly string AccentSoftBg = "#F0F4EC";
        public static readonly string BlueSoftBg = "#E3F2FD";

        public static void StyleHeaderCell(IXLCell cell, string hexBg = "#1B3A2D")
        {
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Font.FontSize = 10;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(hexBg);
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.Black;
        }

        public static void StyleTotalRow(IXLRange range, string hexBg = "#E3F2FD")
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml(hexBg);
            range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            range.Style.Border.TopBorderColor = XLColor.Black;
            range.Style.Border.BottomBorder = XLBorderStyleValues.Double;
            range.Style.Border.BottomBorderColor = XLColor.Black;
        }

        public static void FormatTable(IXLWorksheet worksheet, int headerRow = 1, bool enableFilter = true)
        {
            if (enableFilter)
            {
                var range = worksheet.RangeUsed();
                if (range != null && range.RowCount() >= 1)
                {
                    worksheet.Range(headerRow, 1, range.LastRow().RowNumber(), range.LastColumn().ColumnNumber())
                             .SetAutoFilter();
                }
            }

            // Sin inmovilizar filas (NO freeze)
            worksheet.Columns().AdjustToContents(10, 50);
        }

        public static void SaveWithDialog(XLWorkbook workbook, string defaultFileName, string title = "Guardar reporte Excel")
        {
            var saveDialog = new SaveFileDialog
            {
                Title = title,
                Filter = "Libro de Excel (*.xlsx)|*.xlsx",
                FileName = defaultFileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ? defaultFileName : defaultFileName + ".xlsx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                workbook.SaveAs(saveDialog.FileName);
                AppDialog.Show("Archivo Excel exportado exitosamente.", "Éxito");
            }
        }
    }
}
