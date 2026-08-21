using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;
using NinOS.Domain.ViewModels;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface ICommissionService
    {
        Task<commission[]> get_pending_commissions_by_seller_async(int id_seller);
        Task process_liquidation_async(int[] commission_ids);
        Task<IEnumerable<commission_dto>> get_commissions_by_seller_async(int id_seller);
        Task<IEnumerable<commission_dto>> get_commissions_by_seller_and_month_async(int id_seller, string month_year);
        Task<IEnumerable<seller>> get_sellers_with_commissions_async();
        Task register_commission_payment_async(int[] commission_ids, decimal exchange_rate, string payment_type, string reference_number);
    }
}