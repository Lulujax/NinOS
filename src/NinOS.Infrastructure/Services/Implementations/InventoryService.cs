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
    public class InventoryService : IInventoryService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public InventoryService(IServiceScopeFactory scopeFactory)
        {
            if (scopeFactory == null) throw new ArgumentNullException(nameof(scopeFactory));
            _scopeFactory = scopeFactory;
        }

        public async Task<IEnumerable<product>> get_all_products_async()
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                    return await db_context.products.ToListAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting products: {ex.Message}");
                return new List<product>();
            }
        }

        public async Task add_product_async(product new_product)
        {
            if (new_product == null) throw new ArgumentNullException(nameof(new_product));
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                await db_context.products.AddAsync(new_product);
                await db_context.SaveChangesAsync();
            }
        }

        public async Task update_product_async(product product_to_update)
        {
            if (product_to_update == null) throw new ArgumentNullException(nameof(product_to_update));
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                db_context.products.Update(product_to_update);
                await db_context.SaveChangesAsync();
            }
        }

        public async Task delete_product_async(product product_to_delete)
        {
            if (product_to_delete == null) throw new ArgumentNullException(nameof(product_to_delete));
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                db_context.products.Remove(product_to_delete);
                await db_context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<promotion>> get_all_promotions_async()
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                    return await db_context.promotions
                        .Include(p => p.items)
                        .ThenInclude(i => i.product)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting promotions: {ex.Message}");
                return new List<promotion>();
            }
        }

        public async Task add_promotion_async(promotion new_promotion)
        {
            if (new_promotion == null) throw new ArgumentNullException(nameof(new_promotion));
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                await db_context.promotions.AddAsync(new_promotion);
                await db_context.SaveChangesAsync();
            }
        }

        public async Task update_promotion_async(promotion promotion_to_update)
        {
            if (promotion_to_update == null) throw new ArgumentNullException(nameof(promotion_to_update));
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                db_context.promotions.Update(promotion_to_update);
                await db_context.SaveChangesAsync();
            }
        }

        public async Task delete_promotion_async(promotion promotion_to_delete)
        {
            if (promotion_to_delete == null) throw new ArgumentNullException(nameof(promotion_to_delete));
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                db_context.promotions.Remove(promotion_to_delete);
                await db_context.SaveChangesAsync();
            }
        }
    }
}