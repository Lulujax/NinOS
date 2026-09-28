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

                stock_movement_writer.registrar_carga_inicial(
                    db_context, new_product, $"ALTA-{new_product.product_code}");

                await db_context.SaveChangesAsync();
            }
        }

        public async Task update_product_async(product product_to_update)
        {
            if (product_to_update == null) throw new ArgumentNullException(nameof(product_to_update));
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                int? stock_actual = await db_context.products
                    .AsNoTracking()
                    .Where(p => p.id_product == product_to_update.id_product)
                    .Select(p => (int?)p.stock_quantity)
                    .FirstOrDefaultAsync();

                int diferencia = product_to_update.stock_quantity - (stock_actual ?? 0);

                db_context.products.Update(product_to_update);

                // Si el usuario edito el stock a mano, queda asentado en el kardex como ajuste.
                if (stock_actual.HasValue && diferencia != 0)
                {
                    stock_movement_writer.registrar_ajuste(
                        db_context,
                        product_to_update.id_product,
                        diferencia,
                        $"AJUSTE-{product_to_update.product_code}",
                        DateTime.UtcNow,
                        product_to_update.unit_price_usd);
                }

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

                // El kardex es la fuente de la verdad: aqui ya estan las ventas, las salidas por
                // obsequio, las entradas por devolucion, las anulaciones y los ajustes manuales.
                var movements = await db.stock_movements
                    .AsNoTracking()
                    .Where(m => m.id_product == id_product)
                    .OrderByDescending(m => m.movement_date)
                    .ThenByDescending(m => m.id_stock_movement)
                    .ToListAsync();

                if (movements.Count == 0) return Enumerable.Empty<product_sales_history_dto>();

                var customer_ids = movements
                    .Where(m => m.id_customer != null)
                    .Select(m => m.id_customer!.Value)
                    .Distinct()
                    .ToList();

                var customers = customer_ids.Count == 0
                    ? new Dictionary<int, customer>()
                    : await db.customers
                        .AsNoTracking()
                        .Where(c => customer_ids.Contains(c.id_customer))
                        .ToDictionaryAsync(c => c.id_customer);

                var seller_ids = movements
                    .Where(m => m.id_seller != null)
                    .Select(m => m.id_seller!.Value)
                    .Distinct()
                    .ToList();

                var sellers = seller_ids.Count == 0
                    ? new Dictionary<int, seller>()
                    : await db.sellers
                        .AsNoTracking()
                        .Where(s => seller_ids.Contains(s.id_seller))
                        .ToDictionaryAsync(s => s.id_seller);

                var result = new List<product_sales_history_dto>();

                foreach (stock_movement m in movements)
                {
                    bool es_entrada = m.movement_type == stock_movement.Entrada;
                    int unidades = m.promotion_units ?? m.quantity;

                    result.Add(new product_sales_history_dto
                    {
                        id_delivery_note = m.id_delivery_note,
                        note_number = m.document_number,
                        creation_date = m.movement_date,
                        customer_name = m.id_customer != null && customers.TryGetValue(m.id_customer.Value, out var c)
                            ? c.business_name
                            : string.Empty,
                        seller_name = m.id_seller != null && sellers.TryGetValue(m.id_seller.Value, out var s)
                            ? s.full_name
                            : string.Empty,
                        line_description = m.line_description ?? string.Empty,
                        sold_as = m.sold_as ?? string.Empty,
                        units_sold = unidades,
                        unit_price_usd = m.unit_price_usd,
                        line_subtotal_usd = m.unit_price_usd * unidades,
                        status = m.document_status ?? string.Empty,
                        movement_type = m.movement_type,
                        movement_reason = m.reason,
                        document_type = m.document_type,
                        is_credit_note = m.id_credit_note != null
                    });
                }

                return result;
            }
        }

        public async Task<IEnumerable<promotion_sales_history_dto>> get_promotion_sales_history_async(int id_promotion)
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                // La promocion genera un movimiento por cada producto que la compone;
                // para la grilla se agrupa por documento y se muestra la cantidad de
                // promociones vendidas/devueltas (promotion_units) y su precio unitario.
                var movements = await db.stock_movements
                    .AsNoTracking()
                    .Where(m => m.id_promotion == id_promotion)
                    .OrderByDescending(m => m.movement_date)
                    .ThenByDescending(m => m.id_stock_movement)
                    .ToListAsync();

                if (movements.Count == 0) return Enumerable.Empty<promotion_sales_history_dto>();

                var customer_ids = movements
                    .Where(m => m.id_customer != null)
                    .Select(m => m.id_customer!.Value)
                    .Distinct()
                    .ToList();

                var customers = customer_ids.Count == 0
                    ? new Dictionary<int, customer>()
                    : await db.customers
                        .AsNoTracking()
                        .Where(c => customer_ids.Contains(c.id_customer))
                        .ToDictionaryAsync(c => c.id_customer);

                var seller_ids = movements
                    .Where(m => m.id_seller != null)
                    .Select(m => m.id_seller!.Value)
                    .Distinct()
                    .ToList();

                var sellers = seller_ids.Count == 0
                    ? new Dictionary<int, seller>()
                    : await db.sellers
                        .AsNoTracking()
                        .Where(s => seller_ids.Contains(s.id_seller))
                        .ToDictionaryAsync(s => s.id_seller);

                var result = new List<promotion_sales_history_dto>();

                var groups = movements.GroupBy(m => new
                {
                    m.id_delivery_note,
                    m.id_credit_note,
                    m.document_number,
                    m.movement_type,
                    m.reason,
                    m.document_type,
                    m.document_status,
                    m.id_customer,
                    m.id_seller,
                    m.unit_price_usd,
                    m.movement_date
                });

                foreach (var group in groups)
                {
                    stock_movement first = group.First();
                    int unidades = group.Max(x => x.promotion_units ?? x.quantity);

                    result.Add(new promotion_sales_history_dto
                    {
                        id_delivery_note = first.id_delivery_note,
                        note_number = first.document_number,
                        creation_date = first.movement_date,
                        customer_name = first.id_customer != null && customers.TryGetValue(first.id_customer.Value, out var c)
                            ? c.business_name
                            : string.Empty,
                        seller_name = first.id_seller != null && sellers.TryGetValue(first.id_seller.Value, out var s)
                            ? s.full_name
                            : string.Empty,
                        line_description = first.line_description ?? string.Empty,
                        sold_as = first.sold_as ?? string.Empty,
                        units_sold = unidades,
                        unit_price_usd = first.unit_price_usd,
                        line_subtotal_usd = first.unit_price_usd * unidades,
                        status = first.document_status ?? string.Empty,
                        movement_type = first.movement_type,
                        movement_reason = first.reason,
                        document_type = first.document_type,
                        is_credit_note = first.id_credit_note != null
                    });
                }

                return result
                    .OrderByDescending(r => r.creation_date)
                    .ThenByDescending(r => r.id_delivery_note)
                    .ToList();
            }
        }
    }
}