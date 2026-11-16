using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class OriginalAmountCalculatorTests
{
    [Fact]
    public void feliz_suma_renglones_a_precio_de_lista_sin_devoluciones()
    {
        var lineas = new[]
        {
            new linea_nota(1, 10, null, 2, 10m),
            new linea_nota(1, 20, null, 1, 5.5m),
        };

        var resultado = original_amount_calculator.calcular(lineas, Array.Empty<devolucion_nota>());

        Assert.Equal(25.50m, resultado[1]);
    }

    [Fact]
    public void feliz_devolucion_de_una_unidad_baja_solo_esa_unidad()
    {
        var lineas = new[] { new linea_nota(1, 10, null, 2, 10m), new linea_nota(1, 20, null, 1, 5.5m) };
        var devoluciones = new[] { new devolucion_nota(1, 10, null, 1) };

        var resultado = original_amount_calculator.calcular(lineas, devoluciones);

        Assert.Equal(15.50m, resultado[1]);
    }

    [Fact]
    public void limite_producto_repetido_en_dos_renglones_se_descuenta_una_sola_vez()
    {
        var lineas = new[] { new linea_nota(1, 10, null, 2, 10m), new linea_nota(1, 10, null, 3, 10m) };
        var devoluciones = new[] { new devolucion_nota(1, 10, null, 1) };

        var resultado = original_amount_calculator.calcular(lineas, devoluciones);

        Assert.Equal(40.00m, resultado[1]);
    }

    [Fact]
    public void limite_devolver_mas_de_lo_vendido_deja_cero()
    {
        var lineas = new[] { new linea_nota(1, 10, null, 2, 10m) };
        var devoluciones = new[] { new devolucion_nota(1, 10, null, 5) };

        var resultado = original_amount_calculator.calcular(lineas, devoluciones);

        Assert.Equal(0m, resultado[1]);
    }

    [Fact]
    public void limite_promocion_sin_producto_se_identifica_por_promocion()
    {
        var lineas = new[] { new linea_nota(1, null, 3, 1, 7m) };
        var devoluciones = new[] { new devolucion_nota(1, null, 3, 1) };

        var resultado = original_amount_calculator.calcular(lineas, devoluciones);

        Assert.Equal(0m, resultado[1]);
    }

    [Fact]
    public void limite_devolucion_de_otra_nota_no_baja_esta_nota()
    {
        var lineas = new[] { new linea_nota(1, 10, null, 2, 10m) };
        var devoluciones = new[] { new devolucion_nota(2, 10, null, 2) };

        var resultado = original_amount_calculator.calcular(lineas, devoluciones);

        Assert.Equal(20.00m, resultado[1]);
    }

    [Fact]
    public void error_cantidad_negativa_se_rechaza()
    {
        var lineas = new[] { new linea_nota(1, 10, null, -1, 10m) };

        Assert.ThrowsAny<ArgumentException>(() => original_amount_calculator.calcular(lineas, Array.Empty<devolucion_nota>()));
    }

    [Fact]
    public void error_precio_negativo_se_rechaza()
    {
        var lineas = new[] { new linea_nota(1, 10, null, 1, -10m) };

        Assert.ThrowsAny<ArgumentException>(() => original_amount_calculator.calcular(lineas, Array.Empty<devolucion_nota>()));
    }

    [Fact]
    public void error_devolucion_negativa_se_rechaza()
    {
        var lineas = new[] { new linea_nota(1, 10, null, 1, 10m) };
        var devoluciones = new[] { new devolucion_nota(1, 10, null, -1) };

        Assert.ThrowsAny<ArgumentException>(() => original_amount_calculator.calcular(lineas, devoluciones));
    }
}
