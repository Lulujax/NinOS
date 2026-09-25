using Microsoft.Win32;
using NinOS.Domain.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;

namespace NinOS.UI.Common
{
    public static class NotePdfGenerator
    {
        private static string PrimaryColor = "#1B3A2D";
        private static readonly string LightBorder = "#B0B0B0";
        private static string AccentBg = "#F0F4EC";

        public static void generate(note_print_dto note)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            PrimaryColor = string.IsNullOrWhiteSpace(note.accent_color) ? "#1B3A2D" : note.accent_color;
            AccentBg = string.IsNullOrWhiteSpace(note.accent_soft_color) ? "#F0F4EC" : note.accent_soft_color;

            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar nota de entrega",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"NotaEntrega_{note.note_number}.pdf"
            };

            if (save_dialog.ShowDialog() != true) return;

            generate_to_file(note, save_dialog.FileName);
        }

        public static void generate_to_file(note_print_dto note, string output_path)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            PrimaryColor = string.IsNullOrWhiteSpace(note.accent_color) ? "#1B3A2D" : note.accent_color;
            AccentBg = string.IsNullOrWhiteSpace(note.accent_soft_color) ? "#F0F4EC" : note.accent_soft_color;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginLeft(1, Unit.Centimetre);
                    page.MarginTop(1, Unit.Centimetre);
                    page.MarginRight(1, Unit.Centimetre);
                    page.MarginBottom(1, Unit.Centimetre);
                    page.Header().Column(col =>
                    {
                        if (note.is_promo && !string.IsNullOrWhiteSpace(note.promo_banner_text))
                        {
                            col.Item().PaddingBottom(4).Border(0.8f).BorderColor(PrimaryColor).PaddingHorizontal(6).PaddingVertical(2).AlignCenter()
                                .Text(note.promo_banner_text).FontSize(14).Bold().FontColor(PrimaryColor);
                        }

                        col.Item().Row(row =>
                        {
                            row.RelativeItem(3).Column(left =>
                            {
                                left.Item().Text(string.IsNullOrWhiteSpace(note.header_title) ? note.company_name : note.header_title).FontSize(13).Bold().FontColor(PrimaryColor);
                                left.Item().Text("Caracas - Venezuela").FontSize(10).FontColor("#555555");
                            });

                            row.RelativeItem(2).Column(right =>
                            {
                                right.Item().AlignRight().Text(note.document_label).FontSize(16).Bold().FontColor(PrimaryColor);
                                right.Item().PaddingTop(2).AlignRight().Text($"Nro: {note.note_number}").FontSize(11).Bold();
                            });
                        });

                        col.Item().PaddingTop(3).LineHorizontal(1.5f).LineColor(PrimaryColor);
                    });

                    page.Content().PaddingVertical(2).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("VENDEDOR:").FontSize(8).Bold().FontColor("#555555");
                                c.Item().PaddingTop(1).Text(note.seller_name).FontSize(10).Bold();
                            });
                        });

                        col.Item().PaddingTop(2).Border(0.5f).BorderColor(LightBorder).Padding(2).Column(grid =>
                        {
                            grid.Item().Row(r =>
                            {
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Razon Social").FontSize(6f).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.customer_business_name).FontSize(8);
                                });
                                r.ConstantItem(80).Column(c =>
                                {
                                    c.Item().Text("Codigo").FontSize(6f).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.customer_code).FontSize(8);
                                });
                                r.ConstantItem(120).Column(c =>
                                {
                                    c.Item().Text("RIF").FontSize(6f).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.customer_rif).FontSize(8);
                                });
                            });

                            grid.Item().PaddingTop(1).LineHorizontal(0.25f).LineColor("#DDDDDD");

                            grid.Item().PaddingTop(1).Row(r =>
                            {
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Domicilio Fiscal").FontSize(6f).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.fiscal_address).FontSize(8);
                                });
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Direccion de Entrega").FontSize(6f).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.customer_delivery_address).FontSize(8);
                                });
                            });

                            grid.Item().PaddingTop(1).LineHorizontal(0.25f).LineColor("#DDDDDD");

                            grid.Item().PaddingTop(1).Row(r =>
                            {
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Fecha Emision").FontSize(6f).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.creation_date.ToString("dd/MM/yyyy")).FontSize(8);
                                });
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Fecha Vencimiento").FontSize(6f).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.due_date.ToString("dd/MM/yyyy")).FontSize(8);
                                });
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Telefono").FontSize(6f).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.customer_phone).FontSize(8);
                                });
                            });

                            bool has_contacto = !string.IsNullOrWhiteSpace(note.customer_contact);
                            bool has_credit_days = !string.IsNullOrWhiteSpace(note.credit_days_text) && !note.is_promo;

                            if (has_contacto || has_credit_days)
                            {
                                grid.Item().PaddingTop(1).LineHorizontal(0.25f).LineColor("#DDDDDD");

                                grid.Item().PaddingTop(1).Row(r =>
                                {
                                    if (has_contacto)
                                    {
                                        r.RelativeItem().Column(c =>
                                        {
                                            c.Item().Text("Contacto").FontSize(6f).Bold().FontColor("#555555");
                                            c.Item().PaddingTop(1).Text(note.customer_contact).FontSize(8);
                                        });
                                    }
                                    if (has_credit_days)
                                    {
                                        if (has_contacto) r.ConstantItem(10);
                                        r.RelativeItem().Column(c =>
                                        {
                                            c.Item().Text("Dias de Credito").FontSize(6f).Bold().FontColor("#555555");
                                            c.Item().PaddingTop(1).Text(note.credit_days_text).FontSize(8);
                                        });
                                    }
                                });
                            }
                        });

                        col.Item().PaddingTop(2).Table(table =>
                        {
                            bool is_promo_table = note.is_promo;

                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1.2f);
                                if (is_promo_table)
                                    columns.RelativeColumn(1.1f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(PrimaryColor).Padding(0.8f).AlignCenter().Text("CANT.").FontColor(Colors.White).Bold().FontSize(9.5f);
                                header.Cell().Background(PrimaryColor).Padding(0.8f).Text("CODIGO").FontColor(Colors.White).Bold().FontSize(9.5f);
                                header.Cell().Background(PrimaryColor).Padding(0.8f).Text("DESCRIPCION").FontColor(Colors.White).Bold().FontSize(9.5f);
                                header.Cell().Background(PrimaryColor).Padding(0.8f).AlignCenter().Text("PRECIO U.").FontColor(Colors.White).Bold().FontSize(9.5f);
                                if (is_promo_table)
                                    header.Cell().Background(PrimaryColor).Padding(0.8f).AlignCenter().Text($"DESCUENTO {note.promo_discount_percentage ?? 0:0.##}%").FontColor(Colors.White).Bold().FontSize(9.5f);
                                header.Cell().Background(PrimaryColor).Padding(0.8f).AlignCenter().Text("PRECIO P.").FontColor(Colors.White).Bold().FontSize(9.5f);
                                header.Cell().Background(PrimaryColor).Padding(0.8f).AlignCenter().Text("SUBTOTAL").FontColor(Colors.White).Bold().FontSize(9.5f);
                            });

                            bool alternate = false;
                            foreach (var d in note.details)
                            {
                                string bg = alternate ? AccentBg : Colors.White;
                                table.Cell().Background(bg).Padding(0.8f).AlignCenter().Text(d.quantity.ToString()).FontSize(9.5f);
                                table.Cell().Background(bg).Padding(0.8f).Text(d.code).FontSize(9.5f);
                                table.Cell().Background(bg).Padding(0.8f).Text(d.name).FontSize(9.5f);
                                table.Cell().Background(bg).Padding(0.8f).AlignCenter().Text(d.unit_price_usd.ToString("N2")).FontSize(9.5f);
                                if (is_promo_table)
                                    table.Cell().Background(bg).Padding(0.8f).AlignCenter().Text(d.discount_usd.ToString("N2")).FontSize(9.5f);
                                table.Cell().Background(bg).Padding(0.8f).AlignCenter().Text(d.promo_price_usd.ToString("N2")).FontSize(9.5f);
                                table.Cell().Background(bg).Padding(0.8f).AlignCenter().Text(d.subtotal_usd.ToString("N2")).FontSize(9.5f);
                                alternate = !alternate;
                            }
                        });

                        col.Item().ShowEntire().Column(inner =>
                        {
                            // ---- Fila 1: condicion de pago + primer Total General ----
                            inner.Item().PaddingTop(2).Row(row =>
                            {
                                if (!string.IsNullOrWhiteSpace(note.conditions_text))
                                {
                                    row.AutoItem().Border(0.5f).BorderColor(LightBorder).Padding(3).Column(cond =>
                                    {
                                        cond.Item().Text(note.conditions_text).FontSize(8).Bold().Italic();
                                    });
                                }

                                row.RelativeItem();

                                row.ConstantItem(130).Border(0.5f).BorderColor(LightBorder).Padding(3).Column(tg =>
                                {
                                    tg.Item().AlignCenter().Text("Total General").FontSize(8).Bold();
                                    tg.Item().AlignCenter().Text($"{note.gross_total_usd:N2}").FontSize(12).Bold().FontColor(PrimaryColor);
                                });
                            });

                            // ---- Fila 2: DATOS PARA PAGO (3 columnas) ----
                            inner.Item().PaddingTop(2).Border(0.5f).BorderColor(LightBorder).Column(pay =>
                            {
                                pay.Item().Column(bank_header =>
                                {
                                    bank_header.Item().Background(PrimaryColor).Padding(2).AlignCenter().Text("FORMAS DE PAGO").FontSize(9).Bold().FontColor(Colors.White);

                                    bank_header.Item().PaddingTop(1).Row(r =>
                                    {
                                        r.RelativeItem().Column(c =>
                                        {
                                            c.Item().Text("TRANSFERENCIA").FontSize(7).Bold().FontColor(PrimaryColor);
                                            c.Item().PaddingTop(1).Text(note.is_pro_venta ? "BANCO VENEZUELA  _  CUENTA CORRIENTE" : "BANCO MERCANTIL  _  CUENTA CORRIENTE").FontSize(7);
                                            c.Item().PaddingTop(1).Text(note.is_pro_venta ? "NRO DE CUENTA  _  0102-0868-84-00000-27-407" : "NRO DE CUENTA  _  0105-0120-23-11200-92426").FontSize(7);
                                            c.Item().PaddingTop(1).Text(note.is_pro_venta ? "CEDULA  _  6.266.986" : "CEDULA  _  13.046.042").FontSize(7);
                                        });

                                        r.ConstantItem(16);

                                        r.RelativeItem().Column(c =>
                                        {
                                            c.Item().Text("PAGO MOVIL").FontSize(7).Bold().FontColor(PrimaryColor);
                                            c.Item().PaddingTop(1).Text(note.is_pro_venta ? "BANCO VENEZUELA" : "BANCO MERCANTIL").FontSize(7);
                                            c.Item().PaddingTop(1).Text(note.is_pro_venta ? "NRO TELEFONO  _  0414.598.68.65" : "NRO TELEFONO  _  0424.496.01.02").FontSize(7);
                                            c.Item().PaddingTop(1).Text(note.is_pro_venta ? "CEDULA  _  6.266.986" : "CEDULA  _  13.046.042").FontSize(7);
                                        });
                                    });

                                    bank_header.Item().PaddingTop(1).LineHorizontal(0.5f).LineColor(LightBorder);
                                });

                                pay.Item().PaddingTop(1).Row(row =>
                                {
                                    if (!note.is_promo)
                                    {
                                        row.ConstantItem(190).Border(0.5f).BorderColor(LightBorder).Padding(3).Column(left =>
                                        {
                                            if (!string.IsNullOrWhiteSpace(note.discount_conditions_text))
                                            {
                                                left.Item().Row(r =>
                                                {
                                                    r.RelativeItem().Text(note.discount_conditions_text).FontSize(7).Bold().AlignCenter();
                                                });
                                            }

                                            left.Item().PaddingTop(2).Row(r =>
                                            {
                                                r.RelativeItem().Text("sub total").FontSize(8).Bold();
                                                r.RelativeItem().AlignRight().Text($"{note.gross_total_usd:N2}").FontSize(8).Bold().FontColor(PrimaryColor);
                                            });

                                            if (note.promo_discount_amount > 0)
                                            {
                                                left.Item().PaddingTop(1).Row(r =>
                                                {
                                                    r.RelativeItem().Text($"{note.promo_discount_percentage:0.##}% PROMO").FontSize(8);
                                                    r.RelativeItem().AlignRight().Text($"-{note.promo_discount_amount:N2}").FontSize(8).FontColor("#CC0000");
                                                });
                                            }

                                            left.Item().PaddingTop(1).Row(r =>
                                            {
                                                r.RelativeItem().Text($"{note.discount_percentage:0.##}%").FontSize(8);
                                                r.RelativeItem().AlignRight().Text(note.discount_amount > 0 ? $"-{note.discount_amount:N2}" : $"{note.discount_amount:N2}").FontSize(8).FontColor(note.discount_amount > 0 ? "#CC0000" : "#666666");
                                            });

                                            left.Item().PaddingTop(2).Row(r =>
                                            {
                                                r.RelativeItem().Text("Total General").FontSize(9).Bold();
                                                r.RelativeItem().AlignRight().Text($"{note.discounted_total_usd:N2}").FontSize(9).Bold().FontColor(PrimaryColor);
                                            });
                                        });

                                        row.ConstantItem(8);
                                    }

                                    row.ConstantItem(200).Border(0.5f).BorderColor(LightBorder).Padding(3).Column(manual =>
                                    {
                                        manual.Item().Text("DATOS PARA PAGO").FontSize(7).Bold().FontColor(PrimaryColor).AlignCenter();
                                        manual.Item().PaddingTop(3).Row(r =>
                                        {
                                            r.RelativeItem().Text("Fecha de pago:").FontSize(7);
                                            r.RelativeItem().AlignRight().Text("____________________").FontSize(7);
                                        });
                                        manual.Item().PaddingTop(2).Row(r =>
                                        {
                                            r.RelativeItem().Text("Monto Bs:").FontSize(7);
                                            r.RelativeItem().AlignRight().Text("____________________").FontSize(7);
                                        });
                                        manual.Item().PaddingTop(2).Row(r =>
                                        {
                                            r.RelativeItem().Text("Nro Referencia:").FontSize(7);
                                            r.RelativeItem().AlignRight().Text("____________________").FontSize(7);
                                        });
                                        manual.Item().PaddingTop(2).Row(r =>
                                        {
                                            r.RelativeItem().Text("Banco:").FontSize(7);
                                            r.RelativeItem().AlignRight().Text("____________________").FontSize(7);
                                        });
                                        manual.Item().PaddingTop(2).Row(r =>
                                        {
                                            r.RelativeItem().Text("Equivalente a:").FontSize(7);
                                            r.RelativeItem().AlignRight().Text("____________________").FontSize(7);
                                        });
                                    });

                                    row.ConstantItem(8);

                                    if (note.is_promo)
                                    {
                                        row.RelativeItem();
                                    }
                                    else
                                    {
                                        row.RelativeItem().PaddingTop(2).Column(mid =>
                                        {
                                            mid.Item().AlignCenter().Text("FECHA _ FIRMA Y SELLO DEL CLIENTE").FontSize(7).Italic().FontColor("#555555");
                                            mid.Item().PaddingTop(30).LineHorizontal(0.5f).LineColor(LightBorder);
                                        });
                                    }
                                });
                            });

                            if (note.is_promo)
                            {
                                inner.Item().PaddingTop(3).Column(firma =>
                                {
                                    firma.Item().AlignCenter().Text("FECHA _ FIRMA Y SELLO DEL CLIENTE").FontSize(7).Italic().FontColor("#555555");
                                    firma.Item().PaddingTop(35).LineHorizontal(0.5f).LineColor(LightBorder);
                                });
                            }

                            // ---- Fila 3: Descuento por volumen (cuadro independiente, solo notas generales) ----
                            if (!note.is_promo)
                            {
                                inner.Item().PaddingTop(2).Row(tmp =>
                                {
                                    tmp.ConstantItem(190).Border(0.5f).BorderColor(LightBorder).Padding(3).Column(vol =>
                                    {
                                        vol.Item().Row(r =>
                                        {
                                            r.RelativeItem().Text($"Descuento por volumen {note.volume_discount_percentage:0.##}%")
                                                .FontSize(9).Bold()
                                                .FontColor(note.volume_discount_amount > 0 ? Colors.Black : "#666666");

                                            r.RelativeItem().AlignRight()
                                                .Text(note.volume_discount_amount > 0
                                                    ? $"-{note.volume_discount_amount:N2}"
                                                    : $"{note.volume_discount_amount:N2}")
                                                .FontSize(11).Bold()
                                                .FontColor(note.volume_discount_amount > 0 ? "#CC0000" : "#666666");
                                        });

                                        vol.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(LightBorder);

                                        vol.Item().PaddingTop(2).Row(r =>
                                        {
                                            r.RelativeItem().Text("TOTAL A PAGAR").FontSize(12).Bold();
                                            r.RelativeItem().AlignRight()
                                                .Text($"{note.total_amount_usd:N2}")
                                                .FontSize(12).Bold().FontColor(PrimaryColor);
                                        });
                                    });

                                    tmp.ConstantItem(8);
                                    tmp.RelativeItem();
                                    tmp.ConstantItem(8);
                                    tmp.ConstantItem(200);
                                });
                            }
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                        col.Item().PaddingTop(1).Row(row =>
                        {
                            row.RelativeItem().Text($"Nota: {note.note_number}").FontSize(7).FontColor("#888888");
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

            document.GeneratePdf(output_path);
        }
    }
}