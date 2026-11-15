using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class UpdatePolicyService : IUpdatePolicyService
    {
        private readonly IServiceScopeFactory _scope_factory;

        public UpdatePolicyService(IServiceScopeFactory scope_factory)
        {
            _scope_factory = scope_factory ?? throw new ArgumentNullException(nameof(scope_factory));
        }

        public async Task<update_policy_dto?> get_policy_async()
        {
            using IServiceScope scope = _scope_factory.CreateScope();
            NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            app_update_policy? policy = await db_context.app_update_policies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.id_policy == app_update_policy.SingleRowId);

            if (policy == null)
            {
                AppLog.Warn("La tabla app_update_policy no tiene la fila de politica: no se exige ni se recomienda ninguna version");
                return null;
            }

            DateTime server_time = await db_context.Database
                .SqlQuery<DateTime>($"SELECT now() AS \"Value\"")
                .SingleAsync();

            update_policy_dto result = new update_policy_dto
            {
                minimum_version = policy.minimum_version,
                recommended_version = policy.recommended_version,
                notice_at_utc = to_utc(policy.notice_at_utc),
                notice_message = policy.notice_message,
                server_time_utc = DateTime.SpecifyKind(server_time, DateTimeKind.Utc)
            };

            if (string.IsNullOrWhiteSpace(result.minimum_version) || string.IsNullOrWhiteSpace(result.recommended_version))
                throw new InvalidOperationException("La politica de actualizacion tiene la version minima o la recomendada vacia.");

            return result;
        }

        private static DateTime? to_utc(DateTime? value)
        {
            if (value == null) return null;
            return value.Value.Kind == DateTimeKind.Utc ? value : value.Value.ToUniversalTime();
        }
    }
}
