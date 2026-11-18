using NinOS.Infrastructure.Common;

namespace NinOS.Tests.Dominio;

public class TransientRetryTests
{
    [Fact]
    public async Task Operacion_que_funciona_al_primer_intento_devuelve_su_resultado()
    {
        int llamadas = 0;

        string resultado = await TransientRetry.run_async(() =>
        {
            llamadas++;
            return Task.FromResult("000_016");
        }, "prueba", max_attempts: 3);

        Assert.Equal("000_016", resultado);
        Assert.Equal(1, llamadas);
    }

    [Fact]
    public async Task Timeout_se_reintenta_y_al_segundo_intento_devuelve_resultado()
    {
        int llamadas = 0;

        string resultado = await TransientRetry.run_async<string>(() =>
        {
            llamadas++;
            if (llamadas == 1) throw new TimeoutException("Timeout during reading attempt");
            return Task.FromResult("ok");
        }, "prueba", max_attempts: 3);

        Assert.Equal("ok", resultado);
        Assert.Equal(2, llamadas);
    }

    [Fact]
    public async Task Al_agotar_los_intentos_relanza_la_ultima_excepcion()
    {
        int llamadas = 0;

        await Assert.ThrowsAsync<TimeoutException>(() => TransientRetry.run_async<string>(() =>
        {
            llamadas++;
            throw new TimeoutException("siempre falla");
        }, "prueba", max_attempts: 2));

        Assert.Equal(2, llamadas);
    }

    [Fact]
    public async Task Error_que_no_es_de_conexion_no_se_reintenta()
    {
        int llamadas = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => TransientRetry.run_async<string>(() =>
        {
            llamadas++;
            throw new InvalidOperationException("dato invalido");
        }, "prueba", max_attempts: 3));

        Assert.Equal(1, llamadas);
    }

    [Fact]
    public async Task Con_un_solo_intento_no_reintenta()
    {
        int llamadas = 0;

        await Assert.ThrowsAsync<TimeoutException>(() => TransientRetry.run_async<string>(() =>
        {
            llamadas++;
            throw new TimeoutException("falla");
        }, "prueba", max_attempts: 1));

        Assert.Equal(1, llamadas);
    }

    [Fact]
    public async Task Cero_intentos_lanza_excepcion()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => TransientRetry.run_async(() => Task.FromResult("x"), "prueba", max_attempts: 0));
    }

    [Fact]
    public async Task Operacion_nula_lanza_excepcion()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => TransientRetry.run_async<string>(null!, "prueba", max_attempts: 1));
    }

    [Fact]
    public async Task Nombre_de_operacion_vacio_lanza_excepcion()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => TransientRetry.run_async(() => Task.FromResult("x"), " ", max_attempts: 1));
    }
}
