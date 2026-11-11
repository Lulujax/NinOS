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

            var grouped = rows
                .GroupBy(r => string.IsNullOrWhiteSpace(r.seller_name) ? "SIN VENDEDOR" : r.seller_name.Trim())
                .OrderBy(g => g.Key)
                .ToList();

            return Document.Create(container =>
            {
                if (grouped.Count == 0)
                {
                    container.Page(page => BuildEmptyPage(page, period_cap));
                    return;
                }

                // Páginas individuales por vendedora con su detalle y su liquidación bancaria
                for (int i = 0; i < grouped.Count; i++)
                {
                    var g = grouped[i];
                    bool is_last = (i == grouped.Count - 1);
                    container.Page(page => BuildSellerPage(page, period_cap, g.Key, g.ToList(), is_last, rows, grouped));
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

        private static void BuildEmptyPage(PageDescriptor page, string period_cap)
        {
            SetupPage(page, period_cap);

            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Item().Text("No se encontraron registros de comisiones para los criterios seleccionados.")
                    .FontSize(11).Italic().FontColor("#555555");
            });
        }

        private static void BuildSellerPage(
            PageDescriptor page,
            string period_cap,
            string seller_name,
            List<commission_row_dto> srows,
            bool is_last = false,
            List<commission_row_dto>? all_rows = null,
            List<IGrouping<string, commission_row_dto>>? all_groups = null)
        {
            SetupPage(page, period_cap);

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
                        columns.RelativeColumn(1.1f);   // MONTO FACTURADO
                        columns.RelativeColumn(1.6f);   // FECHA DE PAGO
                        columns.RelativeColumn(1.1f);   // MONTO PAGADO
                        columns.RelativeColumn(1.0f);   // PRONTO PAGO
                        columns.RelativeColumn(1.0f);   // COMISION 10%
                        columns.RelativeColumn(1.1f);   // TOTAL COMISION
                        columns.RelativeColumn(1.0f);   // PAGO COMISION
                    });

                    table.Header(header =>
                    {
                        void h(string text, bool left = false)
                        {
                            var c = header.Cell().Background(PrimaryColor).Border(0.4f).BorderColor(LightBorder).PaddingVertical(2).PaddingHorizontal(1.5f);
                            var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                            t.FontColor(Colors.White).Bold().FontSize(6.5f);
                        }

                        h("NRO");
                        h("CLIENTE", left: true);
                        h("MONTO\nFACTURADO");
                        h("FECHA DE\nPAGO");
                        h("MONTO\nPAGADO");
                        h("PRONTO\nPAGO");
                        h("COMISION");
                        h("TOTAL\nCOMISION");
                        h("PAGO\nCOMISION");
                    });

                    bool alternate = false;
                    foreach (var r in srows)
                    {
                        string bg = alternate ? AccentBg : Colors.White;

                        void cell(string text, bool left = false, bool green = false)
                        {
                            var c = table.Cell().Background(bg).Border(0.4f).BorderColor(LightBorder)
                                .PaddingVertical(2).PaddingHorizontal(1.5f);
                            var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                            t.FontSize(7.5f);
                            if (green) t.FontColor("#2E7D32").Bold();
                        }

                        cell(r.note_number);
                        cell(r.customer_name, left: true);
                        cell(Money(r.invoiced_amount));
                        cell(r.note_payments_text?.Replace("$", string.Empty) ?? string.Empty);
                        cell(Money(r.paid_amount));
                        cell(r.early_payment_text);
                        cell(Money(r.amount_usd), green: true);
                        cell(Money(r.amount_usd), green: true);
                        cell(r.commission_paid_date_text);

                        alternate = !alternate;
                    }
                });

                // TABLA DE TOTALES Y LIQUIDACIÓN BANCARIA DE LA VENDEDORA
                decimal sub_total = srows.Sum(r => r.amount_usd);
                decimal comision_pagada = srows.Sum(r => r.paid_amount_usd > 0 ? r.paid_amount_usd : (r.is_paid || r.remaining_amount_usd <= 0.005m ? r.amount_usd : 0m));

                var paid_payments = srows
                    .Where(r => r.paid_amount_usd > 0 || !string.IsNullOrWhiteSpace(r.reference_number) || !string.IsNullOrWhiteSpace(r.bank_name) || r.amount_bs > 0)
                    .GroupBy(r => new
                    {
                        Bank = string.IsNullOrWhiteSpace(r.bank_name) ? "-" : r.bank_name.Trim(),
                        Ref = string.IsNullOrWhiteSpace(r.reference_number) ? "-" : r.reference_number.Trim(),
                        Date = !string.IsNullOrWhiteSpace(r.commission_paid_date_text)
                            ? r.commission_paid_date_text
                            : (r.commission_paid_date != null ? DateDMon(r.commission_paid_date) : (r.payout_date != null ? DateDMon(r.payout_date) : "-"))
                    })
                    .Select(g => new
                    {
                        Bank = g.Key.Bank,
                        Ref = g.Key.Ref,
                        Date = g.Key.Date,
                        AmountBs = g.Sum(x => x.amount_bs),
                        AmountUsd = g.Sum(x => x.paid_amount_usd > 0 ? x.paid_amount_usd : x.amount_usd)
                    })
                    .ToList();

                if (paid_payments.Count == 0)
                {
                    paid_payments.Add(new
                    {
                        Bank = "-",
                        Ref = "-",
                        Date = "-",
                        AmountBs = 0m,
                        AmountUsd = comision_pagada
                    });
                }

                decimal total_pay_bs = paid_payments.Sum(p => p.AmountBs);
                decimal total_pay_usd = paid_payments.Sum(p => p.AmountUsd);
                if (total_pay_usd <= 0 && comision_pagada > 0) total_pay_usd = comision_pagada;

                col.Item().PaddingTop(10).Row(outerRow =>
                {
                    outerRow.RelativeItem();

                    outerRow.ConstantItem(320).Border(0.5f).BorderColor(LightBorder).Column(bottom =>
                    {
                        bottom.Item().Padding(4).Row(r =>
                        {
                            r.RelativeItem().Text("SUB TOTAL").FontSize(9).Bold().FontColor("#000000");
                            r.ConstantItem(120).AlignRight().Text(Money(sub_total)).FontSize(10).Bold();
                        });
                        bottom.Item().LineHorizontal(0.5f).LineColor(LightBorder);

                        bottom.Item().Padding(4).Row(r =>
                        {
                            r.RelativeItem().Text("COMISION PAGADA").FontSize(9).Bold().FontColor(PrimaryColor);
                            r.ConstantItem(120).AlignRight().Text(Money(comision_pagada)).FontSize(12).Bold().FontColor(PrimaryColor);
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

                            foreach (var pay in paid_payments)
                            {
                                payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                    .PaddingVertical(3).PaddingHorizontal(4).AlignCenter()
                                    .Text(pay.Bank).FontSize(8.5f);

                                payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                    .PaddingVertical(3).PaddingHorizontal(4).AlignCenter()
                                    .Text(pay.Ref).FontSize(8.5f);

                                payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                    .PaddingVertical(3).PaddingHorizontal(4).AlignCenter()
                                    .Text(MoneyBs(pay.AmountBs)).FontSize(8.5f);

                                payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                    .PaddingVertical(3).PaddingHorizontal(4).AlignCenter()
                                    .Text(Money(pay.AmountUsd)).FontSize(8.5f);

                                payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                    .PaddingVertical(3).PaddingHorizontal(4).AlignCenter()
                                    .Text(pay.Date).FontSize(8.5f);
                            }

                            payTable.Cell().ColumnSpan(2).Background(PrimaryColor).Border(0.5f).BorderColor(LightBorder)
                                .PaddingVertical(3).PaddingHorizontal(4).AlignCenter()
                                .Text("TOTAL COMISION").FontColor(Colors.White).Bold().FontSize(8.5f);
                            payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                .PaddingVertical(3).PaddingHorizontal(4).AlignCenter()
                                .Text(MoneyBs(total_pay_bs)).Bold().FontSize(8.5f);
                            payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                .PaddingVertical(3).PaddingHorizontal(4).AlignCenter()
                                .Text(Money(total_pay_usd)).Bold().FontSize(8.5f);
                            payTable.Cell().Border(0.5f).BorderColor(LightBorder)
                                .PaddingVertical(3).PaddingHorizontal(4)
                                .Text(string.Empty).FontSize(8.5f);
                        });
                    });
                });

                if (is_last && all_rows != null && all_rows.Count > 0)
                {
                    BuildGrandTotalBlock(col, all_rows, all_groups);
                }
            });
        }

        private static void BuildGrandTotalBlock(
            ColumnDescriptor sumCol,
            List<commission_row_dto> all_rows,
            List<IGrouping<string, commission_row_dto>>? all_groups)
        {
            if (all_groups != null && all_groups.Count > 1)
            {
                sumCol.Item().PaddingTop(10).Border(1.2f).BorderColor(PrimaryColor).Column(b =>
                {
                    b.Item().Background(PrimaryColor).PaddingVertical(3).PaddingHorizontal(6).Row(r =>
                    {
                        r.RelativeItem().Text("RESUMEN GENERAL DE COMISIONES").FontSize(8.5f).Bold().FontColor(Colors.White);
                        r.ConstantItem(180).AlignRight().Text($"Consolidado ({all_groups.Count} vendedoras)").FontSize(7.5f).FontColor(Colors.White);
                    });

                    b.Item().Padding(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.0f);   // VENDEDORA
                            columns.RelativeColumn(0.8f);   // NOTAS
                            columns.RelativeColumn(1.4f);   // FACTURADO
                            columns.RelativeColumn(1.4f);   // PAGADO
                            columns.RelativeColumn(1.4f);   // COMISION
                            columns.RelativeColumn(1.4f);   // PAGADA
                            columns.RelativeColumn(1.4f);   // PENDIENTE
                        });

                        table.Header(header =>
                        {
                            void h(string text, bool left = false)
                            {
                                var c = header.Cell().Background(AccentBg).Border(0.4f).BorderColor(LightBorder).PaddingVertical(2).PaddingHorizontal(2);
                                var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                                t.FontColor(PrimaryColor).Bold().FontSize(6.5f);
                            }

                            h("VENDEDORA", left: true);
                            h("NOTAS");
                            h("FACTURADO");
                            h("PAGADO");
                            h("COMISION");
                            h("PAGADA");
                            h("PENDIENTE");
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
                            var gRows = g.ToList();

                            int count = gRows.Count;
                            decimal facturado = gRows.Sum(r => r.invoiced_amount);
                            decimal pagado = gRows.Sum(r => r.paid_amount);
                            decimal comision = gRows.Sum(r => r.amount_usd);
                            decimal pagada = gRows.Where(r => r.remaining_amount_usd <= 0.005m).Sum(r => r.amount_usd);
                            decimal pendiente = gRows.Where(r => r.remaining_amount_usd > 0.005m).Sum(r => r.remaining_amount_usd);

                            grand_notas += count;
                            grand_facturado += facturado;
                            grand_pagado += pagado;
                            grand_comision += comision;
                            grand_pagada += pagada;
                            grand_pendiente += pendiente;

                            void cell(string text, bool left = false)
                            {
                                var c = table.Cell().Background(bg).Border(0.4f).BorderColor(LightBorder)
                                    .PaddingVertical(2).PaddingHorizontal(2);
                                var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                                t.FontSize(7.5f);
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
                            var c = table.Cell().Background("#E8F5E9").Border(0.5f).BorderColor(LightBorder)
                                .PaddingVertical(3).PaddingHorizontal(2);
                            var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                            t.FontSize(8f).Bold().FontColor(PrimaryColor);
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
            else
            {
                decimal grand_facturado = all_rows.Sum(r => r.invoiced_amount);
                decimal grand_pagado = all_rows.Sum(r => r.paid_amount);
                decimal grand_comision = all_rows.Sum(r => r.amount_usd);
                decimal grand_pagada = all_rows.Where(r => r.remaining_amount_usd <= 0.005m).Sum(r => r.amount_usd);
                decimal grand_pendiente = all_rows.Where(r => r.remaining_amount_usd > 0.005m).Sum(r => r.remaining_amount_usd);

                sumCol.Item().PaddingTop(10).Border(1.2f).BorderColor(PrimaryColor).Column(grand =>
                {
                    grand.Item().Background(PrimaryColor).Padding(3).AlignCenter()
                        .Text("TOTAL GENERAL DE COMISIONES").FontSize(9.5f).Bold().FontColor(Colors.White);

                    grand.Item().Padding(4).Row(r =>
                    {
                        r.RelativeItem().Text("TOTAL FACTURADO $").FontSize(9).Bold().FontColor("#000000");
                        r.ConstantItem(120).AlignRight().Text(Money(grand_facturado)).FontSize(10f).Bold();
                    });
                    grand.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                    grand.Item().Padding(4).Row(r =>
                    {
                        r.RelativeItem().Text("TOTAL COMISIÓN GENERADA").FontSize(9).Bold().FontColor(PrimaryColor);
                        r.ConstantItem(120).AlignRight().Text(Money(grand_comision)).FontSize(11.5f).Bold().FontColor(PrimaryColor);
                    });
                    grand.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                    grand.Item().Padding(4).Row(r =>
                    {
                        r.RelativeItem().Text("TOTAL COMISIÓN PAGADA").FontSize(8.5f).Bold().FontColor("#2E7D32");
                        r.ConstantItem(120).AlignRight().Text(Money(grand_pagada)).FontSize(10f).Bold().FontColor("#2E7D32");
                    });
                    grand.Item().PaddingHorizontal(4).PaddingBottom(4).Row(r =>
                    {
                        r.RelativeItem().Text("TOTAL COMISIÓN PENDIENTE").FontSize(8.5f).Bold().FontColor(grand_pendiente > 0.005m ? "#C62828" : "#000000");
                        r.ConstantItem(120).AlignRight().Text(Money(grand_pendiente)).FontSize(10.5f).Bold().FontColor(grand_pendiente > 0.005m ? "#C62828" : "#000000");
                    });
                });
            }
        }

        private static void SetupPage(PageDescriptor page, string period_cap)
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
                    row.RelativeItem().AlignRight().Text(period_cap).FontSize(10).Bold().FontColor("#000000");
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

        private static string Money(decimal value) => value.ToString("#,##0.00", Ve);

        private static string MoneyBs(decimal value) => "Bs " + value.ToString("#,##0.00", Ve);

        private static string DateDMon(DateTime? value)
        {
            if (value == null || value.Value == default) return string.Empty;
            return value.Value.ToString("dd-MMM", Ve).Replace(".", string.Empty).ToLowerInvariant();
        }

        private static string Capitalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return char.ToUpper(text[0]) + text.Substring(1);
        }
    }
}
