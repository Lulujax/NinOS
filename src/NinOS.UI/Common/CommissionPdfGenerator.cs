using System;
using System.Collections.Generic;
using Microsoft.Win32;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;
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

        public static void generate(commission_row_dto commission, List<commission_payment_dto> payments)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            DateTime pay_date = payments.Count > 0 ? payments[0].payment_date : DateTime.Today;
            string reference = payments.Count > 0 ? payments[0].reference_number : string.Empty;

            string base_name = $"Comision_{commission.note_number}_{pay_date:yyyyMMdd}";
            if (!string.IsNullOrWhiteSpace(reference)) base_name += $"_{reference}";

            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar comprobante de comision",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = base_name + ".pdf"
            };

            if (save_dialog.ShowDialog() != true) return;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginVertical(20);
                    page.MarginHorizontal(25);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("COMPROBANTE DE LIQUIDACION DE COMISION").FontSize(16).Bold().FontColor(PrimaryColor);
                                left.Item().PaddingTop(1).Text("Comisiones por ventas").FontSize(9).FontColor("#555555");
                            });
                            row.RelativeItem().AlignRight().Column(right =>
                            {
                                right.Item().AlignRight().Text("FECHA").FontSize(7).Bold().FontColor("#555555");
                                right.Item().AlignRight().Text(pay_date.ToString("dd/MM/yyyy")).FontSize(11).Bold();
                            });
                        });
                        col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(PrimaryColor);
                    });

                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("VENDEDORA:").FontSize(8).Bold().FontColor("#555555");
                                c.Item().PaddingTop(1).Text(commission.seller_name).FontSize(11).Bold();
                            });
                        });

                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("NOTA:").FontSize(8).Bold().FontColor("#555555");
                                c.Item().PaddingTop(1).Text(commission.note_number).FontSize(10).Bold();
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("CLIENTE:").FontSize(8).Bold().FontColor("#555555");
                                c.Item().PaddingTop(1).Text(commission.customer_name).FontSize(10);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("COMISION ($):").FontSize(8).Bold().FontColor("#555555");
                                c.Item().PaddingTop(1).Text(commission.amount_usd.ToString("N2")).FontSize(10).Bold();
                            });
                        });

                        col.Item().PaddingTop(8).Text("PAGOS DE COMISION").FontSize(9).Bold().FontColor(PrimaryColor);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.0f);
                                columns.RelativeColumn(1.3f);
                                columns.RelativeColumn(1.0f);
                                columns.RelativeColumn(1.3f);
                            });

                            table.Header(header =>
                            {
                                Action<IContainer, string> l = (cell, text) => cell.Background(PrimaryColor).Padding(4).Text(text).FontColor(Colors.White).Bold().FontSize(8);
                                Action<IContainer, string> r = (cell, text) => { cell.Background(PrimaryColor).Padding(4).AlignRight().Text(text).FontColor(Colors.White).Bold().FontSize(8); };
                                l(header.Cell(), "FECHA");
                                l(header.Cell(), "USD");
                                r(header.Cell(), "TASA");
                                l(header.Cell(), "BS");
                                l(header.Cell(), "TIPO");
                                l(header.Cell(), "REFERENCIA");
                            });

                            bool alternate = false;
                            foreach (var p in payments)
                            {
                                string bg = alternate ? AccentBg : Colors.White;
                                table.Cell().Background(bg).Padding(3).Text(p.payment_date.ToString("dd/MM/yyyy")).FontSize(8);
                                table.Cell().Background(bg).Padding(3).AlignRight().Text(p.amount_usd.ToString("N2")).FontSize(8);
                                table.Cell().Background(bg).Padding(3).AlignRight().Text(p.exchange_rate.ToString("N2")).FontSize(8);
                                table.Cell().Background(bg).Padding(3).AlignRight().Text(p.amount_bs.ToString("N2")).FontSize(8);
                                table.Cell().Background(bg).Padding(3).Text(p.payment_type).FontSize(8);
                                table.Cell().Background(bg).Padding(3).Text(p.reference_number).FontSize(8);
                                alternate = !alternate;
                            }
                        });

                        decimal total_bs = payments.Sum(p => p.amount_bs);
                        decimal total_usd = payments.Sum(p => p.amount_usd);

                        col.Item().PaddingTop(6).AlignRight().Row(row =>
                        {
                            row.ConstantItem(240).Border(0.5f).BorderColor(LightBorder).Padding(6).Column(details =>
                            {
                                details.Item().Row(t =>
                                {
                                    t.RelativeItem().Text("TOTAL PAGADO USD:").FontSize(9);
                                    t.RelativeItem().AlignRight().Text($"{total_usd:N2}").FontSize(9).Bold();
                                });
                                details.Item().PaddingTop(2).Row(t =>
                                {
                                    t.RelativeItem().Text("TOTAL PAGADO BS:").FontSize(9);
                                    t.RelativeItem().AlignRight().Text($"{total_bs:N2}").FontSize(9).Bold();
                                });
                                details.Item().PaddingTop(2).Row(t =>
                                {
                                    t.RelativeItem().Text("COMISION DE LA NOTA:").FontSize(9);
                                    t.RelativeItem().AlignRight().Text($"{commission.amount_usd:N2}").FontSize(9);
                                });
                                details.Item().PaddingTop(2).Row(t =>
                                {
                                    t.RelativeItem().Text("SALDO PENDIENTE:").FontSize(9).FontColor("#C62828");
                                    t.RelativeItem().AlignRight().Text($"{Math.Max(0, commission.amount_usd - total_usd):N2}").FontSize(9).Bold().FontColor("#C62828");
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
                });
            });

            document.GeneratePdf(save_dialog.FileName);
        }
    }
}