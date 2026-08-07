using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Repositories.Interfaces;

namespace NinOS.Infrastructure.Repositories.Implementations
{
    public class DeliveryNoteRepository : GenericRepository<delivery_note>, IDeliveryNoteRepository
    {
        private readonly NinOSDbContext _context;

        public DeliveryNoteRepository(NinOSDbContext context) : base(context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _context = context;
        }

        public async Task<string> get_next_correlative_async(int id_seller)
        {
            if (id_seller <= 0) throw new ArgumentException(nameof(id_seller));

            seller? current_seller = await _context.sellers.FindAsync(id_seller);
            if (current_seller == null) throw new InvalidOperationException();
            
            string prefix = current_seller.seller_code;

            delivery_note? last_note = await _context.delivery_notes
                .Where(n => n.id_seller == id_seller)
                .OrderByDescending(n => n.id_delivery_note)
                .FirstOrDefaultAsync();

            int next_number = 1;
            if (last_note != null && !string.IsNullOrWhiteSpace(last_note.note_number))
            {
                string[] parts = last_note.note_number.Split('_');
                if (parts.Length > 0 && int.TryParse(parts.Last(), out int last_number))
                {
                    next_number = last_number + 1;
                }
            }

            return $"{prefix}_{next_number:D3}";
        }
    }
}