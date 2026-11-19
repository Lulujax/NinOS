using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using NinOS.Domain.CargaMasiva;
using NinOS.UI.Common.CargaMasiva;
using Xunit;

namespace NinOS.Tests.Documentos;

public class PlantillaInventarioExcelTests : IDisposable
{
    private static readonly DateTime Ahora = new DateTime(2026, 11, 19, 10, 0, 0, DateTimeKind.Utc);
    private readonly string carpeta = Path.Combine(Path.GetTempPath(), "ninos-plantilla-tests-" + Guid.NewGuid().ToString("N"));

    public PlantillaInventarioExcelTests()
    {
        Directory.CreateDirectory(carpeta);
    }

    public void Dispose()
    {
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, recursive: true);
    }

    private string ruta(string nombre) => Path.Combine(carpeta, nombre);

    private static List<ProductoPlantilla> productos_de_prueba()
    {
        return new List<ProductoPlantilla>
        {
            new ProductoPlantilla(1, "DEF-001", "DEFILE", "Shampoo X", 10m, 5m, 4.5m),
            new ProductoPlantilla(2, "DEF-002", "DEFILE", "Crema Y", 0m, 3m, 3m)
        };
    }

    private static CabeceraPlantilla cabecera_cantidad() => new CabeceraPlantilla(TipoPlantilla.Cantidad, null, string.Empty, false, Ahora);

    private static CabeceraPlantilla cabecera_principal() => new CabeceraPlantilla(TipoPlantilla.Precio, 1, "Principal", true, Ahora);

    private static CabeceraPlantilla cabecera_lista() => new CabeceraPlantilla(TipoPlantilla.Precio, 7, "Mayoristas", false, Ahora);

    [Fact]
    public void Plantilla_de_cantidad_se_lee_con_sus_productos_y_la_celda_editable_vacia()
    {
        string archivo = ruta("cantidad.xlsx");
        PlantillaInventarioExcelWriter.generar(cabecera_cantidad(), productos_de_prueba(), archivo);

        LecturaPlantilla lectura = PlantillaInventarioExcelReader.leer(archivo);

        Assert.Equal(TipoPlantilla.Cantidad, lectura.cabecera.tipo);
        Assert.Equal(2, lectura.filas.Count);
        Assert.Equal(1, lectura.filas[0].id_product);
        Assert.Equal("DEF-001", lectura.filas[0].codigo);
        Assert.Equal(10m, lectura.filas[0].valor_archivo);
        Assert.True(lectura.filas[0].esta_vacia);
    }

    [Fact]
    public void Valor_escrito_en_cantidad_se_recupera_al_leer()
    {
        string archivo = ruta("cantidad-escrita.xlsx");
        PlantillaInventarioExcelWriter.generar(cabecera_cantidad(), productos_de_prueba(), archivo);
        escribir_en_columna(archivo, 5, 6, 15);

        LecturaPlantilla lectura = PlantillaInventarioExcelReader.leer(archivo);

        Assert.Equal(15m, lectura.filas[0].valor_escrito);
        Assert.True(lectura.filas[1].esta_vacia);
    }

    [Fact]
    public void Plantilla_de_precio_principal_usa_precio_actual_como_referencia()
    {
        string archivo = ruta("principal.xlsx");
        PlantillaInventarioExcelWriter.generar(cabecera_principal(), productos_de_prueba(), archivo);
        escribir_en_columna(archivo, 5, 6, 5.5);

        LecturaPlantilla lectura = PlantillaInventarioExcelReader.leer(archivo);

        Assert.True(lectura.cabecera.es_principal);
        Assert.Equal(5m, lectura.filas[0].valor_archivo);
        Assert.Equal(5.5m, lectura.filas[0].valor_escrito);
    }

    [Fact]
    public void Plantilla_de_lista_secundaria_usa_precio_en_esta_lista_como_referencia()
    {
        string archivo = ruta("lista.xlsx");
        PlantillaInventarioExcelWriter.generar(cabecera_lista(), productos_de_prueba(), archivo);

        LecturaPlantilla lectura = PlantillaInventarioExcelReader.leer(archivo);

        Assert.False(lectura.cabecera.es_principal);
        Assert.Equal(7, lectura.cabecera.id_price_list);
        Assert.Equal("Mayoristas", lectura.cabecera.nombre_lista);
        Assert.Equal(4.5m, lectura.filas[0].valor_archivo);
    }

    [Fact]
    public void Texto_en_la_celda_editable_se_marca_como_texto_escrito()
    {
        string archivo = ruta("texto.xlsx");
        PlantillaInventarioExcelWriter.generar(cabecera_cantidad(), productos_de_prueba(), archivo);
        escribir_texto(archivo, 5, 6, "abc");

        LecturaPlantilla lectura = PlantillaInventarioExcelReader.leer(archivo);

        Assert.Equal("abc", lectura.filas[0].texto_escrito);
        Assert.Null(lectura.filas[0].valor_escrito);
    }

    [Fact]
    public void Plantilla_sin_productos_se_genera_y_se_lee_sin_filas()
    {
        string archivo = ruta("vacia.xlsx");
        PlantillaInventarioExcelWriter.generar(cabecera_cantidad(), new List<ProductoPlantilla>(), archivo);

        LecturaPlantilla lectura = PlantillaInventarioExcelReader.leer(archivo);

        Assert.Empty(lectura.filas);
    }

    [Fact]
    public void La_hoja_queda_protegida_y_solo_la_columna_editable_se_desbloquea()
    {
        string archivo = ruta("protegida.xlsx");
        PlantillaInventarioExcelWriter.generar(cabecera_principal(), productos_de_prueba(), archivo);

        using XLWorkbook libro = new XLWorkbook(archivo);
        IXLWorksheet hoja = libro.Worksheet(FormatoPlantilla.NombreHoja);

        Assert.True(hoja.IsProtected);
        Assert.True(hoja.Cell(5, 1).Style.Protection.Locked);
        Assert.True(hoja.Cell(5, 5).Style.Protection.Locked);
        Assert.False(hoja.Cell(5, 6).Style.Protection.Locked);
    }

    [Fact]
    public void Hoja_oculta_de_identificacion_queda_muy_oculta()
    {
        string archivo = ruta("oculta.xlsx");
        PlantillaInventarioExcelWriter.generar(cabecera_cantidad(), productos_de_prueba(), archivo);

        using XLWorkbook libro = new XLWorkbook(archivo);

        Assert.Equal(XLWorksheetVisibility.VeryHidden, libro.Worksheet(FormatoPlantilla.NombreHojaOculta).Visibility);
    }

    [Fact]
    public void Excel_ajeno_se_rechaza_con_mensaje_de_no_es_plantilla()
    {
        string archivo = ruta("ajeno.xlsx");
        using (XLWorkbook libro = new XLWorkbook())
        {
            libro.Worksheets.Add("Hoja1").Cell(1, 1).Value = "hola";
            libro.SaveAs(archivo);
        }

        PlantillaInvalidaException error = Assert.Throws<PlantillaInvalidaException>(() => PlantillaInventarioExcelReader.leer(archivo));

        Assert.Equal(PlantillaInventarioExcelReader.MensajeNoEsPlantilla, error.Message);
    }

    [Fact]
    public void Archivo_danado_se_rechaza_con_mensaje_claro()
    {
        string archivo = ruta("danado.xlsx");
        File.WriteAllText(archivo, "esto no es un excel");

        PlantillaInvalidaException error = Assert.Throws<PlantillaInvalidaException>(() => PlantillaInventarioExcelReader.leer(archivo));

        Assert.Equal(PlantillaInventarioExcelReader.MensajeDanado, error.Message);
    }

    [Fact]
    public void Version_mas_nueva_que_la_soportada_se_rechaza()
    {
        string archivo = ruta("version.xlsx");
        PlantillaInventarioExcelWriter.generar(cabecera_cantidad(), productos_de_prueba(), archivo);
        escribir_en_hoja_oculta(archivo, 2, 2, "99");

        PlantillaInvalidaException error = Assert.Throws<PlantillaInvalidaException>(() => PlantillaInventarioExcelReader.leer(archivo));

        Assert.Equal(PlantillaInventarioExcelReader.MensajeNoEsPlantilla, error.Message);
    }

    [Fact]
    public void Archivo_inexistente_se_rechaza()
    {
        PlantillaInvalidaException error = Assert.Throws<PlantillaInvalidaException>(() => PlantillaInventarioExcelReader.leer(ruta("no-existe.xlsx")));

        Assert.Equal("El archivo no existe.", error.Message);
    }

    [Fact]
    public void Ruta_vacia_es_error_de_argumento()
    {
        Assert.Throws<ArgumentException>(() => PlantillaInventarioExcelReader.leer(" "));
    }

    [Fact]
    public void Generar_con_argumentos_nulos_se_rechaza()
    {
        Assert.Throws<ArgumentNullException>(() => PlantillaInventarioExcelWriter.generar(null!, productos_de_prueba(), ruta("x.xlsx")));
        Assert.Throws<ArgumentNullException>(() => PlantillaInventarioExcelWriter.generar(cabecera_cantidad(), null!, ruta("x.xlsx")));
        Assert.Throws<ArgumentException>(() => PlantillaInventarioExcelWriter.generar(cabecera_cantidad(), productos_de_prueba(), ""));
    }

    private static void escribir_en_columna(string archivo, int fila, int columna, double valor)
    {
        using XLWorkbook libro = new XLWorkbook(archivo);
        libro.Worksheet(FormatoPlantilla.NombreHoja).Cell(fila, columna).Value = valor;
        libro.Save();
    }

    private static void escribir_texto(string archivo, int fila, int columna, string texto)
    {
        using XLWorkbook libro = new XLWorkbook(archivo);
        libro.Worksheet(FormatoPlantilla.NombreHoja).Cell(fila, columna).Value = texto;
        libro.Save();
    }

    private static void escribir_en_hoja_oculta(string archivo, int fila, int columna, string texto)
    {
        using XLWorkbook libro = new XLWorkbook(archivo);
        libro.Worksheet(FormatoPlantilla.NombreHojaOculta).Cell(fila, columna).Value = texto;
        libro.Save();
    }
}
