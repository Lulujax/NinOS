using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Common
{
    public class ProductMovementGroup
    {
        public int IdProduct { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public decimal UnitPriceUsd { get; set; }
        public List<InventoryHistoryPdfRow> Rows { get; set; } = new();

        public int TotalEntradas => Rows.Where(r => r.EsEntrada).Sum(r => r.Unidades);
        public int TotalSalidas => Rows.Where(r => !r.EsEntrada).Sum(r => r.Unidades);
    }

    public class AllMovementsPdfModel
    {
        public DateTime EmissionDate { get; set; } = DateTime.Now;
        public List<ProductMovementGroup> ProductGroups { get; set; } = new();

        public int TotalProducts => ProductGroups.Count;
        public int TotalMovements => ProductGroups.Sum(g => g.Rows.Count);
        public int TotalEntradas => ProductGroups.Sum(g => g.TotalEntradas);
        public int TotalSalidas => ProductGroups.Sum(g => g.TotalSalidas);
    }

    public static class AllMovementsPdfGenerator
    {
        private static readonly string PrimaryColor = "#0D62B5";
        private static readonly string DarkText = "#1A1A1A";
        private static readonly string MutedText = "#555555";
        private static readonly string LightBorder = "#D0D7DE";
        private static readonly string TableHeaderBg = "#0D62B5";
        private static readonly string ZebraBg = "#F8FAFC";
        private static readonly string CardBg = "#F1F5F9";
        private static readonly string GreenColor = "#2E7D32";
        private static readonly string RedColor = "#C62828";
        private static readonly string OrangeColor = "#E65100";
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        public static void generate(AllMovementsPdfModel model)
        {
            if (model == null) return;

            var saveDialog = new SaveFileDialog
            {
                Title = "Guardar todos los movimientos de inventario en PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"MOVIMIENTOS_{DateTime.Now:yyyyMMdd_HHmm}.pdf".ToUpperInvariant()
            };

            if (saveDialog.ShowDialog() != true) return;

            generate_to_file(model, saveDialog.FileName);
        }

        public static void generate_to_file(AllMovementsPdfModel model, string outputPath)
        {
            if (model == null || string.IsNullOrWhiteSpace(outputPath)) return;

            build_document(model).GeneratePdf(outputPath);
        }

        public static IDocument build_document(AllMovementsPdfModel model)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var groups = model.ProductGroups ?? new List<ProductMovementGroup>();

            // Calculamos saldos individuales para cada grupo de producto
            foreach (var group in groups)
            {
                calcular_saldos(group);
            }

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginLeft(0.8f, Unit.Centimetre);
                    page.MarginTop(1, Unit.Centimetre);
                    page.MarginRight(0.8f, Unit.Centimetre);
                    page.MarginBottom(1, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(7.5f).FontColor(DarkText));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text("MOVIMIENTOS").FontSize(13).Bold().FontColor(PrimaryColor);
                                c.Item().PaddingTop(1).Text("Auditoría completa de entradas, salidas y ajustes de inventario").FontSize(8.5f).FontColor(MutedText);
                            });

                            row.RelativeItem(1).AlignRight().Column(c =>
                            {
                                c.Item().Text($"EMISIÓN: {model.EmissionDate:dd/MM/yyyy HH:mm}").FontSize(8).Bold().FontColor(MutedText);
                                c.Item().PaddingTop(1).Text($"TOTAL MOVIMIENTOS: {model.TotalMovements:N0}").FontSize(8).Bold().FontColor(PrimaryColor);
                            });
                        });

                        col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(PrimaryColor);
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text("NinOS - Sistema Administrativo").FontSize(7).FontColor(MutedText);
                        row.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span("Página ").FontSize(7).FontColor(MutedText);
                            t.CurrentPageNumber().FontSize(7).FontColor(MutedText);
                            t.Span(" de ").FontSize(7).FontColor(MutedText);
                            t.TotalPages().FontSize(7).FontColor(MutedText);
                        });
                    });

                    page.Content().PaddingVertical(6).Column(col =>
                    {
                        BuildGlobalKpi(col, model);

                        if (groups.Count == 0)
                        {
                            col.Item().PaddingTop(12).Border(1).BorderColor(LightBorder).Background(CardBg).Padding(10)
                                .AlignCenter().Text("No hay movimientos de inventario registrados.").Italic().FontColor(MutedText);
                            return;
                        }

                        foreach (var group in groups)
                        {
                            BuildProductMovementSection(col, group);
                        }
                    });
                });
            });
        }

        private static void calcular_saldos(ProductMovementGroup group)
        {
            if (group.Rows == null) return;

            int saldo = group.CurrentStock;
            foreach (var row in group.Rows)
            {
                row.SaldoUnidades = saldo;
                saldo -= row.SignedUnits;
            }
        }

        private static void BuildGlobalKpi(ColumnDescriptor col, AllMovementsPdfModel model)
        {
            col.Item().PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("PRODUCTOS").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text(model.TotalProducts.ToString("N0", Ve)).FontSize(11).Bold().FontColor(PrimaryColor);
                    c.Item().AlignCenter().Text("con movimientos").FontSize(6.5f).FontColor(MutedText);
                });

                row.Spacing(4);

                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("REGISTROS TOTALES").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text(model.TotalMovements.ToString("N0", Ve)).FontSize(11).Bold().FontColor(PrimaryColor);
                    c.Item().AlignCenter().Text("movimientos en total").FontSize(6.5f).FontColor(MutedText);
                });

                row.Spacing(4);

                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("TOTAL ENTRADAS").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text($"+{model.TotalEntradas.ToString("N0", Ve)}").FontSize(11).Bold().FontColor(GreenColor);
                    c.Item().AlignCenter().Text("unidades").FontSize(6.5f).FontColor(GreenColor);
                });

                row.Spacing(4);

                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("TOTAL SALIDAS").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text($"-{model.TotalSalidas.ToString("N0", Ve)}").FontSize(11).Bold().FontColor(RedColor);
                    c.Item().AlignCenter().Text("unidades").FontSize(6.5f).FontColor(RedColor);
                });
            });
        }

        private static void BuildProductMovementSection(ColumnDescriptor col, ProductMovementGroup group)
        {
            // Encabezado del producto
            col.Item().PaddingTop(6).PaddingBottom(2).Border(1).BorderColor(LightBorder).Background(CardBg).Padding(5).Row(row =>
            {
                row.RelativeItem(2).Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span($"[{group.ProductCode}] ").Bold().FontSize(8.5f).FontColor(PrimaryColor);
                        t.Span(group.ProductName).Bold().FontSize(8.5f).FontColor(DarkText);
                    });
                    c.Item().PaddingTop(1).Text(t =>
                    {
                        t.Span("LÍNEA: ").Bold().FontSize(7.5f).FontColor(MutedText);
                        t.Span(string.IsNullOrWhiteSpace(group.Category) ? "OTROS" : group.Category).FontSize(7.5f);
                        t.Span("   |   PRECIO: ").Bold().FontSize(7.5f).FontColor(MutedText);
                        t.Span($"$ {group.UnitPriceUsd.ToString("N2", Ve)}").Bold().FontSize(7.5f).FontColor(GreenColor);
                    });
                });

                row.RelativeItem(1).AlignRight().Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("EXISTENCIA HOY: ").Bold().FontSize(8).FontColor(MutedText);
                        t.Span(group.CurrentStock.ToString("N0", Ve)).Bold().FontSize(9f).FontColor(OrangeColor);
                        t.Span(" unid.").FontSize(7.5f);
                    });
                    c.Item().PaddingTop(1).Text($"{group.Rows.Count} movimiento(s)").FontSize(7.5f).FontColor(MutedText);
                });
            });

            if (group.Rows.Count == 0)
            {
                col.Item().PaddingBottom(4).Border(0.5f).BorderColor(LightBorder).Padding(4)
                    .Text("Sin movimientos registrados para este producto.").Italic().FontColor(MutedText);
                return;
            }

            // Tabla de movimientos del producto
            col.Item().PaddingBottom(6).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.05f); // Nota
                    columns.RelativeColumn(0.95f); // Fecha
                    columns.RelativeColumn(1.20f); // Documento
                    columns.RelativeColumn(1.35f); // Motivo
                    columns.RelativeColumn(2.15f); // Cliente
                    columns.RelativeColumn(1.15f); // Vendedor
                    columns.RelativeColumn(1.05f); // Movimiento
                    columns.RelativeColumn(0.80f); // Unidades
                    columns.RelativeColumn(0.80f); // Stock
                    columns.RelativeColumn(0.90f); // Precio ($)
                    columns.RelativeColumn(1.10f); // Estado
                });

                table.Header(header =>
                {
                    void h(string text, bool right = false, bool center = false)
                    {
                        var cell = header.Cell().Background(TableHeaderBg).PaddingVertical(2).PaddingHorizontal(2);
                        var t = right ? cell.AlignRight().Text(text) : (center ? cell.AlignCenter().Text(text) : cell.Text(text));
                        t.FontColor(Colors.White).Bold().FontSize(6.5f);
                    }

                    h("NOTA");
                    h("FECHA");
                    h("DOCUMENTO");
                    h("MOTIVO");
                    h("CLIENTE");
                    h("VENDEDOR");
                    h("MOVIMIENTO", center: true);
                    h("UNIDADES", right: true);
                    h("SUBTOTAL\nSTOCK", right: true);
                    h("PRECIO ($)", right: true);
                    h("ESTADO");
                });

                int idx = 0;
                foreach (var r in group.Rows)
                {
                    string bg = (idx++ % 2 == 1) ? ZebraBg : "#FFFFFF";
                    string dirColor = r.EsEntrada ? GreenColor : RedColor;

                    void cellText(string text, bool right = false, bool center = false, string? customColor = null, bool bold = false)
                    {
                        var cell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(LightBorder)
                            .PaddingVertical(1.8f).PaddingHorizontal(2);
                        var t = right ? cell.AlignRight().Text(text) : (center ? cell.AlignCenter().Text(text) : cell.Text(text));
                        t.FontSize(6.8f);
                        if (bold) t.Bold();
                        t.FontColor(customColor ?? DarkText);
                    }

                    cellText(string.IsNullOrWhiteSpace(r.NoteNumber) ? "-" : r.NoteNumber, bold: true);
                    cellText(r.FechaLocal.ToString("dd/MM/yyyy"));
                    cellText(string.IsNullOrWhiteSpace(r.Documento) ? "-" : r.Documento);
                    cellText(string.IsNullOrWhiteSpace(r.Motivo) ? "-" : r.Motivo);
                    cellText(string.IsNullOrWhiteSpace(r.Cliente) ? "-" : r.Cliente);
                    cellText(string.IsNullOrWhiteSpace(r.Vendedor) ? "-" : r.Vendedor);
                    cellText(r.EsEntrada ? "ENTRADA" : "SALIDA", center: true, customColor: dirColor, bold: true);
                    cellText(r.SignedUnits > 0 ? "+" + r.SignedUnits : r.SignedUnits.ToString(), right: true, customColor: dirColor, bold: true);
                    cellText(r.SaldoUnidades.ToString("N0", Ve), right: true, bold: true);
                    cellText(r.PrecioUsd.ToString("N2", Ve), right: true);
                    cellText(string.IsNullOrWhiteSpace(r.Estado) ? "-" : r.Estado);
                }

                // Totales del producto
                table.Cell().ColumnSpan(6).Background(CardBg).PaddingVertical(2).PaddingHorizontal(3)
                    .AlignRight().Text("TOTAL ENTRADAS / SALIDAS:").Bold().FontSize(6.8f).FontColor(DarkText);

                table.Cell().ColumnSpan(2).Background(CardBg).PaddingVertical(2).PaddingHorizontal(2)
                    .AlignCenter().Text($"E: +{group.TotalEntradas} | S: -{group.TotalSalidas}").Bold().FontSize(6.8f).FontColor(PrimaryColor);

                table.Cell().ColumnSpan(2).Background(CardBg).PaddingVertical(2).PaddingHorizontal(2)
                    .AlignRight().Text($"Saldo: {group.CurrentStock}").Bold().FontSize(6.8f).FontColor(OrangeColor);

                table.Cell().ColumnSpan(1).Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).Text("");
            });
        }
    }
}

