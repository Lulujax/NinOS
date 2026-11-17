using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class NotaLimiteProductosTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(19)]
    [InlineData(20)]
    public void feliz_nota_con_hasta_20_productos_se_acepta(int cantidad)
    {
        nota_limite_productos.validar(cantidad);
    }

    [Fact]
    public void error_nota_con_21_productos_se_rechaza()
    {
        var excepcion = Assert.Throws<InvalidOperationException>(() => nota_limite_productos.validar(21));

        Assert.Contains("20", excepcion.Message);
    }

    [Fact]
    public void error_nota_sin_productos_se_rechaza()
    {
        Assert.Throws<InvalidOperationException>(() => nota_limite_productos.validar(0));
    }
}
