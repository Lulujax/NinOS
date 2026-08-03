using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class CustomerService : ICustomerService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public CustomerService(IServiceScopeFactory scopeFactory)
        {
            if (scopeFactory == null) throw new ArgumentNullException(nameof(scopeFactory));
            _scopeFactory = scopeFactory;
        }

        public async Task<IEnumerable<customer>> GetAllCustomersAsync()
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                    return await db_context.customers.ToListAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting customers: {ex.Message}");
                return new List<customer>();
            }
        }

        public async Task<customer?> GetCustomerByIdAsync(int id)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.customers.FirstOrDefaultAsync(c => c.id_customer == id);
            }
        }

        public async Task<customer?> GetCustomerByCodeAsync(string code)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.customers.FirstOrDefaultAsync(c => c.customer_code == code);
            }
        }

        public async Task AddCustomerAsync(customer newCustomer)
        {
            if (newCustomer == null) throw new ArgumentNullException(nameof(newCustomer));
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                await db_context.customers.AddAsync(newCustomer);
                await db_context.SaveChangesAsync();
            }
        }

        public async Task UpdateCustomerAsync(customer existingCustomer)
        {
            if (existingCustomer == null) throw new ArgumentNullException(nameof(existingCustomer));
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                db_context.customers.Update(existingCustomer);
                await db_context.SaveChangesAsync();
            }
        }

        public async Task DeleteCustomerAsync(int id)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                customer? customerToDelete = await db_context.customers.FirstOrDefaultAsync(c => c.id_customer == id);
                if (customerToDelete != null)
                {
                    db_context.customers.Remove(customerToDelete);
                    await db_context.SaveChangesAsync();
                }
            }
        }
    }
}