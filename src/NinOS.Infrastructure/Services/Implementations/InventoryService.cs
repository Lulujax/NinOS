using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
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

                bool has_note_details = await db_context.note_details.AnyAsync(d => d.id_product == product_to_delete.id_product);
                if (has_note_details)
                    throw new InvalidOperationException("Este producto tiene notas de venta asociadas y no puede eliminarse. Solo puede editarse.");

                bool has_promotion_items = await db_context.promotion_items.AnyAsync(i => i.id_product == product_to_delete.id_product);
                if (has_promotion_items)
                    throw new InvalidOperationException("Este producto forma parte de promociones y no puede eliminarse. Solo puede editarse.");

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

                bool has_note_details = await db_context.note_details.AnyAsync(d => d.id_promotion == promotion_to_delete.id_promotion);
                if (has_note_details)
                    throw new InvalidOperationException("Esta promoción tiene notas de venta asociadas y no puede eliminarse. Solo puede editarse.");

                db_context.promotions.Remove(promotion_to_delete);
                await db_context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<product_sales_history_dto>> get_product_sales_history_async(int id_product)
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                var promotions = await db.promotions
                    .AsNoTracking()
                    .Include(p => p.items)
                    .ToListAsync();

                var promotions_with_product = promotions
                    .Where(p => p.items != null && p.items.Any(i => i.id_product == id_product))
                    .ToDictionary(p => p.id_promotion, p => p.items.First(i => i.id_product == id_product).quantity_required);

                var promotion_ids = promotions_with_product.Keys.ToList();

                var details = new List<note_detail>();
                details.AddRange(await db.note_details
                    .AsNoTracking()
                    .Where(d => d.id_product == id_product)
                    .ToListAsync());

                if (promotion_ids.Count > 0)
                {
                    details.AddRange(await db.note_details
                        .AsNoTracking()
                        .Where(d => d.id_promotion != null && promotion_ids.Contains(d.id_promotion.Value))
                        .ToListAsync());
                }

                if (details.Count == 0) return Enumerable.Empty<product_sales_history_dto>();

                var note_ids = details.Select(d => d.id_delivery_note).Distinct().ToList();
                var notes = await db.delivery_notes
                    .AsNoTracking()
                    .Where(n => note_ids.Contains(n.id_delivery_note))
                    .ToListAsync();

                var customer_ids = notes.Select(n => n.id_customer).Distinct().ToList();
                var customers = await db.customers
                    .AsNoTracking()
                    .Where(c => customer_ids.Contains(c.id_customer))
                    .ToDictionaryAsync(c => c.id_customer);

                var seller_ids = notes.Select(n => n.id_seller).Distinct().ToList();
                var sellers = await db.sellers
                    .AsNoTracking()
                    .Where(s => seller_ids.Contains(s.id_seller))
                    .ToDictionaryAsync(s => s.id_seller);

                var result = new List<product_sales_history_dto>();

                foreach (note_detail d in details)
                {
                    delivery_note? note = notes.FirstOrDefault(n => n.id_delivery_note == d.id_delivery_note);
                    if (note == null) continue;

                    int units;
                    string sold_as;
                    string line_description;

                    if (d.id_product == id_product)
                    {
                        units = d.quantity;
                        sold_as = "Producto";
                        line_description = "";
                    }
                    else if (d.id_promotion != null && promotions_with_product.TryGetValue(d.id_promotion.Value, out int qty_required))
                    {
                        units = d.quantity * qty_required;
                        promotion? promo = promotions.FirstOrDefault(p => p.id_promotion == d.id_promotion.Value);
                        sold_as = "Promoción";
                        line_description = promo?.name ?? "Promoción";
                    }
                    else
                    {
                        continue;
                    }

                    result.Add(new product_sales_history_dto
                    {
                        id_delivery_note = note.id_delivery_note,
                        note_number = note.note_number,
                        creation_date = note.creation_date,
                        customer_name = customers.TryGetValue(note.id_customer, out var c) ? c.business_name : string.Empty,
                        seller_name = sellers.TryGetValue(note.id_seller, out var s) ? s.full_name : string.Empty,
                        line_description = line_description,
                        sold_as = sold_as,
                        units_sold = units,
                        unit_price_usd = d.unit_price_usd,
                        line_subtotal_usd = d.subtotal_usd,
                        status = note.status,
                        movement_type = note.status == "Anulada" ? "ENTRADA" : "SALIDA"
                    });
                }

                return result.OrderByDescending(r => r.creation_date).ToList();
            }
        }
    }
}