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

        public async Task<string> get_next_correlative_async()
        {
            var all_note_numbers = await _context.delivery_notes
                .AsNoTracking()
                .Select(n => n.note_number)
                .ToListAsync();

            return SeriesCalculator.GetNextGlobal6(all_note_numbers);
        }
    }
}