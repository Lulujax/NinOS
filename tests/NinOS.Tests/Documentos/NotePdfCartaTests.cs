using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common;
using QuestPDF.Fluent;
using Xunit;

namespace NinOS.Tests.Documentos
{
    public class NotePdfCartaTests
    {
        private const double ANCHO_CARTA_PUNTOS = 612;
        private const double ALTO_CARTA_PUNTOS = 792;
        private const double TOLERANCIA_PUNTOS = 0.5;

        private static note_print_dto crear_nota(string document_label, int cantidad_renglones)
        {
            List<note_detail_print_dto> renglones = Enumerable.Range(1, cantidad_renglones)
                .Select(i => new note_detail_print_dto
                {
                    code = $"P{i:000}",
                    name = $"Producto de prueba {i}",
                    quantity = i,
                    unit_price_usd = 1.50m,
                    promo_price_usd = 1.50m,
                    subtotal_usd = 1.50m * i
                })
                .ToList();

            decimal total = renglones.Sum(r => r.subtotal_usd);

            return new note_print_dto
            {
                note_number = "000_001",
                company_name = "NINOS",
                header_title = "NINOS",
                document_label = document_label,
                creation_date = new DateTime(2026, 11, 12, 21, 0, 0),
                due_date = new DateTime(2026, 12, 3),
                seller_name = "Vendedora de prueba",
                customer_code = "00001",
                customer_business_name = "Cliente de prueba",
                customer_rif = "J-00000000-0",
                gross_total_usd = total,
                discounted_total_usd = total,
                total_amount_usd = total,
                credit_days_text = "21 DIAS",
                details = renglones
            };
        }

        private static List<(double ancho, double alto)> medir_paginas(byte[] pdf)
        {
            string contenido = Encoding.Latin1.GetString(pdf);
            MatchCollection cajas = Regex.Matches(contenido, @"/MediaBox\s*\[\s*0\s+0\s+([\d.]+)\s+([\d.]+)\s*\]");

            return cajas
                .Select(caja => (
                    double.Parse(caja.Groups[1].Value, CultureInfo.InvariantCulture),
                    double.Parse(caja.Groups[2].Value, CultureInfo.InvariantCulture)))
                .ToList();
        }

        private static void verificar_todas_carta(byte[] pdf, int paginas_minimas, string caso)
        {
            List<(double ancho, double alto)> paginas = medir_paginas(pdf);

            Assert.True(paginas.Count >= paginas_minimas,
                $"[{caso}] se esperaban al menos {paginas_minimas} paginas y el PDF trae {paginas.Count}.");

            for (int i = 0; i < paginas.Count; i++)
            {
                (double ancho, double alto) = paginas[i];
                bool es_carta = Math.Abs(ancho - ANCHO_CARTA_PUNTOS) < TOLERANCIA_PUNTOS
                             && Math.Abs(alto - ALTO_CARTA_PUNTOS) < TOLERANCIA_PUNTOS;
                Assert.True(es_carta,
                    $"[{caso}] la pagina {i + 1} mide {ancho} x {alto} puntos; carta es {ANCHO_CARTA_PUNTOS} x {ALTO_CARTA_PUNTOS}.");
            }
        }

        [Fact]
        public void caso_feliz_nota_de_entrega_sale_en_carta()
        {
            byte[] pdf = NotePdfGenerator.build_document(crear_nota("NOTA DE ENTREGA", 5)).GeneratePdf();

            verificar_todas_carta(pdf, 2, "nota de entrega con 5 renglones");
        }

        [Fact]
        public void caso_feliz_nota_de_credito_sola_sale_en_carta()
        {
            byte[] pdf = NotePdfGenerator.build_document(crear_nota("NOTA DE CREDITO", 3)).GeneratePdf();

            verificar_todas_carta(pdf, 2, "nota de credito sola");
        }

        [Fact]
        public void caso_feliz_nota_de_entrega_con_nota_de_credito_sale_en_carta()
        {
            byte[] pdf = NotePdfGenerator.build_document(
                crear_nota("NOTA DE ENTREGA", 5),
                crear_nota("NOTA DE CREDITO", 2)).GeneratePdf();

            verificar_todas_carta(pdf, 4, "nota de entrega mas nota de credito");
        }

        [Fact]
        public void caso_limite_nota_sin_renglones_sale_en_carta()
        {
            byte[] pdf = NotePdfGenerator.build_document(crear_nota("NOTA DE ENTREGA", 0)).GeneratePdf();

            verificar_todas_carta(pdf, 2, "nota sin renglones");
        }

        [Fact]
        public void caso_limite_nota_con_el_maximo_de_20_renglones_sale_en_carta()
        {
            byte[] pdf = NotePdfGenerator.build_document(crear_nota("NOTA DE ENTREGA", 20)).GeneratePdf();

            verificar_todas_carta(pdf, 2, "nota con 20 renglones");
        }

        [Fact]
        public void caso_error_nota_nula_no_genera_pdf()
        {
            Assert.ThrowsAny<Exception>(() => NotePdfGenerator.build_document(null!).GeneratePdf());
        }
    }
}
