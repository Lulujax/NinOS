using System;
using System.Threading;
using System.Threading.Tasks;

namespace NinOS.UI.Updates
{
    public class DisabledAppUpdater : IAppUpdater
    {
        public string current_version => string.Empty;

        public Task<update_check_result> check_for_update_async(CancellationToken cancellation_token)
            => Task.FromResult(new update_check_result(false, null, null));

        public Task download_update_async(IProgress<int> progress_percent, CancellationToken cancellation_token)
            => throw new InvalidOperationException("Las actualizaciones estan desactivadas en esta copia.");

        public void restart_app_to_update()
            => throw new InvalidOperationException("Las actualizaciones estan desactivadas en esta copia.");

        public void apply_update_and_restart()
            => throw new InvalidOperationException("Las actualizaciones estan desactivadas en esta copia.");
    }
}
