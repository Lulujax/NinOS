using System;
using System.Threading;
using System.Threading.Tasks;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Updates
{
    public enum update_flow_outcome
    {
        restarting,
        nothing_to_update,
        check_failed
    }

    public record update_flow_result(update_flow_outcome outcome, string? version, string? message);

    public static class AppUpdateFlow
    {
        public static async Task<update_flow_result> run_async(IAppUpdater updater, IProgress<int> progress, CancellationToken cancellation_token)
        {
            if (updater == null) throw new ArgumentNullException(nameof(updater));
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            AppLog.Info("Actualizacion: buscando la version antes de descargar.");
            update_check_result check = await updater.check_for_update_async(cancellation_token);

            if (!check.is_available)
            {
                if (check.error_message != null)
                {
                    AppLog.Warn("Actualizacion: no se pudo comprobar, no se descarga nada.");
                    return new update_flow_result(update_flow_outcome.check_failed, null, check.error_message);
                }

                AppLog.Info("Actualizacion: no hay version nueva, no se descarga nada.");
                return new update_flow_result(update_flow_outcome.nothing_to_update, null, null);
            }

            AppLog.Info($"Actualizacion: descargando la version {check.available_version}.");
            await updater.download_update_async(progress, cancellation_token);

            AppLog.Info("Actualizacion: descarga lista, aplicando y reiniciando.");
            updater.apply_update_and_restart();
            return new update_flow_result(update_flow_outcome.restarting, check.available_version, null);
        }
    }
}
