using NinOS.Domain;

namespace NinOS.Tests.Dominio;

/// <summary>
/// Spec 008: los tres porcentajes de Pro Venta, por nota y sobre el monto SIN descuento.
/// </summary>
public class ProVentaCalculatorTests
{
    // ---------- Caso feliz: el ejemplo que acordamos con el cliente ----------

    [Fact]
    public void caso_feliz_sobre_cien_da_diez_veinticinco_y_once_con_veinticinco()
    {
        Assert.Equal(10.00m, pro_venta_calculator.comision(100m));
        Assert.Equal(25.00m, pro_venta_calculator.gastos_operativos(100m));
        Assert.Equal(11.25m, pro_venta_calculator.gastos_administrativos(100m));
    }

    [Fact]
    public void los_gastos_administrativos_salen_de_lo_que_queda_tras_los_operativos()
    {
        // Antes se calculaba 15% del total (15,00) en vez de 15% de lo que queda (11,25).
        decimal original = 100m;
        decimal resto = original - pro_venta_calculator.gastos_operativos(original);

        Assert.Equal(75m, resto);
        Assert.Equal(Money.round(resto * 0.15m), pro_venta_calculator.gastos_administrativos(original));
        Assert.NotEqual(15.00m, pro_venta_calculator.gastos_administrativos(original));
    }

    [Fact]
    public void caso_feliz_con_el_monto_real_de_una_nota_de_testing()
    {
        // La nota 000_001 tiene renglones por 325,44 (su total con descuento es 292,90).
        decimal original = 325.44m;

        Assert.Equal(32.54m, pro_venta_calculator.comision(original));
        Assert.Equal(81.36m, pro_venta_calculator.gastos_operativos(original));
        Assert.Equal(36.61m, pro_venta_calculator.gastos_administrativos(original));
    }

    // ---------- Casos limite ----------

    [Fact]
    public void caso_limite_monto_cero_da_todo_cero()
    {
        Assert.Equal(0m, pro_venta_calculator.comision(0m));
        Assert.Equal(0m, pro_venta_calculator.gastos_operativos(0m));
        Assert.Equal(0m, pro_venta_calculator.gastos_administrativos(0m));
    }

    [Fact]
    public void caso_limite_todo_queda_redondeado_a_dos_decimales()
    {
        decimal original = 33.33m;

        foreach (decimal valor in new[]
        {
            pro_venta_calculator.comision(original),
            pro_venta_calculator.gastos_operativos(original),
            pro_venta_calculator.gastos_administrativos(original)
        })
        {
            Assert.Equal(valor, Money.round(valor));
            Assert.Equal(valor, decimal.Round(valor, 2));
        }
    }

    [Fact]
    public void caso_limite_un_monto_muy_grande_no_desborda()
    {
        decimal original = 1_000_000m;

        Assert.Equal(100_000m, pro_venta_calculator.comision(original));
        Assert.Equal(250_000m, pro_venta_calculator.gastos_operativos(original));
        Assert.Equal(112_500m, pro_venta_calculator.gastos_administrativos(original));
    }

    // ---------- Casos de error ----------

    [Fact]
    public void caso_error_un_monto_negativo_se_trata_como_cero()
    {
        Assert.Equal(0m, pro_venta_calculator.comision(-50m));
        Assert.Equal(0m, pro_venta_calculator.gastos_operativos(-50m));
        Assert.Equal(0m, pro_venta_calculator.gastos_administrativos(-50m));
    }

    [Fact]
    public void los_porcentajes_son_los_que_confirmo_el_cliente()
    {
        Assert.Equal(0.10m, pro_venta_calculator.PorcentajeComision);
        Assert.Equal(0.25m, pro_venta_calculator.PorcentajeGastosOperativos);
        Assert.Equal(0.15m, pro_venta_calculator.PorcentajeGastosAdministrativos);
    }
}
