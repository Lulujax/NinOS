using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Repositories.Interfaces;

namespace NinOS.Infrastructure.Repositories.Implementations
{
    public class CreditNoteRepository : GenericRepository<credit_note>, ICreditNoteRepository
    {
        private readonly NinOSDbContext _context;

        public CreditNoteRepository(NinOSDbContext context) : base(context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _context = context;
        }

        public async Task<string> get_next_credit_correlative_async(int id_seller)
        {
            if (id_seller <= 0) throw new ArgumentException(nameof(id_seller));

            seller? current_seller = await _context.sellers.FindAsync(id_seller);
            if (current_seller == null) throw new InvalidOperationException();

            // Serie unica por vendedor, compartida con las notas de entrega (ej: 3300_042).
            string prefix = current_seller.seller_code + "_";

            int max_number = await GetMaxSeriesNumberAsync(id_seller, prefix);

            return $"{prefix}{max_number + 1:D3}";
        }

        private async Task<int> GetMaxSeriesNumberAsync(int id_seller, string prefix)
        {
            var delivery_numbers = await _context.delivery_notes
                .Where(n => n.id_seller == id_seller)
                .Select(n => n.note_number)
                .ToListAsync();

            var credit_numbers = await _context.credit_notes
                .Where(n => n.id_seller == id_seller)
                .Select(n => n.note_number)
                .ToListAsync();

            int max_number = 0;
            foreach (string note_number in delivery_numbers.Concat(credit_numbers))
            {
                if (string.IsNullOrWhiteSpace(note_number) || !note_number.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

                string[] parts = note_number.Split('_');
                if (parts.Length > 0 && int.TryParse(parts.Last(), out int parsed) && parsed > max_number)
                {
                    max_number = parsed;
                }
            }

            return max_number;
        }
    }
}