using NinOS.Domain.CargaMasiva;
using Xunit;

namespace NinOS.Tests.Dominio;

public class CargaMasivaCantidadTests
{
    private static FilaPlantilla fila(decimal? escrito, decimal? stock_archivo = 10m, string? texto = null)
    {
        return new FilaPlantilla(3, 12, "DEF-001", stock_archivo, escrito, texto);
    }

    private static ProductoActual producto(decimal stock_actual, bool activo = true)
    {
        return new ProductoActual(12, "DEF-001", activo, stock_actual);
    }

    [Fact]
    public void Conteo_con_valor_distinto_queda_en_cambia_con_el_valor_escrito()
    {
        ResultadoFila resultado = EvaluadorCantidad.evaluar(fila(25m), producto(10m), TipoCargaCantidad.Conteo);

        Assert.Equal(EstadoFila.Cambia, resultado.estado);
        Assert.Equal(25m, resultado.valor_final);
        Assert.Equal(15m, resultado.diferencia);
    }

    [Fact]
    public void Ajuste_positivo_suma_al_stock_actual()
    {
        ResultadoFila resultado = EvaluadorCantidad.evaluar(fila(15m), producto(10m), TipoCargaCantidad.Ajuste);

        Assert.Equal(EstadoFila.Cambia, resultado.estado);
        Assert.Equal(25m, resultado.valor_final);
    }

    [Fact]
    public void Ajuste_negativo_resta_del_stock_actual()
    {
        ResultadoFila resultado = EvaluadorCantidad.evaluar(fila(-3m), producto(10m), TipoCargaCantidad.Ajuste);

        Assert.Equal(EstadoFila.Cambia, resultado.estado);
        Assert.Equal(7m, resultado.valor_final);
    }

    [Fact]
    public void Ajuste_que_deja_el_stock_en_cero_es_valido()
    {
        ResultadoFila resultado = EvaluadorCantidad.evaluar(fila(-10m), producto(10m), TipoCargaCantidad.Ajuste);

        Assert.Equal(EstadoFila.Cambia, resultado.estado);
        Assert.Equal(0m, resultado.valor_final);
    }

    [Fact]
    public void Conteo_en_cero_es_valido_cuando_el_stock_actual_es_mayor()
    {
        ResultadoFila resultado = EvaluadorCantidad.evaluar(fila(0m), producto(10m), TipoCargaCantidad.Conteo);

        Assert.Equal(EstadoFila.Cambia, resultado.estado);
        Assert.Equal(0m, resultado.valor_final);
    }

    [Fact]
    public void Conteo_con_el_mismo_valor_queda_igual()
    {
        ResultadoFila resultado = EvaluadorCantidad.evaluar(fila(10m), producto(10m), TipoCargaCantidad.Conteo);

        Assert.Equal(EstadoFila.Igual, resultado.estado);
    }

    [Fact]
    public void Ajuste_en_cero_queda_igual_y_no_cambia_nada()
    {
        ResultadoFila resultado = EvaluadorCantidad.evaluar(fila(0m), producto(10m), TipoCargaCantidad.Ajuste);

        Assert.Equal(EstadoFila.Igual, resultado.estado);
        Assert.Equal(10m, resultado.valor_final);
    }

    [Fact]
    public void Conteo_avisa_cuando_el_stock_cambio_desde_la_descarga()
    {
        ResultadoFila resultado = EvaluadorCantidad.evaluar(fila(25m, stock_archivo: 10m), producto(7m), TipoCargaCantidad.Conteo);

        Assert.Equal(EstadoFila.Aviso, resultado.estado);
        Assert.Contains("archivo 10", resultado.motivo);
        Assert.Contains("ahora 7", resultado.motivo);
    }

    [Fact]
    public void Ajuste_no_avisa_por_cambios_desde_la_descarga()
    {
        ResultadoFila resultado = EvaluadorCantidad.evaluar(fila(5m, stock_archivo: 10m), producto(7m), TipoCargaCantidad.Ajuste);

        Assert.Equal(EstadoFila.Cambia, resultado.estado);
        Assert.Equal(12m, resultado.valor_final);
    }

    [Fact]
    public void Conteo_negativo_es_error()
    {
        ResultadoFila resultado = EvaluadorCantidad.evaluar(fila(-1m), producto(10m), TipoCargaCantidad.Conteo);

        Assert.Equal(EstadoFila.Error, resultado.estado);
        Assert.Contains("negativa", resultado.motivo);
        Assert.Equal(3, resultado.numero_fila);
    }

    [Fact]
    public void Ajuste_que_deja_stock_negativo_es_error()
    {
        ResultadoFila resultado = EvaluadorCantidad.evaluar(fila(-11m), producto(10m), TipoCargaCantidad.Ajuste);

        Assert.Equal(EstadoFila.Error, resultado.estado);
        Assert.Contains("-1", resultado.motivo);
    }

    [Fact]
    public void Decimal_es_error()
    {
        ResultadoFila resultado = EvaluadorCantidad.evaluar(fila(2.5m), producto(10m), TipoCargaCantidad.Conteo);

        Assert.Equal(EstadoFila.Error, resultado.estado);
        Assert.Contains("entero", resultado.motivo);
    }

    [Fact]
    public void Texto_en_la_celda_es_error_con_numero_de_fila()
    {
        ResultadoFila resultado = EvaluadorCantidad.evaluar(fila(null, texto: "abc"), producto(10m), TipoCargaCantidad.Conteo);

        Assert.Equal(EstadoFila.Error, resultado.estado);
        Assert.StartsWith("Fila 3:", resultado.motivo);
    }

    [Fact]
    public void Numero_demasiado_grande_es_error()
    {
        ResultadoFila resultado = EvaluadorCantidad.evaluar(fila(3_000_000_000m), producto(10m), TipoCargaCantidad.Conteo);

        Assert.Equal(EstadoFila.Error, resultado.estado);
    }

    [Fact]
    public void Argumentos_nulos_se_rechazan()
    {
        Assert.Throws<ArgumentNullException>(() => EvaluadorCantidad.evaluar(null!, producto(1m), TipoCargaCantidad.Conteo));
        Assert.Throws<ArgumentNullException>(() => EvaluadorCantidad.evaluar(fila(1m), null!, TipoCargaCantidad.Conteo));
    }
}
