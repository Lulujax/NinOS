using System.Threading.Tasks;
using NinOS.Domain;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface ICommissionService
    {
        Task<commission[]> get_pending_commissions_by_seller_async(int id_seller);
        Task process_liquidation_async(int[] commission_ids);
    }
}