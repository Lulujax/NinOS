using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class CustomerService : ICustomerService
    {
        private readonly NinOSDbContext _dbContext;

        public CustomerService(NinOSDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<IEnumerable<customer>> GetAllCustomersAsync()
        {
            return await _dbContext.customers.ToListAsync();
        }

        public async Task<customer?> GetCustomerByIdAsync(int id)
        {
            return await _dbContext.customers.FirstOrDefaultAsync(c => c.id_customer == id);
        }

        public async Task<customer?> GetCustomerByCodeAsync(string code)
        {
            return await _dbContext.customers.FirstOrDefaultAsync(c => c.customer_code == code);
        }

        public async Task AddCustomerAsync(customer newCustomer)
        {
            if (newCustomer == null) throw new ArgumentNullException(nameof(newCustomer));
            await _dbContext.customers.AddAsync(newCustomer);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateCustomerAsync(customer existingCustomer)
        {
            if (existingCustomer == null) throw new ArgumentNullException(nameof(existingCustomer));
            _dbContext.customers.Update(existingCustomer);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteCustomerAsync(int id)
        {
            customer? customerToDelete = await GetCustomerByIdAsync(id);
            if (customerToDelete != null)
            {
                _dbContext.customers.Remove(customerToDelete);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}