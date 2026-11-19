using NinOS.Domain.CargaMasiva;
using Xunit;

namespace NinOS.Tests.Dominio;

public class CargaMasivaArmadorTests
{
    private static readonly DateTime Ahora = new DateTime(2026, 11, 19, 10, 0, 0, DateTimeKind.Utc);

    private static CabeceraPlantilla cabecera_cantidad()
    {
        return new CabeceraPlantilla(TipoPlantilla.Cantidad, null, string.Empty, false, Ahora);
    }

    private static CabeceraPlantilla cabecera_precio_principal()
    {
        return new CabeceraPlantilla(TipoPlantilla.Precio, 1, "Principal", true, Ahora);
    }

    private static IReadOnlyDictionary<int, ProductoActual> productos(params ProductoActual[] lista)
    {
        return lista.ToDictionary(p => p.id_product);
    }

    [Fact]
    public void Filas_vacias_no_aparecen_en_la_vista_previa()
    {
        var filas = new List<FilaPlantilla>
        {
            new FilaPlantilla(4, 1, "A-1", 10m, null, null),
            new FilaPlantilla(5, 2, "A-2", 10m, 12m, null)
        };

        VistaPreviaCarga vista = ArmadorVistaPrevia.armar(
            cabecera_cantidad(), TipoCargaCantidad.Conteo, filas,
            productos(new ProductoActual(1, "A-1", true, 10m), new ProductoActual(2, "A-2", true, 10m)), 0);

        Assert.Single(vista.resultados);
        Assert.Equal(5, vista.resultados[0].numero_fila);
        Assert.Equal(1, vista.cambian);
        Assert.True(vista.puede_aplicar);
    }

    [Fact]
    public void Producto_repetido_es_error_en_todas_sus_filas()
    {
        var filas = new List<FilaPlantilla>
        {
            new FilaPlantilla(4, 1, "A-1", 10m, 12m, null),
            new FilaPlantilla(9, 1, "A-1", 10m, 15m, null)
        };

        VistaPreviaCarga vista = ArmadorVistaPrevia.armar(
            cabecera_cantidad(), TipoCargaCantidad.Conteo, filas,
            productos(new ProductoActual(1, "A-1", true, 10m)), 0);

        Assert.Equal(2, vista.errores);
        Assert.False(vista.puede_aplicar);
        Assert.All(vista.resultados, r => Assert.Contains("más de una vez", r.motivo));
    }

    [Fact]
    public void Id_inexistente_es_error()
    {
        var filas = new List<FilaPlantilla> { new FilaPlantilla(4, 99, "X-9", 10m, 12m, null) };

        VistaPreviaCarga vista = ArmadorVistaPrevia.armar(
            cabecera_cantidad(), TipoCargaCantidad.Conteo, filas, productos(), 0);

        Assert.Equal(EstadoFila.Error, vista.resultados[0].estado);
        Assert.Contains("ya no existe", vista.resultados[0].motivo);
    }

    [Fact]
    public void Producto_eliminado_es_error()
    {
        var filas = new List<FilaPlantilla> { new FilaPlantilla(4, 1, "A-1", 10m, 12m, null) };

        VistaPreviaCarga vista = ArmadorVistaPrevia.armar(
            cabecera_cantidad(), TipoCargaCantidad.Conteo, filas,
            productos(new ProductoActual(1, "A-1", false, 10m)), 0);

        Assert.Equal(EstadoFila.Error, vista.resultados[0].estado);
        Assert.Contains("eliminado", vista.resultados[0].motivo);
    }

    [Fact]
    public void Codigo_que_no_coincide_con_el_id_es_error()
    {
        var filas = new List<FilaPlantilla> { new FilaPlantilla(4, 1, "OTRO", 10m, 12m, null) };

        VistaPreviaCarga vista = ArmadorVistaPrevia.armar(
            cabecera_cantidad(), TipoCargaCantidad.Conteo, filas,
            productos(new ProductoActual(1, "A-1", true, 10m)), 0);

        Assert.Equal(EstadoFila.Error, vista.resultados[0].estado);
        Assert.Contains("no coincide", vista.resultados[0].motivo);
    }

