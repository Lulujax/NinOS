using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;
using NinOS.UI.Common.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Common
{
    public static class CommissionsReportPdfGenerator
    {
        private static readonly string PrimaryColor = "#1B3A2D";
        private static readonly string LightBorder = "#000000";
        private static readonly string AccentBg = "#F0F4EC";
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        public static string get_default_file_name(string period_label, string filter, string? seller_scope)
        {
            string scope = string.IsNullOrWhiteSpace(seller_scope) ? "TODAS" : seller_scope.Trim().ToUpperInvariant();
            string p = string.IsNullOrWhiteSpace(period_label) ? "GENERAL" : period_label.Trim().ToUpperInvariant();
            string f = string.IsNullOrWhiteSpace(filter) ? "TODAS" : filter.Trim().ToUpperInvariant();
            return $"REPORTE COMISIONES {p} - {scope} - {f}.pdf";
        }

        public static IDocument build_document(
            string period_label,
            string filter,
            string? seller_scope,
            List<commission_row_dto> rows)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            string period_cap = Capitalize(period_label);
            string filter_label = string.IsNullOrWhiteSpace(filter) ? "Todas" : filter;

            var grouped = rows
                .GroupBy(r => string.IsNullOrWhiteSpace(r.seller_name) ? "SIN VENDEDOR" : r.seller_name.Trim())
                .OrderBy(g => g.Key)
                .ToList();

            return Document.Create(container =>
            {
                if (grouped.Count == 0)
                {
                    container.Page(page => BuildEmptyPage(page, period_cap, filter_label));
                    return;
                }

                for (int i = 0; i < grouped.Count; i++)
                {
                    var g = grouped[i];
                    bool is_last = (i == grouped.Count - 1);
                    container.Page(page => BuildSellerPage(page, period_cap, filter_label, g.Key, g.ToList(), is_last, grouped));
                }
            });
        }

        public static void generate(
            string period_label,
            string filter,
            string? seller_scope,
            List<commission_row_dto> rows)
        {
            if (rows == null) rows = new List<commission_row_dto>();

            string file_name = get_default_file_name(period_label, filter, seller_scope);

            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar reporte de comisiones",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = file_name
            };

            if (save_dialog.ShowDialog() != true) return;

            var doc = build_document(period_label, filter, seller_scope, rows);
            doc.GeneratePdf(save_dialog.FileName);
        }

        private static void BuildEmptyPage(PageDescriptor page, string period_cap, string filter_label)
        {
            SetupPage(page, period_cap, filter_label);

            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Item().Text("No se encontraron registros de comisiones para los criterios seleccionados.")
                    .FontSize(11).Italic().FontColor("#555555");
            });
        }

        private static void BuildSellerPage(
            PageDescriptor page,
            string period_cap,
            string filter_label,
            string seller_name,
            List<commission_row_dto> srows,
            bool is_last,
            List<IGrouping<string, commission_row_dto>> all_groups)
        {
            SetupPage(page, period_cap, filter_label);

            page.Content().PaddingVertical(4).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("VENDEDORA").FontSize(6.5f).Bold().FontColor("#000000");
                        c.Item().PaddingTop(1).Text(seller_name.ToUpperInvariant()).FontSize(9).Bold().FontColor(PrimaryColor);
                    });
                });

                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(0.9f);   // NRO
                        columns.RelativeColumn(2.6f);   // CLIENTE
                        columns.RelativeColumn(1.1f);   // MONTO FACTURADO $
                        columns.RelativeColumn(1.6f);   // FECHA DE PAGO
                        columns.RelativeColumn(1.1f);   // MONTO PAGADO $
                        columns.RelativeColumn(1.0f);   // PRONTO PAGO
                        columns.RelativeColumn(1.0f);   // COMISION 10%
                        columns.RelativeColumn(1.1f);   // TOTAL COMISION $
                        columns.RelativeColumn(1.0f);   // PAGO COMISION
                    });

                    table.Header(header =>
                    {
                        void h(string text, bool left = false)
                        {
                            var c = header.Cell().Background(PrimaryColor).PaddingVertical(2).PaddingHorizontal(1.5f);
                            var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                            t.FontColor(Colors.White).Bold().FontSize(6.5f);
                        }

                        h("NRO");
                        h("CLIENTE", left: true);
                        h("MONTO\nFACTURADO $");
                        h("FECHA DE\nPAGO");
                        h("MONTO\nPAGADO $");
                        h("PRONTO\nPAGO");
                        h("COMISION\n10%");
                        h("TOTAL\nCOMISION $");
                        h("PAGO\nCOMISION");
                    });

                    bool alternate = false;
                    foreach (var r in srows)
                    {
                        string bg = alternate ? AccentBg : Colors.White;

                        void cell(string text, bool left = false, bool green = false)
                        {
                            var c = table.Cell().Background(bg).BorderBottom(0.4f).BorderColor(LightBorder)
                                .PaddingVertical(2).PaddingHorizontal(1.5f);
                            var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                            t.FontSize(7.5f);
                            if (green) t.FontColor("#2E7D32").Bold();
                        }

                        cell(r.note_number);
                        cell(r.customer_name, left: true);
                        cell(Money(r.invoiced_amount));
                        cell(r.note_payments_text);
                        cell(Money(r.paid_amount));
                        cell(r.early_payment_text);
                        cell(Money(r.amount_usd), green: true);
                        cell(Money(r.amount_usd), green: true);
                        cell(r.commission_paid_date_text);

                        alternate = !alternate;
                    }
                });

                decimal total_facturado = srows.Sum(r => r.invoiced_amount);
                decimal total_pagado = srows.Sum(r => r.paid_amount);
                decimal total_comision = srows.Sum(r => r.amount_usd);
                decimal comision_pagada = srows.Where(r => r.remaining_amount_usd <= 0.005m).Sum(r => r.amount_usd);
                decimal comision_pendiente = srows.Where(r => r.remaining_amount_usd > 0.005m).Sum(r => r.remaining_amount_usd);

                col.Item().PaddingTop(8).Row(outerRow =>
                {
                    outerRow.RelativeItem();

                    outerRow.ConstantItem(320).Border(0.5f).BorderColor(LightBorder).Column(bottom =>
                    {
                        bottom.Item().Padding(3).Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL FACTURADO:").FontSize(8).Bold().FontColor("#000000");
                            r.ConstantItem(120).AlignRight().Text(Money(total_facturado)).FontSize(8.5f).Bold();
                        });
                        bottom.Item().Padding(3).Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL PAGADO CLIENTES:").FontSize(8).Bold().FontColor("#000000");
                            r.ConstantItem(120).AlignRight().Text(Money(total_pagado)).FontSize(8.5f).Bold();
                        });
                        bottom.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                        bottom.Item().Padding(3).Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL COMISION:").FontSize(8.5f).Bold().FontColor("#000000");
                            r.ConstantItem(120).AlignRight().Text(Money(total_comision)).FontSize(9).Bold();
                        });
                        bottom.Item().Padding(3).Row(r =>
                        {
                            r.RelativeItem().Text("COMISION PAGADA:").FontSize(8).Bold().FontColor("#2E7D32");
                            r.ConstantItem(120).AlignRight().Text(Money(comision_pagada)).FontSize(8.5f).Bold().FontColor("#2E7D32");
                        });
                        bottom.Item().Padding(3).Row(r =>
                        {
                            r.RelativeItem().Text("COMISION PENDIENTE:").FontSize(8).Bold().FontColor("#F57C00");
                            r.ConstantItem(120).AlignRight().Text(Money(comision_pendiente)).FontSize(8.5f).Bold().FontColor("#F57C00");
                        });
                    });
                });

                // Si es la última vendedora y hay más de un grupo, imprimir el RESUMEN GENERAL en seguidilla continua (sin forzar salto de página)
                if (is_last && all_groups.Count > 1)
                {
                    col.Item().PaddingTop(16).Column(sumCol =>
                    {
                        sumCol.Item().Text("RESUMEN GENERAL DE COMISIONES")
                            .FontSize(11).Bold().FontColor(PrimaryColor);
                        sumCol.Item().PaddingTop(2).Text("Consolidado de todas las vendedoras del período.")
                            .FontSize(8).FontColor("#555555");

                        sumCol.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.0f);   // VENDEDORA
                                columns.RelativeColumn(0.8f);   // NOTAS
                                columns.RelativeColumn(1.4f);   // TOTAL FACTURADO $
                                columns.RelativeColumn(1.4f);   // TOTAL PAGADO $
                                columns.RelativeColumn(1.4f);   // TOTAL COMISION $
                                columns.RelativeColumn(1.4f);   // COM. PAGADA $
                                columns.RelativeColumn(1.4f);   // COM. PENDIENTE $
                            });

                            table.Header(header =>
                            {
                                void h(string text, bool left = false)
                                {
                                    var c = header.Cell().Background(PrimaryColor).PaddingVertical(3).PaddingHorizontal(2);
                                    var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                                    t.FontColor(Colors.White).Bold().FontSize(7);
                                }

                                h("VENDEDORA", left: true);
                                h("NOTAS");
                                h("FACTURADO $");
                                h("PAGADO $");
                                h("COMISION $");
                                h("PAGADA $");
                                h("PENDIENTE $");
                            });

                            bool alternateSummary = false;
                            decimal grand_facturado = 0;
                            decimal grand_pagado = 0;
                            decimal grand_comision = 0;
                            decimal grand_pagada = 0;
                            decimal grand_pendiente = 0;
                            int grand_notas = 0;

                            foreach (var g in all_groups)
                            {
                                string bg = alternateSummary ? AccentBg : Colors.White;
                                var rows = g.ToList();

                                int count = rows.Count;
                                decimal facturado = rows.Sum(r => r.invoiced_amount);
                                decimal pagado = rows.Sum(r => r.paid_amount);
                                decimal comision = rows.Sum(r => r.amount_usd);
                                decimal pagada = rows.Where(r => r.remaining_amount_usd <= 0.005m).Sum(r => r.amount_usd);
                                decimal pendiente = rows.Where(r => r.remaining_amount_usd > 0.005m).Sum(r => r.remaining_amount_usd);

                                grand_notas += count;
                                grand_facturado += facturado;
                                grand_pagado += pagado;
                                grand_comision += comision;
                                grand_pagada += pagada;
                                grand_pendiente += pendiente;

                                void cell(string text, bool left = false)
                                {
                                    var c = table.Cell().Background(bg).BorderBottom(0.4f).BorderColor(LightBorder)
                                        .PaddingVertical(3).PaddingHorizontal(2);
                                    var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                                    t.FontSize(8);
                                }

                                cell(g.Key.ToUpperInvariant(), left: true);
                                cell(count.ToString());
                                cell(Money(facturado));
                                cell(Money(pagado));
                                cell(Money(comision));
                                cell(Money(pagada));
                                cell(Money(pendiente));

                                alternateSummary = !alternateSummary;
                            }

                            void grandCell(string text, bool left = false)
                            {
                                var c = table.Cell().Background("#E8F5E9").BorderTop(1.0f).BorderBottom(1.0f).BorderColor(PrimaryColor)
                                    .PaddingVertical(4).PaddingHorizontal(2);
                                var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                                t.FontSize(8.5f).Bold().FontColor(PrimaryColor);
                            }

                            grandCell("TOTAL GENERAL", left: true);
                            grandCell(grand_notas.ToString());
                            grandCell(Money(grand_facturado));
                            grandCell(Money(grand_pagado));
                            grandCell(Money(grand_comision));
                            grandCell(Money(grand_pagada));
                            grandCell(Money(grand_pendiente));
                        });
                    });
                }
            });
        }

        private static void SetupPage(PageDescriptor page, string period_cap, string filter_label)
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
                    row.RelativeItem().Text("REPORTE DE COMISIONES").FontSize(13).Bold().FontColor(PrimaryColor);
                    row.RelativeItem().AlignRight().Text($"{period_cap} | Filtro: {filter_label}").FontSize(10).Bold().FontColor("#000000");
                });
                col.Item().PaddingTop(3).LineHorizontal(1.5f).LineColor(PrimaryColor);
            });

            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                col.Item().PaddingTop(2).Row(row =>
                {
                    row.RelativeItem().Text("Reporte de comisiones").FontSize(7).FontColor("#000000");
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
    }
}
