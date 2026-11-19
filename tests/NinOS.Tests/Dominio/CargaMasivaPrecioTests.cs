using NinOS.Domain.CargaMasiva;
using Xunit;

namespace NinOS.Tests.Dominio;

public class CargaMasivaPrecioTests
{
    private static FilaPlantilla fila(decimal? escrito, decimal? precio_archivo = 5m, string? texto = null)
    {
        return new FilaPlantilla(7, 12, "DEF-001", precio_archivo, escrito, texto);
    }

    private static ProductoActual producto(decimal precio_actual)
    {
        return new ProductoActual(12, "DEF-001", true, precio_actual);
    }

    [Fact]
    public void Precio_nuevo_queda_en_cambia()
    {
        ResultadoFila resultado = EvaluadorPrecio.evaluar(fila(5.5m), producto(5m));

        Assert.Equal(EstadoFila.Cambia, resultado.estado);
        Assert.Equal(5.5m, resultado.valor_final);
    }

    [Fact]
    public void Formula_con_mas_decimales_se_redondea_a_dos()
    {
        ResultadoFila resultado = EvaluadorPrecio.evaluar(fila(5.5000001m), producto(5m));

        Assert.Equal(EstadoFila.Cambia, resultado.estado);
        Assert.Equal(5.50m, resultado.valor_final);
    }

    [Fact]
    public void Mismo_precio_queda_igual()
    {
        ResultadoFila resultado = EvaluadorPrecio.evaluar(fila(5m), producto(5m));

        Assert.Equal(EstadoFila.Igual, resultado.estado);
        Assert.Equal(5m, resultado.valor_final);
    }

    [Fact]
    public void Precio_cero_no_cambia_y_conserva_el_precio_actual()
    {
        ResultadoFila resultado = EvaluadorPrecio.evaluar(fila(0m), producto(5m));

        Assert.Equal(EstadoFila.Igual, resultado.estado);
        Assert.Equal(5m, resultado.valor_final);
    }

    [Fact]
    public void Valor_que_redondea_a_cero_no_cambia_y_conserva_el_precio()
    {
        ResultadoFila resultado = EvaluadorPrecio.evaluar(fila(0.004m), producto(5m));

        Assert.Equal(EstadoFila.Igual, resultado.estado);
        Assert.Equal(5m, resultado.valor_final);
    }

    [Fact]
    public void Exactamente_cincuenta_por_ciento_no_avisa()
    {
        ResultadoFila resultado = EvaluadorPrecio.evaluar(fila(7.5m), producto(5m));

        Assert.Equal(EstadoFila.Cambia, resultado.estado);
    }

    [Fact]
    public void Mas_de_cincuenta_por_ciento_avisa()
    {
        ResultadoFila resultado = EvaluadorPrecio.evaluar(fila(9m), producto(5m));

        Assert.Equal(EstadoFila.Aviso, resultado.estado);
        Assert.Contains("+80%", resultado.motivo);
    }

    [Fact]
    public void Bajada_de_mas_de_cincuenta_por_ciento_avisa()
    {
        ResultadoFila resultado = EvaluadorPrecio.evaluar(fila(2m, precio_archivo: 10m), producto(10m));

        Assert.Equal(EstadoFila.Aviso, resultado.estado);
        Assert.Contains("-80%", resultado.motivo);
    }

    [Fact]
    public void Precio_que_cambio_desde_la_descarga_avisa()
    {
        ResultadoFila resultado = EvaluadorPrecio.evaluar(fila(6m, precio_archivo: 5m), producto(5.8m));

        Assert.Equal(EstadoFila.Aviso, resultado.estado);
        Assert.Contains("archivo 5.00", resultado.motivo);
        Assert.Contains("ahora 5.80", resultado.motivo);
    }

    [Fact]
    public void Precio_negativo_es_error()
    {
        ResultadoFila resultado = EvaluadorPrecio.evaluar(fila(-1m), producto(5m));

        Assert.Equal(EstadoFila.Error, resultado.estado);
        Assert.Contains("negativo", resultado.motivo);
    }

    [Fact]
    public void Texto_en_precio_es_error()
    {
        ResultadoFila resultado = EvaluadorPrecio.evaluar(fila(null, texto: "cinco"), producto(5m));

        Assert.Equal(EstadoFila.Error, resultado.estado);
        Assert.StartsWith("Fila 7:", resultado.motivo);
    }

    [Fact]
    public void Argumentos_nulos_se_rechazan()
    {
        Assert.Throws<ArgumentNullException>(() => EvaluadorPrecio.evaluar(null!, producto(1m)));
        Assert.Throws<ArgumentNullException>(() => EvaluadorPrecio.evaluar(fila(1m), null!));
    }
}
