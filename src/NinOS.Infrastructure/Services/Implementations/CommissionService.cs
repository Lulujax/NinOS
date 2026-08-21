using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class CommissionService : ICommissionService
    {
        private readonly IServiceScopeFactory _scope_factory;

        public CommissionService(IServiceScopeFactory scope_factory)
        {
            if (scope_factory == null) throw new ArgumentNullException(nameof(scope_factory));
            _scope_factory = scope_factory;
        }

        public async Task<commission[]> get_pending_commissions_by_seller_async(int id_seller)
        {
            using var scope = _scope_factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.commissions
                .AsNoTracking()
                .Where(c => c.id_seller == id_seller && !c.is_paid)
                .ToArrayAsync();
        }

        public async Task process_liquidation_async(int[] commission_ids)
        {
            if (commission_ids == null || commission_ids.Length == 0) throw new ArgumentException("Se debe proporcionar al menos una comision.");

            using var scope = _scope_factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                for (int i = 0; i < commission_ids.Length; i++)
                {
                    commission current_commission = await db.commissions.FindAsync(commission_ids[i]);
                    
                    if (current_commission == null) throw new InvalidOperationException($"Comision con ID {commission_ids[i]} no encontrada.");
                    if (current_commission.is_paid) throw new InvalidOperationException($"La comision con ID {commission_ids[i]} ya fue pagada.");

                    current_commission.is_paid = true;
                    current_commission.payout_date = DateTime.UtcNow;
                    
                    db.commissions.Update(current_commission);
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<commission_dto>> get_commissions_by_seller_async(int id_seller)
        {
            using var scope = _scope_factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var commissions = await db.commissions
                .AsNoTracking()
                .Where(c => c.id_seller == id_seller)
                .OrderByDescending(c => c.id_delivery_note)
                .ToListAsync();

            if (commissions.Count == 0) return Enumerable.Empty<commission_dto>();

            var note_ids = commissions.Select(c => c.id_delivery_note).Distinct().ToList();
            var notes = await db.delivery_notes
                .AsNoTracking()
                .Where(n => note_ids.Contains(n.id_delivery_note))
                .ToDictionaryAsync(n => n.id_delivery_note);

            var customer_ids = notes.Values.Select(n => n.id_customer).Distinct().ToList();
            var customers = await db.customers
                .AsNoTracking()
                .Where(c => customer_ids.Contains(c.id_customer))
                .ToDictionaryAsync(c => c.id_customer, c => c.business_name);

            var sellers = await db.sellers
                .AsNoTracking()
                .Where(s => s.id_seller == id_seller)
                .ToDictionaryAsync(s => s.id_seller, s => s.full_name);

            return commissions.Select(c =>
            {
                notes.TryGetValue(c.id_delivery_note, out var note);
                string customer_name = note != null && customers.TryGetValue(note.id_customer, out var cn) ? cn : string.Empty;
                sellers.TryGetValue(c.id_seller, out string? seller_name);
                return new commission_dto
                {
                    id_commission = c.id_commission,
                    id_seller = c.id_seller,
                    seller_name = seller_name ?? string.Empty,
                    id_delivery_note = c.id_delivery_note,
                    note_number = note?.note_number ?? string.Empty,
                    customer_name = customer_name,
                    creation_date = note?.creation_date ?? DateTime.MinValue,
                    commission_percentage = c.commission_percentage,
                    amount_usd = c.amount_usd,
                    amount_bs = c.amount_bs,
                    exchange_rate = c.exchange_rate,
                    reference_number = c.reference_number,
                    is_paid = c.is_paid,
                    payout_date = c.payout_date
                };
            }).ToList();
        }

        public async Task<IEnumerable<commission_dto>> get_commissions_by_seller_and_month_async(int id_seller, string month_year)
        {
            using var scope = _scope_factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var target_date = DateTime.ParseExact(month_year, "MMMM yyyy", new System.Globalization.CultureInfo("es-VE"));

            var commissions = await db.commissions
                .AsNoTracking()
                .Where(c => c.id_seller == id_seller)
                .ToListAsync();

            if (commissions.Count == 0) return Enumerable.Empty<commission_dto>();

            var note_ids = commissions.Select(c => c.id_delivery_note).Distinct().ToList();
            var notes = await db.delivery_notes
                .AsNoTracking()
                .Where(n => note_ids.Contains(n.id_delivery_note))
                .ToDictionaryAsync(n => n.id_delivery_note);

            var customer_ids = notes.Values.Select(n => n.id_customer).Distinct().ToList();
            var customers = await db.customers
                .AsNoTracking()
                .Where(c => customer_ids.Contains(c.id_customer))
                .ToDictionaryAsync(c => c.id_customer, c => c.business_name);

            var sellers = await db.sellers
                .AsNoTracking()
                .Where(s => s.id_seller == id_seller)
                .ToDictionaryAsync(s => s.id_seller, s => s.full_name);

            return commissions
                .Where(c => notes.TryGetValue(c.id_delivery_note, out var note)
                         && note.creation_date.Year == target_date.Year
                         && note.creation_date.Month == target_date.Month)
                .Select(c =>
                {
                    notes.TryGetValue(c.id_delivery_note, out var note);
                    string customer_name = note != null && customers.TryGetValue(note.id_customer, out var cn) ? cn : string.Empty;
                    sellers.TryGetValue(c.id_seller, out string? seller_name);
                    return new commission_dto
                    {
                        id_commission = c.id_commission,
                        id_seller = c.id_seller,
                        seller_name = seller_name ?? string.Empty,
                        id_delivery_note = c.id_delivery_note,
                        note_number = note?.note_number ?? string.Empty,
                        customer_name = customer_name,
                        creation_date = note?.creation_date ?? DateTime.MinValue,
                        commission_percentage = c.commission_percentage,
                        amount_usd = c.amount_usd,
                        amount_bs = c.amount_bs,
                        exchange_rate = c.exchange_rate,
                        reference_number = c.reference_number,
                        is_paid = c.is_paid,
                        payout_date = c.payout_date
                    };
                })
                .OrderByDescending(c => c.creation_date)
                .ToList();
        }

        public async Task<IEnumerable<seller>> get_sellers_with_commissions_async()
        {
            using var scope = _scope_factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.commissions
                .AsNoTracking()
                .Select(c => c.id_seller)
                .Distinct()
                .Join(db.sellers.AsNoTracking(), id => id, s => s.id_seller, (id, s) => s)
                .ToListAsync();
        }

        public async Task register_commission_payment_async(int[] commission_ids, decimal exchange_rate, string payment_type, string reference_number)
        {
            if (commission_ids == null || commission_ids.Length == 0)
                throw new ArgumentException("Debe seleccionar al menos una comision.");

            using var scope = _scope_factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                for (int i = 0; i < commission_ids.Length; i++)
                {
                    var current_commission = await db.commissions.FindAsync(commission_ids[i]);
                    if (current_commission == null) throw new InvalidOperationException($"Comision ID {commission_ids[i]} no encontrada.");
                    if (current_commission.is_paid) throw new InvalidOperationException($"La comision ID {commission_ids[i]} ya fue pagada.");

                    current_commission.is_paid = true;
                    current_commission.payout_date = DateTime.UtcNow;
                    current_commission.exchange_rate = exchange_rate;
                    current_commission.amount_bs = current_commission.amount_usd * exchange_rate;
                    current_commission.reference_number = reference_number;

                    db.commissions.Update(current_commission);
                }

                await db.SaveChangesAsync();
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
