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
    public class CreditNoteRepository : GenericRepository<credit_note>, ICreditNoteRepository
    {
        private readonly NinOSDbContext _context;

        public CreditNoteRepository(NinOSDbContext context) : base(context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _context = context;
        }

        public async Task<string> get_next_credit_correlative_async()
        {
            var credit_notes = await _context.credit_notes
                .AsNoTracking()
                .Select(n => n.note_number)
                .ToListAsync();

            return SeriesCalculator.GetNextGlobal6(credit_notes);
        }
    }
}