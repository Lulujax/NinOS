using System.Threading.Tasks;
using NinOS.Domain;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IPaymentService
    {
        Task register_payment_async(payment new_payment);
    }
}