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

            string prefix_match = prefix + "_";

            var seller_notes = await _context.delivery_notes
                .Where(n => n.id_seller == id_seller)
                .Select(n => n.note_number)
                .ToListAsync();

            int max_number = 0;
            foreach (string note_number in seller_notes)
            {
                if (string.IsNullOrWhiteSpace(note_number) || !note_number.StartsWith(prefix_match, StringComparison.OrdinalIgnoreCase)) continue;

                string[] parts = note_number.Split('_');
                if (parts.Length > 0 && int.TryParse(parts.Last(), out int parsed) && parsed > max_number)
                {
                    max_number = parsed;
                }
            }

            return $"{prefix}_{max_number + 1:D3}";
        }
    }
}