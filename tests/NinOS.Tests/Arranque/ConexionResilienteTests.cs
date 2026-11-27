using NinOS.Infrastructure.Data;
using Npgsql;

namespace NinOS.Tests.Arranque;

public class ConexionResilienteTests
{
    private const string CADENA_BASE = "Host=servidor.prueba;Port=5432;Database=ninos_db_testing;Username=usuario;Password=clave";

    [Fact]
    public void Agrega_keepalive_y_vida_corta_a_conexiones_inactivas()
    {
        string ajustada = DbConnectionFactory.apply_resilience_settings(CADENA_BASE);
        NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(ajustada);

        Assert.Equal(DbConnectionFactory.KEEPALIVE_SECONDS, builder.KeepAlive);
        Assert.True(builder.TcpKeepAlive);
        Assert.Equal(DbConnectionFactory.IDLE_LIFETIME_SECONDS, builder.ConnectionIdleLifetime);
        Assert.Equal("servidor.prueba", builder.Host);
    }

    [Fact]
    public void Respeta_valores_que_ya_venian_en_la_cadena()
    {
        string cadena = CADENA_BASE + ";Keepalive=10;Connection Idle Lifetime=60";

        NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(DbConnectionFactory.apply_resilience_settings(cadena));

        Assert.Equal(10, builder.KeepAlive);
        Assert.Equal(60, builder.ConnectionIdleLifetime);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Cadena_vacia_lanza_excepcion(string cadena)
    {
        Assert.Throws<ArgumentException>(() => DbConnectionFactory.apply_resilience_settings(cadena));
    }

    [Fact]
    public void Cadena_ilegible_se_devuelve_sin_cambios()
    {
        string ilegible = "esto-no-es-una-cadena";

        Assert.Equal(ilegible, DbConnectionFactory.apply_resilience_settings(ilegible));
    }

    [Fact]
    public void Conexion_inactiva_mas_del_umbral_pide_comprobacion()
    {
        DateTime ahora = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

        bool pide = ConnectionFreshnessPolicy.needs_ping(ahora.AddSeconds(-60), ahora, TimeSpan.FromSeconds(15));

        Assert.True(pide);
    }

    [Fact]
    public void Conexion_usada_hace_poco_no_pide_comprobacion()
    {
        DateTime ahora = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

        Assert.False(ConnectionFreshnessPolicy.needs_ping(ahora.AddSeconds(-2), ahora, TimeSpan.FromSeconds(15)));
    }

    [Fact]
    public void Justo_en_el_umbral_pide_comprobacion()
    {
        DateTime ahora = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

        Assert.True(ConnectionFreshnessPolicy.needs_ping(ahora.AddSeconds(-15), ahora, TimeSpan.FromSeconds(15)));
    }

    [Fact]
    public void Sin_actividad_previa_no_pide_comprobacion()
    {
        Assert.False(ConnectionFreshnessPolicy.needs_ping(null, DateTime.UtcNow, TimeSpan.FromSeconds(15)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Umbral_no_positivo_lanza_excepcion(int segundos)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConnectionFreshnessPolicy.needs_ping(DateTime.UtcNow, DateTime.UtcNow, TimeSpan.FromSeconds(segundos)));
    }
}
