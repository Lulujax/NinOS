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
    public class CustomerHistoryPdfModel
    {
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Rif { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public string FiscalAddress { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string PeriodLabel { get; set; } = "Historial Completo";

        public decimal TotalInvoicedUsd { get; set; }
        public int TotalNotesCount { get; set; }
        public decimal TotalPaidUsd { get; set; }
        public decimal BalanceDueUsd { get; set; }
        public decimal TotalCreditNotesUsd { get; set; }
        public int TotalCreditNotesCount { get; set; }

        public List<accounts_receivable_dto> DeliveryNotes { get; set; } = new();
        public List<credit_note_dto> CreditNotes { get; set; } = new();
        public List<payment_dto> Payments { get; set; } = new();
        public List<CustomerHistoryProductDto> TopProducts { get; set; } = new();
    }

    public static class CustomerHistoryPdfGenerator
    {
        private static readonly string PrimaryColor = "#0D62B5";
        private static readonly string DarkText = "#1A1A1A";
        private static readonly string MutedText = "#555555";
        private static readonly string LightBorder = "#D0D7DE";
        private static readonly string TableHeaderBg = "#0D62B5";
        private static readonly string ZebraBg = "#F8FAFC";
        private static readonly string CardBg = "#F1F5F9";
        private static readonly string GreenColor = "#2E7D32";
        private static readonly string RedColor = "#C62828";
        private static readonly string PurpleColor = "#6A1B9A";
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        public static void generate(CustomerHistoryPdfModel model)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            string safeCustomer = SanitizeFileName(string.IsNullOrWhiteSpace(model.CustomerName) ? "CLIENTE" : model.CustomerName);
            string defaultName = $"HISTORIAL_{safeCustomer}_{DateTime.Now:yyyyMMdd}.pdf".ToUpperInvariant();

            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar historial del cliente en PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = defaultName
            };

            if (save_dialog.ShowDialog() != true) return;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginLeft(1, Unit.Centimetre);
                    page.MarginTop(1, Unit.Centimetre);
                    page.MarginRight(1, Unit.Centimetre);
                    page.MarginBottom(1, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(7.5f).FontColor(DarkText));

                    // Encabezado de pagina
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("HISTORIAL DE CLIENTE").FontSize(13).Bold().FontColor(PrimaryColor);
                                c.Item().PaddingTop(1).Text(model.CustomerName).FontSize(11).Bold().FontColor(DarkText);
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text($"EMISION: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).Bold().FontColor(MutedText);
                                c.Item().PaddingTop(1).Text($"PERIODO: {model.PeriodLabel.ToUpperInvariant()}").FontSize(8).Bold().FontColor(PrimaryColor);
                            });
                        });

                        col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(PrimaryColor);
                    });

                    // Pie de pagina
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text("NinOS - Sistema de Gestion").FontSize(7).FontColor(MutedText);
                        row.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span("Pagina ").FontSize(7).FontColor(MutedText);
                            t.CurrentPageNumber().FontSize(7).FontColor(MutedText);
                            t.Span(" de ").FontSize(7).FontColor(MutedText);
                            t.TotalPages().FontSize(7).FontColor(MutedText);
                        });
                    });

                    // Contenido
                    page.Content().PaddingVertical(6).Column(col =>
                    {
                        // 1. Ficha del Cliente
                        BuildCustomerInfoBox(col, model);

                        // 2. Tarjetas de Resumen KPI
                        BuildKpiSummary(col, model);

                        // 3. Notas de Entrega
                        BuildDeliveryNotesSection(col, model.DeliveryNotes);

                        // 4. Notas de Credito
                        if (model.CreditNotes != null && model.CreditNotes.Count > 0)
                        {
                            BuildCreditNotesSection(col, model.CreditNotes);
                        }

                        // 5. Cobranzas y Abonos
                        if (model.Payments != null && model.Payments.Count > 0)
                        {
                            BuildPaymentsSection(col, model.Payments);
                        }

                        // 6. Articulos Mas Comprados
                        if (model.TopProducts != null && model.TopProducts.Count > 0)
                        {
                            BuildTopProductsSection(col, model.TopProducts);
                        }
                    });
                });
            });

            document.GeneratePdf(save_dialog.FileName);
        }

        private static void BuildCustomerInfoBox(ColumnDescriptor col, CustomerHistoryPdfModel model)
        {
            col.Item().PaddingBottom(6).Border(1).BorderColor(LightBorder).Background(CardBg).Padding(6).Column(box =>
            {
                box.Item().Row(row =>
                {
                    row.RelativeItem(2).Column(c =>
                    {
                        c.Item().Text(t =>
                        {
                            t.Span("RIF: ").Bold();
                            t.Span(string.IsNullOrWhiteSpace(model.Rif) ? "-" : model.Rif);
                            t.Span("   |   CODIGO: ").Bold();
                            t.Span(string.IsNullOrWhiteSpace(model.CustomerCode) ? "-" : model.CustomerCode);
                        });
                        c.Item().PaddingTop(2).Text(t =>
                        {
                            t.Span("CONTACTO: ").Bold();
                            t.Span(string.IsNullOrWhiteSpace(model.ContactName) ? "-" : model.ContactName);
                            t.Span("   |   TEL: ").Bold();
                            t.Span(string.IsNullOrWhiteSpace(model.Phone) ? "-" : model.Phone);
                        });
                    });

                    row.RelativeItem(1.5f).AlignRight().Column(c =>
                    {
                        c.Item().Text(t =>
                        {
                            t.Span("ZONA: ").Bold();
                            t.Span(string.IsNullOrWhiteSpace(model.ZoneName) ? "-" : model.ZoneName).Bold().FontColor(PrimaryColor);
                        });
                    });
                });

                box.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(LightBorder);

                box.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("DIR. FISCAL: ").Bold();
                        t.Span(string.IsNullOrWhiteSpace(model.FiscalAddress) ? "-" : model.FiscalAddress);
                    });
                    row.RelativeItem().PaddingLeft(8).Text(t =>
                    {
                        t.Span("DIR. DESPACHO: ").Bold();
                        t.Span(string.IsNullOrWhiteSpace(model.DeliveryAddress) ? "-" : model.DeliveryAddress);
                    });
                });
            });
        }

        private static void BuildKpiSummary(ColumnDescriptor col, CustomerHistoryPdfModel model)
        {
            col.Item().PaddingBottom(8).Row(row =>
            {
                // Facturado
                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("TOTAL FACTURADO").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text($"${model.TotalInvoicedUsd:N2}").FontSize(11).Bold().FontColor(PrimaryColor);
                    c.Item().AlignCenter().Text($"{model.TotalNotesCount} notas").FontSize(6.5f).FontColor(MutedText);
                });

                row.Spacing(4);

                // Abonado
                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("TOTAL COBRADO").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text($"${model.TotalPaidUsd:N2}").FontSize(11).Bold().FontColor(GreenColor);
                    c.Item().AlignCenter().Text("Cobrado").FontSize(6.5f).FontColor(GreenColor);
                });

                row.Spacing(4);

                // Pendiente
                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("SALDO PENDIENTE").FontSize(7).Bold().FontColor(MutedText);
                    var balanceColor = model.BalanceDueUsd > 0 ? RedColor : GreenColor;
                    c.Item().AlignCenter().Text($"${model.BalanceDueUsd:N2}").FontSize(11).Bold().FontColor(balanceColor);
                    c.Item().AlignCenter().Text(model.BalanceDueUsd > 0 ? "Por Cobrar" : "Solvente").FontSize(6.5f).FontColor(balanceColor);
                });

                row.Spacing(4);

                // Notas de Credito
                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("NOTAS DE CREDITO").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text($"${model.TotalCreditNotesUsd:N2}").FontSize(11).Bold().FontColor(PurpleColor);
                    c.Item().AlignCenter().Text($"{model.TotalCreditNotesCount} NCs").FontSize(6.5f).FontColor(MutedText);
                });
            });
        }

        private static void BuildDeliveryNotesSection(ColumnDescriptor col, List<accounts_receivable_dto> notes)
        {
            col.Item().PaddingTop(4).PaddingBottom(2).Row(row =>
            {
                row.RelativeItem().Text("NOTAS DE ENTREGA").FontSize(9.5f).Bold().FontColor(PrimaryColor);
                row.RelativeItem().AlignRight().Text($"{notes.Count} documento(s)").FontSize(7.5f).FontColor(MutedText);
            });

            if (notes.Count == 0)
            {
                col.Item().PaddingBottom(8).Border(1).BorderColor(LightBorder).Padding(6).Text("No se registraron notas de entrega.").Italic().FontColor(MutedText);
                return;
            }

            col.Item().PaddingBottom(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.9f); // Fecha
                    columns.RelativeColumn(0.9f); // Despacho
                    columns.RelativeColumn(1.0f); // Nro Nota
                    columns.RelativeColumn(1.4f); // Vendedor
                    columns.RelativeColumn(0.8f); // Tipo
                    columns.RelativeColumn(1.0f); // Total $
                    columns.RelativeColumn(1.0f); // Abonado $
                    columns.RelativeColumn(1.0f); // Saldo $
                    columns.RelativeColumn(0.9f); // Estado
                });

                table.Header(header =>
                {
                    void h(string text, bool right = false)
                    {
                        var cell = header.Cell().Background(TableHeaderBg).PaddingVertical(2).PaddingHorizontal(2);
                        var t = right ? cell.AlignRight().Text(text) : cell.Text(text);
                        t.FontColor(Colors.White).Bold().FontSize(6.5f);
                    }

                    h("EMISION");
                    h("DESPACHO");
                    h("N NOTA");
                    h("VENDEDOR");
                    h("TIPO");
                    h("TOTAL ($)", right: true);
                    h("ABONADO ($)", right: true);
                    h("SALDO ($)", right: true);
                    h("ESTADO");
                });

                int idx = 0;
                foreach (var n in notes)
                {
                    string bg = (idx++ % 2 == 1) ? ZebraBg : "#FFFFFF";
                    bool isAnulada = string.Equals(n.status, "Anulada", StringComparison.OrdinalIgnoreCase);

                    void cellText(string text, bool right = false, string? customColor = null)
                    {
                        var cell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(LightBorder).PaddingVertical(1.8f).PaddingHorizontal(2);
                        var t = right ? cell.AlignRight().Text(text) : cell.Text(text);
                        t.FontSize(6.8f);
                        if (customColor != null) t.FontColor(customColor).Bold();
                        else if (isAnulada) t.FontColor(RedColor);
                    }

                    cellText(n.creation_date.ToString("dd/MM/yyyy"));
                    cellText(n.dispatch_date.HasValue ? n.dispatch_date.Value.ToString("dd/MM/yyyy") : "-");
                    cellText(n.note_number);
                    cellText(string.IsNullOrWhiteSpace(n.seller_name) ? "-" : n.seller_name);
                    cellText(string.IsNullOrWhiteSpace(n.note_type_name) ? "GEN" : n.note_type_name);
                    cellText(n.total_amount_usd.ToString("N2", Ve), right: true);
                    cellText(n.paid_amount_usd.ToString("N2", Ve), right: true, customColor: GreenColor);
                    cellText(n.balance_due_usd.ToString("N2", Ve), right: true, customColor: n.balance_due_usd > 0 ? RedColor : null);
                    cellText(n.status, customColor: isAnulada ? RedColor : null);
                }

                // Fila de totales
                var validNotes = notes.Where(x => !string.Equals(x.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase)).ToList();
                table.Cell().ColumnSpan(5).Background(CardBg).PaddingVertical(2).PaddingHorizontal(3).AlignRight().Text("TOTALES:").Bold().FontSize(7);
                table.Cell().Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).AlignRight().Text(validNotes.Sum(x => x.total_amount_usd).ToString("N2", Ve)).Bold().FontSize(7);
                table.Cell().Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).AlignRight().Text(validNotes.Sum(x => x.paid_amount_usd).ToString("N2", Ve)).Bold().FontSize(7).FontColor(GreenColor);
                table.Cell().Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).AlignRight().Text(validNotes.Sum(x => x.balance_due_usd).ToString("N2", Ve)).Bold().FontSize(7).FontColor(RedColor);
                table.Cell().Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).Text("");
            });
        }

        private static void BuildCreditNotesSection(ColumnDescriptor col, List<credit_note_dto> cns)
        {
            col.Item().PaddingTop(4).PaddingBottom(2).Row(row =>
            {
                row.RelativeItem().Text("NOTAS DE CREDITO / DEVOLUCIONES").FontSize(9.5f).Bold().FontColor(PurpleColor);
                row.RelativeItem().AlignRight().Text($"{cns.Count} registro(s)").FontSize(7.5f).FontColor(MutedText);
            });

            col.Item().PaddingBottom(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.9f); // Fecha
                    columns.RelativeColumn(1.0f); // Nro NC
                    columns.RelativeColumn(1.1f); // Nota Origen
                    columns.RelativeColumn(1.1f); // Categoria
                    columns.RelativeColumn(1.4f); // Vendedor
                    columns.RelativeColumn(1.2f); // Total $
                    columns.RelativeColumn(1.0f); // Estado
                });

                table.Header(header =>
                {
                    void h(string text, bool right = false)
                    {
                        var cell = header.Cell().Background(PurpleColor).PaddingVertical(2).PaddingHorizontal(2);
                        var t = right ? cell.AlignRight().Text(text) : cell.Text(text);
                        t.FontColor(Colors.White).Bold().FontSize(6.5f);
                    }

                    h("FECHA");
                    h("N NC");
                    h("NOTA ORIGEN");
                    h("CATEGORIA");
                    h("VENDEDOR");
                    h("TOTAL ($)", right: true);
                    h("ESTADO");
                });

                int idx = 0;
                foreach (var c in cns)
                {
                    string bg = (idx++ % 2 == 1) ? ZebraBg : "#FFFFFF";

                    void cellText(string text, bool right = false, string? customColor = null)
                    {
                        var cell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(LightBorder).PaddingVertical(1.8f).PaddingHorizontal(2);
                        var t = right ? cell.AlignRight().Text(text) : cell.Text(text);
                        t.FontSize(6.8f);
                        if (customColor != null) t.FontColor(customColor).Bold();
                    }

                    cellText(c.creation_date.ToString("dd/MM/yyyy"));
                    cellText(c.note_number);
                    cellText(string.IsNullOrWhiteSpace(c.source_note_number) ? "-" : c.source_note_number);
                    cellText(string.IsNullOrWhiteSpace(c.category) ? "Credito" : c.category);
                    cellText(string.IsNullOrWhiteSpace(c.seller_name) ? "-" : c.seller_name);
                    cellText(c.total_amount_usd.ToString("N2", Ve), right: true, customColor: PurpleColor);
                    cellText(string.IsNullOrWhiteSpace(c.status) ? "Emitida" : c.status);
                }

                // Totales
                var validCns = cns.Where(x => !string.Equals(x.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase)).ToList();
                table.Cell().ColumnSpan(5).Background(CardBg).PaddingVertical(2).PaddingHorizontal(3).AlignRight().Text("TOTAL NOTAS DE CREDITO:").Bold().FontSize(7);
                table.Cell().Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).AlignRight().Text(validCns.Sum(x => x.total_amount_usd).ToString("N2", Ve)).Bold().FontSize(7).FontColor(PurpleColor);
                table.Cell().Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).Text("");
            });
        }

        private static void BuildPaymentsSection(ColumnDescriptor col, List<payment_dto> payments)
        {
            col.Item().PaddingTop(4).PaddingBottom(2).Row(row =>
            {
                row.RelativeItem().Text("HISTORIAL DE PAGOS Y ABONOS").FontSize(9.5f).Bold().FontColor(GreenColor);
                row.RelativeItem().AlignRight().Text($"{payments.Count} pago(s)").FontSize(7.5f).FontColor(MutedText);
            });

            col.Item().PaddingBottom(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.9f); // Fecha
                    columns.RelativeColumn(1.0f); // Nro Nota
                    columns.RelativeColumn(1.3f); // Forma de pago
                    columns.RelativeColumn(1.4f); // Referencia
                    columns.RelativeColumn(1.1f); // Monto $
                    columns.RelativeColumn(1.1f); // Monto Bs
                    columns.RelativeColumn(0.9f); // Tasa
                    columns.RelativeColumn(1.3f); // Observaciones
                });

                table.Header(header =>
                {
                    void h(string text, bool right = false)
                    {
                        var cell = header.Cell().Background(GreenColor).PaddingVertical(2).PaddingHorizontal(2);
                        var t = right ? cell.AlignRight().Text(text) : cell.Text(text);
                        t.FontColor(Colors.White).Bold().FontSize(6.5f);
                    }

                    h("FECHA");
                    h("N NOTA");
                    h("METODO");
                    h("REFERENCIA");
                    h("MONTO ($)", right: true);
                    h("MONTO BS", right: true);
                    h("TASA (BS/$)", right: true);
                    h("OBSERVACIONES");
                });

                int idx = 0;
                foreach (var p in payments)
                {
                    string bg = (idx++ % 2 == 1) ? ZebraBg : "#FFFFFF";

                    void cellText(string text, bool right = false, string? customColor = null)
                    {
                        var cell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(LightBorder).PaddingVertical(1.8f).PaddingHorizontal(2);
                        var t = right ? cell.AlignRight().Text(text) : cell.Text(text);
                        t.FontSize(6.8f);
                        if (customColor != null) t.FontColor(customColor).Bold();
                    }

                    cellText(p.payment_date.ToString("dd/MM/yyyy"));
                    cellText(string.IsNullOrWhiteSpace(p.note_number) ? "-" : p.note_number);
                    cellText(string.IsNullOrWhiteSpace(p.payment_type) ? "-" : p.payment_type);
                    cellText(string.IsNullOrWhiteSpace(p.reference_number) ? "-" : p.reference_number);
                    cellText(p.amount_usd.ToString("N2", Ve), right: true, customColor: GreenColor);
                    cellText(p.amount_bs > 0 ? p.amount_bs.ToString("N2", Ve) : "-", right: true);
                    cellText(p.exchange_rate.HasValue ? p.exchange_rate.Value.ToString("N2", Ve) : "-", right: true);
                    cellText(string.IsNullOrWhiteSpace(p.notes) ? "-" : p.notes);
                }

                // Totales
                var validPayments = payments.Where(p => p.amount_usd > 0).ToList();
                table.Cell().ColumnSpan(4).Background(CardBg).PaddingVertical(2).PaddingHorizontal(3).AlignRight().Text("TOTAL ABONADO ($):").Bold().FontSize(7);
                table.Cell().Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).AlignRight().Text(validPayments.Sum(x => x.amount_usd).ToString("N2", Ve)).Bold().FontSize(7).FontColor(GreenColor);
                table.Cell().Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).AlignRight().Text(validPayments.Sum(x => x.amount_bs).ToString("N2", Ve)).Bold().FontSize(7);
                table.Cell().ColumnSpan(2).Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).Text("");
            });
        }

        private static void BuildTopProductsSection(ColumnDescriptor col, List<CustomerHistoryProductDto> prods)
        {
            col.Item().PaddingTop(4).PaddingBottom(2).Row(row =>
            {
                row.RelativeItem().Text("ARTICULOS MAS COMPRADOS").FontSize(9.5f).Bold().FontColor("#E65100");
                row.RelativeItem().AlignRight().Text($"{prods.Count} producto(s)").FontSize(7.5f).FontColor(MutedText);
            });

            col.Item().PaddingBottom(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.0f); // Codigo
                    columns.RelativeColumn(3.2f); // Producto
                    columns.RelativeColumn(0.9f); // Cantidad
                    columns.RelativeColumn(1.2f); // Ultimo Precio $
                    columns.RelativeColumn(1.3f); // Total Facturado $
                    columns.RelativeColumn(1.0f); // Ultima Compra
                });

                table.Header(header =>
                {
                    void h(string text, bool right = false)
                    {
                        var cell = header.Cell().Background("#E65100").PaddingVertical(2).PaddingHorizontal(2);
                        var t = right ? cell.AlignRight().Text(text) : cell.Text(text);
                        t.FontColor(Colors.White).Bold().FontSize(6.5f);
                    }

                    h("CODIGO");
                    h("PRODUCTO");
                    h("CANT.", right: true);
                    h("ULT. PRECIO ($)", right: true);
                    h("TOTAL ($)", right: true);
                    h("ULT. COMPRA");
                });

                int idx = 0;
                foreach (var p in prods)
                {
                    string bg = (idx++ % 2 == 1) ? ZebraBg : "#FFFFFF";

                    void cellText(string text, bool right = false, string? customColor = null)
                    {
                        var cell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(LightBorder).PaddingVertical(1.8f).PaddingHorizontal(2);
                        var t = right ? cell.AlignRight().Text(text) : cell.Text(text);
                        t.FontSize(6.8f);
                        if (customColor != null) t.FontColor(customColor).Bold();
                    }

                    cellText(p.ProductCode);
                    cellText(p.ProductName);
                    cellText(p.TotalQuantity.ToString(), right: true);
                    cellText(p.LastPriceUsd.ToString("N2", Ve), right: true);
                    cellText(p.TotalAmountUsd.ToString("N2", Ve), right: true, customColor: PrimaryColor);
                    cellText(p.LastPurchaseDate != DateTime.MinValue ? p.LastPurchaseDate.ToString("dd/MM/yyyy") : "-");
                }

                // Total
                table.Cell().ColumnSpan(2).Background(CardBg).PaddingVertical(2).PaddingHorizontal(3).AlignRight().Text("TOTALES:").Bold().FontSize(7);
                table.Cell().Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).AlignRight().Text(prods.Sum(x => x.TotalQuantity).ToString()).Bold().FontSize(7);
                table.Cell().Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).Text("");
                table.Cell().Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).AlignRight().Text(prods.Sum(x => x.TotalAmountUsd).ToString("N2", Ve)).Bold().FontSize(7).FontColor(PrimaryColor);
                table.Cell().Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).Text("");
            });
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            return cleaned.Replace(' ', '_');
        }
    }
}
