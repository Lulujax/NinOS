using System;
using System.Collections.Generic;
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

        public async Task<IEnumerable<product>> get_all_products_async()
        {
            return await _db_context.products.ToListAsync();
        }

        public async Task add_product_async(product new_product)
        {
            if (new_product == null) throw new ArgumentNullException(nameof(new_product));
            await _db_context.products.AddAsync(new_product);
            await _db_context.SaveChangesAsync();
        }

        public async Task update_product_async(product product_to_update)
        {
            if (product_to_update == null) throw new ArgumentNullException(nameof(product_to_update));
            _db_context.products.Update(product_to_update);
            await _db_context.SaveChangesAsync();
        }

        public async Task delete_product_async(product product_to_delete)
        {
            if (product_to_delete == null) throw new ArgumentNullException(nameof(product_to_delete));
            _db_context.products.Remove(product_to_delete);
            await _db_context.SaveChangesAsync();
        }

        public async Task<IEnumerable<promotion>> get_all_promotions_async()
        {
            return await _db_context.promotions
                .Include(p => p.items)
                .ThenInclude(i => i.product)
                .ToListAsync();
        }

        public async Task delete_promotion_async(promotion promotion_to_delete)
        {
            if (promotion_to_delete == null) throw new ArgumentNullException(nameof(promotion_to_delete));
            _db_context.promotions.Remove(promotion_to_delete);
            await _db_context.SaveChangesAsync();
        }

        public async Task add_promotion_async(promotion new_promotion)
        {
            if (new_promotion == null) throw new ArgumentNullException(nameof(new_promotion));
            await _db_context.promotions.AddAsync(new_promotion);
            await _db_context.SaveChangesAsync();
        }
    }
}