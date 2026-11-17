using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class ProVentaNoteEditTests
{
    [Fact]
    public void monto_desde_descuento_da_292_90_al_diez_por_ciento()
    {
        Assert.Equal(292.90m, pro_venta_note_edit.monto_desde_descuento(325.44m, 10m));
    }

    [Fact]
    public void monto_desde_descuento_sin_descuento_da_el_monto_original()
    {
        Assert.Equal(325.44m, pro_venta_note_edit.monto_desde_descuento(325.44m, 0m));
    }

    [Fact]
    public void monto_desde_descuento_al_cinco_por_ciento_da_309_17()
    {
        Assert.Equal(309.17m, pro_venta_note_edit.monto_desde_descuento(325.44m, 5m));
    }

    [Fact]
    public void limite_cien_por_ciento_da_cero()
    {
        Assert.Equal(0m, pro_venta_note_edit.monto_desde_descuento(325.44m, 100m));
    }

    [Fact]
    public void caso_de_mil_doscientos_cincuenta_al_diez_por_ciento()
    {
        Assert.Equal(1125.00m, pro_venta_note_edit.monto_desde_descuento(1250m, 10m));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void error_descuento_fuera_de_rango_se_rechaza(decimal descuento)
    {
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => pro_venta_note_edit.monto_desde_descuento(325.44m, descuento));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void error_monto_original_cero_o_negativo_se_rechaza(decimal monto_original)
    {
        Assert.ThrowsAny<ArgumentException>(() => pro_venta_note_edit.monto_desde_descuento(monto_original, 10m));
    }
}
