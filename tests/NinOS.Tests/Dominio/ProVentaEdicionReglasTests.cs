using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class ProVentaEdicionReglasTests
{
    [Fact]
    public void feliz_nota_vigente_con_monto_esperado_se_acepta()
    {
        var resultado = pro_venta_note_edit.evaluar_edicion("Pendiente", false, 325.44m, 292.90m, 292.90m, 325.44m);

        Assert.True(resultado.aceptada);
    }

    [Fact]
    public void limite_monto_igual_al_original_se_acepta()
    {
        var resultado = pro_venta_note_edit.evaluar_edicion("Pendiente", false, 325.44m, 325.44m, 325.44m, 325.44m);

        Assert.True(resultado.aceptada);
    }

    [Fact]
    public void limite_monto_cero_se_acepta()
    {
        var resultado = pro_venta_note_edit.evaluar_edicion("Pendiente", false, 325.44m, 325.44m, 325.44m, 0m);

        Assert.True(resultado.aceptada);
    }

    [Theory]
    [InlineData("Anulada")]
    [InlineData("Devuelta")]
    public void error_nota_anulada_o_devuelta_se_rechaza(string estado)
    {
        var resultado = pro_venta_note_edit.evaluar_edicion(estado, false, 325.44m, 325.44m, 325.44m, 300m);

        Assert.False(resultado.aceptada);
        Assert.Contains("anulada o devuelta", resultado.motivo);
    }

    [Fact]
    public void error_relacion_pagada_bloquea_la_edicion_con_mensaje_de_pagada()
    {
        var resultado = pro_venta_note_edit.evaluar_edicion("Pendiente", true, 325.44m, 325.44m, 325.44m, 300m);

        Assert.False(resultado.aceptada);
        Assert.Contains("ya está pagada", resultado.motivo);
    }

    [Fact]
    public void error_nota_que_cambio_desde_que_se_abrio_se_rechaza()
    {
        var resultado = pro_venta_note_edit.evaluar_edicion("Pendiente", false, 325.44m, 292.90m, 325.44m, 300m);

        Assert.False(resultado.aceptada);
        Assert.Contains("cambió", resultado.motivo);
    }

    [Fact]
    public void error_monto_mayor_al_original_se_rechaza_con_el_maximo()
    {
        var resultado = pro_venta_note_edit.evaluar_edicion("Pendiente", false, 325.44m, 325.44m, 325.44m, 400m);

        Assert.False(resultado.aceptada);
        Assert.Contains("325,44", resultado.motivo);
    }

    [Fact]
    public void error_monto_negativo_se_rechaza()
    {
        var resultado = pro_venta_note_edit.evaluar_edicion("Pendiente", false, 325.44m, 325.44m, 325.44m, -1m);

        Assert.False(resultado.aceptada);
    }
}
