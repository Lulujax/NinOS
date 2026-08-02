using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<IEnumerable<customer>> GetAllCustomersAsync();
        Task<customer?> GetCustomerByIdAsync(int id);
        Task<customer?> GetCustomerByCodeAsync(string code);
        Task AddCustomerAsync(customer newCustomer);
        Task UpdateCustomerAsync(customer existingCustomer);
        Task DeleteCustomerAsync(int id);
    }
}