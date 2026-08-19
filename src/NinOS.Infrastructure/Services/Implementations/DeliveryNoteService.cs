using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Repositories.Interfaces;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class DeliveryNoteService : IDeliveryNoteService
    {
        private readonly IDeliveryNoteRepository _delivery_note_repository;
        private readonly NinOSDbContext _db_context;

        public DeliveryNoteService(
            IDeliveryNoteRepository delivery_note_repository, 
            NinOSDbContext db_context)
        {
            if (delivery_note_repository == null) throw new ArgumentNullException(nameof(delivery_note_repository));
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));

            _delivery_note_repository = delivery_note_repository;
            _db_context = db_context;
        }

        public async Task<IEnumerable<delivery_note>> get_all_notes_async()
        {
            return await _delivery_note_repository.get_all_async();
        }

        public async Task<string> generate_correlative_async(int id_seller)
        {
            if (id_seller <= 0) throw new ArgumentException(nameof(id_seller));
            
            return await _delivery_note_repository.get_next_correlative_async(id_seller);
        }

        public async Task create_delivery_note_async(delivery_note new_note, IEnumerable<note_detail> details)
        {
            if (new_note == null) throw new ArgumentNullException(nameof(new_note));
            if (details == null) throw new InvalidOperationException("Los detalles no pueden ser nulos.");

            using (var transaction = await _db_context.Database.BeginTransactionAsync())
            {
                try
                {
                    var details_list = details.ToList();

                    var direct_product_ids = new HashSet<int>();
                    var promotion_ids = new HashSet<int>();

                    foreach (note_detail detail in details_list)
                    {
                        if (detail.id_product != null) direct_product_ids.Add(detail.id_product.Value);
                        if (detail.id_promotion != null) promotion_ids.Add(detail.id_promotion.Value);
                    }

                    var promotions = await _db_context.promotions
                        .Include(pr => pr.items)
                        .Where(p => promotion_ids.Contains(p.id_promotion))
                        .ToDictionaryAsync(p => p.id_promotion);

                    var all_product_ids = new HashSet<int>(direct_product_ids);
                    foreach (var promo in promotions.Values)
                    {
                        if (promo.items != null)
                        {
                            foreach (var item in promo.items)
                            {
                                all_product_ids.Add(item.id_product);
                            }
                        }
                    }

                    var products = await _db_context.products
                        .Where(p => all_product_ids.Contains(p.id_product))
                        .ToDictionaryAsync(p => p.id_product);

                    foreach (note_detail detail in details_list)
                    {
                        if (detail.id_product != null)
                        {
                            if (!products.TryGetValue(detail.id_product.Value, out var p))
                                throw new InvalidOperationException($"Producto con ID {detail.id_product} no encontrado.");
                            if (p.stock_quantity < detail.quantity)
                                throw new InvalidOperationException($"Stock insuficiente para {p.name}");
                            p.stock_quantity -= detail.quantity;
                        }
                        else if (detail.id_promotion != null)
                        {
                            if (!promotions.TryGetValue(detail.id_promotion.Value, out var promo))
                                throw new InvalidOperationException($"Promocion con ID {detail.id_promotion} no encontrada.");
                            if (promo.items == null || promo.items.Count == 0)
                                throw new InvalidOperationException($"La promocion {promo.name} no tiene productos asignados.");

                            foreach (var p_item in promo.items)
                            {
                                if (!products.TryGetValue(p_item.id_product, out var p))
                                {
                                    throw new InvalidOperationException(
                                        $"La promocion '{promo.name}' referencia el producto ID {p_item.id_product} que ya no existe en el inventario. " +
                                        $"Elimine esta promocion y cree una nueva con productos validos.");
                                }
                                int required_qty = detail.quantity * p_item.quantity_required;
                                if (p.stock_quantity < required_qty)
                                    throw new InvalidOperationException($"Stock insuficiente del producto {p.name} para armar la promocion.");
                                p.stock_quantity -= required_qty;
                            }
                        }
                    }

                    await _db_context.delivery_notes.AddAsync(new_note);
                    await _db_context.SaveChangesAsync();

                    foreach (note_detail detail in details_list)
                    {
                        detail.id_delivery_note = new_note.id_delivery_note;
                        await _db_context.note_details.AddAsync(detail);
                    }

                    await _db_context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}
