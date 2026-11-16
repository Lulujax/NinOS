using System.IO;
using NinOS.UI.Updates;

namespace NinOS.Tests.Updates;

public class AppUpdateFlowTests
{
    private class FalsoActualizador : IAppUpdater
    {
        private readonly update_check_result _resultado_de_busqueda;
        private readonly bool _fallar_al_descargar;
        private bool _hay_pendiente;

        public List<string> llamadas { get; } = new List<string>();

        public FalsoActualizador(update_check_result resultado_de_busqueda, bool fallar_al_descargar = false)
        {
            _resultado_de_busqueda = resultado_de_busqueda;
            _fallar_al_descargar = fallar_al_descargar;
        }

        public string current_version => "1.0.14";

        public Task<update_check_result> check_for_update_async(CancellationToken cancellation_token)
        {
            llamadas.Add("buscar");
            _hay_pendiente = _resultado_de_busqueda.is_available;
            return Task.FromResult(_resultado_de_busqueda);
        }

        public Task download_update_async(IProgress<int> progress_percent, CancellationToken cancellation_token)
        {
            llamadas.Add("descargar");
            if (!_hay_pendiente) throw new InvalidOperationException("No hay actualizacion pendiente de descargar.");
            if (_fallar_al_descargar) throw new IOException("Se corto la descarga.");
            progress_percent.Report(100);
            return Task.CompletedTask;
        }

        public void apply_update_and_restart()
        {
            llamadas.Add("aplicar");
            if (!_hay_pendiente) throw new InvalidOperationException("No hay actualizacion pendiente de aplicar.");
        }

        public void restart_app_to_update()
        {
            llamadas.Add("reiniciar");
        }
    }

    [Fact]
    public async Task caso_feliz_busca_descarga_y_aplica_en_ese_orden()
    {
        var actualizador = new FalsoActualizador(new update_check_result(true, "1.0.15", null));

        update_flow_result resultado = await AppUpdateFlow.run_async(actualizador, new Progress<int>(), CancellationToken.None);

        Assert.Equal(new[] { "buscar", "descargar", "aplicar" }, actualizador.llamadas);
        Assert.Equal(update_flow_outcome.restarting, resultado.outcome);
        Assert.Equal("1.0.15", resultado.version);
    }

    [Fact]
    public async Task caso_regresion_el_boton_no_descarga_sin_buscar_antes()
    {
        var actualizador = new FalsoActualizador(new update_check_result(true, "1.0.15", null));

        Exception? error = await Record.ExceptionAsync(() => AppUpdateFlow.run_async(actualizador, new Progress<int>(), CancellationToken.None));

        Assert.Null(error);
        Assert.Equal("buscar", actualizador.llamadas[0]);
    }

    [Fact]
    public async Task caso_limite_sin_version_nueva_no_descarga_ni_aplica()
    {
        var actualizador = new FalsoActualizador(new update_check_result(false, null, null));

        update_flow_result resultado = await AppUpdateFlow.run_async(actualizador, new Progress<int>(), CancellationToken.None);

        Assert.Equal(new[] { "buscar" }, actualizador.llamadas);
        Assert.Equal(update_flow_outcome.nothing_to_update, resultado.outcome);
    }

    [Fact]
    public async Task caso_limite_si_no_se_pudo_comprobar_devuelve_el_motivo_y_no_descarga()
    {
        var actualizador = new FalsoActualizador(new update_check_result(false, null, "No se pudo comprobar."));

        update_flow_result resultado = await AppUpdateFlow.run_async(actualizador, new Progress<int>(), CancellationToken.None);

        Assert.Equal(new[] { "buscar" }, actualizador.llamadas);
        Assert.Equal(update_flow_outcome.check_failed, resultado.outcome);
        Assert.Equal("No se pudo comprobar.", resultado.message);
    }

    [Fact]
    public async Task caso_error_si_la_descarga_falla_no_se_aplica_nada()
    {
        var actualizador = new FalsoActualizador(new update_check_result(true, "1.0.15", null), fallar_al_descargar: true);

        await Assert.ThrowsAsync<IOException>(() => AppUpdateFlow.run_async(actualizador, new Progress<int>(), CancellationToken.None));

        Assert.Equal(new[] { "buscar", "descargar" }, actualizador.llamadas);
    }

    [Fact]
    public async Task caso_error_actualizador_nulo_lanza_excepcion()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => AppUpdateFlow.run_async(null!, new Progress<int>(), CancellationToken.None));
    }

    [Fact]
    public async Task caso_error_progreso_nulo_lanza_excepcion()
    {
        var actualizador = new FalsoActualizador(new update_check_result(false, null, null));

        await Assert.ThrowsAsync<ArgumentNullException>(() => AppUpdateFlow.run_async(actualizador, null!, CancellationToken.None));
    }
}
