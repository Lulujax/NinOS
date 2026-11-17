using NinOS.Domain;
using Xunit;

namespace NinOS.Tests.Dominio
{
    public class SalesNoteScopeTests
    {
        [Theory]
        [InlineData("GEN")]
        [InlineData("PRM")]
        [InlineData("VOL")]
        public void caso_feliz_tipos_generales_son_visibles_en_general(string code)
        {
            Assert.True(SalesNoteScope.is_visible_in_sales(code, business_unit.General));
        }

        [Theory]
        [InlineData("VES")]
        [InlineData("VESPRM")]
        [InlineData("VESVOL")]
        public void caso_feliz_tipos_vesupronta_son_visibles_solo_en_vesupronta(string code)
        {
            Assert.True(SalesNoteScope.is_visible_in_sales(code, business_unit.Vesupronta));
            Assert.False(SalesNoteScope.is_visible_in_sales(code, business_unit.General));
            Assert.False(SalesNoteScope.is_visible_in_sales(code, business_unit.ProVenta));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("gen")]
        [InlineData("  VOL  ")]
        public void caso_limite_nulo_vacio_minusculas_espacios_cuentan_como_general(string? code)
        {
            Assert.True(SalesNoteScope.is_visible_in_sales(code, business_unit.General));
            Assert.False(SalesNoteScope.is_visible_in_sales(code, business_unit.Vesupronta));
        }

        [Theory]
        [InlineData("MAR")]
        [InlineData("PVP")]
        [InlineData("VOLMAR")]
        [InlineData("mar")]
        [InlineData("  MAR  ")]
        public void caso_error_tipos_pro_venta_no_son_visibles_en_general_ni_vesupronta(string code)
        {
            Assert.False(SalesNoteScope.is_visible_in_sales(code, business_unit.General));
            Assert.False(SalesNoteScope.is_visible_in_sales(code, business_unit.Vesupronta));
            Assert.True(SalesNoteScope.is_visible_in_sales(code, business_unit.ProVenta));
        }
    }
}
