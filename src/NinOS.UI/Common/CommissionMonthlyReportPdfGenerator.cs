using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using NinOS.Domain.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Common
{
    public static class CommissionMonthlyReportPdfGenerator
    {
        private static readonly string PrimaryColor = "#1B3A2D";
        private static readonly string LightBorder = "#B0B0B0";
        private static readonly string AccentBg = "#F0F4EC";

        public static void generate(string month, List<commission_month_payment_dto> payments)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            string file_name = $"REPORTE COMISIONES PAGADAS {month}.pdf".ToUpperInvariant();

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
            List<commission_month_payment_dto>? seller_rows)
        {
            page.Size(PageSizes.A4);
            page.MarginVertical(18);
            page.MarginHorizontal(22);
            page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("COMISIONES PAGADAS DEL MES").FontSize(13).Bold().FontColor(PrimaryColor);
                    row.RelativeItem().AlignRight().Text(month_cap).FontSize(11).Bold().FontColor("#666666");
                });
                col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(PrimaryColor);
            });

            page.Content().PaddingVertical(8).Column(col =>
            {
                if (seller_name == null)
                {
                    col.Item().Text("No hubo pagos de comision en el mes seleccionado.").FontSize(11).FontColor("#888888");
                    return;
                }

                var srows = seller_rows ?? new List<commission_month_payment_dto>();

                col.Item().Text($"VENDEDORA: {seller_name}").FontSize(12).Bold().FontColor(PrimaryColor);
                col.Item().PaddingTop(2).Text("DETALLE DE PAGOS DE COMISION").FontSize(10).Bold().FontColor("#666666");

                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.0f);   // FECHA
                        columns.RelativeColumn(1.2f);   // NOTA
                        columns.RelativeColumn(3.2f);   // CLIENTE
                        columns.RelativeColumn(1.4f);   // TIPO
                        columns.RelativeColumn(1.6f);   // REFERENCIA
                        columns.RelativeColumn(1.2f);   // MONTO USD
                        columns.RelativeColumn(1.3f);   // MONTO BS
                    });

                    table.Header(header =>
                    {
                        Action<IContainer, string> l = (cell, text) => cell.Background(PrimaryColor).Padding(4).Text(text).FontColor(Colors.White).Bold().FontSize(8);
                        Action<IContainer, string> r = (cell, text) => { cell.Background(PrimaryColor).Padding(4).AlignRight().Text(text).FontColor(Colors.White).Bold().FontSize(8); };
                        l(header.Cell(), "FECHA");
                        l(header.Cell(), "NOTA");
                        l(header.Cell(), "CLIENTE");
                        l(header.Cell(), "TIPO");
                        l(header.Cell(), "REFERENCIA");
                        r(header.Cell(), "USD");
                        r(header.Cell(), "BS");
                    });

                    bool alternate = false;
                    foreach (var p in srows)
                    {
                        string bg = alternate ? AccentBg : Colors.White;
                        table.Cell().Background(bg).Padding(3).Text(p.payment_date.ToString("dd/MM/yyyy")).FontSize(8);
                        table.Cell().Background(bg).Padding(3).Text(p.note_number).FontSize(8);
                        table.Cell().Background(bg).Padding(3).Text(p.customer_name).FontSize(8);
                        table.Cell().Background(bg).Padding(3).Text(p.payment_type).FontSize(8);
                        table.Cell().Background(bg).Padding(3).Text(p.reference_number).FontSize(8);
                        table.Cell().Background(bg).Padding(3).AlignRight().Text(p.amount_usd.ToString("N2")).FontSize(8);
                        table.Cell().Background(bg).Padding(3).AlignRight().Text(p.amount_bs.ToString("N2")).FontSize(8);
                        alternate = !alternate;
                    }
                });

                decimal seller_usd = srows.Sum(x => x.amount_usd);
                decimal seller_bs = srows.Sum(x => x.amount_bs);
                int seller_count = srows.Count;

                col.Item().PaddingTop(6).AlignRight().Row(row =>
                {
                    row.ConstantItem(250).Border(0.5f).BorderColor(LightBorder).Padding(6).Column(totals =>
                    {
                        totals.Item().Row(t =>
                        {
                            t.RelativeItem().Text("PAGOS:").FontSize(9);
                            t.RelativeItem().AlignRight().Text(seller_count.ToString()).FontSize(9);
                        });
                        totals.Item().PaddingTop(2).Row(t =>
                        {
                            t.RelativeItem().Text("TOTAL USD:").FontSize(9);
                            t.RelativeItem().AlignRight().Text($"{seller_usd:N2}").FontSize(9);
                        });
                        totals.Item().PaddingTop(2).Row(t =>
                        {
                            t.RelativeItem().Text("TOTAL BS:").FontSize(9);
                            t.RelativeItem().AlignRight().Text($"{seller_bs:N2}").FontSize(9);
                        });
                        totals.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(LightBorder);
                        totals.Item().PaddingTop(3).Row(t =>
                        {
                            t.RelativeItem().Text("TOTAL:").FontSize(10).Bold();
                            t.RelativeItem().AlignRight().Text($"{seller_usd:N2}").FontSize(10).Bold().FontColor(PrimaryColor);
                        });
                    });
                });
            });

            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("COMISIONES").FontSize(7).FontColor("#888888");
                    row.RelativeItem().AlignCenter().Text($"Impreso: {DateTime.UtcNow:dd/MM/yyyy HH:mm}").FontSize(7).FontColor("#888888");
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Pagina ").FontSize(7).FontColor("#888888");
                        t.CurrentPageNumber().FontSize(7).FontColor("#888888");
                    });
                });
            });
        }

        private static string Capitalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return char.ToUpper(text[0]) + text.Substring(1);
        }
    }
}
