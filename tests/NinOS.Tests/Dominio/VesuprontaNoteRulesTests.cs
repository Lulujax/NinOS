using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class VesuprontaNoteRulesTests
{
    [Fact]
    public void feliz_liquidacion_de_344_48_cuadra_con_la_hoja_de_luis()
    {
        var liquidacion = vesupronta_note_rules.calcular_liquidacion(344.48m);

        Assert.Equal(344.48m, liquidacion.total_general);
        Assert.Equal(86.12m, liquidacion.veinticinco_por_ciento_lista);
        Assert.Equal(258.36m, liquidacion.total_general_menos_veinticinco);
        Assert.Equal(38.75m, liquidacion.quince_por_ciento_lista);
        Assert.Equal(219.61m, liquidacion.total_a_pagar_pronta);
        Assert.Equal(124.87m, liquidacion.diferencial);
    }

    [Fact]
    public void limite_total_cero_da_todas_las_cifras_en_cero()
    {
        var liquidacion = vesupronta_note_rules.calcular_liquidacion(0m);

        Assert.Equal(0m, liquidacion.total_a_pagar_pronta);
        Assert.Equal(0m, liquidacion.diferencial);
    }

    [Fact]
    public void limite_redondeo_mitad_hacia_arriba_y_restas_sobre_cifras_redondeadas()
    {
        var liquidacion = vesupronta_note_rules.calcular_liquidacion(10.10m);

        Assert.Equal(2.53m, liquidacion.veinticinco_por_ciento_lista);
        Assert.Equal(7.57m, liquidacion.total_general_menos_veinticinco);
        Assert.Equal(1.14m, liquidacion.quince_por_ciento_lista);
        Assert.Equal(6.43m, liquidacion.total_a_pagar_pronta);
        Assert.Equal(3.67m, liquidacion.diferencial);
    }

    [Fact]
    public void error_total_negativo_se_rechaza()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => vesupronta_note_rules.calcular_liquidacion(-1m));
    }

    [Theory]
    [InlineData("VES", true)]
    [InlineData("VESVOL", true)]
    [InlineData("VESPRM", true)]
    [InlineData("GEN", false)]
    [InlineData("MAR", false)]
    [InlineData(null, false)]
    public void feliz_reconoce_los_tipos_de_vesupronta(string? codigo, bool esperado)
    {
        Assert.Equal(esperado, NoteTypeCodes.is_vesupronta(codigo));
    }
}
