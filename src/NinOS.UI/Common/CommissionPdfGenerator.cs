using System;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;
using NinOS.Domain.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Common
{
    public static class CommissionPdfGenerator
    {
        private static readonly string PrimaryColor = "#1B3A2D";
        private static readonly string LightBorder = "#B0B0B0";
        private static readonly string AccentBg = "#F0F4EC";
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        public static void generate(commission_receipt_dto receipt)
        {
            if (receipt == null || receipt.rows.Count == 0) return;

            string seller = (receipt.seller_name ?? string.Empty).ToUpperInvariant();
            string seller_tag = seller.Replace(" ", string.Empty);
            string base_name = $"Comprobante_Comision_{seller_tag}_{receipt.payment_date:yyyyMMdd}";

            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar comprobante de pago de comision",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = base_name + ".pdf"
            };

            if (save_dialog.ShowDialog() != true) return;

            generate(receipt, save_dialog.FileName);
        }

        public static void generate(commission_receipt_dto receipt, string file_path)
        {
            if (receipt == null || receipt.rows.Count == 0) return;

            QuestPDF.Settings.License = LicenseType.Community;

            string seller = (receipt.seller_name ?? string.Empty).ToUpperInvariant();
            decimal sub_total = receipt.rows.Sum(r => r.commission_10);

            var document = Document.Create(container =>
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
                        col.Item().Text("COMPROBANTE DE PAGO DE COMISION")
                            .FontSize(13).Bold().FontColor(PrimaryColor);
                        col.Item().PaddingTop(3).LineHorizontal(1.5f).LineColor(PrimaryColor);
                    });

                    page.Content().PaddingVertical(4).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("VENDEDORA").FontSize(6.5f).Bold().FontColor("#555555");
                                c.Item().PaddingTop(1).Text(seller).FontSize(9).Bold();
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
                                h("MONTO\nFACTURADO $");
                                h("FECHA DE\nPAGO");
                                h("MONTO\nPAGADO $");
                                h("PRONTO\nPAGO");
                                h("COMISION\n10% $");
                                h("TOTAL\nCOMISION $");
                                h("PAGO\nCOMISION");
                            });

                            bool alternate = false;
                            foreach (var r in receipt.rows)
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
                                cell(r.payment_date?.ToString("d/M/yyyy") ?? string.Empty);
                                cell(Money(r.paid_amount));
                                cell(r.early_payment_discount > 0 ? "SI" : "NO");
                                cell(Money(r.commission_10));
                                cell(Money(r.commission_10));
                                cell(DateDMon(r.commission_paid_date));

                                alternate = !alternate;
                            }
                        });

                        col.Item().PaddingTop(10).Row(outerRow =>
                        {
                            outerRow.RelativeItem();

                            outerRow.ConstantItem(320).Border(0.5f).BorderColor(LightBorder).Column(bottom =>
                            {
                                bottom.Item().Padding(4).Row(r =>
                                {
                                    r.RelativeItem().Text("SUB TOTAL $").FontSize(9).Bold().FontColor("#555555");
                                    r.ConstantItem(120).AlignRight().Text(Money(sub_total)).FontSize(10).Bold();
                                });
                                bottom.Item().LineHorizontal(0.5f).LineColor(LightBorder);

                                bottom.Item().Padding(4).Row(r =>
                                {
                                    r.RelativeItem().Text("COMISION PAGADA $").FontSize(9).Bold().FontColor(PrimaryColor);
                                    r.ConstantItem(120).AlignRight().Text(Money(sub_total)).FontSize(12).Bold().FontColor(PrimaryColor);
                                });
                                bottom.Item().LineHorizontal(0.5f).LineColor(LightBorder);

                                bottom.Item().Table(payTable =>
                                {
                                    payTable.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.2f);
                                    });

                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(3).PaddingHorizontal(4).AlignCenter()
                                        .Text("BANCO").FontSize(6.5f).Bold();

                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(3).PaddingHorizontal(4).AlignCenter()
                                        .Text("NRO REF").FontSize(6.5f).Bold();

                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(3).PaddingHorizontal(4).AlignCenter()
                                        .Text("MONTO BS").FontSize(6.5f).Bold();

                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(3).PaddingHorizontal(4).AlignCenter()
                                        .Text("MONTO $").FontSize(6.5f).Bold();

                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(3).PaddingHorizontal(4).AlignCenter()
                                        .Text("FECHA").FontSize(6.5f).Bold();

                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                        .Text(receipt.bank_name ?? string.Empty).FontSize(9);

                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                        .Text(receipt.reference_number ?? string.Empty).FontSize(9);

                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                        .Text(MoneyBs(receipt.total_bs)).FontSize(9);

                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                        .Text(Money(receipt.total_usd)).FontSize(9);

                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                        .Text(DateDMon(receipt.payment_date)).FontSize(9);

                                    payTable.Cell().ColumnSpan(2).Background(PrimaryColor).Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                        .Text("TOTAL COMISION").FontColor(Colors.White).Bold().FontSize(9);
                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                        .Text(MoneyBs(receipt.total_bs)).Bold().FontSize(9);
                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(4).PaddingHorizontal(4).AlignCenter()
                                        .Text(Money(receipt.total_usd)).Bold().FontSize(9);
                                    payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                        .PaddingVertical(4).PaddingHorizontal(4)
                                        .Text(string.Empty).FontSize(9);
                                });
                            });
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                        col.Item().PaddingTop(2).Row(row =>
                        {
                            row.RelativeItem().Text("Comprobante de pago de comision").FontSize(7).FontColor("#888888");
                            row.RelativeItem().AlignCenter().Text($"Impreso: {DateTime.UtcNow:dd/MM/yyyy HH:mm}").FontSize(7).FontColor("#888888");
                            row.RelativeItem().AlignRight().Text(t =>
                            {
                                t.Span("Pagina ").FontSize(7).FontColor("#888888");
                                t.CurrentPageNumber().FontSize(7).FontColor("#888888");
                            });
                        });
                    });
                });
            });

            document.GeneratePdf(file_path);
        }

        private static string Money(decimal value) => "$" + value.ToString("#,##0.00", Ve);

        private static string MoneyBs(decimal value) => "Bs " + value.ToString("#,##0.00", Ve);

        private static string DateDMon(DateTime? value)
        {
            if (value == null || value.Value == default) return string.Empty;
            return value.Value.ToString("dd-MMM", Ve).Replace(".", string.Empty).ToLowerInvariant();
        }
    }
}