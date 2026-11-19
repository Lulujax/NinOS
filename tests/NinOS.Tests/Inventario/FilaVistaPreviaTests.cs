using NinOS.Domain.CargaMasiva;
using NinOS.UI.Common.CargaMasiva;
using Xunit;

namespace NinOS.Tests.Inventario;

public class FilaVistaPreviaTests
{
    private static ResultadoFila resultado(decimal? actual, decimal? final_, EstadoFila estado, string? motivo = null, string? descripcion = "Shampoo X")
    {
        var fila = new FilaPlantilla(8, 1, "DEF-001", 10m, 25m, null, descripcion);
        return new ResultadoFila(fila, actual, final_, estado, motivo);
    }

    [Fact]
    public void Cantidad_con_aumento_muestra_signo_mas()
    {
        FilaVistaPrevia fila = FilaVistaPrevia.desde(resultado(10m, 25m, EstadoFila.Cambia), TipoPlantilla.Cantidad);

        Assert.Equal("+15", fila.cambio);
        Assert.Equal("25", fila.nuevo);
        Assert.Equal("Cambia", fila.estado);
        Assert.Equal("8", fila.numero_fila);
    }

    [Fact]
    public void Cantidad_con_baja_muestra_signo_menos()
    {
        FilaVistaPrevia fila = FilaVistaPrevia.desde(resultado(10m, 7m, EstadoFila.Cambia), TipoPlantilla.Cantidad);

        Assert.Equal("-3", fila.cambio);
    }

    [Fact]
    public void Precio_muestra_dos_decimales_y_porcentaje()
    {
        ResultadoFila precio = resultado(5m, 5.5m, EstadoFila.Cambia);

        FilaVistaPrevia fila = FilaVistaPrevia.desde(precio, TipoPlantilla.Precio);

        Assert.Equal("5.00", fila.actual);
        Assert.Equal("5.50", fila.nuevo);
        Assert.Equal("+10%", fila.cambio);
    }

    [Fact]
    public void Error_sin_valor_final_muestra_guion_y_el_motivo()
    {
        ResultadoFila error = ResultadoFila.error(new FilaPlantilla(9, 1, "DEF-001", 10m, null, "abc"), 10m, "la cantidad tiene letras o no es un número");

        FilaVistaPrevia fila = FilaVistaPrevia.desde(error, TipoPlantilla.Cantidad);

        Assert.Equal("—", fila.nuevo);
        Assert.Equal("—", fila.cambio);
        Assert.Equal("Error", fila.estado);
        Assert.StartsWith("Fila 9:", fila.motivo);
    }

    [Fact]
    public void Aviso_se_muestra_como_revisar()
    {
        Assert.Equal("Revisar", FilaVistaPrevia.texto_estado(EstadoFila.Aviso));
    }

    [Fact]
    public void Igual_se_muestra_como_sin_cambio()
    {
        Assert.Equal("Sin cambio", FilaVistaPrevia.texto_estado(EstadoFila.Igual));
    }

    [Fact]
    public void Descripcion_del_archivo_se_copia_a_la_fila()
    {
        FilaVistaPrevia fila = FilaVistaPrevia.desde(resultado(10m, 25m, EstadoFila.Cambia, descripcion: "Crema Y"), TipoPlantilla.Cantidad);

        Assert.Equal("Crema Y", fila.descripcion);
    }

    [Fact]
    public void Estado_fuera_de_rango_se_rechaza()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FilaVistaPrevia.texto_estado((EstadoFila)99));
    }

    [Fact]
    public void Resultado_nulo_se_rechaza()
    {
        Assert.Throws<ArgumentNullException>(() => FilaVistaPrevia.desde(null!, TipoPlantilla.Cantidad));
    }
}
