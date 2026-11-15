using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class UpdateNoticeCountdownTests
{
    private static readonly DateTime AhoraUtc = new DateTime(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void caso_feliz_sin_aviso_y_nunca_hubo_uno_es_sin_aviso()
    {
        notice_countdown cuenta = UpdateNoticeCountdown.calculate(null, AhoraUtc, had_notice_before: false);

        Assert.Equal(notice_state.sin_aviso, cuenta.state);
    }

    [Fact]
    public void caso_feliz_aviso_a_5_minutos_esta_contando_y_lo_dice()
    {
        notice_countdown cuenta = UpdateNoticeCountdown.calculate(AhoraUtc.AddMinutes(5), AhoraUtc, had_notice_before: false);

        Assert.Equal(notice_state.contando, cuenta.state);
        Assert.Equal(TimeSpan.FromMinutes(5), cuenta.remaining);
        Assert.Contains("5 minutos", cuenta.text);
    }

    [Fact]
    public void caso_limite_faltando_un_minuto_exacto_lo_dice_en_singular()
    {
        notice_countdown cuenta = UpdateNoticeCountdown.calculate(AhoraUtc.AddMinutes(1), AhoraUtc, had_notice_before: false);

        Assert.Equal(notice_state.contando, cuenta.state);
        Assert.Contains("1 minuto", cuenta.text);
    }

    [Fact]
    public void caso_limite_faltando_menos_de_un_minuto_lo_dice()
    {
        notice_countdown cuenta = UpdateNoticeCountdown.calculate(AhoraUtc.AddSeconds(30), AhoraUtc, had_notice_before: false);

        Assert.Equal(notice_state.contando, cuenta.state);
        Assert.Contains("menos de 1 minuto", cuenta.text);
    }

    [Fact]
    public void caso_limite_la_hora_exacta_del_aviso_ya_esta_vencida()
    {
        notice_countdown cuenta = UpdateNoticeCountdown.calculate(AhoraUtc, AhoraUtc, had_notice_before: false);

        Assert.Equal(notice_state.vencido, cuenta.state);
        Assert.Equal(TimeSpan.Zero, cuenta.remaining);
    }

    [Fact]
    public void caso_error_aviso_en_el_pasado_esta_vencido()
    {
        notice_countdown cuenta = UpdateNoticeCountdown.calculate(AhoraUtc.AddMinutes(-2), AhoraUtc, had_notice_before: true);

        Assert.Equal(notice_state.vencido, cuenta.state);
    }

    [Fact]
    public void caso_cancelado_si_habia_aviso_y_ya_no_hay_momento()
    {
        notice_countdown cuenta = UpdateNoticeCountdown.calculate(null, AhoraUtc, had_notice_before: true);

        Assert.Equal(notice_state.cancelado, cuenta.state);
    }

    [Fact]
    public void caso_error_hora_actual_vacia_lanza_excepcion()
    {
        Assert.Throws<ArgumentException>(() => UpdateNoticeCountdown.calculate(AhoraUtc.AddMinutes(5), default, had_notice_before: false));
    }
}
