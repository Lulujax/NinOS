using Microsoft.Win32;
using NinOS.Domain.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Linq;

namespace NinOS.UI.Common
{
    public static class NotePdfGenerator
    {
        private static readonly string LightBorder = "#000000";

        /// <summary>
        /// Deja el texto en ASCII sin acentos ni simbolos raros. La fuente incrustada en el
        /// PDF no resuelve bien los caracteres acentuados y los muestra corruptos
        /// (por ejemplo "CRÃ‰DITO"), asi que los datos que vienen de la base (razon social,
        /// direccion, nombre del producto...) pasan por aqui antes de imprimirse.
        /// </summary>
        private static string ascii(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            char[] source = value.ToCharArray();
            char[] result = new char[source.Length];

            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                switch (c)
                {
                    case '\u00E1': result[i] = 'a'; break;
                    case '\u00C1': result[i] = 'A'; break;
                    case '\u00E9': result[i] = 'e'; break;
                    case '\u00C9': result[i] = 'E'; break;
                    case '\u00ED': result[i] = 'i'; break;
                    case '\u00CD': result[i] = 'I'; break;
                    case '\u00F3': result[i] = 'o'; break;
                    case '\u00D3': result[i] = 'O'; break;
                    case '\u00FA': result[i] = 'u'; break;
                    case '\u00DA': result[i] = 'U'; break;
                    case '\u00FC': result[i] = 'u'; break;
                    case '\u00DC': result[i] = 'U'; break;
                    case '\u00F1': result[i] = 'n'; break;
                    case '\u00D1': result[i] = 'N'; break;
                    case '\u00B0': result[i] = 'o'; break;
                    case '\u2013': case '\u2014': case '\u2018': case '\u2019': result[i] = '-'; break;
                    case '\u201C': case '\u201D': result[i] = '"'; break;
                    case '\n': case '\r': case '\t': result[i] = ' '; break;
                    default: result[i] = c <= 127 ? c : ' '; break;
                }
            }

