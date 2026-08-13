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
        private readonly IServiceScopeFactory _scope_factory;

        public InventoryService(IServiceScopeFactory scope_factory)
        {
            if (scope_factory == null) throw new ArgumentNullException(nameof(scope_factory));
            _scope_factory = scope_factory;
        }

        public async Task<IEnumerable<product>> get_all_products_async()
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.products.ToListAsync();
            }
        }

        public async Task add_product_async(product new_product)
        {
            if (new_product == null) throw new ArgumentNullException(nameof(new_product));
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                await db_context.products.AddAsync(new_product);
                await db_context.SaveChangesAsync();
            }
        }

        public async Task update_product_async(product product_to_update)
        {
            if (product_to_update == null) throw new ArgumentNullException(nameof(product_to_update));
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                db_context.products.Update(product_to_update);
                await db_context.SaveChangesAsync();
            }
        }

        public async Task delete_product_async(product product_to_delete)
        {
            if (product_to_delete == null) throw new ArgumentNullException(nameof(product_to_delete));
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                db_context.products.Remove(product_to_delete);
                await db_context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<promotion>> get_all_promotions_async()
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.promotions
                    .Include(p => p.items)
                    .ThenInclude(i => i.product)
                    .ToListAsync();
            }
        }

        public async Task add_promotion_async(promotion new_promotion)
        {
            if (new_promotion == null) throw new ArgumentNullException(nameof(new_promotion));
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                await db_context.promotions.AddAsync(new_promotion);
                await db_context.SaveChangesAsync();
            }
        }

        public async Task update_promotion_async(promotion promotion_to_update)
        {
            if (promotion_to_update == null) throw new ArgumentNullException(nameof(promotion_to_update));
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                
                promotion? existing_promo = await db_context.promotions
                    .Include(p => p.items)
                    .FirstOrDefaultAsync(p => p.id_promotion == promotion_to_update.id_promotion);
                
                if (existing_promo != null)
                {
                    existing_promo.name = promotion_to_update.name;
                    existing_promo.unit_price_usd = promotion_to_update.unit_price_usd;
                    existing_promo.category = promotion_to_update.category;
                    
                    db_context.promotion_items.RemoveRange(existing_promo.items);
                    existing_promo.items.Clear();
                    
                    foreach (promotion_item item in promotion_to_update.items)
                    {
                        existing_promo.items.Add(new promotion_item(item.id_product, item.quantity_required));
                    }
                    
                    db_context.promotions.Update(existing_promo);
                    await db_context.SaveChangesAsync();
                }
            }
        }

        public async Task delete_promotion_async(promotion promotion_to_delete)
        {
            if (promotion_to_delete == null) throw new ArgumentNullException(nameof(promotion_to_delete));
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                db_context.promotions.Remove(promotion_to_delete);
                await db_context.SaveChangesAsync();
            }
        }
    }
}