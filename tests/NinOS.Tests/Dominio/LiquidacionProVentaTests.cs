using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class LiquidacionProVentaTests
{
    [Fact]
    public void feliz_liquidacion_sale_sobre_el_monto_original_sin_descuento()
    {
        decimal cobrado = liquidacion_pro_venta.total_cobrado(325.44m, 32.54m);
        decimal nota = liquidacion_pro_venta.nota_por_pagar(325.44m, 81.36m, 36.61m);

        Assert.Equal(292.90m, cobrado);
        Assert.Equal(207.47m, nota);
        Assert.Equal(85.43m, liquidacion_pro_venta.diferencial(cobrado, nota));
    }

    [Fact]
    public void limite_relacion_sin_notas_da_todo_cero()
    {
        decimal cobrado = liquidacion_pro_venta.total_cobrado(0m, 0m);
        decimal nota = liquidacion_pro_venta.nota_por_pagar(0m, 0m, 0m);

        Assert.Equal(0m, cobrado);
        Assert.Equal(0m, nota);
        Assert.Equal(0m, liquidacion_pro_venta.diferencial(cobrado, nota));
    }

    [Fact]
    public void limite_redondeo_a_dos_decimales_en_el_diferencial()
    {
        decimal cobrado = liquidacion_pro_venta.total_cobrado(0.01m, 0.004m);

        Assert.Equal(0.01m, cobrado);
    }

    [Fact]
    public void error_monto_original_negativo_se_rechaza()
    {
        Assert.ThrowsAny<ArgumentException>(() => liquidacion_pro_venta.total_cobrado(-1m, 0m));
    }

    [Fact]
    public void error_gasto_negativo_se_rechaza()
    {
        Assert.ThrowsAny<ArgumentException>(() => liquidacion_pro_venta.nota_por_pagar(100m, -1m, 0m));
    }
}
