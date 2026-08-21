using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;
using NinOS.Domain.ViewModels;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IPaymentService
    {
        Task register_payment_async(payment new_payment);
        Task<IEnumerable<payment_dto>> get_payments_by_note_async(int id_delivery_note);
        Task<IEnumerable<payment_dto>> get_payments_by_month_async(string month_year);
        Task<IEnumerable<payment_dto>> get_payments_by_month_and_seller_async(string month_year, int id_seller);
    }
}