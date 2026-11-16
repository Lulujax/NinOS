using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.UI.Updates
{
    public record update_policy_snapshot(
        update_status status,
        notice_countdown countdown,
        string current_version,
        string minimum_version,
        string recommended_version,
        string? notice_message);

    public class UpdatePolicyMonitor
    {
        public static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(10);

        private readonly IServiceScopeFactory _scope_factory;
        private readonly IAppUpdater _updater;
        private readonly DispatcherTimer _timer;
        private bool _had_notice;

        public event Action<update_policy_snapshot?>? snapshot_changed;

        public UpdatePolicyMonitor(IServiceScopeFactory scope_factory, IAppUpdater updater)
        {
            _scope_factory = scope_factory ?? throw new ArgumentNullException(nameof(scope_factory));
            _updater = updater ?? throw new ArgumentNullException(nameof(updater));
            _timer = new DispatcherTimer { Interval = CheckInterval };
            _timer.Tick += async (sender, args) => await refresh_async();
        }

        public void start()
        {
            _timer.Start();
            _ = refresh_async();
        }

        public async Task refresh_async()
        {
            update_policy_snapshot? snapshot = await read_snapshot_async();
            snapshot_changed?.Invoke(snapshot);
        }

        private async Task<update_policy_snapshot?> read_snapshot_async()
        {
            try
            {
                using IServiceScope scope = _scope_factory.CreateScope();
                IUpdatePolicyService policy_service = scope.ServiceProvider.GetRequiredService<IUpdatePolicyService>();
                update_policy_dto? policy = await policy_service.get_policy_async();
                if (policy == null) return null;

                string current = _updater.current_version;
                update_status status = UpdatePolicyEvaluator.evaluate(current, policy.minimum_version, policy.recommended_version);
                notice_countdown countdown = UpdateNoticeCountdown.calculate(policy.notice_at_utc, policy.server_time_utc, _had_notice);
                _had_notice = policy.notice_at_utc != null;

                AppLog.Info($"Politica de actualizacion: version {current}, estado {status}, aviso {countdown.state}");

                return new update_policy_snapshot(status, countdown, current, policy.minimum_version, policy.recommended_version, policy.notice_message);
            }
            catch (Exception exception)
            {
                AppLog.Warn($"No se pudo revisar la politica de actualizacion ({exception.GetType().Name}). Se sigue trabajando con la version actual.");
                return null;
            }
        }
    }
}
