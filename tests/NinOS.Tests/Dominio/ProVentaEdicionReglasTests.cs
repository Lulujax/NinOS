using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class ProVentaEdicionReglasTests
{
    [Fact]
    public void feliz_monto_nuevo_con_monto_esperado_se_acepta()
    {
        var resultado = pro_venta_note_edit.evaluar_monto("Pendiente", false, 325.44m, 325.44m, 50m);

        Assert.True(resultado.aceptada);
    }

    [Fact]
    public void limite_monto_cero_se_acepta()
    {
        var resultado = pro_venta_note_edit.evaluar_monto("Pendiente", false, 325.44m, 325.44m, 0m);

        Assert.True(resultado.aceptada);
    }

    [Fact]
    public void limite_monto_mayor_al_original_se_acepta_sin_tope()
    {
        var resultado = pro_venta_note_edit.evaluar_monto("Pendiente", false, 325.44m, 325.44m, 400m);

        Assert.True(resultado.aceptada);
    }

    [Fact]
    public void error_monto_negativo_se_rechaza()
    {
        var resultado = pro_venta_note_edit.evaluar_monto("Pendiente", false, 325.44m, 325.44m, -1m);

        Assert.False(resultado.aceptada);
        Assert.Contains("negativo", resultado.motivo);
    }

    [Theory]
    [InlineData("Anulada")]
    [InlineData("Devuelta")]
    public void error_nota_anulada_o_devuelta_se_rechaza(string estado)
    {
        var resultado = pro_venta_note_edit.evaluar_monto(estado, false, 325.44m, 325.44m, 50m);

        Assert.False(resultado.aceptada);
    }

    [Fact]
    public void error_relacion_pagada_bloquea_la_edicion_con_mensaje_de_pagada()
    {
        var resultado = pro_venta_note_edit.evaluar_monto("Pendiente", true, 325.44m, 325.44m, 50m);

        Assert.False(resultado.aceptada);
        Assert.Contains("pagada", resultado.motivo);
    }

    [Fact]
    public void error_nota_que_cambio_desde_que_se_abrio_se_rechaza()
    {
        var resultado = pro_venta_note_edit.evaluar_monto("Pendiente", false, 292.90m, 325.44m, 50m);

        Assert.False(resultado.aceptada);
        Assert.Contains("cambió", resultado.motivo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(100)]
    public void feliz_descuento_entre_cero_y_cien_se_acepta(decimal descuento)
    {
        var resultado = pro_venta_note_edit.evaluar_descuento("Pendiente", false, 325.44m, 325.44m, descuento);

        Assert.True(resultado.aceptada);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void error_descuento_fuera_de_rango_se_rechaza(decimal descuento)
    {
        var resultado = pro_venta_note_edit.evaluar_descuento("Pendiente", false, 325.44m, 325.44m, descuento);

        Assert.False(resultado.aceptada);
        Assert.Contains("entre 0 y 100", resultado.motivo);
    }

    [Fact]
    public void error_descuento_en_relacion_pagada_se_rechaza()
    {
        var resultado = pro_venta_note_edit.evaluar_descuento("Pendiente", true, 325.44m, 325.44m, 5m);

        Assert.False(resultado.aceptada);
    }
}
