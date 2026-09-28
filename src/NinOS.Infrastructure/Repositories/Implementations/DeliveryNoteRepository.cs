using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Common;
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

            // Serie independiente por vendedor sobre SOLO notas de entrega (ej: 3200_001, 3200_100,
            // 3200_999 -> 3201_000). Las notas de credito usan su propia serie.
            var seller_notes = await _context.delivery_notes
                .Where(n => n.id_seller == id_seller)
                .Select(n => n.note_number)
                .ToListAsync();

            return SeriesCalculator.GetNext(seller_notes, current_seller.seller_code);
        }
    }
}