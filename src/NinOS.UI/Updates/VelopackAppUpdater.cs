using System;
using System.Threading;
using System.Threading.Tasks;
using NinOS.Infrastructure.Logging;
using Velopack;
using Velopack.Sources;

namespace NinOS.UI.Updates
{
    public class VelopackAppUpdater : IAppUpdater
    {
        private const double DOWNLOAD_TIMEOUT_SECONDS = 600;

        private readonly UpdateManager _manager;
        private UpdateInfo? _pending_update;

        public VelopackAppUpdater(string base_url)
        {
            if (string.IsNullOrWhiteSpace(base_url)) throw new ArgumentException("La direccion de actualizaciones esta vacia.", nameof(base_url));

            var source = new SimpleWebSource(base_url, new PinnedFileDownloader(), DOWNLOAD_TIMEOUT_SECONDS);
            _manager = new UpdateManager(source);
            AppLog.Info("Actualizaciones: actualizador listo con huella fijada del servidor.");
        }

        public string current_version => _manager.CurrentVersion?.ToString() ?? string.Empty;

        public async Task<update_check_result> check_for_update_async(CancellationToken cancellation_token)
        {
            cancellation_token.ThrowIfCancellationRequested();
            AppLog.Info("Actualizaciones: buscando version nueva.");

            try
            {
                UpdateInfo? info = await _manager.CheckForUpdatesAsync();
                _pending_update = info;

                if (info == null)
                {
                    AppLog.Info("Actualizaciones: la version actual ya es la mas reciente.");
                    return new update_check_result(false, null, null);
                }

                string available = info.TargetFullRelease.Version.ToString();
                AppLog.Info($"Actualizaciones: hay una version nueva disponible ({available}).");
                return new update_check_result(true, available, null);
            }
            catch (Exception exception)
            {
                AppLog.Warn($"Actualizaciones: no se pudo comprobar ({exception.GetType().Name}).");
                return new update_check_result(false, null, "No se pudo comprobar si hay actualizacion. Se sigue con la version actual.");
            }
        }

        public async Task download_update_async(IProgress<int> progress_percent, CancellationToken cancellation_token)
        {
            if (_pending_update == null) throw new InvalidOperationException("No hay actualizacion pendiente de descargar.");

            AppLog.Info("Actualizaciones: descargando paquete.");
            await _manager.DownloadUpdatesAsync(_pending_update, percent => progress_percent.Report(percent), cancellation_token);
            AppLog.Info("Actualizaciones: paquete descargado y verificado.");
        }

        public void apply_update_and_restart()
        {
            if (_pending_update == null) throw new InvalidOperationException("No hay actualizacion pendiente de aplicar.");

            AppLog.Info("Actualizaciones: aplicando y reiniciando.");
            _manager.ApplyUpdatesAndRestart(_pending_update.TargetFullRelease);
        }
    }
}
