using System;
using ClosedXML.Excel;
using NinOS.Domain;

namespace NinOS.UI.Common
{
    public static class RelacionVesuprontaExcelExporter
    {
        private const string MoneyFormat = "#,##0.00";
        private static readonly string[] Headers =
        {
            "FECHA DE DESPACHO",
            "NRO",
            "CLIENTE",
            "MONTO FACTURADO",
            "DESCUENTO",
            "FECHA DE PAGO",
            "MONTO PAGADO",
            "10% COMISION",
            "SALDO ACTUALIZADO",
            "FECHA COMISION CANCELADA"
        };

        public static void export(relacion_vesupronta_reporte reporte)
        {
            ArgumentNullException.ThrowIfNull(reporte);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Relacion Vesupronta");

            var title_range = worksheet.Range(1, 1, 1, Headers.Length);
            title_range.Merge();
            title_range.Value = reporte.titulo;
            title_range.Style.Font.Bold = true;
            title_range.Style.Font.FontSize = 13;
            title_range.Style.Alignment.WrapText = true;
            title_range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Row(1).Height = 36;

            const int headerRow = 3;
            for (int column = 0; column < Headers.Length; column++)
            {
                worksheet.Cell(headerRow, column + 1).Value = Headers[column];
            }
            var header_range = worksheet.Range(headerRow, 1, headerRow, Headers.Length);
            header_range.Style.Fill.BackgroundColor = XLColor.FromHtml("#4A148C");
            header_range.Style.Font.FontColor = XLColor.White;
            header_range.Style.Font.Bold = true;
            header_range.Style.Alignment.WrapText = true;

            int row = headerRow + 1;
            foreach (relacion_vesupronta_fila fila in reporte.filas)
            {
                worksheet.Cell(row, 1).Value = fila.despacho_texto;
                worksheet.Cell(row, 2).Value = fila.nro;
                worksheet.Cell(row, 3).Value = fila.cliente;
                worksheet.Cell(row, 4).Value = fila.monto_facturado;
                worksheet.Cell(row, 5).Value = fila.descuento_texto;
                worksheet.Cell(row, 6).Value = fila.fecha_pago_texto;
                worksheet.Cell(row, 7).Value = fila.monto_pagado;
                worksheet.Cell(row, 8).Value = fila.comision;
                worksheet.Cell(row, 9).Value = fila.saldo_actualizado;
                worksheet.Cell(row, 10).Value = fila.fecha_comision_texto;
                worksheet.Row(row).Height = Math.Max(30, Math.Ceiling(fila.cliente.Length / 26.0) * 15 + 6);
                row++;
            }

            int last_data_row = row - 1;
            if (last_data_row >= headerRow + 1)
            {
                worksheet.Range(headerRow + 1, 4, last_data_row, 4).Style.NumberFormat.Format = MoneyFormat;
                worksheet.Range(headerRow + 1, 7, last_data_row, 9).Style.NumberFormat.Format = MoneyFormat;
                worksheet.Range(headerRow + 1, 3, last_data_row, 3).Style.Alignment.WrapText = true;
            }

            int totals_row = row + 1;
            worksheet.Cell(totals_row, 3).Value = reporte.etiqueta_total_ventas;
            worksheet.Cell(totals_row, 3).Style.Font.Bold = true;
            worksheet.Cell(totals_row, 4).Value = reporte.total_ventas;
            worksheet.Cell(totals_row, 4).Style.NumberFormat.Format = MoneyFormat;
            worksheet.Cell(totals_row, 4).Style.Font.Bold = true;

            worksheet.Cell(totals_row, 7).Value = "TOTAL POR COBRAR";
            worksheet.Cell(totals_row, 7).Style.Font.Bold = true;
            worksheet.Cell(totals_row, 8).Value = reporte.total_por_cobrar;
            worksheet.Cell(totals_row, 8).Style.NumberFormat.Format = MoneyFormat;
            worksheet.Cell(totals_row, 8).Style.Font.Bold = true;

            int[] anchos_columnas = { 12, 12, 30, 13, 11, 12, 13, 12, 13, 14 };
            for (int column = 0; column < anchos_columnas.Length; column++)
            {
                worksheet.Column(column + 1).Width = anchos_columnas[column];
            }

            worksheet.Row(headerRow).Height = 48;
            header_range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            header_range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var body_range = worksheet.Range(headerRow + 1, 1, totals_row, Headers.Length);
            body_range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            body_range.Style.Alignment.WrapText = true;
            worksheet.Range(headerRow + 1, 1, last_data_row, Headers.Length).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(headerRow + 1, 3, last_data_row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            worksheet.Row(totals_row).Height = 30;

            ExcelFlatExporter.SaveWithDialog(workbook, get_default_file_name(reporte), "Guardar relación de Vesupronta en Excel");
        }

        private static string get_default_file_name(relacion_vesupronta_reporte reporte)
        {
            string periodo = reporte.periodo_texto.Replace(" ", "_").ToUpperInvariant();
            return $"RELACION_VESUPRONTA_{periodo}_{DateTime.Now:yyyyMMdd}";
        }
    }
}