    [Fact]
    public void Codigo_con_mayusculas_distintas_si_coincide()
    {
        var filas = new List<FilaPlantilla> { new FilaPlantilla(4, 1, "a-1", 10m, 12m, null) };

        VistaPreviaCarga vista = ArmadorVistaPrevia.armar(
            cabecera_cantidad(), TipoCargaCantidad.Conteo, filas,
            productos(new ProductoActual(1, "A-1", true, 10m)), 0);

        Assert.Equal(EstadoFila.Cambia, vista.resultados[0].estado);
    }

    [Fact]
    public void Con_un_error_no_se_puede_aplicar_aunque_haya_cambios()
    {
        var filas = new List<FilaPlantilla>
        {
            new FilaPlantilla(4, 1, "A-1", 10m, 12m, null),
            new FilaPlantilla(5, 2, "A-2", 10m, null, "abc")
        };

        VistaPreviaCarga vista = ArmadorVistaPrevia.armar(
            cabecera_cantidad(), TipoCargaCantidad.Conteo, filas,
            productos(new ProductoActual(1, "A-1", true, 10m), new ProductoActual(2, "A-2", true, 10m)), 0);

        Assert.Equal(1, vista.cambian);
        Assert.Equal(1, vista.errores);
        Assert.False(vista.puede_aplicar);
    }

    [Fact]
    public void Sin_cambios_no_se_puede_aplicar()
    {
        var filas = new List<FilaPlantilla> { new FilaPlantilla(4, 1, "A-1", 10m, 10m, null) };

        VistaPreviaCarga vista = ArmadorVistaPrevia.armar(
            cabecera_cantidad(), TipoCargaCantidad.Conteo, filas,
            productos(new ProductoActual(1, "A-1", true, 10m)), 0);

        Assert.Equal(1, vista.iguales);
        Assert.False(vista.puede_aplicar);
    }

    [Fact]
    public void Precio_usa_el_evaluador_de_precio_y_cuenta_otras_listas()
    {
        var filas = new List<FilaPlantilla> { new FilaPlantilla(4, 1, "A-1", 5m, 5.5m, null) };

        VistaPreviaCarga vista = ArmadorVistaPrevia.armar(
            cabecera_precio_principal(), null, filas,
            productos(new ProductoActual(1, "A-1", true, 5m)), 3);

        Assert.Equal(5.5m, vista.resultados[0].valor_final);
        Assert.Equal(3, vista.productos_en_otras_listas);
    }

    [Fact]
    public void Cantidad_sin_tipo_de_carga_se_rechaza()
    {
        Assert.Throws<ArgumentException>(() => ArmadorVistaPrevia.armar(
            cabecera_cantidad(), null, new List<FilaPlantilla>(), productos(), 0));
    }

    [Fact]
    public void Argumentos_nulos_se_rechazan()
    {
        Assert.Throws<ArgumentNullException>(() => ArmadorVistaPrevia.armar(
            null!, TipoCargaCantidad.Conteo, new List<FilaPlantilla>(), productos(), 0));
    }

    [Fact]
    public void Cabecera_de_cantidad_no_puede_llevar_lista()
    {
        Assert.Throws<ArgumentException>(() =>
            new CabeceraPlantilla(TipoPlantilla.Cantidad, 4, string.Empty, false, Ahora));
    }

    [Fact]
    public void Cabecera_de_precio_necesita_lista_y_nombre()
    {
        Assert.Throws<ArgumentException>(() => new CabeceraPlantilla(TipoPlantilla.Precio, null, "X", false, Ahora));
        Assert.Throws<ArgumentException>(() => new CabeceraPlantilla(TipoPlantilla.Precio, 2, " ", false, Ahora));
    }

    [Fact]
    public void Fila_con_numero_de_fila_cero_se_rechaza()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FilaPlantilla(0, 1, "A-1", 1m, 1m, null));
    }
}
