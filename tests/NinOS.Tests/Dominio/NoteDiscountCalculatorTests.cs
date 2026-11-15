using System;
using NinOS.Domain;
using Xunit;

namespace NinOS.Tests.Dominio
{
    public class NoteDiscountCalculatorTests
    {
        [Fact]
        public void caso_feliz_condicion_y_volumen_se_suman_sobre_la_misma_base()
        {
            var result = note_discount_calculator.calculate(1000m, 10m, 5m);

            Assert.Equal(100m, result.condition_amount);
            Assert.Equal(50m, result.volume_amount);
            Assert.Equal(850m, result.total_amount_usd);
        }

        [Fact]
        public void caso_limite_sin_descuentos_el_total_queda_igual_al_base()
        {
            var result = note_discount_calculator.calculate(500m, 0m, 0m);

            Assert.Equal(0m, result.condition_amount);
            Assert.Equal(0m, result.volume_amount);
            Assert.Equal(500m, result.total_amount_usd);
        }

        [Fact]
        public void caso_limite_descuentos_que_suman_100_dejan_total_en_cero()
        {
            var result = note_discount_calculator.calculate(200m, 60m, 40m);

            Assert.Equal(0m, result.total_amount_usd);
        }

        [Fact]
        public void caso_error_base_negativa_lanza_excepcion()
        {
            Assert.Throws<ArgumentException>(() => note_discount_calculator.calculate(-1m, 10m, 5m));
        }

        [Fact]
        public void caso_error_porcentaje_fuera_de_rango_lanza_excepcion()
        {
            Assert.Throws<ArgumentException>(() => note_discount_calculator.calculate(1000m, 110m, 0m));
        }
    }
}
