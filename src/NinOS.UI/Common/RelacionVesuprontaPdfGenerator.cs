using System;
using Microsoft.Win32;
using NinOS.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Common
{
    public static class RelacionVesuprontaPdfGenerator
    {
        private const string Accent = "#4A148C";
        private const string SoftAccent = "#F3E5F5";
        private const string LightBorder = "#B0BEC5";
        private const string GreenColor = "#2E7D32";
        private const string RedColor = "#C62828";

        public static string get_default_file_name(relacion_vesupronta_reporte reporte)
        {
            ArgumentNullException.ThrowIfNull(reporte);
            string periodo = reporte.periodo_texto.Replace(" ", "_").Replace("/", "-").ToUpperInvariant();
            return $"RELACION_VESUPRONTA_{periodo}_{DateTime.Now:yyyyMMdd}.pdf";
        }

        public static IDocument build_document(relacion_vesupronta_reporte reporte)
        {
            ArgumentNullException.ThrowIfNull(reporte);
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(8));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(reporte.titulo).FontSize(12).Bold().FontColor(Accent);
                        col.Item().PaddingTop(2).Text($"EMISION: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#555555");
                        col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(Accent);
                    });

                    page.Content().PaddingVertical(6).Column(col =>
                    {
                        if (reporte.filas.Count == 0)
                        {
                            col.Item().Text("No hay notas de Vesupronta para el período seleccionado.").FontSize(10).FontColor("#555555");
                        }
                        else
                        {
                            BuildTable(col, reporte);
                        }

                        col.Item().PaddingTop(12).Row(row =>
                        {
                            BuildTotalBox(row, reporte.etiqueta_total_ventas, $"{reporte.total_ventas:N2}", Accent);
                            row.ConstantItem(16);
                            BuildTotalBox(row, "TOTAL POR COBRAR", $"{reporte.total_por_cobrar:N2}", RedColor);
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                        col.Item().PaddingTop(2).Row(row =>
                        {
                            row.RelativeItem().Text("NinOS - Módulo Vesupronta").FontSize(7).FontColor("#555555");
                            row.RelativeItem().AlignCenter().Text($"Impreso: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor("#555555");
                            row.RelativeItem().AlignRight().Text(t =>
                            {
                                t.Span("Página ").FontSize(7).FontColor("#555555");
                                t.CurrentPageNumber().FontSize(7).FontColor("#555555");
                                t.Span(" de ").FontSize(7).FontColor("#555555");
                                t.TotalPages().FontSize(7).FontColor("#555555");
                            });
                        });
                    });
                });
            });
        }

        public static void generate(relacion_vesupronta_reporte reporte)
        {
            ArgumentNullException.ThrowIfNull(reporte);
            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar relación de Vesupronta en PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = get_default_file_name(reporte)
            };

            if (save_dialog.ShowDialog() != true) return;

            build_document(reporte).GeneratePdf(save_dialog.FileName);
        }

        private static void BuildTable(ColumnDescriptor col, relacion_vesupronta_reporte reporte)
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.0f);
                    columns.RelativeColumn(2.8f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(0.9f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(1.4f);
                });

                table.Header(header =>
                {
                    void HeaderCell(string text, bool right = false)
                    {
                        var cell = header.Cell().Background(Accent).PaddingVertical(3).PaddingHorizontal(3);
                        var t = right ? cell.AlignRight().Text(text) : cell.AlignCenter().Text(text);
                        t.FontColor(Colors.White).Bold().FontSize(7.5f);
                    }

                    HeaderCell("FECHA DE DESPACHO");
                    HeaderCell("NRO");
                    HeaderCell("CLIENTE");
                    HeaderCell("MONTO FACTURADO", right: true);
                    HeaderCell("DESCUENTO");
                    HeaderCell("FECHA DE PAGO");
                    HeaderCell("MONTO PAGADO", right: true);
                    HeaderCell("10% COMISION", right: true);
                    HeaderCell("SALDO ACTUALIZADO", right: true);
                    HeaderCell("FECHA COMISION CANCELADA");
                });

                bool alternate = false;
                foreach (var fila in reporte.filas)
                {
                    string background = alternate ? SoftAccent : Colors.White;
                    alternate = !alternate;

                    void BodyCell(string text, bool right = false, string? color = null, bool bold = false)
                    {
                        var cell = table.Cell().Background(background).BorderBottom(0.4f).BorderColor(LightBorder)
                            .PaddingVertical(2.5f).PaddingHorizontal(3);
                        var t = right ? cell.AlignRight().Text(text) : cell.AlignCenter().Text(text);
                        t.FontSize(7.5f);
                        if (bold) t.Bold();
                        if (color != null) t.FontColor(color);
                    }

                    BodyCell(fila.despacho_texto);
                    BodyCell(fila.nro, bold: true);
                    BodyCell(fila.cliente);
                    BodyCell(fila.monto_facturado_texto, right: true);
                    BodyCell(fila.descuento_texto, color: fila.descuento_aplicado ? "#E65100" : null);
                    BodyCell(fila.fecha_pago_texto);
                    BodyCell(fila.monto_pagado_texto, right: true, bold: true);
                    BodyCell(fila.comision_texto, right: true);
                    BodyCell(fila.saldo_actualizado_texto, right: true, bold: true,
                        color: fila.saldo_actualizado > 0 ? RedColor : GreenColor);
                    BodyCell(fila.fecha_comision_texto);
                }

                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).Text("");
                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).Text("");
                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).AlignCenter().Text("TOTAL").Bold();
                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).Text("");
                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).Text("");
                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).Text("");
                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).AlignRight().Text($"{reporte.total_ventas:N2}").Bold();
                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).Text("");
                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).AlignRight().Text($"{reporte.total_por_cobrar:N2}").Bold().FontColor(RedColor);
                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).Text("");
            });
        }

        private static void BuildTotalBox(RowDescriptor row, string label, string amount, string color)
        {
            row.RelativeItem().Border(0.8f).BorderColor(color).Background(SoftAccent).Padding(8).Column(box =>
            {
                box.Item().Text(label).FontSize(9).Bold().FontColor(color);
                box.Item().PaddingTop(3).AlignRight().Text(amount).FontSize(13).Bold().FontColor(color);
            });
        }
    }
}
