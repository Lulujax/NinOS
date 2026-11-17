using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class ProVentaWeekTests
{
    [Fact]
    public void feliz_miercoles_al_mediodia_de_venezuela_da_el_lunes_de_esa_semana()
    {
        DateTime miercoles_utc = new DateTime(2026, 11, 18, 16, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 11, 16), pro_venta_week.lunes_de(miercoles_utc));
    }

    [Fact]
    public void limite_domingo_2359_de_venezuela_sigue_en_la_semana_que_termina_ese_domingo()
    {
        DateTime domingo_2359_utc = new DateTime(2026, 11, 23, 3, 59, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 11, 16), pro_venta_week.lunes_de(domingo_2359_utc));
    }

    [Fact]
    public void limite_domingo_nueve_de_la_noche_queda_en_la_semana_de_ese_domingo()
    {
        DateTime domingo_2100_utc = new DateTime(2026, 11, 23, 1, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 11, 16), pro_venta_week.lunes_de(domingo_2100_utc));
    }

    [Fact]
    public void limite_lunes_a_medianoche_de_venezuela_abre_la_semana_nueva()
    {
        DateTime lunes_0000_utc = new DateTime(2026, 11, 23, 4, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 11, 23), pro_venta_week.lunes_de(lunes_0000_utc));
    }

    [Fact]
    public void limite_cambio_de_mes_cae_en_el_lunes_del_mes_anterior()
    {
        DateTime martes_1_dic_utc = new DateTime(2026, 12, 1, 16, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 11, 30), pro_venta_week.lunes_de(martes_1_dic_utc));
    }

    [Fact]
    public void limite_cambio_de_anio_cae_en_el_lunes_de_diciembre()
    {
        DateTime viernes_1_ene_utc = new DateTime(2027, 1, 1, 16, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 12, 28), pro_venta_week.lunes_de(viernes_1_ene_utc));
    }

    [Fact]
    public void error_fecha_vacia_se_rechaza()
    {
        Assert.ThrowsAny<ArgumentException>(() => pro_venta_week.lunes_de(default));
    }
}
