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
    public void descuento_desde_monto_reconoce_el_pago_sin_descuento()
    {
        Assert.Equal(0m, pro_venta_note_edit.descuento_desde_monto(325.44m, 325.44m));
    }

    [Fact]
    public void descuento_desde_monto_reconoce_el_diez_por_ciento_y_el_cinco()
    {
        Assert.Equal(10.00m, pro_venta_note_edit.descuento_desde_monto(325.44m, 292.90m));
        Assert.Equal(5.00m, pro_venta_note_edit.descuento_desde_monto(325.44m, 309.17m));
    }

    [Fact]
    public void limite_cien_por_ciento_da_cero_y_monto_cero_da_cien_por_ciento()
    {
        Assert.Equal(0m, pro_venta_note_edit.monto_desde_descuento(325.44m, 100m));
        Assert.Equal(100m, pro_venta_note_edit.descuento_desde_monto(325.44m, 0m));
    }

    [Fact]
    public void limite_monto_igual_al_original_da_descuento_cero()
    {
        Assert.Equal(0m, pro_venta_note_edit.descuento_desde_monto(1250m, 1250m));
    }

    [Fact]
    public void caso_de_mil_doscientos_cincuenta_al_diez_por_ciento()
    {
        Assert.Equal(1125.00m, pro_venta_note_edit.monto_desde_descuento(1250m, 10m));
        Assert.Equal(10.00m, pro_venta_note_edit.descuento_desde_monto(1250m, 1125.00m));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void error_descuento_fuera_de_rango_se_rechaza(decimal descuento)
    {
        Assert.ThrowsAny<ArgumentException>(() => pro_venta_note_edit.monto_desde_descuento(325.44m, descuento));
    }

    [Fact]
    public void error_monto_a_cobrar_mayor_al_original_se_rechaza()
    {
        Assert.ThrowsAny<ArgumentException>(() => pro_venta_note_edit.descuento_desde_monto(325.44m, 400m));
    }

    [Fact]
    public void error_monto_a_cobrar_negativo_se_rechaza()
    {
        Assert.ThrowsAny<ArgumentException>(() => pro_venta_note_edit.descuento_desde_monto(325.44m, -1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void error_monto_original_cero_o_negativo_se_rechaza(decimal monto_original)
    {
        Assert.ThrowsAny<ArgumentException>(() => pro_venta_note_edit.monto_desde_descuento(monto_original, 10m));
        Assert.ThrowsAny<ArgumentException>(() => pro_venta_note_edit.descuento_desde_monto(monto_original, 0m));
    }
}
