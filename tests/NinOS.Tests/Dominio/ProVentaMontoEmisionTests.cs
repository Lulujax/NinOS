using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class ProVentaMontoEmisionTests
{
    [Fact]
    public void feliz_nota_de_renglones_suma_precio_de_lista_sin_descuento()
    {
        var renglones = new[] { (2, 100m), (1, 125.44m) };

        Assert.Equal(325.44m, original_amount_calculator.monto_al_emitir(renglones));
    }

    [Fact]
    public void limite_un_renglon_de_una_unidad_da_su_precio()
    {
        Assert.Equal(0.01m, original_amount_calculator.monto_al_emitir(new[] { (1, 0.01m) }));
    }

    [Fact]
    public void limite_renglon_de_precio_cero_da_cero()
    {
        Assert.Equal(0m, original_amount_calculator.monto_al_emitir(new[] { (3, 0m) }));
    }

    [Fact]
    public void error_nota_sin_renglones_se_rechaza()
    {
        Assert.ThrowsAny<ArgumentException>(() => original_amount_calculator.monto_al_emitir(Array.Empty<(int, decimal)>()));
    }

    [Fact]
    public void error_cantidad_negativa_en_renglon_se_rechaza()
    {
        Assert.ThrowsAny<ArgumentException>(() => original_amount_calculator.monto_al_emitir(new[] { (-1, 10m) }));
    }
}
