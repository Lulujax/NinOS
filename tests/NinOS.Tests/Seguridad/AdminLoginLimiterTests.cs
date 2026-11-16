using System;
using NinOS.Domain;

namespace NinOS.Tests.Seguridad;

public class AdminLoginLimiterTests
{
    private static readonly DateTime Momento = new DateTime(2026, 11, 16, 12, 0, 0);

    [Fact]
    public void caso_feliz_sin_fallos_no_hay_bloqueo()
    {
        AdminLoginLimiter limitador = new AdminLoginLimiter();

        Assert.False(limitador.is_locked(Momento));
        Assert.Equal(0, limitador.remaining_delay_seconds(Momento));
    }

    [Fact]
    public void caso_feliz_quinto_fallo_bloquea_cinco_segundos()
    {
        AdminLoginLimiter limitador = new AdminLoginLimiter();
        for (int intento = 0; intento < AdminLoginLimiter.MaxAttempts; intento++)
        {
            limitador.register_failure(Momento);
        }

        Assert.True(limitador.is_locked(Momento));
        Assert.Equal(5, limitador.remaining_delay_seconds(Momento));
    }

    [Fact]
    public void caso_limite_cuatro_fallos_todavia_no_bloquean()
    {
        AdminLoginLimiter limitador = new AdminLoginLimiter();
        for (int intento = 0; intento < AdminLoginLimiter.MaxAttempts - 1; intento++)
        {
            limitador.register_failure(Momento);
        }

        Assert.False(limitador.is_locked(Momento));
    }

    [Fact]
    public void caso_limite_la_espera_nunca_pasa_del_maximo()
    {
        AdminLoginLimiter limitador = new AdminLoginLimiter();
        for (int intento = 0; intento < 50; intento++)
        {
            limitador.register_failure(Momento);
        }

        Assert.Equal(AdminLoginLimiter.MaxDelaySeconds, limitador.remaining_delay_seconds(Momento));
    }

    [Fact]
    public void caso_limite_el_bloqueo_termina_al_cumplirse_la_espera()
    {
        AdminLoginLimiter limitador = new AdminLoginLimiter();
        for (int intento = 0; intento < AdminLoginLimiter.MaxAttempts; intento++)
        {
            limitador.register_failure(Momento);
        }

        DateTime despues = Momento.AddSeconds(5);

        Assert.False(limitador.is_locked(despues));
        Assert.Equal(0, limitador.remaining_delay_seconds(despues));
    }

    [Fact]
    public void caso_feliz_reset_quita_el_bloqueo_y_los_fallos()
    {
        AdminLoginLimiter limitador = new AdminLoginLimiter();
        for (int intento = 0; intento < AdminLoginLimiter.MaxAttempts; intento++)
        {
            limitador.register_failure(Momento);
        }

        limitador.reset();

        Assert.False(limitador.is_locked(Momento));
        limitador.register_failure(Momento);
        Assert.False(limitador.is_locked(Momento), "Tras el reset debia empezar a contar de cero.");
    }

    [Fact]
    public void caso_error_sexto_fallo_alarga_la_espera()
    {
        AdminLoginLimiter limitador = new AdminLoginLimiter();
        for (int intento = 0; intento < AdminLoginLimiter.MaxAttempts + 1; intento++)
        {
            limitador.register_failure(Momento);
        }

        Assert.Equal(10, limitador.remaining_delay_seconds(Momento));
    }
}
