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
    public static class CommissionMonthlyReportPdfGenerator
    {
        private static readonly string PrimaryColor = "#1B3A2D";
        private static readonly string LightBorder = "#B0B0B0";
        private static readonly string AccentBg = "#F0F4EC";
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

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

            var paid_payments = payments
                .Where(p => p.is_paid)
                .ToList();

            var grouped = paid_payments
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
                    row.RelativeItem().Text("COMISIONES PAGADAS DEL MES").FontSize(13).Bold().FontColor(PrimaryColor);
                    row.RelativeItem().AlignRight().Text(month_cap).FontSize(11).Bold().FontColor("#666666");
                });
                col.Item().PaddingTop(3).LineHorizontal(1.5f).LineColor(PrimaryColor);
            });

            page.Content().PaddingVertical(4).Column(col =>
            {
                if (seller_name == null)
                {
                    col.Item().Text("No hubo pagos de comision en el mes seleccionado.").FontSize(11).FontColor("#888888");
                    return;
                }

                var srows = seller_rows ?? new List<commission_month_payment_dto>();

                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("VENDEDORA").FontSize(6.5f).Bold().FontColor("#555555");
                        c.Item().PaddingTop(1).Text(seller_name).FontSize(9).Bold();
                    });
                });

                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.15f);
                        columns.RelativeColumn(0.95f);
                        columns.RelativeColumn(3.0f);
                        columns.RelativeColumn(1.25f);
                        columns.RelativeColumn(1.15f);
                        columns.RelativeColumn(1.15f);
                        columns.RelativeColumn(1.25f);
                        columns.RelativeColumn(1.05f);
                        columns.RelativeColumn(1.15f);
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

                        h("FECHA\nDESPACHO");
                        h("NRO");
                        h("CLIENTE", left: true);
                        h("MONTO\nFACTURADO");
                        h("FECHA DE\nPAGO");
                        h("MONTO\nPAGADO");
                        h("PRONTO\nPAGO");
                        h("COMISION\n10%");
                        h("TOTAL\nCOMISION");
                        h("PAGO\nCOMISION");
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

                        cell(r.dispatch_date?.ToString("d/M/yyyy") ?? string.Empty);
                        cell(r.note_number);
                        cell(r.customer_name, left: true);
                        cell(Money(r.invoiced_amount));
                        cell(r.note_payment_date?.ToString("d/M/yyyy") ?? string.Empty);
                        cell(Money(r.paid_amount));
                        cell(r.early_payment_discount > 0 ? "SI" : "NO");
                        cell(Money(r.commission_10));
                        cell(Money(r.commission_10));
                        cell(DateDMon(r.payment_date));

                        alternate = !alternate;
                    }
                });

                decimal sub_total = srows.Sum(r => r.commission_10);

                var seller_payments = srows
                    .GroupBy(r => new { r.reference_number, r.bank_name, Date = r.payment_date.Date })
                    .Select(g => new
                    {
                        bank_name = g.Select(x => x.bank_name).FirstOrDefault(b => !string.IsNullOrWhiteSpace(b)) ?? string.Empty,
                        reference_number = g.Key.reference_number,
                        amount_bs = g.Sum(x => x.amount_bs),
                        amount_usd = g.Sum(x => x.amount_usd),
                        payment_date = g.Max(x => x.payment_date)
                    })
                    .OrderBy(p => p.payment_date)
                    .ToList();

                col.Item().PaddingTop(10).Row(outerRow =>
                {
                    outerRow.RelativeItem();

                    outerRow.ConstantItem(320).Border(0.5f).BorderColor(LightBorder).Column(bottom =>
                    {
                        bottom.Item().Padding(4).Row(r =>
                        {
                            r.RelativeItem().Text("SUB TOTAL").FontSize(9).Bold().FontColor("#555555");
                            r.ConstantItem(120).AlignRight().Text(Money(sub_total)).FontSize(10).Bold();
                        });
                        bottom.Item().LineHorizontal(0.5f).LineColor(LightBorder);

                        bottom.Item().Padding(4).Row(r =>
                        {
                            r.RelativeItem().Text("COMISION PAGADA").FontSize(9).Bold().FontColor(PrimaryColor);
                            r.ConstantItem(120).AlignRight().Text(Money(sub_total)).FontSize(12).Bold().FontColor(PrimaryColor);
                        });
                        bottom.Item().LineHorizontal(0.5f).LineColor(LightBorder);

                        if (seller_payments.Count > 0)
                        {
                            bottom.Item().PaddingTop(3).PaddingHorizontal(4).Text("PAGOS REALIZADOS")
                                .FontSize(7).Bold().FontColor("#555555");

                            bottom.Item().PaddingTop(2).Table(payTable =>
                            {
                                payTable.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.2f);
                                });

                                void ph(IContainer cell, string text)
                                {
                                    cell.Background(PrimaryColor).PaddingVertical(3).PaddingHorizontal(4).AlignCenter()
                                        .Text(text).FontColor(Colors.White).Bold().FontSize(6.5f);
                                }

                                ph(payTable.Cell(), "BANCO");
                                ph(payTable.Cell(), "NRO REF");
                                ph(payTable.Cell(), "MONTO BS");
                                ph(payTable.Cell(), "MONTO $");
                                ph(payTable.Cell(), "FECHA");

                                foreach (var p in seller_payments)
                                {
                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                        .Text(p.bank_name).FontSize(8);
                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                        .Text(p.reference_number ?? string.Empty).FontSize(8);
                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                        .Text(Money(p.amount_bs)).FontSize(8);
                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                        .Text(Money(p.amount_usd)).FontSize(8);
                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                        .Text(DateDMon(p.payment_date)).FontSize(8);
                                }

                                decimal total_paid_bs = seller_payments.Sum(p => p.amount_bs);
                                decimal total_paid_usd = seller_payments.Sum(p => p.amount_usd);
                                payTable.Cell().ColumnSpan(2).Background(PrimaryColor).Border(0.5f).BorderColor(LightBorder)
                                    .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                    .Text($"TOTAL COMISION {month_cap.ToUpperInvariant()}").FontColor(Colors.White).Bold().FontSize(8);
                                payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                    .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                    .Text(Money(total_paid_bs)).Bold().FontSize(8);
                                payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                    .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                    .Text(Money(total_paid_usd)).Bold().FontSize(8);
                                payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                    .PaddingVertical(4).PaddingHorizontal(4)
                                    .Text(string.Empty).FontSize(8);
                            });
                        }
                    });
                });
            });

            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                col.Item().PaddingTop(2).Row(row =>
                {
                    row.RelativeItem().Text("Reporte de comisiones pagadas del mes").FontSize(7).FontColor("#888888");
                    row.RelativeItem().AlignCenter().Text($"Impreso: {DateTime.UtcNow:dd/MM/yyyy HH:mm}").FontSize(7).FontColor("#888888");
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Pagina ").FontSize(7).FontColor("#888888");
                        t.CurrentPageNumber().FontSize(7).FontColor("#888888");
                    });
                });
            });
        }

        private static string Money(decimal value) => value.ToString("#,##0.00", Ve);

        private static string DateDMon(DateTime value)
        {
            if (value == default) return string.Empty;
            return value.ToString("dd-MMM", Ve).Replace(".", string.Empty).ToLowerInvariant();
        }

        private static string Capitalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return char.ToUpper(text[0]) + text.Substring(1);
        }
    }
}