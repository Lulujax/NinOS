using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using NinOS.UI.Common.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Common
{
    public class CustomersReportPdfModel
    {
        public DateTime EmissionDate { get; set; } = DateTime.Now;
        public string Scope { get; set; } = "Todas las Zonas";
        public string? SearchFilter { get; set; }
        public List<CustomerRowDto> Customers { get; set; } = new();

        public int TotalCustomers => Customers.Count;
        public int TotalZones => Customers
            .Select(c => c.ZonaName)
            .Where(z => !string.IsNullOrWhiteSpace(z) && z != "-")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        public int WithRifCount => Customers.Count(c => !string.IsNullOrWhiteSpace(c.Rif));
        public int WithPhoneCount => Customers.Count(c => !string.IsNullOrWhiteSpace(c.PhoneNumber));
    }

    public static class CustomersPdfGenerator
    {
        private static readonly string PrimaryColor = "#1B3A2D";
        private static readonly string DarkText = "#1A1A1A";
        private static readonly string MutedText = "#555555";
        private static readonly string LightBorder = "#D0D7DE";
        private static readonly string ZebraBg = "#F8FAFC";
        private static readonly string CardBg = "#F1F5F9";
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        public static string get_default_file_name(CustomersReportPdfModel model)
        {
            string safeScope = SanitizeFileName(string.IsNullOrWhiteSpace(model.Scope) ? "GENERAL" : model.Scope);
            return $"LISTADO_CLIENTES_{safeScope}_{DateTime.Now:yyyyMMdd_HHmm}.pdf".ToUpperInvariant();
        }

        public static void generate(CustomersReportPdfModel model)
        {
            if (model == null) return;

            var saveDialog = new SaveFileDialog
            {
                Title = "Guardar listado de clientes en PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = get_default_file_name(model)
            };

            if (saveDialog.ShowDialog() != true) return;

            build_document(model).GeneratePdf(saveDialog.FileName);
        }

        public static IDocument build_document(CustomersReportPdfModel model)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var customers = model.Customers ?? new List<CustomerRowDto>();

            // Agrupar por zona para facilitar lectura si hay múltiples zonas
            bool isMultiZone = !model.Scope.StartsWith("Zona", StringComparison.OrdinalIgnoreCase) && model.TotalZones > 1;

            var groupedByZone = customers
                .GroupBy(c => string.IsNullOrWhiteSpace(c.ZonaName) || c.ZonaName == "-" ? "Sin Zona Asignada" : c.ZonaName.Trim())
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter.Landscape());
                    page.MarginLeft(1, Unit.Centimetre);
                    page.MarginTop(1, Unit.Centimetre);
                    page.MarginRight(1, Unit.Centimetre);
                    page.MarginBottom(1, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(7.5f).FontColor(DarkText));

                    // Encabezado
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text("LISTADO GENERAL DE CLIENTES").FontSize(13).Bold().FontColor(PrimaryColor);
                                string subtitle = $"ALCANCE: {model.Scope.ToUpperInvariant()}";
                                if (!string.IsNullOrWhiteSpace(model.SearchFilter))
                                {
                                    subtitle += $" | BÚSQUEDA: \"{model.SearchFilter.Trim()}\"";
                                }
                                c.Item().PaddingTop(1).Text(subtitle).FontSize(8).FontColor(MutedText);
                            });

                            row.RelativeItem(1).AlignRight().Column(c =>
                            {
                                c.Item().Text($"EMISIÓN: {model.EmissionDate:dd/MM/yyyy HH:mm}").FontSize(8).Bold().FontColor(MutedText);
                                c.Item().PaddingTop(1).Text($"TOTAL CLIENTES: {model.TotalCustomers:N0}").FontSize(8.5f).Bold().FontColor(PrimaryColor);
                            });
                        });

                        col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(PrimaryColor);
                    });

                    // Pie de página
                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                        col.Item().PaddingTop(2).Row(row =>
                        {
                            row.RelativeItem().Text("NinOS - Sistema Administrativo").FontSize(7).FontColor(MutedText);
                            row.RelativeItem().AlignCenter().Text("Listado de Clientes").FontSize(7).FontColor(MutedText);
                            row.RelativeItem().AlignRight().Text(t =>
                            {
                                t.Span("Página ").FontSize(7).FontColor(MutedText);
                                t.CurrentPageNumber().FontSize(7).FontColor(MutedText);
                                t.Span(" de ").FontSize(7).FontColor(MutedText);
                                t.TotalPages().FontSize(7).FontColor(MutedText);
                            });
                        });
                    });

                    // Contenido
                    page.Content().PaddingVertical(6).Column(col =>
                    {
                        // Tarjetas de Resumen KPI
                        BuildKpiSummary(col, model);

                        if (customers.Count == 0)
                        {
                            col.Item().PaddingTop(15).Border(1).BorderColor(LightBorder).Background(CardBg).Padding(12)
                                .AlignCenter().Text("No se encontraron clientes registrados con los criterios seleccionados.").Italic().FontColor(MutedText);
                            return;
                        }

                        if (isMultiZone)
                        {
                            foreach (var grp in groupedByZone)
                            {
                                BuildZoneSection(col, grp.Key, grp.ToList());
                            }
                        }
                        else
                        {
                            BuildTableSection(col, customers, showZoneColumn: false);
                        }
                    });
                });
            });
        }

        private static void BuildKpiSummary(ColumnDescriptor col, CustomersReportPdfModel model)
        {
            col.Item().PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("TOTAL CLIENTES").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text(model.TotalCustomers.ToString("N0", Ve)).FontSize(11).Bold().FontColor(PrimaryColor);
                    c.Item().AlignCenter().Text("registrados").FontSize(6.5f).FontColor(MutedText);
                });

                row.Spacing(4);

                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("ZONAS ATENDIDAS").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text(model.TotalZones.ToString("N0", Ve)).FontSize(11).Bold().FontColor(PrimaryColor);
                    c.Item().AlignCenter().Text("zonas activas").FontSize(6.5f).FontColor(MutedText);
                });

                row.Spacing(4);

                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("CON IDENTIFICACIÓN").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text(model.WithRifCount.ToString("N0", Ve)).FontSize(11).Bold().FontColor("#2E7D32");
                    c.Item().AlignCenter().Text("con RIF registrado").FontSize(6.5f).FontColor("#2E7D32");
                });

                row.Spacing(4);

                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("CON TELÉFONO").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text(model.WithPhoneCount.ToString("N0", Ve)).FontSize(11).Bold().FontColor("#1565C0");
                    c.Item().AlignCenter().Text("con contacto telefónico").FontSize(6.5f).FontColor("#1565C0");
                });
            });
        }

        private static void BuildZoneSection(ColumnDescriptor col, string zoneName, List<CustomerRowDto> items)
        {
            col.Item().PaddingTop(6).PaddingBottom(2).Row(r =>
            {
                r.RelativeItem().Border(1).BorderColor(PrimaryColor).Background(PrimaryColor).PaddingVertical(3).PaddingHorizontal(6).Row(row =>
                {
                    row.RelativeItem().Text($"ZONA: {zoneName.ToUpperInvariant()}").FontSize(8).Bold().FontColor(Colors.White);
                    row.ConstantItem(100).AlignRight().Text($"{items.Count} clientes").FontSize(7.5f).Bold().FontColor(Colors.White);
                });
            });

            BuildTableSection(col, items, showZoneColumn: false);
        }

        private static void BuildTableSection(ColumnDescriptor col, List<CustomerRowDto> items, bool showZoneColumn)
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.45f); // #
                    columns.RelativeColumn(0.85f); // Código
                    columns.RelativeColumn(2.6f);  // Razón Social
                    columns.RelativeColumn(1.2f);  // RIF
                    columns.RelativeColumn(1.7f);  // Contacto
                    columns.RelativeColumn(1.3f);  // Teléfono
                    columns.RelativeColumn(2.4f);  // Dir. Fiscal
                    columns.RelativeColumn(2.4f);  // Dir. Entrega
                    if (showZoneColumn)
                    {
                        columns.RelativeColumn(1.3f); // Zona
                    }
                    columns.RelativeColumn(1.1f);  // Vendedor
                });

                // Encabezado de la tabla
                table.Header(header =>
                {
                    void h(string text, bool left = false)
                    {
                        var c = header.Cell().Background(PrimaryColor).PaddingVertical(2.5f).PaddingHorizontal(2);
                        var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                        t.FontColor(Colors.White).Bold().FontSize(6.5f);
                    }

                    h("#");
                    h("CÓDIGO");
                    h("RAZÓN SOCIAL / CLIENTE", left: true);
                    h("RIF");
                    h("CONTACTO", left: true);
                    h("TELÉFONO");
                    h("DIRECCIÓN FISCAL", left: true);
                    h("DIRECCIÓN ENTREGA", left: true);
                    if (showZoneColumn)
                    {
                        h("ZONA");
                    }
                    h("VENDEDOR");
                });

                int index = 1;
                foreach (var cust in items)
                {
                    string bg = (index % 2 == 0) ? ZebraBg : "#FFFFFF";

                    void c(Action<IContainer> content)
                    {
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E0E0E0")
                            .PaddingVertical(2.5f).PaddingHorizontal(2).Element(content);
                    }

                    // #
                    c(cell => cell.AlignCenter().Text(index.ToString()).FontSize(6.5f).Bold());

                    // Código
                    c(cell => cell.AlignCenter().Text(cust.CustomerCode ?? "-").FontSize(6.5f).Bold().FontColor(PrimaryColor));

                    // Razón Social
                    c(cell => cell.Text(cust.BusinessName ?? "-").FontSize(6.8f).Bold());

                    // RIF
                    c(cell => cell.AlignCenter().Text(cust.Rif ?? "-").FontSize(6.5f));

                    // Contacto
                    c(cell => cell.Text(cust.ContactName ?? "-").FontSize(6.5f));

                    // Teléfono
                    c(cell => cell.AlignCenter().Text(cust.PhoneNumber ?? "-").FontSize(6.5f));

                    // Dirección Fiscal
                    c(cell => cell.Text(string.IsNullOrWhiteSpace(cust.FiscalAddress) ? "-" : cust.FiscalAddress).FontSize(6.2f).FontColor("#444444"));

                    // Dirección Entrega
                    c(cell => cell.Text(string.IsNullOrWhiteSpace(cust.EffectiveDeliveryAddress) ? "-" : cust.EffectiveDeliveryAddress).FontSize(6.2f).FontColor("#444444"));

                    // Zona (opcional si es tabla plana)
                    if (showZoneColumn)
                    {
                        c(cell => cell.AlignCenter().Text(cust.ZonaName ?? "-").FontSize(6.5f));
                    }

                    // Vendedor
                    c(cell => cell.AlignCenter().Text(cust.SellerName ?? "-").FontSize(6.5f));

                    index++;
                }
            });
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "GENERAL";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Trim().Replace(' ', '_');
        }
    }
}
