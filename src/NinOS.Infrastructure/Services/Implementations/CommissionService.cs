using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class CommissionService : ICommissionService
    {
        private readonly NinOSDbContext _db_context;

        public CommissionService(NinOSDbContext db_context)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));
            _db_context = db_context;
        }

        public async Task<commission[]> get_pending_commissions_by_seller_async(int id_seller)
        {
            return await _db_context.commissions
                .Where(c => c.id_seller == id_seller && !c.is_paid)
                .ToArrayAsync();
        }

        public async Task process_liquidation_async(int[] commission_ids)
        {
            if (commission_ids == null || commission_ids.Length == 0) throw new ArgumentException();

            using var transaction = await _db_context.Database.BeginTransactionAsync();
            try
            {
                for (int i = 0; i < commission_ids.Length; i++)
                {
                    commission current_commission = await _db_context.commissions.FindAsync(commission_ids[i]);
                    
                    if (current_commission == null) throw new InvalidOperationException();
                    if (current_commission.is_paid) throw new InvalidOperationException();

                    current_commission.is_paid = true;
                    current_commission.payout_date = DateTime.Now;
                    
                    _db_context.commissions.Update(current_commission);
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