            return new string(result);
        }

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
                    BuildNotePage(page, note, "ORIGINAL");
                });
                container.Page(page =>
                {
                    BuildNotePage(page, note, "COPIA");
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
                    BuildNotePage(page, delivery_note, "ORIGINAL");
                });
                container.Page(page =>
                {
                    BuildNotePage(page, delivery_note, "COPIA");
                });
                container.Page(page =>
                {
                    BuildNotePage(page, credit_note, "ORIGINAL");
                });
                container.Page(page =>
                {
                    BuildNotePage(page, credit_note, "COPIA");
                });
            });

            document.GeneratePdf(output_path);
        }

        private static void BuildNotePage(PageDescriptor page, note_print_dto note, string? copy_type = null)
        {
            string primary = string.IsNullOrWhiteSpace(note.accent_color) ? "#1B3A2D" : note.accent_color;
            string accent = string.IsNullOrWhiteSpace(note.accent_soft_color) ? "#F0F4EC" : note.accent_soft_color;
            bool is_credit = string.Equals(note.document_label, "NOTA DE CREDITO", StringComparison.OrdinalIgnoreCase);

            int count = note.details.Count;
            bool isVeryDense = count > 14;
            bool isDense = count > 10;

            float tableFontSize = isVeryDense ? 8f : (isDense ? 8.5f : 9f);
            float tablePaddingV = isVeryDense ? 0.3f : (isDense ? 0.5f : 0.8f);
            float customerFontSize = isVeryDense ? 8f : 8.5f;
            float customerLabelSize = isVeryDense ? 6.5f : 7f;
            float firmaPaddingTop = isVeryDense ? 18f : (isDense ? 24f : 30f);

            page.Size(PageSizes.Letter);
            // 1 cm en los cuatro lados: con 0.5 cm el recuadro exterior caia dentro de la zona
            // muerta de la impresora y la nota salia cortada por izquierda y derecha. Es el mismo
            // margen que usan los demas reportes.
            page.MarginLeft(1, Unit.Centimetre);
            page.MarginTop(1, Unit.Centimetre);
            page.MarginRight(1, Unit.Centimetre);
            page.MarginBottom(1, Unit.Centimetre);

            page.Header().Column(col =>
            {
                if (note.is_promo)
                {
                    col.Item().PaddingBottom(1.5f).Border(0.8f).BorderColor(primary).PaddingHorizontal(6).PaddingVertical(1).AlignCenter()
                        .Text("PROMOCION").FontSize(11).Bold().FontColor(primary);
                }

                col.Item().Row(row =>
                {
                    row.RelativeItem(3).Column(left =>
                    {
                        left.Item().Text(ascii(string.IsNullOrWhiteSpace(note.header_title) ? note.company_name : note.header_title)).FontSize(12).Bold().FontColor(primary);
                        left.Item().Text("Caracas - Venezuela").FontSize(8.5f).FontColor("#000000");
                    });

                    row.RelativeItem(2).Column(right =>
                    {
                        right.Item().AlignRight().Text(ascii(note.document_label)).FontSize(13).Bold().FontColor(primary);
                    });
                });

                col.Item().PaddingTop(1.5f).Row(subRow =>
                {
                    subRow.RelativeItem().Text(string.IsNullOrWhiteSpace(note.seller_name) ? "" : $"Vendedor: {ascii(note.seller_name)}").FontSize(9.5f).Bold();
                    subRow.RelativeItem().AlignRight().Text($"Nro: {ascii(note.note_number)}").FontSize(9.5f).Bold();
                });

                col.Item().PaddingTop(1.5f).LineHorizontal(1.2f).LineColor(primary);
            });

            page.Content().PaddingVertical(0).Column(col =>
            {
                col.Item().PaddingTop(1.5f).Border(0.5f).BorderColor(LightBorder).Padding(1).Column(grid =>
                {
                    grid.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Razon Social").FontSize(customerLabelSize).Bold().FontColor("#000000");
                            c.Item().PaddingTop(0.5f).Text(ascii(note.customer_business_name)).FontSize(customerFontSize);
                        });
                        r.ConstantItem(75).Column(c =>
                        {
                            c.Item().Text("Codigo").FontSize(customerLabelSize).Bold().FontColor("#000000");
                            c.Item().PaddingTop(0.5f).Text(ascii(note.customer_code)).FontSize(customerFontSize);
                        });
                        r.ConstantItem(110).Column(c =>
                        {
                            c.Item().Text("RIF").FontSize(customerLabelSize).Bold().FontColor("#000000");
                            c.Item().PaddingTop(0.5f).Text(ascii(note.customer_rif)).FontSize(customerFontSize);
                        });
                    });

                    grid.Item().PaddingTop(0.5f).LineHorizontal(0.25f).LineColor("#000000");

                    grid.Item().PaddingTop(0.8f).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Domicilio Fiscal").FontSize(customerLabelSize).Bold().FontColor("#000000");
                            c.Item().PaddingTop(0.5f).Text(ascii(note.fiscal_address)).FontSize(customerFontSize);
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Direccion de Entrega").FontSize(customerLabelSize).Bold().FontColor("#000000");
                            c.Item().PaddingTop(0.5f).Text(ascii(note.customer_delivery_address)).FontSize(customerFontSize);
                        });
                    });

                    grid.Item().PaddingTop(0.5f).LineHorizontal(0.25f).LineColor("#000000");

                    grid.Item().PaddingTop(0.8f).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Fecha Emision").FontSize(customerLabelSize).Bold().FontColor("#000000");
                            c.Item().PaddingTop(0.5f).Text(note.creation_date.ToString("dd/MM/yyyy")).FontSize(customerFontSize);
                        });
                        if (is_credit || note.is_promo || string.IsNullOrWhiteSpace(note.credit_days_text))
                        {
                            r.RelativeItem();
                        }
                        else
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("CREDITO").FontSize(customerLabelSize).Bold().FontColor("#000000");
                                c.Item().PaddingTop(0.5f).Text(ascii(note.credit_days_text)).FontSize(customerFontSize);
                            });
                        }
                        if (is_credit)
                        {
                            r.RelativeItem();
                        }
                        else
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Fecha Vencimiento").FontSize(customerLabelSize).Bold().FontColor("#000000");
                                c.Item().PaddingTop(0.5f).Text(note.due_date.ToString("dd/MM/yyyy")).FontSize(customerFontSize);
                            });
                        }
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Telefono").FontSize(customerLabelSize).Bold().FontColor("#000000");
                            c.Item().PaddingTop(0.5f).Text(ascii(note.customer_phone)).FontSize(customerFontSize);
                        });
                    });

                    bool has_contacto = !string.IsNullOrWhiteSpace(note.customer_contact);

                    if (has_contacto)
                    {
                        grid.Item().PaddingTop(0.5f).LineHorizontal(0.25f).LineColor("#000000");
                        grid.Item().PaddingTop(0.8f).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Contacto").FontSize(customerLabelSize).Bold().FontColor("#000000");
                                c.Item().PaddingTop(0.5f).Text(ascii(note.customer_contact)).FontSize(customerFontSize);
                            });
                        });
                    }
                });

                col.Item().PaddingTop(1.5f).Table(table =>
                {
                    bool is_promo_table = note.is_promo;

                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(36); // CANT.
                        columns.ConstantColumn(68); // CODIGO
                        columns.RelativeColumn();   // DESCRIPCION
                        columns.ConstantColumn(58); // PRECIO U.
                        if (is_promo_table)
                            columns.ConstantColumn(62); // DESCUENTO
                        columns.ConstantColumn(58); // PRECIO P.
                        columns.ConstantColumn(60); // SUBTOTAL
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(primary).PaddingVertical(tablePaddingV).PaddingHorizontal(0.5f).AlignCenter().Text("CANT.").FontColor(Colors.White).Bold().FontSize(tableFontSize);
                        header.Cell().Background(primary).PaddingVertical(tablePaddingV).PaddingHorizontal(0.5f).Text("CODIGO").FontColor(Colors.White).Bold().FontSize(tableFontSize);
                        header.Cell().Background(primary).PaddingVertical(tablePaddingV).PaddingHorizontal(0.5f).Text("DESCRIPCION").FontColor(Colors.White).Bold().FontSize(tableFontSize);
                        header.Cell().Background(primary).PaddingVertical(tablePaddingV).PaddingHorizontal(0.5f).AlignCenter().Text("PRECIO U.").FontColor(Colors.White).Bold().FontSize(tableFontSize);
                        if (is_promo_table)
                            header.Cell().Background(primary).PaddingVertical(tablePaddingV).PaddingHorizontal(0.5f).AlignCenter().Text($"DESC. {note.promo_discount_percentage ?? 0:0.##}%").FontColor(Colors.White).Bold().FontSize(tableFontSize);
                        header.Cell().Background(primary).PaddingVertical(tablePaddingV).PaddingHorizontal(0.5f).AlignCenter().Text("PRECIO P.").FontColor(Colors.White).Bold().FontSize(tableFontSize);
                        header.Cell().Background(primary).PaddingVertical(tablePaddingV).PaddingHorizontal(0.5f).AlignCenter().Text("SUBTOTAL").FontColor(Colors.White).Bold().FontSize(tableFontSize);
                    });

                    bool alternate = false;
                    foreach (var d in note.details)
                    {
                        string bg = alternate ? accent : Colors.White;
                        table.Cell().Background(bg).PaddingVertical(tablePaddingV).PaddingHorizontal(0.5f).AlignCenter().Text(d.quantity.ToString()).FontSize(tableFontSize);
                        table.Cell().Background(bg).PaddingVertical(tablePaddingV).PaddingHorizontal(0.5f).Text(ascii(d.code)).FontSize(tableFontSize);
                        table.Cell().Background(bg).PaddingVertical(tablePaddingV).PaddingHorizontal(0.5f).Text(ascii(d.name)).FontSize(tableFontSize);
                        table.Cell().Background(bg).PaddingVertical(tablePaddingV).PaddingHorizontal(0.5f).AlignCenter().Text(d.unit_price_usd.ToString("N2")).FontSize(tableFontSize);
                        if (is_promo_table)
                            table.Cell().Background(bg).PaddingVertical(tablePaddingV).PaddingHorizontal(0.5f).AlignCenter().Text(d.discount_usd.ToString("N2")).FontSize(tableFontSize);
                        table.Cell().Background(bg).PaddingVertical(tablePaddingV).PaddingHorizontal(0.5f).AlignCenter().Text(d.promo_price_usd.ToString("N2")).FontSize(tableFontSize);
                        table.Cell().Background(bg).PaddingVertical(tablePaddingV).PaddingHorizontal(0.5f).AlignCenter().Text(d.subtotal_usd.ToString("N2")).FontSize(tableFontSize);
                        alternate = !alternate;
                    }
                });

                col.Item().PaddingTop(1.5f).Row(row =>
                {
                    if (!string.IsNullOrWhiteSpace(note.conditions_text))
                    {
                        row.AutoItem().Border(0.5f).BorderColor(LightBorder).Padding(1.5f).Column(cond =>
                        {
                            cond.Item().Text(ascii(note.conditions_text)).FontSize(8.5f).Bold().Italic();
                        });
                    }

                    row.RelativeItem();

                    row.ConstantItem(125).Border(0.5f).BorderColor(LightBorder).Padding(1.5f).Column(tg =>
                    {
                        string total_label = is_credit
                            ? (note.conditions_text?.IndexOf("OBSEQUIO", System.StringComparison.OrdinalIgnoreCase) >= 0 ? "TOTAL OBSEQUIADO" : "TOTAL DEVUELTO")
                            : "Total General";
                        tg.Item().AlignCenter().Text(total_label).FontSize(9f).Bold();
                        decimal tgValue = is_credit ? note.total_amount_usd : (note.is_promo ? note.discounted_total_usd : note.gross_total_usd);
                        tg.Item().AlignCenter().Text($"{tgValue:N2}").FontSize(11.5f).Bold().FontColor(primary);
                    });
                });

                bool use_promo_layout = note.is_promo || is_credit;
                col.Item().PaddingTop(1.5f).Border(0.5f).BorderColor(LightBorder).Column(pay =>
                {
                    if (!is_credit)
                    {
                        pay.Item().Column(bank_header =>
                        {
                            bank_header.Item().Background(primary).Padding(0.8f).AlignCenter().Text("FORMAS DE PAGO").FontSize(8.5f).Bold().FontColor(Colors.White);

                            bank_header.Item().PaddingTop(0.8f).Row(r =>
                            {
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("TRANSFERENCIA").FontSize(7.5f).Bold().FontColor(primary);
                                    c.Item().PaddingTop(0.5f).Text(note.is_pro_venta ? "BANCO VENEZUELA  _  CUENTA CORRIENTE" : "BANCO MERCANTIL  _  CUENTA CORRIENTE").FontSize(7.5f);
                                    c.Item().PaddingTop(0.5f).Text(note.is_pro_venta ? "NRO DE CUENTA  _  0102-0868-84-00000-27-407" : "NRO DE CUENTA  _  0105-0120-23-11200-92426").FontSize(7.5f);
                                    c.Item().PaddingTop(0.5f).Text(note.is_pro_venta ? "CEDULA  _  6.266.986" : "CEDULA  _  13.046.042").FontSize(7.5f);
                                });

                                r.ConstantItem(10);

                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("PAGO MOVIL").FontSize(7.5f).Bold().FontColor(primary);
                                    c.Item().PaddingTop(0.5f).Text(note.is_pro_venta ? "BANCO VENEZUELA" : "BANCO MERCANTIL").FontSize(7.5f);
                                    c.Item().PaddingTop(0.5f).Text(note.is_pro_venta ? "NRO TELEFONO  _  0414.598.68.65" : "NRO TELEFONO  _  0424.496.01.02").FontSize(7.5f);
                                    c.Item().PaddingTop(0.5f).Text(note.is_pro_venta ? "CEDULA  _  6.266.986" : "CEDULA  _  13.046.042").FontSize(7.5f);
                                });
                            });

                            bank_header.Item().PaddingTop(0.8f).LineHorizontal(0.5f).LineColor(LightBorder);
                        });
                    }

                    pay.Item().PaddingTop(0.8f).Row(row =>
                    {
                        if (!use_promo_layout)
                        {
                            row.ConstantItem(185).Border(0.5f).BorderColor(LightBorder).Padding(1.5f).Column(left =>
                            {
                                if (!string.IsNullOrWhiteSpace(note.discount_conditions_text))
                                {
                                    left.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text(ascii(note.discount_conditions_text)).FontSize(7).Bold().AlignCenter();
                                    });
                                }

                                left.Item().PaddingTop(1).Row(r =>
                                {
                                    r.RelativeItem().Text("sub total").FontSize(8.5f).Bold();
                                    r.RelativeItem().AlignRight().Text($"{note.gross_total_usd:N2}").FontSize(8.5f).Bold().FontColor(primary);
                                });

                                if (note.promo_discount_amount > 0)
                                {
                                    left.Item().PaddingTop(0.5f).Row(r =>
                                    {
                                        r.RelativeItem().Text($"{note.promo_discount_percentage:0.##}% PROMO").FontSize(8.5f);
                                        r.RelativeItem().AlignRight().Text($"-{note.promo_discount_amount:N2}").FontSize(8.5f).FontColor("#CC0000");
                                    });
                                }

                                left.Item().PaddingTop(0.5f).Row(r =>
                                {
                                    r.RelativeItem().Text($"{note.discount_percentage:0.##}%").FontSize(8.5f);
                                    r.RelativeItem().AlignRight().Text(note.discount_amount > 0 ? $"-{note.discount_amount:N2}" : $"{note.discount_amount:N2}").FontSize(8.5f).FontColor(note.discount_amount > 0 ? "#CC0000" : "#000000");
                                });

                                left.Item().PaddingTop(1).Row(r =>
                                {
                                    r.RelativeItem().Text("Total General").FontSize(8.5f).Bold();
                                    r.RelativeItem().AlignRight().Text($"{note.discounted_total_usd:N2}").FontSize(8.5f).Bold().FontColor(primary);
                                });
                            });

                            row.ConstantItem(5);
                        }

                        // NOTA DE CREDITO (devolucion): no se imprime DATOS PARA PAGO. La devolucion no
                        // trae movimiento de dinero, asi que solo queda la firma del cliente.
                        if (!is_credit)
                        {
                            row.ConstantItem(195).Border(0.5f).BorderColor(LightBorder).Padding(1.5f).Column(manual =>
                            {
                                manual.Item().Text("DATOS PARA PAGO").FontSize(7).Bold().FontColor(primary).AlignCenter();
                                manual.Item().PaddingTop(1.5f).Row(r =>
                                {
                                    r.RelativeItem().Text("Fecha de pago:").FontSize(6.8f);
                                    r.RelativeItem().AlignRight().Text("____________________").FontSize(6.8f);
                                });
                                manual.Item().PaddingTop(1.2f).Row(r =>
                                {
                                    r.RelativeItem().Text("Monto Bs:").FontSize(6.8f);
                                    r.RelativeItem().AlignRight().Text("____________________").FontSize(6.8f);
                                });
                                manual.Item().PaddingTop(1.2f).Row(r =>
                                {
                                    r.RelativeItem().Text("Nro Referencia:").FontSize(6.8f);
                                    r.RelativeItem().AlignRight().Text("____________________").FontSize(6.8f);
                                });
                                manual.Item().PaddingTop(1.2f).Row(r =>
                                {
                                    r.RelativeItem().Text("Banco:").FontSize(6.8f);
                                    r.RelativeItem().AlignRight().Text("____________________").FontSize(6.8f);
                                });
                                manual.Item().PaddingTop(1.2f).Row(r =>
                                {
                                    r.RelativeItem().Text("Equivalente a:").FontSize(6.8f);
                                    r.RelativeItem().AlignRight().Text("____________________").FontSize(6.8f);
                                });
                            });

                            row.ConstantItem(5);
                        }

                        row.RelativeItem().Column(mid =>
                        {
                            mid.Item().PaddingTop(10).AlignCenter().Text("FECHA _ FIRMA Y SELLO DEL CLIENTE").FontSize(8.5f).FontColor("#000000");
                            mid.Item().PaddingTop(firmaPaddingTop).LineHorizontal(1).LineColor(LightBorder);
                        });
                    });
                });

                // NOTA GENERAL: no lleva cuadro de volumen, el bloque no se dibuja.
                // NOTA VOLUMEN: cuadro identico al modelo original:
                //   Descuento por volumen X%   -monto
                //   ----------------------------------
                //   TOTAL A PAGAR              total final de la nota
                if (!is_credit && !note.is_promo && note.es_volumen)
                {
                    col.Item().PaddingTop(1.5f).Row(tmp =>
                    {
                        tmp.ConstantItem(185).Border(0.5f).BorderColor(LightBorder).Padding(1.5f).Column(vol =>
                        {
                            vol.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"Descuento por volumen {note.volume_discount_percentage:0.##}%")
                                    .FontSize(8f).Bold()
                                    .FontColor(note.volume_discount_amount > 0 ? Colors.Black : "#000000");

                                r.RelativeItem().AlignRight()
                                    .Text(note.volume_discount_amount > 0
                                        ? $"-{note.volume_discount_amount:N2}"
                                        : $"{note.volume_discount_amount:N2}")
                                    .FontSize(10).Bold()
                                    .FontColor(note.volume_discount_amount > 0 ? "#CC0000" : "#000000");
                            });

                            vol.Item().PaddingTop(1.5f).LineHorizontal(0.5f).LineColor(LightBorder);

                            vol.Item().PaddingTop(1.5f).Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL A PAGAR").FontSize(10).Bold();
                                r.RelativeItem().AlignRight()
                                    .Text($"{note.total_amount_usd:N2}")
                                    .FontSize(10).Bold().FontColor(primary);
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
                    row.AutoItem().AlignLeft().AlignMiddle().Text($"Nota: {note.note_number}").FontSize(7.5f).FontColor("#000000");

                    if (!string.IsNullOrEmpty(copy_type))
                    {
                        row.RelativeItem().AlignCenter().AlignMiddle().Text(copy_type).FontSize(14).Bold().FontColor(primary);
                    }
                    else
                    {
                        row.RelativeItem();
                    }

                    row.AutoItem().AlignRight().AlignMiddle().Text(t =>
                    {
                        t.Span($"Impreso: {NinOS.Domain.AppTimeZone.to_local(DateTime.UtcNow):dd/MM/yyyy HH:mm}   |   Pagina ").FontSize(7f).FontColor("#000000");
                        t.CurrentPageNumber().FontSize(7f).FontColor("#000000");
                    });
                });
            });
        }
    }
}
