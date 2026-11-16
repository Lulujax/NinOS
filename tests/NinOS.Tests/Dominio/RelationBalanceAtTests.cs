using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class RelationBalanceAtTests
{
    private static readonly DateTime CORTE = new DateTime(2026, 10, 4);

    [Fact]
    public void feliz_solo_cuenta_abonos_hasta_el_corte()
    {
        var notas = new[] { new movimiento_con_fecha(new DateTime(2026, 10, 1, 16, 0, 0, DateTimeKind.Utc), 100m) };
        var abonos = new[]
        {
            new movimiento_con_fecha(new DateTime(2026, 10, 2, 16, 0, 0, DateTimeKind.Utc), 40m),
            new movimiento_con_fecha(new DateTime(2026, 10, 10, 16, 0, 0, DateTimeKind.Utc), 30m),
        };

        var resumen = relation_balance_at.calcular(CORTE, notas, abonos, false, 0m);

        Assert.Equal(100m, resumen.monto_por_pagar);
        Assert.Equal(40m, resumen.abonado);
        Assert.Equal(60m, resumen.saldo);
    }

    [Fact]
    public void limite_abono_del_dia_del_corte_a_las_once_de_la_noche_cuenta()
    {
        var notas = new[] { new movimiento_con_fecha(new DateTime(2026, 10, 1, 16, 0, 0, DateTimeKind.Utc), 100m) };
        var abonos = new[] { new movimiento_con_fecha(new DateTime(2026, 10, 5, 3, 0, 0, DateTimeKind.Utc), 25m) };

        var resumen = relation_balance_at.calcular(CORTE, notas, abonos, false, 0m);

        Assert.Equal(25m, resumen.abonado);
        Assert.Equal(75m, resumen.saldo);
    }

    [Fact]
    public void limite_abono_a_medianoche_del_dia_siguiente_no_cuenta()
    {
        var notas = new[] { new movimiento_con_fecha(new DateTime(2026, 10, 1, 16, 0, 0, DateTimeKind.Utc), 100m) };
        var abonos = new[] { new movimiento_con_fecha(new DateTime(2026, 10, 5, 4, 0, 0, DateTimeKind.Utc), 25m) };

        var resumen = relation_balance_at.calcular(CORTE, notas, abonos, false, 0m);

        Assert.Equal(0m, resumen.abonado);
        Assert.Equal(100m, resumen.saldo);
    }

    [Fact]
    public void limite_relacion_heredada_descuenta_solo_abonos_hasta_el_corte()
    {
        var abonos = new[]
        {
            new movimiento_con_fecha(new DateTime(2026, 10, 2, 16, 0, 0, DateTimeKind.Utc), 50m),
            new movimiento_con_fecha(new DateTime(2026, 10, 10, 16, 0, 0, DateTimeKind.Utc), 60m),
        };

        var resumen = relation_balance_at.calcular(CORTE, Array.Empty<movimiento_con_fecha>(), abonos, true, 200m);

        Assert.Equal(200m, resumen.monto_por_pagar);
        Assert.Equal(150m, resumen.saldo);
    }

    [Fact]
    public void error_fecha_de_corte_vacia_se_rechaza()
    {
        Assert.ThrowsAny<ArgumentException>(() => relation_balance_at.calcular(default, Array.Empty<movimiento_con_fecha>(), Array.Empty<movimiento_con_fecha>(), false, 0m));
    }
}
