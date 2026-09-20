using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Common
{
    public class price_list_item
    {
        public string product_code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string brand { get; set; } = string.Empty;
        public decimal unit_price_usd { get; set; }
    }

    public static class PriceListPdfGenerator
    {
        private static readonly string LightBorder = "#B0B0B0";
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        private static readonly Dictionary<string, (string color, string soft)> BrandColors = new()
        {
            { "DEFILE",       ("#1B3A2D", "#E8F0E4") },
            { "BIOLINE",      ("#00695C", "#E0F2F1") },
            { "REMBRANDT",    ("#6A1B9A", "#F3E5F5") },
            { "ESTILISTA",    ("#E65100", "#FFF3E0") },
            { "DEPIL CLEAR",  ("#AD1457", "#FCE4EC") },
            { "KEDAM",        ("#4E342E", "#EFEBE9") },
            { "OLEOS",        ("#1565C0", "#E3F2FD") },
            { "AMAZONIA SECRET", ("#2E7D32", "#E8F5E9") },
            { "CUTIQUE",      ("#C62828", "#FFEBEE") },
        };

        private static (string color, string soft) GetBrandStyle(string brand)
        {
            if (BrandColors.TryGetValue(brand, out var style)) return style;
            return ("#546E7A", "#ECEFF1"); // default grey
        }

        public static void generate(IEnumerable<price_list_item> items)
        {
            if (items == null) return;

            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar lista de precios",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"LISTA_DE_PRECIOS_{DateTime.UtcNow:yyyyMMdd}.pdf"
            };

            if (save_dialog.ShowDialog() != true) return;
            generate_to_file(items, save_dialog.FileName);
        }

        public static void generate_to_file(IEnumerable<price_list_item> items, string output_path)
        {
            if (items == null) return;

            QuestPDF.Settings.License = LicenseType.Community;

            var ordered = items
                .Where(i => !string.IsNullOrWhiteSpace(i.product_code))
                .OrderBy(i => i.brand)
                .ThenBy(i => i.product_code)
                .ToList();

            var grouped = ordered.GroupBy(i => string.IsNullOrWhiteSpace(i.brand) ? "Otros" : i.brand)
                .OrderBy(g => g.Key)
                .ToList();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginLeft(1, Unit.Centimetre);
                    page.MarginTop(1, Unit.Centimetre);
                    page.MarginRight(1, Unit.Centimetre);
                    page.MarginBottom(1, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(8));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("LISTA DE PRECIOS")
                            .FontSize(15).Bold().FontColor("#1B3A2D");
                        col.Item().PaddingTop(2).Text($"ACTUALIZADO: {DateTime.UtcNow:dd/MM/yyyy}")
                            .FontSize(9).FontColor("#666666");
                        col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor("#1B3A2D");
                    });

                    page.Content().PaddingVertical(6).Column(col =>
                    {
                        if (ordered.Count == 0)
                        {
                            col.Item().Text("No hay productos para listar.").FontSize(11).FontColor("#888888");
                            return;
                        }

                        // ---- INDICE DE MARCAS (top) ----
                        col.Item().PaddingBottom(6).Row(idx =>
                        {
                            idx.AutoItem().PaddingRight(4).AlignMiddle().Text("MARCAS:").FontSize(7).Bold().FontColor("#333333");
                            foreach (var grp in grouped)
                            {
                                var (clr, _) = GetBrandStyle(grp.Key);
                                idx.AutoItem().PaddingRight(3).Row(b =>
                                {
                                    b.ConstantItem(8).Background(clr).Padding(2);
                                    b.AutoItem().PaddingLeft(2).AlignMiddle().Text(grp.Key).FontSize(6.5f).Bold().FontColor(clr);
                                });
                            }
                        });

                        // ---- SECCIONES POR MARCA ----
                        foreach (var grp in grouped)
                        {
                            var (clr, soft) = GetBrandStyle(grp.Key);
                            var brandItems = grp.ToList();

                            // Encabezado de la marca
                            col.Item().PaddingTop(4).PaddingBottom(2).Background(clr).Padding(4).Row(r =>
                            {
                                r.RelativeItem().Text(grp.Key.ToUpper()).FontSize(11).Bold().FontColor(Colors.White);
                                r.AutoItem().AlignMiddle().Text($"{brandItems.Count} producto(s)").FontSize(8).FontColor(Colors.White);
                            });

                            // Tabla de la marca
                            col.Item().PaddingBottom(6).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.1f);
                                    columns.RelativeColumn(4.0f);
                                    columns.RelativeColumn(1.2f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(clr).Padding(3).Text("CODIGO").FontColor(Colors.White).Bold().FontSize(8);
                                    header.Cell().Background(clr).Padding(3).Text("PRODUCTO").FontColor(Colors.White).Bold().FontSize(8);
                                    header.Cell().Background(clr).Padding(3).AlignRight().Text("COSTO ($)").FontColor(Colors.White).Bold().FontSize(8);
                                });

                                bool alt = false;
                                foreach (var item in brandItems)
                                {
                                    string bg = alt ? soft : Colors.White;
                                    table.Cell().Background(bg).Padding(3).Text(item.product_code).FontSize(7.5f).Bold().FontColor("#333333");
                                    table.Cell().Background(bg).Padding(3).Text(item.name).FontSize(7.5f);
                                    table.Cell().Background(bg).Padding(3).AlignRight().Text(Money(item.unit_price_usd)).FontSize(7.5f).Bold();
                                    alt = !alt;
                                }
                            });
                        }
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                        col.Item().PaddingTop(3).Row(row =>
                        {
                            row.RelativeItem().Text($"Total de productos: {ordered.Count}").FontSize(7).FontColor("#888888");
                            row.RelativeItem().AlignRight().Text(t =>
                            {
                                t.Span("Pagina ").FontSize(7).FontColor("#888888");
                                t.CurrentPageNumber().FontSize(7).FontColor("#888888");
                            });
                        });
                    });
                });
            });

            doc.GeneratePdf(output_path);
        }

        private static string Money(decimal value)
        {
            return value.ToString("#,##0.00", Ve);
        }
    }
}