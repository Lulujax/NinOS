using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Repositories.Interfaces;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class DeliveryNoteService : IDeliveryNoteService
    {
        private readonly IServiceScopeFactory _scope_factory;

        public DeliveryNoteService(IServiceScopeFactory scope_factory)
        {
            if (scope_factory == null) throw new ArgumentNullException(nameof(scope_factory));
            _scope_factory = scope_factory;
        }

        public async Task<IEnumerable<delivery_note>> get_all_notes_async()
        {
            using var scope = _scope_factory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDeliveryNoteRepository>();
            return await repo.get_all_async();
        }

        public async Task<string> generate_correlative_async(int id_seller)
        {
            if (id_seller <= 0) throw new ArgumentException(nameof(id_seller));
            using var scope = _scope_factory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDeliveryNoteRepository>();
            return await repo.get_next_correlative_async(id_seller);
        }

        public async Task create_delivery_note_async(delivery_note new_note, IEnumerable<note_detail> details)
        {
            if (new_note == null) throw new ArgumentNullException(nameof(new_note));
            if (details == null) throw new InvalidOperationException("Los detalles no pueden ser nulos.");

            using var scope = _scope_factory.CreateScope();
            var _db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

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

                    if (new_note.note_type_id != null)
                    {
                        bool is_mar = await _db_context.note_types
                            .AsNoTracking()
                            .AnyAsync(t => t.id_note_type == new_note.note_type_id.Value && t.code == "MAR");

                        if (is_mar)
                        {
                            relacion relation = await get_or_create_week_relation_async(_db_context, new_note.creation_date);
                            new_note.id_relacion = relation.id_relacion;
                        }
                    }

                    await _db_context.delivery_notes.AddAsync(new_note);
                    try
                    {
                        await _db_context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (is_unique_note_number_violation(ex))
                    {
                        throw new InvalidOperationException("CORRELATIVO_DUPLICADO");
                    }

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

        private static bool is_unique_note_number_violation(DbUpdateException ex)
        {
            Exception? base_exception = ex.GetBaseException();
            string message = base_exception?.Message ?? string.Empty;
            return message.Contains("23505")
                || message.Contains("IX_delivery_note_note_number")
                || message.Contains("duplicate key");
        }

        private static async Task<relacion> get_or_create_week_relation_async(NinOSDbContext db_context, DateTime date)
        {
            int offset = ((int)date.Date.DayOfWeek + 6) % 7;
            DateTime week_start = date.Date.AddDays(-offset);

            relacion? existing = await db_context.relaciones
                .FirstOrDefaultAsync(r => r.week_start == week_start);
            if (existing != null) return existing;

            int next_number = (await db_context.relaciones.MaxAsync(r => (int?)r.relation_number) ?? 0) + 1;

            relacion relation = new relacion(next_number, week_start, week_start.AddDays(6));
            db_context.relaciones.Add(relation);

            try
            {
                await db_context.SaveChangesAsync();
                return relation;
            }
            catch (DbUpdateException)
            {
                // Otra transaccion pudo crear la relacion de esta semana: se reintenta leyendo.
                db_context.Entry(relation).State = EntityState.Detached;

                relacion? created = await db_context.relaciones
                    .FirstOrDefaultAsync(r => r.week_start == week_start);
                if (created != null) return created;
                throw;
            }
        }
    }
}
