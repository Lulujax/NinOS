using System;
using NinOS.Domain;
using Xunit;

namespace NinOS.Tests.Dominio
{
    public class BusinessUnitRulesTests
    {
        [Theory]
        [InlineData("GEN", business_unit.General)]
        [InlineData("PRM", business_unit.General)]
        [InlineData("VOL", business_unit.General)]
        [InlineData("MAR", business_unit.ProVenta)]
        [InlineData("PVP", business_unit.ProVenta)]
        [InlineData("VOLMAR", business_unit.ProVenta)]
        [InlineData("VES", business_unit.Vesupronta)]
        [InlineData("VESPRM", business_unit.Vesupronta)]
        [InlineData("VESVOL", business_unit.Vesupronta)]
        public void caso_feliz_cada_codigo_devuelve_su_empresa(string code, business_unit esperada)
        {
            Assert.Equal(esperada, BusinessUnitRules.unit_of_note_type(code));
        }

        [Theory]
        [InlineData(null, business_unit.General)]
        [InlineData("", business_unit.General)]
        [InlineData("   ", business_unit.General)]
        [InlineData("gen", business_unit.General)]
        [InlineData("  ves  ", business_unit.Vesupronta)]
        [InlineData("vesvol", business_unit.Vesupronta)]
        public void caso_limite_vacio_nulo_minusculas_y_espacios(string? code, business_unit esperada)
        {
            Assert.Equal(esperada, BusinessUnitRules.unit_of_note_type(code));
        }

        [Theory]
        [InlineData("XYZ")]
        [InlineData("VESX")]
        [InlineData("PRO")]
        public void caso_error_codigo_desconocido_cuenta_como_general_y_nunca_vesupronta(string code)
        {
            var empresa = BusinessUnitRules.unit_of_note_type(code);

            Assert.Equal(business_unit.General, empresa);
            Assert.NotEqual(business_unit.Vesupronta, empresa);
        }

        [Fact]
        public void caso_feliz_zona_sin_banderas_es_general()
        {
            var zone = new zona { name = "Centro", code = "01" };

            Assert.Equal(business_unit.General, BusinessUnitRules.unit_of_zone(zone));
        }

        [Fact]
        public void caso_feliz_zona_pro_venta_y_zona_vesupronta()
        {
            var pro_venta_zone = new zona { name = "Maracay", code = "06", is_pro_venta = true };
            var vesupronta_zone = new zona { name = "Vesu", code = "07", is_vesupronta = true };

            Assert.Equal(business_unit.ProVenta, BusinessUnitRules.unit_of_zone(pro_venta_zone));
            Assert.Equal(business_unit.Vesupronta, BusinessUnitRules.unit_of_zone(vesupronta_zone));
        }

        [Fact]
        public void caso_error_zona_nula_lanza_excepcion()
        {
            Assert.Throws<ArgumentNullException>(() => BusinessUnitRules.unit_of_zone(null!));
        }

        [Fact]
        public void caso_error_zona_pro_venta_y_vesupronta_a_la_vez_lanza_excepcion()
        {
            var zone = new zona { name = "Mixta", code = "08", is_pro_venta = true, is_vesupronta = true };

            Assert.Throws<InvalidOperationException>(() => BusinessUnitRules.unit_of_zone(zone));
        }

        [Fact]
        public void caso_feliz_codigos_de_general()
        {
            Assert.Equal(new[] { "GEN", "PRM", "VOL" }, BusinessUnitRules.codes_of_unit(business_unit.General));
        }

        [Fact]
        public void caso_feliz_codigos_de_pro_venta()
        {
            Assert.Equal(new[] { "MAR", "PVP", "VOLMAR" }, BusinessUnitRules.codes_of_unit(business_unit.ProVenta));
        }

        [Fact]
        public void caso_feliz_codigos_de_vesupronta()
        {
            Assert.Equal(new[] { "VES", "VESPRM", "VESVOL" }, BusinessUnitRules.codes_of_unit(business_unit.Vesupronta));
        }

        [Fact]
        public void caso_error_empresa_fuera_de_rango_lanza_excepcion()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BusinessUnitRules.codes_of_unit((business_unit)99));
        }

        [Theory]
        [InlineData(0, business_unit.General)]
        [InlineData(1, business_unit.ProVenta)]
        [InlineData(2, business_unit.Vesupronta)]
        public void caso_feliz_indice_de_modalidad_a_empresa(int indice, business_unit esperada)
        {
            Assert.Equal(esperada, BusinessUnitRules.unit_from_modalidad_index(indice));
            Assert.Equal(indice, BusinessUnitRules.modalidad_index_of(esperada));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public void caso_error_indice_de_modalidad_fuera_de_rango_lanza_excepcion(int indice)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BusinessUnitRules.unit_from_modalidad_index(indice));
        }

        [Fact]
        public void caso_limite_empresa_fuera_de_rango_no_tiene_indice_de_modalidad()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BusinessUnitRules.modalidad_index_of((business_unit)99));
        }
    }
}
