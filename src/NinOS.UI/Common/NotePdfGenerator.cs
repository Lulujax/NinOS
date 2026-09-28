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
        private static readonly string LightBorder = "#000000";

        public static void generate(note_print_dto note)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            bool is_credit = string.Equals(note.document_label, "NOTA DE CREDITO", System.StringComparison.OrdinalIgnoreCase);
            var save_dialog = new SaveFileDialog
            {
                Title = is_credit ? "Guardar nota de credito" : "Guardar nota de entrega",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = is_credit ? $"NotaCredito_{note.note_number}.pdf" : $"NotaEntrega_{note.note_number}.pdf"
            };

            if (save_dialog.ShowDialog() != true) return;

            generate_to_file(note, save_dialog.FileName);
        }

        public static void generate(note_print_dto delivery_note, note_print_dto credit_note)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar nota de entrega con devolucion",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"NotaEntrega_{delivery_note.note_number}_NotaCredito_{credit_note.note_number}.pdf"
            };

            if (save_dialog.ShowDialog() != true) return;

            generate_to_file(delivery_note, credit_note, save_dialog.FileName);
        }

        public static void generate_to_file(note_print_dto note, string output_path)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    BuildNotePage(page, note);
                });
            });

            document.GeneratePdf(output_path);
        }

        public static void generate_to_file(note_print_dto delivery_note, note_print_dto credit_note, string output_path)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    BuildNotePage(page, delivery_note);
                });
                container.Page(page =>
                {
                    BuildNotePage(page, credit_note);
                });
            });

            document.GeneratePdf(output_path);
        }

        private static void BuildNotePage(PageDescriptor page, note_print_dto note)
        {
            string primary = string.IsNullOrWhiteSpace(note.accent_color) ? "#1B3A2D" : note.accent_color;
            string accent = string.IsNullOrWhiteSpace(note.accent_soft_color) ? "#F0F4EC" : note.accent_soft_color;
            bool is_credit = string.Equals(note.document_label, "NOTA DE CREDITO", StringComparison.OrdinalIgnoreCase);

            page.Size(PageSizes.Letter);
            page.MarginLeft(0.7f, Unit.Centimetre);
            page.MarginTop(0.7f, Unit.Centimetre);
            page.MarginRight(0.7f, Unit.Centimetre);
            page.MarginBottom(0.7f, Unit.Centimetre);
            page.Header().Column(col =>
            {
                if (note.is_promo)
                {
                    col.Item().PaddingBottom(2).Border(0.8f).BorderColor(primary).PaddingHorizontal(6).PaddingVertical(1).AlignCenter()
                        .Text("PROMOCION").FontSize(12).Bold().FontColor(primary);
                }

                col.Item().Row(row =>
                {
                    row.RelativeItem(3).Column(left =>
                    {
                        left.Item().Text(string.IsNullOrWhiteSpace(note.header_title) ? note.company_name : note.header_title).FontSize(12).Bold().FontColor(primary);
                        left.Item().Text("Caracas - Venezuela").FontSize(9).FontColor("#000000");
                    });

                    row.RelativeItem(2).Column(right =>
                    {
                        right.Item().AlignRight().Text(note.document_label).FontSize(15).Bold().FontColor(primary);
                        right.Item().PaddingTop(2).AlignRight().Text($"Nro: {note.note_number}").FontSize(10.5f).Bold();
                    });
                });

                col.Item().PaddingTop(2).LineHorizontal(1.5f).LineColor(primary);
            });

            page.Content().PaddingVertical(0).Column(col =>
            {
                col.Item().PaddingTop(2).Border(0.5f).BorderColor(LightBorder).Padding(1).Column(grid =>
                {
                    grid.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Razon Social").FontSize(5.5f).Bold().FontColor("#000000");
                            c.Item().PaddingTop(0.5f).Text(note.customer_business_name).FontSize(7.5f);
                        });
                        r.ConstantItem(80).Column(c =>
                        {
                            c.Item().Text("Codigo").FontSize(5.5f).Bold().FontColor("#000000");
                            c.Item().PaddingTop(0.5f).Text(note.customer_code).FontSize(7.5f);
                        });
                        r.ConstantItem(120).Column(c =>
                        {
                            c.Item().Text("RIF").FontSize(5.5f).Bold().FontColor("#000000");
                            c.Item().PaddingTop(0.5f).Text(note.customer_rif).FontSize(7.5f);
                        });
                    });

                    grid.Item().PaddingTop(0.5f).LineHorizontal(0.25f).LineColor("#000000");

                    grid.Item().PaddingTop(1).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Domicilio Fiscal").FontSize(5.5f).Bold().FontColor("#000000");
                            c.Item().PaddingTop(0.5f).Text(note.fiscal_address).FontSize(7.5f);
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Direccion de Entrega").FontSize(5.5f).Bold().FontColor("#000000");
                            c.Item().PaddingTop(0.5f).Text(note.customer_delivery_address).FontSize(7.5f);
                        });
                    });

                    grid.Item().PaddingTop(0.5f).LineHorizontal(0.25f).LineColor("#000000");

                    grid.Item().PaddingTop(1).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Fecha Emision").FontSize(5.5f).Bold().FontColor("#000000");
                            c.Item().PaddingTop(0.5f).Text(note.creation_date.ToString("dd/MM/yyyy")).FontSize(7.5f);
                        });
                        if (is_credit)
                        {
                            r.RelativeItem();
                        }
                        else
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Fecha Vencimiento").FontSize(5.5f).Bold().FontColor("#000000");
                                c.Item().PaddingTop(0.5f).Text(note.due_date.ToString("dd/MM/yyyy")).FontSize(7.5f);
                            });
                        }
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Telefono").FontSize(5.5f).Bold().FontColor("#000000");
                            c.Item().PaddingTop(0.5f).Text(note.customer_phone).FontSize(7.5f);
                        });
                    });

                    bool has_contacto = !string.IsNullOrWhiteSpace(note.customer_contact);
                    bool has_credit_days = !string.IsNullOrWhiteSpace(note.credit_days_text) && !note.is_promo;

                    if (has_contacto || has_credit_days)
                    {
                        grid.Item().PaddingTop(0.5f).LineHorizontal(0.25f).LineColor("#000000");

                        grid.Item().PaddingTop(1).Row(r =>
                        {
                            if (has_contacto)
                            {
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Contacto").FontSize(5.5f).Bold().FontColor("#000000");
                                    c.Item().PaddingTop(0.5f).Text(note.customer_contact).FontSize(7.5f);
                                });
                            }
                            if (has_credit_days)
                            {
                                if (has_contacto) r.ConstantItem(10);
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Dias de Credito").FontSize(5.5f).Bold().FontColor("#000000");
                                    c.Item().PaddingTop(0.5f).Text(note.credit_days_text).FontSize(7.5f);
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
                        columns.RelativeColumn(is_promo_table ? 1.6f : 1.2f);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1.2f);
                        if (is_promo_table)
                            columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(primary).Padding(0.5f).AlignCenter().Text("CANT.").FontColor(Colors.White).Bold().FontSize(9);
                        header.Cell().Background(primary).Padding(0.5f).Text("CODIGO").FontColor(Colors.White).Bold().FontSize(9);
                        header.Cell().Background(primary).Padding(0.5f).Text("DESCRIPCION").FontColor(Colors.White).Bold().FontSize(9);
                        header.Cell().Background(primary).Padding(0.5f).AlignCenter().Text("PRECIO U.").FontColor(Colors.White).Bold().FontSize(9);
                        if (is_promo_table)
                            header.Cell().Background(primary).Padding(0.5f).AlignCenter().Text($"DESCUENTO {note.promo_discount_percentage ?? 0:0.##}%").FontColor(Colors.White).Bold().FontSize(9);
                        header.Cell().Background(primary).Padding(0.5f).AlignCenter().Text("PRECIO P.").FontColor(Colors.White).Bold().FontSize(9);
                        header.Cell().Background(primary).Padding(0.5f).AlignCenter().Text("SUBTOTAL").FontColor(Colors.White).Bold().FontSize(9);
                    });

                    bool alternate = false;
                    foreach (var d in note.details)
                    {
                        string bg = alternate ? accent : Colors.White;
                        table.Cell().Background(bg).Padding(0.5f).AlignCenter().Text(d.quantity.ToString()).FontSize(9);
                        table.Cell().Background(bg).Padding(0.5f).Text(d.code).FontSize(9);
                        table.Cell().Background(bg).Padding(0.5f).Text(d.name).FontSize(9);
                        table.Cell().Background(bg).Padding(0.5f).AlignCenter().Text(d.unit_price_usd.ToString("N2")).FontSize(9);
                        if (is_promo_table)
                            table.Cell().Background(bg).Padding(0.5f).AlignCenter().Text(d.discount_usd.ToString("N2")).FontSize(9);
                        table.Cell().Background(bg).Padding(0.5f).AlignCenter().Text(d.promo_price_usd.ToString("N2")).FontSize(9);
                        table.Cell().Background(bg).Padding(0.5f).AlignCenter().Text(d.subtotal_usd.ToString("N2")).FontSize(9);
                        alternate = !alternate;
                    }
                });

                col.Item().PaddingTop(2).Row(row =>
                {
                    if (!string.IsNullOrWhiteSpace(note.conditions_text))
                    {
                        row.AutoItem().Border(0.5f).BorderColor(LightBorder).Padding(2).Column(cond =>
                        {
                            cond.Item().Text(note.conditions_text).FontSize(7.5f).Bold().Italic();
                        });
                    }

                    row.RelativeItem();

                    row.ConstantItem(130).Border(0.5f).BorderColor(LightBorder).Padding(2).Column(tg =>
                    {
                        string total_label = is_credit
                            ? (note.conditions_text?.IndexOf("OBSEQUIO", System.StringComparison.OrdinalIgnoreCase) >= 0 ? "TOTAL OBSEQUIADO" : "TOTAL DEVUELTO")
                            : "Total General";
                        tg.Item().AlignCenter().Text(total_label).FontSize(8).Bold();
                        decimal tgValue = is_credit ? note.total_amount_usd : (note.is_promo ? note.discounted_total_usd : note.gross_total_usd);
                        tg.Item().AlignCenter().Text($"{tgValue:N2}").FontSize(12).Bold().FontColor(primary);
                    });
                });

                bool use_promo_layout = note.is_promo || is_credit;
                col.Item().PaddingTop(2).Border(0.5f).BorderColor(LightBorder).Column(pay =>
                {
                    if (!is_credit)
                    {
                        pay.Item().Column(bank_header =>
                        {
                            bank_header.Item().Background(primary).Padding(1).AlignCenter().Text("FORMAS DE PAGO").FontSize(8).Bold().FontColor(Colors.White);

                            bank_header.Item().PaddingTop(1).Row(r =>
                            {
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("TRANSFERENCIA").FontSize(6.5f).Bold().FontColor(primary);
                                    c.Item().PaddingTop(1).Text(note.is_pro_venta ? "BANCO VENEZUELA  _  CUENTA CORRIENTE" : "BANCO MERCANTIL  _  CUENTA CORRIENTE").FontSize(6.5f);
                                    c.Item().PaddingTop(1).Text(note.is_pro_venta ? "NRO DE CUENTA  _  0102-0868-84-00000-27-407" : "NRO DE CUENTA  _  0105-0120-23-11200-92426").FontSize(6.5f);
                                    c.Item().PaddingTop(1).Text(note.is_pro_venta ? "CEDULA  _  6.266.986" : "CEDULA  _  13.046.042").FontSize(6.5f);
                                });

                                r.ConstantItem(12);

                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("PAGO MOVIL").FontSize(6.5f).Bold().FontColor(primary);
                                    c.Item().PaddingTop(1).Text(note.is_pro_venta ? "BANCO VENEZUELA" : "BANCO MERCANTIL").FontSize(6.5f);
                                    c.Item().PaddingTop(1).Text(note.is_pro_venta ? "NRO TELEFONO  _  0414.598.68.65" : "NRO TELEFONO  _  0424.496.01.02").FontSize(6.5f);
                                    c.Item().PaddingTop(1).Text(note.is_pro_venta ? "CEDULA  _  6.266.986" : "CEDULA  _  13.046.042").FontSize(6.5f);
                                });
                            });

                            bank_header.Item().PaddingTop(1).LineHorizontal(0.5f).LineColor(LightBorder);
                        });
                    }

                    pay.Item().PaddingTop(1).Row(row =>
                    {
                        if (!use_promo_layout)
                        {
                            row.ConstantItem(190).Border(0.5f).BorderColor(LightBorder).Padding(2).Column(left =>
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
                                    r.RelativeItem().AlignRight().Text($"{note.gross_total_usd:N2}").FontSize(8).Bold().FontColor(primary);
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
                                    r.RelativeItem().AlignRight().Text(note.discount_amount > 0 ? $"-{note.discount_amount:N2}" : $"{note.discount_amount:N2}").FontSize(8).FontColor(note.discount_amount > 0 ? "#CC0000" : "#000000");
                                });

                                left.Item().PaddingTop(2).Row(r =>
                                {
                                    r.RelativeItem().Text("Total General").FontSize(9).Bold();
                                    r.RelativeItem().AlignRight().Text($"{note.discounted_total_usd:N2}").FontSize(9).Bold().FontColor(primary);
                                });
                            });

                            row.ConstantItem(6);
                        }

                        row.ConstantItem(200).Border(0.5f).BorderColor(LightBorder).Padding(2).Column(manual =>
                        {
                            manual.Item().Text("DATOS PARA PAGO").FontSize(7).Bold().FontColor(primary).AlignCenter();
                            manual.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text("Fecha de pago:").FontSize(7);
                                r.RelativeItem().AlignRight().Text("____________________").FontSize(7);
                            });
                            manual.Item().PaddingTop(1.5f).Row(r =>
                            {
                                r.RelativeItem().Text("Monto Bs:").FontSize(7);
                                r.RelativeItem().AlignRight().Text("____________________").FontSize(7);
                            });
                            manual.Item().PaddingTop(1.5f).Row(r =>
                            {
                                r.RelativeItem().Text("Nro Referencia:").FontSize(7);
                                r.RelativeItem().AlignRight().Text("____________________").FontSize(7);
                            });
                            manual.Item().PaddingTop(1.5f).Row(r =>
                            {
                                r.RelativeItem().Text("Banco:").FontSize(7);
                                r.RelativeItem().AlignRight().Text("____________________").FontSize(7);
                            });
                            manual.Item().PaddingTop(1.5f).Row(r =>
                            {
                                r.RelativeItem().Text("Equivalente a:").FontSize(7);
                                r.RelativeItem().AlignRight().Text("____________________").FontSize(7);
                            });
                        });

                        row.ConstantItem(6);

                        row.RelativeItem().Column(mid =>
                        {
                            mid.Item().PaddingTop(14).AlignCenter().Text("FECHA _ FIRMA Y SELLO DEL CLIENTE").FontSize(8).FontColor("#000000");
                            mid.Item().PaddingTop(36).LineHorizontal(1).LineColor(LightBorder);
                        });
                    });
                });

                if (!is_credit && !note.is_promo)
                {
                    col.Item().PaddingTop(2).Row(tmp =>
                    {
                        tmp.ConstantItem(190).Border(0.5f).BorderColor(LightBorder).Padding(2).Column(vol =>
                        {
                            vol.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"Descuento por volumen {note.volume_discount_percentage:0.##}%")
                                    .FontSize(8.5f).Bold()
                                    .FontColor(note.volume_discount_amount > 0 ? Colors.Black : "#000000");

                                r.RelativeItem().AlignRight()
                                    .Text(note.volume_discount_amount > 0
                                        ? $"-{note.volume_discount_amount:N2}"
                                        : $"{note.volume_discount_amount:N2}")
                                    .FontSize(11).Bold()
                                    .FontColor(note.volume_discount_amount > 0 ? "#CC0000" : "#000000");
                            });

                            vol.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(LightBorder);

                            vol.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL A PAGAR").FontSize(11).Bold();
                                r.RelativeItem().AlignRight()
                                    .Text($"{note.total_amount_usd:N2}")
                                    .FontSize(11).Bold().FontColor(primary);
                            });
                        });

                        tmp.RelativeItem();
                    });
                }
            });

            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                col.Item().PaddingTop(1).Row(row =>
                {
                    row.RelativeItem().Text($"Nota: {note.note_number}").FontSize(7).FontColor("#000000");
                    row.RelativeItem().AlignCenter().Text($"Impreso: {DateTime.UtcNow:dd/MM/yyyy HH:mm}").FontSize(7).FontColor("#000000");
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Pagina ").FontSize(7).FontColor("#000000");
                        t.CurrentPageNumber().FontSize(7).FontColor("#000000");
                    });
                });
            });
        }
    }
}