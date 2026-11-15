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
        public void caso_feliz_tipos_generales_son_visibles(string code)
        {
            Assert.True(SalesNoteScope.is_visible_in_sales(code));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("gen")]
        [InlineData("  VOL  ")]
        public void caso_limite_nulo_vacio_minusculas_espacios(string? code)
        {
            Assert.True(SalesNoteScope.is_visible_in_sales(code));
        }

        [Theory]
        [InlineData("MAR")]
        [InlineData("PVP")]
        [InlineData("VOLMAR")]
        [InlineData("mar")]
        [InlineData("  MAR  ")]
        public void caso_error_tipos_pro_venta_se_ocultan(string code)
        {
            Assert.False(SalesNoteScope.is_visible_in_sales(code));
        }
    }
}
