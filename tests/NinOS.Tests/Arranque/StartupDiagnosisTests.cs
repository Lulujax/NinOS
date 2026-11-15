using System.Net.Sockets;
using NinOS.Infrastructure.Data;
using NinOS.UI.Common;

namespace NinOS.Tests.Arranque;

/// <summary>
/// Spec 007, tarea 8: el error del arranque dice la causa real.
/// Antes la pantalla de carga culpaba siempre a internet, aunque el problema fuera la
/// contrasena, la base inexistente o una migracion fallida.
/// </summary>
public class StartupDiagnosisTests
{
    /// <summary>
    /// Imita un error de PostgreSQL. StartupDiagnosis lee SqlState por reflexion, asi que
    /// esta clase sirve para probar la clasificacion sin depender de Npgsql.
    /// </summary>
    private sealed class ErrorDePostgresFalso : Exception
    {
        public string SqlState { get; }

        public ErrorDePostgresFalso(string sql_state, string mensaje = "error del servidor")
            : base(mensaje)
        {
            SqlState = sql_state;
        }
    }

    // ---------- Caso feliz: cada causa se reconoce ----------

    [Fact]
    public void caso_feliz_falta_de_configuracion_se_reconoce()
    {
        Exception error = new InvalidOperationException(DbConnectionFactory.MissingConfigurationMessage);

        startup_diagnosis diagnostico = StartupDiagnosis.describe(error);

        Assert.Equal(startup_failure_cause.sin_configuracion, diagnostico.cause);
        Assert.Contains("no tiene configurada", diagnostico.mensaje);
    }

    [Theory]
    [InlineData("28P01")]
    [InlineData("28000")]
    public void caso_feliz_credenciales_incorrectas_se_reconocen(string sql_state)
    {
        startup_diagnosis diagnostico = StartupDiagnosis.describe(new ErrorDePostgresFalso(sql_state));

        Assert.Equal(startup_failure_cause.credenciales, diagnostico.cause);
        Assert.Contains("contrasena", diagnostico.mensaje);
    }

    [Fact]
    public void caso_feliz_base_inexistente_se_reconoce()
    {
        startup_diagnosis diagnostico = StartupDiagnosis.describe(new ErrorDePostgresFalso("3D000"));

        Assert.Equal(startup_failure_cause.base_inexistente, diagnostico.cause);
        Assert.Contains("no existe", diagnostico.mensaje);
    }

    [Fact]
    public void caso_feliz_falta_de_permisos_se_reconoce()
    {
        startup_diagnosis diagnostico = StartupDiagnosis.describe(new ErrorDePostgresFalso("42501"));

        Assert.Equal(startup_failure_cause.permisos, diagnostico.cause);
        Assert.Contains("permisos", diagnostico.mensaje);
    }

    [Fact]
    public void caso_feliz_sin_red_se_reconoce_y_sugiere_reintentar()
    {
        startup_diagnosis diagnostico = StartupDiagnosis.describe(new SocketException(10060));

        Assert.Equal(startup_failure_cause.sin_conexion, diagnostico.cause);
        Assert.Contains("internet", diagnostico.mensaje);
        Assert.True(diagnostico.vale_la_pena_reintentar, "Sin red, reintentar si tiene sentido.");
    }

    [Fact]
    public void caso_feliz_fallo_de_migracion_se_reconoce()
    {
        Exception error = new Exception("duplicate key value violates unique constraint __EFMigrationsHistory_pkey");

        startup_diagnosis diagnostico = StartupDiagnosis.describe(error);

        Assert.Equal(startup_failure_cause.migracion, diagnostico.cause);
        Assert.Contains("estructura", diagnostico.mensaje);
    }

    // ---------- Casos limite ----------

    [Fact]
    public void caso_limite_el_error_viene_envuelto_en_otro_y_se_encuentra_igual()
    {
        Exception error = new Exception("fallo al iniciar",
            new InvalidOperationException("capa intermedia",
                new ErrorDePostgresFalso("28P01")));

        Assert.Equal(startup_failure_cause.credenciales, StartupDiagnosis.describe(error).cause);
    }

