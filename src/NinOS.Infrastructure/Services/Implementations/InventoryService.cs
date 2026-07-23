using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class InventoryService : IInventoryService
    {
        private readonly NinOSDbContext _db_context;

        public InventoryService(NinOSDbContext db_context)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));
            _db_context = db_context;
        }

        public async Task<product[]> get_all_products_async()
        {
            return await _db_context.products.ToArrayAsync();
        }

        public async Task add_product_async(product new_product)
        {
            if (new_product == null) throw new ArgumentNullException(nameof(new_product));
            
            await _db_context.products.AddAsync(new_product);
            await _db_context.SaveChangesAsync();
        }

        public async Task update_product_async(product target_product)
        {
            if (target_product == null) throw new ArgumentNullException(nameof(target_product));
            
            _db_context.products.Update(target_product);
            await _db_context.SaveChangesAsync();
        }
    }
}