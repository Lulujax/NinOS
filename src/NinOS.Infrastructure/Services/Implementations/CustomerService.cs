using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Common;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;
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
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.customers.AsNoTracking()
                    .Where(c => c.is_active)
                    .OrderBy(c => c.customer_code)
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<customer>> GetDeletedCustomersAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.customers.AsNoTracking()
                    .Where(c => !c.is_active)
                    .OrderByDescending(c => c.deleted_at)
                    .ToListAsync();
            }
        }

        public async Task<customer?> GetCustomerByIdAsync(int id)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.customers.AsNoTracking().FirstOrDefaultAsync(c => c.id_customer == id);
            }
        }

        public async Task<customer?> GetCustomerByCodeAsync(string code)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.customers.AsNoTracking().FirstOrDefaultAsync(c => c.customer_code == code);
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
                await UpdateSellerLastCustomerNumberAsync(db_context, newCustomer);
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
                await UpdateSellerLastCustomerNumberAsync(db_context, existingCustomer);
            }
        }

        private static async Task UpdateSellerLastCustomerNumberAsync(NinOSDbContext db_context, customer changedCustomer)
        {
            if (string.IsNullOrWhiteSpace(changedCustomer.customer_code)) return;

            long full_number = SeriesCalculator.ParseFullNumber(changedCustomer.customer_code);
            if (full_number <= 0) return;

            seller? target_seller = await db_context.sellers
                .FirstOrDefaultAsync(s => s.full_name == changedCustomer.seller_name);
            if (target_seller == null) return;

            if (full_number <= target_seller.last_customer_number) return;

            target_seller.last_customer_number = full_number;
            await db_context.SaveChangesAsync();
        }

        public async Task SoftDeleteCustomerAsync(int id, string? reason)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                customer? customer_to_delete = await db_context.customers.FirstOrDefaultAsync(c => c.id_customer == id);
                if (customer_to_delete == null || !customer_to_delete.is_active) return;

                customer_to_delete.is_active = false;
                customer_to_delete.deleted_at = DateTime.UtcNow;
                customer_to_delete.deleted_reason = string.IsNullOrWhiteSpace(reason) ? "Sin motivo" : reason.Trim();
                await db_context.SaveChangesAsync();
                AppLog.Info($"Cliente {customer_to_delete.customer_code} enviado a la papelera. Motivo: {customer_to_delete.deleted_reason}");
            }
        }

        public async Task RestoreCustomerAsync(int id)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                customer? customer_to_restore = await db_context.customers.FirstOrDefaultAsync(c => c.id_customer == id);
                if (customer_to_restore == null || customer_to_restore.is_active) return;

                customer_to_restore.is_active = true;
                customer_to_restore.deleted_at = null;
                customer_to_restore.deleted_reason = null;
                await db_context.SaveChangesAsync();
                AppLog.Info($"Cliente {customer_to_restore.customer_code} restaurado desde la papelera.");
            }
        }
    }
}