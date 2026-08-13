using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;
using NinOS.Domain.ViewModels;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IAccountsReceivableService
    {
        Task<IEnumerable<string>> get_pending_months_async();
        Task<IEnumerable<accounts_receivable_dto>> get_receivables_by_month_async(string month_year);
        Task annul_delivery_note_async(int id_delivery_note);
        Task add_payment_async(payment new_payment);
    }
}