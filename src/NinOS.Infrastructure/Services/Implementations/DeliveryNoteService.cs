using System;
using System.Threading.Tasks;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class DeliveryNoteService : IDeliveryNoteService
    {
        private readonly NinOSDbContext _db_context;

        public DeliveryNoteService(NinOSDbContext db_context)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));
            _db_context = db_context;
        }

        public async Task create_delivery_note_async(delivery_note new_note, note_detail[] details)
        {
            if (new_note == null) throw new ArgumentNullException(nameof(new_note));
            if (details == null || details.Length == 0) throw new ArgumentException();

            using var transaction = await _db_context.Database.BeginTransactionAsync();
            try
            {
                new_note.status = "Pending";
                await _db_context.delivery_notes.AddAsync(new_note);
                await _db_context.SaveChangesAsync();

                for (int i = 0; i < details.Length; i++)
                {
                    note_detail current_detail = details[i];
                    product current_product = await _db_context.products.FindAsync(current_detail.id_product);
                    
                    if (current_product == null) throw new InvalidOperationException();
                    if (current_product.stock_quantity < current_detail.quantity) throw new InvalidOperationException();
                    
                    current_product.stock_quantity -= current_detail.quantity;
                    _db_context.products.Update(current_product);
                    
                    current_detail.id_delivery_note = new_note.id_delivery_note;
                    await _db_context.note_details.AddAsync(current_detail);
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