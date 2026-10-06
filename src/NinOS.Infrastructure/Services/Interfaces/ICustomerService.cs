using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;
using NinOS.Domain.ViewModels;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<IEnumerable<customer>> GetAllCustomersAsync();
        Task<IEnumerable<customer>> GetDeletedCustomersAsync();
        Task<customer?> GetCustomerByIdAsync(int id);
        Task<customer?> GetCustomerByCodeAsync(string code);
        Task AddCustomerAsync(customer newCustomer);
        Task UpdateCustomerAsync(customer existingCustomer);
        Task SoftDeleteCustomerAsync(int id, string? reason);
        Task RestoreCustomerAsync(int id);
        Task<string> GetNextCustomerCodeAsync();
        Task<CustomerHistoryDataDto?> GetCustomerHistoryAsync(int customerId);
    }
}