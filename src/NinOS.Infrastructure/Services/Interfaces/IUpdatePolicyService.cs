using System.Threading.Tasks;
using NinOS.Domain.ViewModels;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IUpdatePolicyService
    {
        Task<update_policy_dto?> get_policy_async();
    }
}
