using System;
using System.Threading;
using System.Threading.Tasks;

namespace NinOS.UI.Updates
{
    public record update_check_result(bool is_available, string? available_version, string? error_message);

    public interface IAppUpdater
    {
        string current_version { get; }

        Task<update_check_result> check_for_update_async(CancellationToken cancellation_token);

        Task download_update_async(IProgress<int> progress_percent, CancellationToken cancellation_token);

        void apply_update_and_restart();

        void restart_app_to_update();
    }
}
