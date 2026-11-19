using NinOS.Domain.CargaMasiva;
using NinOS.UI.Common.CargaMasiva;
using Xunit;

namespace NinOS.Tests.Documentos;

public class NombreSugeridoPlantillaTests
{
    private static readonly DateTime Ahora = new DateTime(2026, 11, 19, 14, 5, 0);

    [Fact]
    public void Plantilla_de_cantidad_lleva_fecha_y_hora()
    {
        var cabecera = new CabeceraPlantilla(TipoPlantilla.Cantidad, null, string.Empty, false, Ahora);

        Assert.Equal("Plantilla_Cantidad_2026-11-19_1405.xlsx", CargaMasivaFlujo.nombre_sugerido(cabecera, Ahora));
    }

    [Fact]
    public void Plantilla_de_la_principal_usa_la_palabra_principal()
    {
        var cabecera = new CabeceraPlantilla(TipoPlantilla.Precio, 1, "Principal", true, Ahora);

        Assert.Equal("Plantilla_Precio_Principal_2026-11-19_1405.xlsx", CargaMasivaFlujo.nombre_sugerido(cabecera, Ahora));
    }

    [Fact]
    public void Nombre_de_lista_con_caracteres_invalidos_se_limpia()
    {
        var cabecera = new CabeceraPlantilla(TipoPlantilla.Precio, 7, "Mayoristas / Norte", false, Ahora);

        string nombre = CargaMasivaFlujo.nombre_sugerido(cabecera, Ahora);

        Assert.Equal("Plantilla_Precio_Mayoristas_Norte_2026-11-19_1405.xlsx", nombre);
    }

    [Fact]
    public void Nombre_de_lista_que_queda_vacio_usa_lista_por_defecto()
    {
        var cabecera = new CabeceraPlantilla(TipoPlantilla.Precio, 7, "///", false, Ahora);

        Assert.Equal("Plantilla_Precio_Lista_2026-11-19_1405.xlsx", CargaMasivaFlujo.nombre_sugerido(cabecera, Ahora));
    }

    [Fact]
    public void Cabecera_nula_se_rechaza()
    {
        Assert.Throws<ArgumentNullException>(() => CargaMasivaFlujo.nombre_sugerido(null!, Ahora));
    }
}
