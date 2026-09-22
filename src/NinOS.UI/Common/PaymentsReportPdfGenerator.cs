using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;
using NinOS.Domain.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Common
{
    public static class PaymentsReportPdfGenerator
    {
        private static readonly string PrimaryColor = "#1B3A2D";
        private static readonly string LightBorder = "#B0B0B0";
        private static readonly string AccentBg = "#F0F4EC";
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        public static void generate(string month, List<payment_dto> payments)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            string file_name = $"REPORTE PAGOS {month}.pdf".ToUpperInvariant();

            var save_dialog = new SaveFileDialog
            {
                Title = file_name,
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = file_name
            };

            if (save_dialog.ShowDialog() != true) return;

            var month_cap = Capitalize(month);

            var grouped = payments
                .GroupBy(p => string.IsNullOrWhiteSpace(p.seller_name) ? "SIN VENDEDOR" : p.seller_name.Trim())
                .OrderBy(g => g.Key)
                .ToList();

            var document = Document.Create(container =>
            {
                if (grouped.Count == 0)
                {
                    container.Page(page => BuildSellerPage(page, month_cap, null, null));
                    return;
                }

                foreach (var g in grouped)
                {
                    container.Page(page => BuildSellerPage(page, month_cap, g.Key, g.ToList()));
                }
            });

            document.GeneratePdf(save_dialog.FileName);
        }

        private static void BuildSellerPage(
            PageDescriptor page,
            string month_cap,
            string? seller_name,
            List<payment_dto>? seller_rows)
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
                    row.RelativeItem().Text("PAGOS REALIZADOS DEL MES").FontSize(13).Bold().FontColor(PrimaryColor);
                    row.RelativeItem().AlignRight().Text(month_cap).FontSize(11).Bold().FontColor("#666666");
                });
                col.Item().PaddingTop(3).LineHorizontal(1.5f).LineColor(PrimaryColor);
            });

            page.Content().PaddingVertical(4).Column(col =>
            {
                if (seller_name == null)
                {
                    col.Item().Text("No hubo pagos en el mes seleccionado.").FontSize(11).FontColor("#888888");
                    return;
                }

                var srows = seller_rows ?? new List<payment_dto>();

                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("VENDEDOR").FontSize(6.5f).Bold().FontColor("#555555");
                        c.Item().PaddingTop(1).Text(seller_name).FontSize(9).Bold();
                    });
                });

                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.05f);   // FECHA
                        columns.RelativeColumn(0.95f);   // NOTA
                        columns.RelativeColumn(2.6f);    // CLIENTE
                        columns.RelativeColumn(1.35f);   // BANCO
                        columns.RelativeColumn(1.25f);   // NRO REF
                        columns.RelativeColumn(0.95f);   // TIPO
                        columns.RelativeColumn(1.05f);   // MONTO $
                        columns.RelativeColumn(1.05f);   // MONTO BS
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
                        h("NOTA");
                        h("CLIENTE", left: true);
                        h("BANCO");
                        h("NRO REF");
                        h("TIPO");
                        h("MONTO $");
                        h("MONTO BS");
                    });

                    bool alternate = false;
                    foreach (var r in srows)
                    {
                        string bg = alternate ? AccentBg : Colors.White;

                        void cell(string text, bool left = false)
                        {
                            var c = table.Cell().Background(bg).BorderBottom(0.4f).BorderColor(LightBorder)
                                .PaddingVertical(2).PaddingHorizontal(1.5f);
                            var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                            t.FontSize(7.5f);
                        }

                        cell(r.payment_date.ToString("d/M/yyyy"));
                        cell(r.note_number);
                        cell(r.customer_name, left: true);
                        cell(r.bank_name);
                        cell(r.reference_number);
                        cell(r.payment_type);
                        cell(Money(r.amount_usd));
                        cell(r.amount_bs > 0 ? MoneyBs(r.amount_bs) : "-");

                        alternate = !alternate;
                    }
                });

                decimal sub_total_usd = srows.Sum(p => p.amount_usd);
                decimal sub_total_bs = srows.Sum(p => p.amount_bs);

                col.Item().PaddingTop(10).Row(outerRow =>
                {
                    outerRow.RelativeItem();

                    outerRow.ConstantItem(320).Border(0.5f).BorderColor(LightBorder).Column(bottom =>
                    {
                        bottom.Item().Padding(4).Row(r =>
                        {
                            r.RelativeItem().Text("SUB TOTAL $").FontSize(9).Bold().FontColor("#555555");
                            r.ConstantItem(120).AlignRight().Text(Money(sub_total_usd)).FontSize(10).Bold();
                        });
                        bottom.Item().PaddingHorizontal(4).PaddingBottom(4).Row(r =>
                        {
                            r.RelativeItem().Text("SUB TOTAL BS").FontSize(9).Bold().FontColor("#555555");
                            r.ConstantItem(120).AlignRight().Text(MoneyBs(sub_total_bs)).FontSize(10).Bold();
                        });
                        bottom.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                        bottom.Item().Padding(4).Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL PAGADO $").FontSize(9).Bold().FontColor(PrimaryColor);
                            r.ConstantItem(120).AlignRight().Text(Money(sub_total_usd)).FontSize(12).Bold().FontColor(PrimaryColor);
                        });
                    });
                });
            });

            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                col.Item().PaddingTop(2).Row(row =>
                {
                    row.RelativeItem().Text("Reporte de pagos del mes").FontSize(7).FontColor("#888888");
                    row.RelativeItem().AlignCenter().Text($"Impreso: {DateTime.UtcNow:dd/MM/yyyy HH:mm}").FontSize(7).FontColor("#888888");
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Pagina ").FontSize(7).FontColor("#888888");
                        t.CurrentPageNumber().FontSize(7).FontColor("#888888");
                        t.Span(" de ").FontSize(7).FontColor("#888888");
                        t.TotalPages().FontSize(7).FontColor("#888888");
                    });
                });
            });
        }

        private static string Money(decimal value) => "$" + value.ToString("#,##0.00", Ve);

        private static string MoneyBs(decimal value) => "Bs " + value.ToString("#,##0.00", Ve);

        private static string Capitalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return char.ToUpper(text[0]) + text.Substring(1);
        }
    }
}
