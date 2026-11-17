using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class RelationAmountsTests
{
    [Fact]
    public void feliz_dos_notas_y_un_abono_dan_monto_abonado_y_saldo()
    {
        var resumen = relation_amounts.calcular(new[] { 100m, 50m }, new[] { 30m }, false, 0m);

        Assert.Equal(150m, resumen.monto_por_pagar);
        Assert.Equal(30m, resumen.abonado);
        Assert.Equal(120m, resumen.saldo);
        Assert.Equal("Pendiente", resumen.estado);
        Assert.Equal(2, resumen.notas_vigentes);
    }

    [Fact]
    public void limite_sin_notas_vigentes_queda_pagada_con_monto_cero()
    {
        var resumen = relation_amounts.calcular(Array.Empty<decimal>(), Array.Empty<decimal>(), false, 0m);

        Assert.Equal(0m, resumen.monto_por_pagar);
        Assert.Equal(0m, resumen.saldo);
        Assert.Equal("Pagada", resumen.estado);
    }

    [Fact]
    public void limite_abono_igual_al_monto_deja_saldo_cero_y_pagada()
    {
        var resumen = relation_amounts.calcular(new[] { 100m }, new[] { 100m }, false, 0m);

        Assert.Equal(0m, resumen.saldo);
        Assert.Equal("Pagada", resumen.estado);
    }

    [Fact]
    public void limite_abono_mayor_al_monto_no_produce_saldo_negativo()
    {
        var resumen = relation_amounts.calcular(new[] { 100m }, new[] { 150m }, false, 0m);

        Assert.Equal(0m, resumen.saldo);
        Assert.Equal(150m, resumen.abonado);
        Assert.Equal("Pagada", resumen.estado);
    }

    [Fact]
    public void limite_abonos_negativos_no_cuentan()
    {
        var resumen = relation_amounts.calcular(new[] { 100m }, new[] { -20m, 10m }, false, 0m);

        Assert.Equal(10m, resumen.abonado);
        Assert.Equal(90m, resumen.saldo);
    }

    [Fact]
    public void limite_heredada_sin_notas_usa_el_saldo_cargado_y_queda_pendiente()
    {
        var resumen = relation_amounts.calcular(Array.Empty<decimal>(), Array.Empty<decimal>(), true, 200m);

        Assert.Equal(200m, resumen.monto_por_pagar);
        Assert.Equal(200m, resumen.saldo);
        Assert.Equal("Pendiente", resumen.estado);
    }

    [Fact]
    public void error_monto_de_nota_negativo_se_rechaza()
    {
        Assert.ThrowsAny<ArgumentException>(() => relation_amounts.calcular(new[] { -1m }, Array.Empty<decimal>(), false, 0m));
    }

    [Fact]
    public void error_saldo_heredado_negativo_se_rechaza()
    {
        Assert.ThrowsAny<ArgumentException>(() => relation_amounts.calcular(Array.Empty<decimal>(), Array.Empty<decimal>(), true, -5m));
    }
}
