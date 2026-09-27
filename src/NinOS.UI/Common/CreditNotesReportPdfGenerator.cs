using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using NinOS.Domain.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Common
{
    public static class CreditNotesReportPdfGenerator
    {
        private static readonly string PrimaryColor = "#1B3A2D";
        private static readonly string DangerColor = "#C62828";
        private static readonly string GiftColor = "#2E7D32";
        private static readonly string ReturnColor = "#E65100";
        private static readonly string MutedColor = "#6B7A6F";
        private static readonly string LightBorder = "#D5DCD4";
        private static readonly string AltRowBg = "#F4F7F1";
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        public static void generate(credit_note_report_dto report)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            string file_name = $"REPORTE NOTAS DE CREDITO {Slug(report.period_label)}.pdf".ToUpperInvariant();

            var save_dialog = new SaveFileDialog
            {
                Title = file_name,
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = file_name
            };

            if (save_dialog.ShowDialog() != true) return;

            var all_rows = report.rows ?? new List<credit_note_report_row_dto>();
            var all_sellers = report.by_seller ?? new List<credit_note_report_seller_dto>();

            var document = Document.Create(container =>
            {
                container.Page(page => BuildSummaryPage(page, report));

                foreach (var seller in all_sellers)
                {
                    var seller_rows = all_rows
                        .Where(r => string.Equals(r.seller_name, seller.seller_name, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    container.Page(page => BuildSellerPage(page, report, seller, seller_rows));
                }
            });

            document.GeneratePdf(save_dialog.FileName);
        }

        private static void BuildSummaryPage(PageDescriptor page, credit_note_report_dto report)
        {
            SetupPage(page);

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("REPORTE DE NOTAS DE CREDITO").FontSize(14).Bold().FontColor(DangerColor);
                    row.RelativeItem().AlignRight().Text(Capitalize(report.period_label)).FontSize(11).Bold().FontColor(PrimaryColor);
                });
                col.Item().PaddingTop(2)
                    .Text(string.IsNullOrWhiteSpace(report.seller_label) || report.seller_label == "Todos"
                        ? $"Categoria: {report.category_label}"
                        : $"Categoria: {report.category_label}     |     Vendedor: {report.seller_label}")
                    .FontSize(8.5f).FontColor(MutedColor);
                col.Item().PaddingTop(3).LineHorizontal(1.5f).LineColor(PrimaryColor);
            });

            page.Content().PaddingVertical(6).Column(col =>
            {
                col.Item().Text("RESUMEN DEL PERIODO").FontSize(10).Bold().FontColor(PrimaryColor);
                col.Item().PaddingTop(4);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    void Kpi(string label, string value, string color)
                    {
                        table.Cell()
                            .Border(0.5f).BorderColor(LightBorder)
                            .Padding(5)
                            .Column(inner =>
                            {
                                inner.Item().Text(label).FontSize(6.5f).Bold().FontColor(MutedColor);
                                inner.Item().PaddingTop(1).Text(value).FontSize(11).Bold().FontColor(color);
                            });
                    }

                    Kpi("TOTAL NOTAS DE CREDITO $", Money(report.total_usd), DangerColor);
                    Kpi("CANTIDAD DE NOTAS", report.total_notes.ToString(Ve), PrimaryColor);

                    Kpi("PROMEDIO POR NOTA $", Money(report.average_note_usd), PrimaryColor);
                    Kpi("CLIENTES AFECTADOS", report.affected_customers.ToString(Ve), "#6A1B9A");

                    Kpi("OBSEQUIOS $", $"{Money(report.gift_usd)}  ({report.gift_share_display})", GiftColor);
                    Kpi("DEVOLUCIONES $", $"{Money(report.return_usd)}  ({report.return_share_display})", ReturnColor);

                    // El comparativo solo tiene sentido cuando el reporte es de un mes.
                    if (report.previous_total_usd > 0)
                    {
                        Kpi("PERIODO ANTERIOR $", Money(report.previous_total_usd), "#37474F");
                        Kpi("VARIACION", report.variation_display, VariationColor(report));
                    }

                    if (report.voided_notes > 0)
                    {
                        Kpi("NOTAS ANULADAS", $"{report.voided_notes}   ({Money(report.voided_usd)})", DangerColor);
                        Kpi("NOTAS VIGENTES", (report.total_notes - report.voided_notes).ToString(Ve), PrimaryColor);
                    }
                });

                var sellers = report.by_seller ?? new List<credit_note_report_seller_dto>();
                if (sellers.Count > 0)
                {
                    col.Item().PaddingTop(12).Text("DESGLOSE POR VENDEDOR").FontSize(10).Bold().FontColor(PrimaryColor);
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.2f);
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.2f);
                        });

                        table.Header(header =>
                        {
                            void h(string text, bool left = false)
                            {
                                var c = header.Cell().Background(PrimaryColor).PaddingVertical(2).PaddingHorizontal(2);
                                var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                                t.FontColor(Colors.White).Bold().FontSize(7);
                            }

                            h("VENDEDOR", left: true);
                            h("NOTAS");
                            h("OBSEQUIO $");
                            h("DEVOLUCION $");
                            h("TOTAL $");
                        });

                        bool alternate = false;
                        foreach (var s in sellers)
                        {
                            string bg = alternate ? AltRowBg : Colors.White;

                            void cell(string text, bool left = false, string color = "#333333", bool bold = false)
                            {
                                var c = table.Cell().Background(bg).BorderBottom(0.4f).BorderColor(LightBorder)
                                    .PaddingVertical(2).PaddingHorizontal(2);
                                var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                                t.FontSize(8).FontColor(color);
                                if (bold) t.Bold();
                            }

                            cell(s.seller_name, left: true, bold: true);
                            cell(s.notes_count.ToString(Ve));
                            cell(Money(s.gift_usd), color: GiftColor);
                            cell(Money(s.return_usd), color: ReturnColor);
                            cell(Money(s.total_usd), color: DangerColor, bold: true);

                            alternate = !alternate;
                        }
                    });
                }

                var days = report.by_day ?? new List<credit_note_report_day_dto>();
                if (days.Count > 0)
                {
                    col.Item().PaddingTop(12).Text("MOVIMIENTO POR DIA ($)").FontSize(10).Bold().FontColor(PrimaryColor);
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(52);
                            for (int i = 0; i < days.Count; i++) columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(PrimaryColor).PaddingVertical(2).PaddingHorizontal(1).Text(string.Empty);

                            foreach (var d in days)
                            {
                                header.Cell().Background(PrimaryColor).PaddingVertical(2)
                                    .AlignCenter().Text(d.day_display).FontColor(Colors.White).Bold().FontSize(6);
                            }
                        });

                        table.Cell().PaddingVertical(2).PaddingHorizontal(2)
                            .Text("TOTAL $").FontSize(6.5f).Bold().FontColor(MutedColor);

                        foreach (var d in days)
                        {
                            table.Cell().PaddingVertical(2).BorderBottom(0.4f).BorderColor(LightBorder)
                                .AlignCenter()
                                .Text(d.total_usd == 0 ? string.Empty : d.total_usd.ToString("N0", Ve))
                                .FontSize(6.5f)
                                .FontColor(d.total_usd == 0 ? "#C4CCC6" : DangerColor);
                        }
                    });
                }
            });

            BuildFooter(page);
        }

        private static void BuildSellerPage(
            PageDescriptor page,
            credit_note_report_dto report,
            credit_note_report_seller_dto seller,
            List<credit_note_report_row_dto> rows)
        {
            SetupPage(page);

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem()
                        .Text($"NOTAS DE CREDITO - {seller.seller_name.ToUpperInvariant()}")
                        .FontSize(12).Bold().FontColor(DangerColor);
                    row.RelativeItem().AlignRight()
                        .Text(Capitalize(report.period_label))
                        .FontSize(10).Bold().FontColor(PrimaryColor);
                });
                col.Item().PaddingTop(2)
                    .Text($"Categoria: {report.category_label}     |     Vendedor: {seller.seller_name}")
                    .FontSize(8).FontColor(MutedColor);
                col.Item().PaddingTop(3).LineHorizontal(1.5f).LineColor(PrimaryColor);
            });

            page.Content().PaddingVertical(6).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("VENDEDOR").FontSize(6.5f).Bold().FontColor(MutedColor);
                        c.Item().Text(seller.seller_name).FontSize(10).Bold();
                    });
                    row.ConstantItem(90).AlignRight().Column(c =>
                    {
                        c.Item().Text("NOTAS").FontSize(6.5f).Bold().FontColor(MutedColor);
                        c.Item().Text(seller.notes_count.ToString(Ve)).FontSize(10).Bold();
                    });
                    row.ConstantItem(120).AlignRight().Column(c =>
                    {
                        c.Item().Text("TOTAL $").FontSize(6.5f).Bold().FontColor(MutedColor);
                        c.Item().Text(Money(seller.total_usd)).FontSize(10).Bold().FontColor(DangerColor);
                    });
                });

                col.Item().PaddingTop(6);

                if (rows.Count == 0)
                {
                    col.Item().Text("Sin notas de credito para el periodo seleccionado.")
                        .FontSize(9).FontColor(MutedColor);
                }
                else
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(52);
                            columns.ConstantColumn(58);
                            columns.ConstantColumn(62);
                            columns.ConstantColumn(74);
                            columns.RelativeColumn();
                            columns.ConstantColumn(62);
                        });

                        table.Header(header =>
                        {
                            void h(string text)
                            {
                                header.Cell().Background(PrimaryColor).PaddingVertical(2).PaddingHorizontal(1.5f)
                                    .AlignCenter().Text(text).FontColor(Colors.White).Bold().FontSize(6.5f);
                            }

                            h("FECHA");
                            h("CATEGORIA");
                            h("NRO NC");
                            h("NOTA ENTREGA");
                            h("CLIENTE");
                            h("MONTO $");
                        });

                        bool alternate = false;
                        foreach (var r in rows)
                        {
                            string bg = alternate ? AltRowBg : Colors.White;

                            void cell(string text, bool left = false, string color = "#333333", bool bold = false)
                            {
                                var c = table.Cell().Background(bg).BorderBottom(0.4f).BorderColor(LightBorder)
                                    .PaddingVertical(2).PaddingHorizontal(1.5f);
                                var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                                t.FontSize(7.5f).FontColor(color);
                                if (bold) t.Bold();
                            }

                            cell(r.fecha_display);
                            cell(r.categoria_display, color: r.es_obsequio ? GiftColor : ReturnColor, bold: true);
                            cell(r.note_number);
                            cell(r.entrega_display);
                            cell(r.customer_name, left: true);
                            cell(Money(r.total_amount_usd), color: r.esta_anulada ? "#9E9E9E" : DangerColor, bold: true);

                            alternate = !alternate;
                        }
                    });

                    col.Item().PaddingTop(10).Row(outer =>
                    {
                        outer.RelativeItem();
                        outer.ConstantItem(290).Border(0.5f).BorderColor(LightBorder).Column(box =>
                        {
                            box.Item().Padding(4).Row(r =>
                            {
                                r.RelativeItem().Text("OBSEQUIO $").FontSize(8.5f).Bold().FontColor(GiftColor);
                                r.ConstantItem(110).AlignRight().Text(Money(seller.gift_usd)).FontSize(9).Bold().FontColor(GiftColor);
                            });
                            box.Item().PaddingHorizontal(4).PaddingBottom(4).Row(r =>
                            {
                                r.RelativeItem().Text("DEVOLUCION $").FontSize(8.5f).Bold().FontColor(ReturnColor);
                                r.ConstantItem(110).AlignRight().Text(Money(seller.return_usd)).FontSize(9).Bold().FontColor(ReturnColor);
                            });
                            box.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(LightBorder);
                            box.Item().Padding(4).Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL $").FontSize(9.5f).Bold().FontColor(PrimaryColor);
                                r.ConstantItem(110).AlignRight().Text(Money(seller.total_usd)).FontSize(11).Bold().FontColor(PrimaryColor);
                            });
                        });
                    });
                }
            });

            BuildFooter(page);
        }

        private static void SetupPage(PageDescriptor page)
        {
            page.Size(PageSizes.Letter);
            page.MarginLeft(1, Unit.Centimetre);
            page.MarginTop(1, Unit.Centimetre);
            page.MarginRight(1, Unit.Centimetre);
            page.MarginBottom(1, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(8));
        }

        private static void BuildFooter(PageDescriptor page)
        {
            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                col.Item().PaddingTop(2).Row(row =>
                {
                    row.RelativeItem().Text("REPORTE DE NOTAS DE CREDITO").FontSize(7).FontColor(MutedColor);
                    row.RelativeItem().AlignCenter().Text($"Impreso: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor(MutedColor);
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(7).FontColor(MutedColor));
                        t.Span("Pagina ");
                        t.CurrentPageNumber();
                        t.Span(" de ");
                        t.TotalPages();
                    });
                });
            });
        }

        private static string VariationColor(credit_note_report_dto report)
        {
            if (report.variation_down) return DangerColor;
            if (report.variation_up) return GiftColor;
            return "#37474F";
        }

        private static string Money(decimal value) => value.ToString("#,##0.00", Ve);

        private static string Capitalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return char.ToUpper(text[0]) + text.Substring(1);
        }

        private static string Slug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "PERIODO";
            var invalid = Path.GetInvalidFileNameChars();
            var chars = text.Trim().Select(c => invalid.Contains(c) ? '-' : c).ToArray();
            return new string(chars);
        }
    }
}
