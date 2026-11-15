using Microsoft.Extensions.DependencyInjection;
using NinOS.Infrastructure.Services.Implementations;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.UI.Updates
{
    public static class UpdatePolicyServices
    {
        public static void add_update_policy_services(IServiceCollection services)
        {
            services.AddScoped<IUpdatePolicyService, UpdatePolicyService>();
            services.AddSingleton<UpdatePolicyMonitor>();
        }
    }
}
