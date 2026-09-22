using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;
using NinOS.Domain.ViewModels;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IPaymentService
    {
        Task register_payment_async(payment new_payment, bool is_pro_venta = false);
        Task register_relation_payment_async(int id_relacion, decimal amount_usd, DateTime payment_date);
        Task update_payment_async(payment updated_payment, bool is_pro_venta = false);
        Task<IEnumerable<payment_dto>> get_payments_by_note_async(int id_delivery_note);
        Task<IEnumerable<payment_dto>> get_payments_by_month_async(string month_year);
        Task<IEnumerable<payment_dto>> get_payments_by_month_and_seller_async(string month_year, int id_seller);
    }
}
