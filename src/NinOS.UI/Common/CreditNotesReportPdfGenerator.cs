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
    // Sigue el mismo diseno que MonthlyReportPdfGenerator / PaymentsReportPdfGenerator:
    // una hoja por vendedor, encabezado con titulo + periodo y linea divisoria.
    public static class CreditNotesReportPdfGenerator
    {
        private static readonly string PrimaryColor = "#1B3A2D";
        private static readonly string LightBorder = "#000000";
        private static readonly string AccentBg = "#F0F4EC";
        private static readonly string GiftColor = "#2E7D32";
        private static readonly string ReturnColor = "#C62828";
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        public static void generate(credit_note_report_dto report)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            string period = string.IsNullOrWhiteSpace(report.period_label) ? "GENERAL" : report.period_label.Trim();
            string file_name = Slug($"REPORTE NOTAS DE CREDITO {period}").ToUpperInvariant();

            var save_dialog = new SaveFileDialog
            {
                Title = file_name,
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = file_name
            };

            if (save_dialog.ShowDialog() != true) return;

            string period_cap = Capitalize(period);
            string title = BuildTitle(report.category_label);

            var rows = report.rows ?? new List<credit_note_report_row_dto>();

            var grouped = rows
                .GroupBy(r => string.IsNullOrWhiteSpace(r.seller_name) ? "SIN VENDEDOR" : r.seller_name.Trim())
                .OrderBy(g => g.Key)
                .ToList();

            var document = Document.Create(container =>
            {
                if (grouped.Count == 0)
                {
                    container.Page(page => BuildSellerPage(page, title, period_cap, null, null));
                    return;
                }

                foreach (var g in grouped)
                {
                    container.Page(page => BuildSellerPage(page, title, period_cap, g.Key, g.ToList()));
                }
            });

            document.GeneratePdf(save_dialog.FileName);
        }

        private static string BuildTitle(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return "NOTAS DE CREDITO";
            if (string.Equals(category.Trim(), "Obsequio", StringComparison.OrdinalIgnoreCase)) return "NOTAS DE CREDITO - OBSEQUIO";
            if (string.Equals(category.Trim(), "Devolucion", StringComparison.OrdinalIgnoreCase)) return "NOTAS DE CREDITO - DEVOLUCIONES";
            return "NOTAS DE CREDITO";
        }

        private static void BuildSellerPage(
            PageDescriptor page,
            string title,
            string period_cap,
            string? seller_name,
            List<credit_note_report_row_dto>? seller_rows)
        {
            page.Size(PageSizes.Letter);
            page.MarginLeft(1, Unit.Centimetre);
            page.MarginTop(1, Unit.Centimetre);
            page.MarginRight(1, Unit.Centimetre);
            page.MarginBottom(1, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(8));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(title).FontSize(13).Bold().FontColor(PrimaryColor);
                    row.RelativeItem().AlignRight().Text(period_cap).FontSize(11).Bold().FontColor("#000000");
                });
                col.Item().PaddingTop(3).LineHorizontal(1.5f).LineColor(PrimaryColor);
            });

            page.Content().PaddingVertical(4).Column(col =>
            {
                if (seller_name == null)
                {
                    col.Item().Text("No hubo notas de credito en el periodo seleccionado.").FontSize(11).FontColor("#000000");
                    return;
                }

                var srows = seller_rows ?? new List<credit_note_report_row_dto>();

                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("VENDEDOR").FontSize(6.5f).Bold().FontColor("#000000");
                        c.Item().PaddingTop(1).Text(seller_name).FontSize(9).Bold();
                    });
                    row.ConstantItem(90).AlignRight().Column(c =>
                    {
                        c.Item().Text("NOTAS").FontSize(6.5f).Bold().FontColor("#000000");
                        c.Item().PaddingTop(1).Text(srows.Count.ToString(Ve)).FontSize(9).Bold();
                    });
                });

                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.05f);
                        columns.RelativeColumn(1.15f);
                        columns.RelativeColumn(1.15f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(2.7f);
                        columns.RelativeColumn(1.15f);
                    });

                    table.Header(header =>
                    {
                        void h(string text, bool left = false)
                        {
                            var c = header.Cell().Background(PrimaryColor).PaddingVertical(2).PaddingHorizontal(1.5f);
                            var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                            t.FontColor(Colors.White).Bold().FontSize(6.5f);
                        }

                        h("FECHA");
                        h("CATEGORIA");
                        h("NRO NC");
                        h("NOTA ENTREGA");
                        h("CLIENTE", left: true);
                        h("MONTO $");
                    });

                    bool alternate = false;
                    foreach (var r in srows)
                    {
                        string bg = alternate ? AccentBg : Colors.White;

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
                        cell(Money(r.total_amount_usd), bold: true);

                        alternate = !alternate;
                    }
                });

                decimal gift_total = srows.Where(r => r.es_obsequio).Sum(r => r.total_amount_usd);
                decimal return_total = srows.Where(r => !r.es_obsequio).Sum(r => r.total_amount_usd);
                decimal seller_total = gift_total + return_total;

                col.Item().PaddingTop(10).Row(outerRow =>
                {
                    outerRow.RelativeItem();

                    outerRow.ConstantItem(320).Border(0.5f).BorderColor(LightBorder).Column(bottom =>
                    {
                        if (gift_total > 0)
                        {
                            bottom.Item().Padding(4).Row(r =>
                            {
                                r.RelativeItem().Text("OBSEQUIOS $").FontSize(9).Bold().FontColor(GiftColor);
                                r.ConstantItem(120).AlignRight().Text(Money(gift_total)).FontSize(10).Bold().FontColor(GiftColor);
                            });
                            bottom.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                        }

                        if (return_total > 0)
                        {
                            bottom.Item().Padding(4).Row(r =>
                            {
                                r.RelativeItem().Text("DEVOLUCIONES $").FontSize(9).Bold().FontColor(ReturnColor);
                                r.ConstantItem(120).AlignRight().Text(Money(return_total)).FontSize(10).Bold().FontColor(ReturnColor);
                            });
                            bottom.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                        }

                        bottom.Item().Padding(4).Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL $").FontSize(9).Bold().FontColor(PrimaryColor);
                            r.ConstantItem(120).AlignRight().Text(Money(seller_total)).FontSize(12).Bold().FontColor(PrimaryColor);
                        });
                    });
                });
            });

            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                col.Item().PaddingTop(2).Row(row =>
                {
                    row.RelativeItem().Text(title).FontSize(7).FontColor("#000000");
                    row.RelativeItem().AlignCenter().Text($"Impreso: {DateTime.UtcNow:dd/MM/yyyy HH:mm}").FontSize(7).FontColor("#000000");
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Pagina ").FontSize(7).FontColor("#000000");
                        t.CurrentPageNumber().FontSize(7).FontColor("#000000");
                        t.Span(" de ").FontSize(7).FontColor("#000000");
                        t.TotalPages().FontSize(7).FontColor("#000000");
                    });
                });
            });
        }

        private static string Money(decimal value) => "$" + value.ToString("#,##0.00", Ve);

        private static string Capitalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return char.ToUpper(text[0]) + text.Substring(1);
        }

        private static string Slug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "REPORTE";
            var invalid = Path.GetInvalidFileNameChars();
            var chars = text.Trim().Select(c => invalid.Contains(c) ? '-' : c).ToArray();
            return new string(chars);
        }
    }
}
