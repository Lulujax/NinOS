using System;
using System.Collections.Generic;
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
            if (details == null) throw new InvalidOperationException();

            using (var transaction = await _db_context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (note_detail detail in details)
                    {
                        if (detail.id_product != null)
                        {
                            product p = await _db_context.products.FindAsync(detail.id_product);
                            if (p == null) throw new InvalidOperationException($"Producto con ID {detail.id_product} no encontrado.");
                            if (p.stock_quantity < detail.quantity) throw new InvalidOperationException($"Stock insuficiente para {p.name}");
                            p.stock_quantity -= detail.quantity;
                            _db_context.products.Update(p);
                        }
                        else if (detail.id_promotion != null)
                        {
                            promotion promo = await _db_context.promotions
                                .Include(pr => pr.items)
                                .FirstOrDefaultAsync(pr => pr.id_promotion == detail.id_promotion);
                            
                            if (promo == null) throw new InvalidOperationException($"Promoción con ID {detail.id_promotion} no encontrada.");
                            if (promo.items == null || promo.items.Count == 0) throw new InvalidOperationException($"La promoción {promo.name} no tiene productos asignados.");

                            foreach (var p_item in promo.items)
                            {
                                product p = await _db_context.products.FindAsync(p_item.id_product);
                                if (p == null) throw new InvalidOperationException($"Producto con ID {p_item.id_product} no encontrado.");
                                int required_qty = detail.quantity * p_item.quantity_required;
                                if (p.stock_quantity < required_qty) throw new InvalidOperationException($"Stock insuficiente del producto {p.name} para armar la promoción.");
                                p.stock_quantity -= required_qty;
                                _db_context.products.Update(p);
                            }
                        }
                    }

                    await _delivery_note_repository.add_async(new_note);
                    await _db_context.SaveChangesAsync();

                    foreach (note_detail detail in details)
                    {
                        detail.id_delivery_note = new_note.id_delivery_note;
                        await _db_context.note_details.AddAsync(detail);
                    }

                    await _db_context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}