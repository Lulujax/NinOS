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
    public class CurrentInventoryPdfItem
    {
        public string ProductCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public decimal UnitPriceUsd { get; set; }
        public decimal TotalValueUsd => StockQuantity > 0 ? StockQuantity * UnitPriceUsd : 0m;
        public bool IsNegativeStock => StockQuantity < 0;
    }

    public class CurrentInventoryPdfModel
    {
        public DateTime EmissionDate { get; set; } = DateTime.Now;
        public List<CurrentInventoryPdfItem> Items { get; set; } = new();

        public int TotalProducts => Items.Count;
        public int TotalUnits => Items.Sum(i => i.StockQuantity);
        public decimal TotalValuationUsd => Items.Sum(i => i.TotalValueUsd);
        public int ZeroOrNegativeStockCount => Items.Count(i => i.StockQuantity <= 0);
    }

    public static class CurrentInventoryPdfGenerator
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

        private static readonly Dictionary<string, (string color, string soft)> BrandColors =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "DEFILE",          ("#1B3A2D", "#E8F0E4") },
            { "BIOLINE",         ("#00695C", "#E0F2F1") },
            { "REMBRANDT",       ("#6A1B9A", "#F3E5F5") },
            { "ESTILISTA",       ("#E65100", "#FFF3E0") },
            { "DEPIL CLEAR",     ("#AD1457", "#FCE4EC") },
            { "KEDAM",           ("#4E342E", "#EFEBE9") },
            { "OLEOS",           ("#1565C0", "#E3F2FD") },
            { "AMAZONIA SECRET", ("#2E7D32", "#E8F5E9") },
            { "CUTIQUE",         ("#C62828", "#FFEBEE") },
        };

        private static (string color, string soft) GetBrandStyle(string brand)
        {
            if (BrandColors.TryGetValue(brand, out var style)) return style;
            return ("#37474F", "#ECEFF1");
        }

        public static void generate(CurrentInventoryPdfModel model)
        {
            if (model == null) return;

            var saveDialog = new SaveFileDialog
            {
                Title = "Guardar inventario actual en PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"INVENTARIO_ACTUAL_{DateTime.Now:yyyyMMdd_HHmm}.pdf".ToUpperInvariant()
            };

            if (saveDialog.ShowDialog() != true) return;

            generate_to_file(model, saveDialog.FileName);
        }

        public static void generate_to_file(CurrentInventoryPdfModel model, string outputPath)
        {
            if (model == null || string.IsNullOrWhiteSpace(outputPath)) return;

            build_document(model).GeneratePdf(outputPath);
        }

        public static IDocument build_document(CurrentInventoryPdfModel model)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var items = model.Items ?? new List<CurrentInventoryPdfItem>();

            var groupedByBrand = items
                .GroupBy(i => string.IsNullOrWhiteSpace(i.Category) ? "OTROS" : i.Category.Trim().ToUpperInvariant())
                .OrderBy(g => g.Key)
                .ToList();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginLeft(1, Unit.Centimetre);
                    page.MarginTop(1, Unit.Centimetre);
                    page.MarginRight(1, Unit.Centimetre);
                    page.MarginBottom(1, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(7.5f).FontColor(DarkText));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text("REPORTE DE INVENTARIO ACTUAL").FontSize(13).Bold().FontColor(PrimaryColor);
                                c.Item().PaddingTop(1).Text("Existencias físicas disponibles y valorización en almacén").FontSize(8.5f).FontColor(MutedText);
                            });

                            row.RelativeItem(1).AlignRight().Column(c =>
                            {
                                c.Item().Text($"EMISIÓN: {model.EmissionDate:dd/MM/yyyy HH:mm}").FontSize(8).Bold().FontColor(MutedText);
                                c.Item().PaddingTop(1).Text($"TOTAL PRODUCTOS: {model.TotalProducts}").FontSize(8).Bold().FontColor(PrimaryColor);
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
                        BuildKpiSummary(col, model);

                        if (groupedByBrand.Count == 0)
                        {
                            col.Item().PaddingTop(12).Border(1).BorderColor(LightBorder).Background(CardBg).Padding(10)
                                .AlignCenter().Text("No hay productos registrados en el inventario actual.").Italic().FontColor(MutedText);
                            return;
                        }

                        foreach (var group in groupedByBrand)
                        {
                            BuildBrandSection(col, group.Key, group.ToList());
                        }

                        BuildGrandTotalSection(col, model);
                    });
                });
            });
        }

        private static void BuildKpiSummary(ColumnDescriptor col, CurrentInventoryPdfModel model)
        {
            col.Item().PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("PRODUCTOS").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text(model.TotalProducts.ToString("N0", Ve)).FontSize(11).Bold().FontColor(PrimaryColor);
                    c.Item().AlignCenter().Text("registrados").FontSize(6.5f).FontColor(MutedText);
                });

                row.Spacing(4);

                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("EXISTENCIAS FÍSICAS").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text(model.TotalUnits.ToString("N0", Ve)).FontSize(11).Bold().FontColor(GreenColor);
                    c.Item().AlignCenter().Text("unidades en stock").FontSize(6.5f).FontColor(GreenColor);
                });

                row.Spacing(4);

                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("VALOR INVENTARIO").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text($"$ {model.TotalValuationUsd.ToString("N2", Ve)}").FontSize(11).Bold().FontColor(PrimaryColor);
                    c.Item().AlignCenter().Text("USD").FontSize(6.5f).FontColor(MutedText);
                });
            });
        }

        private static void BuildBrandSection(ColumnDescriptor col, string brandName, List<CurrentInventoryPdfItem> items)
        {
            var style = GetBrandStyle(brandName);
            int subtotalUnits = items.Sum(i => i.StockQuantity);

            col.Item().PaddingTop(4).PaddingBottom(2).Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(style.color).Background(style.soft).Padding(4).Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("MARCA / LÍNEA: ").Bold().FontSize(8.5f).FontColor(style.color);
                        t.Span(brandName).Bold().FontSize(9f).FontColor(style.color);
                    });

                    r.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span($"{items.Count} producto(s) | ").FontSize(7.5f).FontColor(style.color);
                        t.Span($"{subtotalUnits:N0} unid. en stock").Bold().FontSize(7.5f).FontColor(style.color);
                    });
                });
            });

            col.Item().PaddingBottom(6).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(24);   // #
                    columns.RelativeColumn(1.2f); // Codigo
                    columns.RelativeColumn(3.8f); // Descripcion
                    columns.RelativeColumn(1.1f); // Stock
                    columns.RelativeColumn(1.1f); // Precio ($)
                });

                table.Header(header =>
                {
                    void h(string text, bool right = false, bool center = false)
                    {
                        var cell = header.Cell().Background(TableHeaderBg).PaddingVertical(2).PaddingHorizontal(2);
                        var t = right ? cell.AlignRight().Text(text) : (center ? cell.AlignCenter().Text(text) : cell.Text(text));
                        t.FontColor(Colors.White).Bold().FontSize(6.8f);
                    }

                    h("#", center: true);
                    h("CÓDIGO");
                    h("DESCRIPCIÓN DEL PRODUCTO");
                    h("STOCK", right: true);
                    h("PRECIO ($)", right: true);
                });

                int idx = 1;
                foreach (var item in items.OrderBy(i => i.ProductCode))
                {
                    string bg = (idx % 2 == 0) ? ZebraBg : "#FFFFFF";

                    void cellText(string text, bool right = false, bool center = false, string? customColor = null, bool bold = false)
                    {
                        var cell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(LightBorder)
                            .PaddingVertical(1.8f).PaddingHorizontal(2);
                        var t = right ? cell.AlignRight().Text(text) : (center ? cell.AlignCenter().Text(text) : cell.Text(text));
                        t.FontSize(6.8f);
                        if (bold) t.Bold();
                        t.FontColor(customColor ?? DarkText);
                    }

                    string qtyColor = item.IsNegativeStock ? RedColor : (item.StockQuantity == 0 ? OrangeColor : DarkText);

                    cellText(idx.ToString(), center: true);
                    cellText(item.ProductCode, bold: true);
                    cellText(item.Name);
                    cellText(item.StockQuantity.ToString("N0", Ve), right: true, customColor: qtyColor, bold: true);
                    cellText(item.UnitPriceUsd.ToString("N2", Ve), right: true);

                    idx++;
                }
            });
        }

        private static void BuildGrandTotalSection(ColumnDescriptor col, CurrentInventoryPdfModel model)
        {
            col.Item().PaddingTop(4).Border(1.5f).BorderColor(PrimaryColor).Background(CardBg).Padding(6).Row(row =>
            {
                row.RelativeItem(2).AlignMiddle().Text("TOTAL GENERAL DE INVENTARIO").FontSize(9.5f).Bold().FontColor(PrimaryColor);

                row.RelativeItem(1).AlignRight().Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("TOTAL UNIDADES: ").Bold().FontSize(8).FontColor(DarkText);
                        t.Span($"{model.TotalUnits.ToString("N0", Ve)} unid.").Bold().FontSize(8.5f).FontColor(GreenColor);
                    });
                    c.Item().PaddingTop(2).Text(t =>
                    {
                        t.Span("VALORIZACIÓN TOTAL: ").Bold().FontSize(8).FontColor(DarkText);
                        t.Span($"$ {model.TotalValuationUsd.ToString("N2", Ve)} USD").Bold().FontSize(9f).FontColor(PrimaryColor);
                    });
                });
            });
        }
    }
}