    [Fact]
    public void caso_limite_el_error_viene_dentro_de_un_AggregateException()
    {
        Exception error = new AggregateException(
            new Exception("otra cosa"),
            new ErrorDePostgresFalso("3D000"));

        Assert.Equal(startup_failure_cause.base_inexistente, StartupDiagnosis.describe(error).cause);
    }

    [Fact]
    public void caso_limite_una_cadena_de_errores_circular_no_cuelga_el_sistema()
    {
        // Una cadena muy larga de errores anidados no debe dejar la pantalla de carga colgada.
        Exception error = new Exception("nivel 0");
        for (int i = 1; i <= 50; i++)
        {
            error = new Exception($"nivel {i}", error);
        }

        startup_diagnosis diagnostico = StartupDiagnosis.describe(error);

        Assert.Equal(startup_failure_cause.desconocida, diagnostico.cause);
    }

    [Fact]
    public void caso_limite_la_causa_mas_externa_manda_sobre_la_interna()
    {
        // Si el problema es que no hay configuracion, eso se dice, aunque mas adentro haya
        // un error de red derivado.
        Exception error = new InvalidOperationException(
            DbConnectionFactory.MissingConfigurationMessage,
            new SocketException(10060));

        Assert.Equal(startup_failure_cause.sin_configuracion, StartupDiagnosis.describe(error).cause);
    }

    // ---------- Casos de error ----------

    [Fact]
    public void caso_error_sin_excepcion_devuelve_mensaje_util_y_no_revienta()
    {
        startup_diagnosis diagnostico = StartupDiagnosis.describe(null);

        Assert.Equal(startup_failure_cause.desconocida, diagnostico.cause);
        Assert.False(string.IsNullOrWhiteSpace(diagnostico.mensaje));
    }

    [Fact]
    public void caso_error_un_error_desconocido_no_culpa_a_internet()
    {
        // Esto es lo que se quiso arreglar: antes CUALQUIER error decia "verifique internet".
        startup_diagnosis diagnostico = StartupDiagnosis.describe(new Exception("algo raro paso"));

        Assert.Equal(startup_failure_cause.desconocida, diagnostico.cause);
        Assert.DoesNotContain("internet", diagnostico.mensaje);
        Assert.False(diagnostico.vale_la_pena_reintentar);
    }

    [Fact]
    public void caso_error_un_codigo_de_postgres_que_no_conocemos_queda_como_desconocido()
    {
        startup_diagnosis diagnostico = StartupDiagnosis.describe(new ErrorDePostgresFalso("99999"));

        Assert.Equal(startup_failure_cause.desconocida, diagnostico.cause);
    }

    [Fact]
    public void todas_las_causas_tienen_un_mensaje_que_no_es_tecnico()
    {
        foreach (startup_failure_cause causa in Enum.GetValues<startup_failure_cause>())
        {
            Exception error = causa switch
            {
                startup_failure_cause.sin_configuracion => new InvalidOperationException(DbConnectionFactory.MissingConfigurationMessage),
                startup_failure_cause.sin_conexion => new SocketException(10060),
                startup_failure_cause.credenciales => new ErrorDePostgresFalso("28P01"),
                startup_failure_cause.base_inexistente => new ErrorDePostgresFalso("3D000"),
                startup_failure_cause.permisos => new ErrorDePostgresFalso("42501"),
                startup_failure_cause.migracion => new Exception("__EFMigrationsHistory"),
                _ => new Exception("otro")
            };

            startup_diagnosis diagnostico = StartupDiagnosis.describe(error);

            Assert.Equal(causa, diagnostico.cause);
            Assert.False(string.IsNullOrWhiteSpace(diagnostico.mensaje), $"La causa {causa} no tiene mensaje.");
            Assert.DoesNotContain("Exception", diagnostico.mensaje);
            Assert.DoesNotContain("SqlState", diagnostico.mensaje);
        }
    }
}
