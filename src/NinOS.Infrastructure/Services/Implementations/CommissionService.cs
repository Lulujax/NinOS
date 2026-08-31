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
                    paid_amount_usd = 0,
                    amount_bs = c.amount_bs,
                    exchange_rate = c.exchange_rate,
                    reference_number = c.reference_number,
                    is_paid = c.is_paid,
                    payout_date = c.payout_date
                };
            }).ToList();
        }

        public async Task<IEnumerable<commission_dto>> get_all_commissions_async()
        {
            using var scope = _scope_factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var commissions = await db.commissions
                .AsNoTracking()
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

            var seller_ids = commissions.Select(c => c.id_seller).Distinct().ToList();
            var sellers = await db.sellers
                .AsNoTracking()
                .Where(s => seller_ids.Contains(s.id_seller))
                .ToDictionaryAsync(s => s.id_seller, s => s.full_name);

            var note_ids_with_payments = await db.payments
                .AsNoTracking()
                .Where(p => note_ids.Contains(p.id_delivery_note))
                .GroupBy(p => p.id_delivery_note)
                .Select(g => new { Id = g.Key, MaxDate = g.Max(p => p.payment_date) })
                .ToDictionaryAsync(x => x.Id, x => x.MaxDate);

            var commission_ids_with_payments = commissions.Select(c => c.id_commission).Distinct().ToList();
            var commissioned_paid_map = await db.commission_payments
                .AsNoTracking()
                .Where(p => commission_ids_with_payments.Contains(p.id_commission))
                .GroupBy(p => p.id_commission)
                .Select(g => new { Id = g.Key, Paid = g.Sum(p => p.amount_usd) })
                .ToDictionaryAsync(x => x.Id, x => x.Paid);

            return commissions.Select(c =>
            {
                notes.TryGetValue(c.id_delivery_note, out var note);
                string customer_name = note != null && customers.TryGetValue(note.id_customer, out var cn) ? cn : string.Empty;
                sellers.TryGetValue(c.id_seller, out string? seller_name);
                note_ids_with_payments.TryGetValue(c.id_delivery_note, out DateTime raw_last_payment_date);
                DateTime? note_last_payment_date = raw_last_payment_date;
                commissioned_paid_map.TryGetValue(c.id_commission, out decimal paid);
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
                    paid_amount_usd = paid,
                    amount_bs = c.amount_bs,
                    exchange_rate = c.exchange_rate,
                    reference_number = c.reference_number,
                    is_paid = c.is_paid,
                    payout_date = c.payout_date,
                    note_last_payment_date = note_last_payment_date
                };
            })
            .OrderByDescending(c => c.creation_date)
            .ToList();
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
                    paid_amount_usd = 0,
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

        public async Task<IEnumerable<commission_payment_dto>> get_commission_payments_async(int id_commission)
        {
            using var scope = _scope_factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            return await db.commission_payments
                .AsNoTracking()
                .Where(p => p.id_commission == id_commission)
                .OrderByDescending(p => p.payment_date)
                .Select(p => new commission_payment_dto
                {
                    id_commission_payment = p.id_commission_payment,
                    id_commission = p.id_commission,
                    amount_usd = p.amount_usd,
                    amount_bs = p.amount_bs,
                    exchange_rate = p.exchange_rate,
                    payment_type = p.payment_type,
                    reference_number = p.reference_number,
                    bank_name = p.bank_name,
                    notes = p.observations,
                    payment_date = p.payment_date
                })
                .ToListAsync();
        }

        public async Task register_commission_payment_async(int[] commission_ids, decimal amount_usd, decimal exchange_rate, string payment_type, string reference_number, decimal amount_bs, DateTime payment_date, string bank_name = "", string observations = "")
        {
            if (commission_ids == null || commission_ids.Length == 0)
                throw new ArgumentException("Debe seleccionar al menos una comision.");
            if (amount_usd <= 0)
                throw new ArgumentException("El monto USD a pagar debe ser mayor a cero.");
            if (exchange_rate <= 0)
                throw new ArgumentException("La tasa BS/USD es obligatoria.");
            if (exchange_rate > 10_000_000m)
                throw new ArgumentException("La tasa BS/USD es demasiado grande.");
            if (amount_usd > 10_000_000m)
                throw new ArgumentException("El monto USD a pagar es demasiado grande.");
            if (string.IsNullOrWhiteSpace(reference_number))
                throw new ArgumentException("La referencia es obligatoria.");

            using var scope = _scope_factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var commissions = new List<commission>();
                foreach (int id in commission_ids)
                {
                    var current_commission = await db.commissions.FindAsync(id);
                    if (current_commission == null) throw new InvalidOperationException($"Comision ID {id} no encontrada.");
                    commissions.Add(current_commission);
                }

                var paid_map = await db.commission_payments
                    .AsNoTracking()
                    .Where(p => commission_ids.Contains(p.id_commission))
                    .GroupBy(p => p.id_commission)
                    .Select(g => new { Id = g.Key, Paid = g.Sum(p => p.amount_usd) })
                    .ToDictionaryAsync(x => x.Id, x => x.Paid);

                var pending = commissions
                    .Where(c => !c.is_paid)
                    .Select(c => new { commission = c, pending = c.amount_usd - (paid_map.TryGetValue(c.id_commission, out var paid) ? paid : 0) })
                    .Where(x => x.pending > 0)
                    .OrderBy(x => x.pending)
                    .ToList();

                if (pending.Count == 0)
                    throw new InvalidOperationException("Las comisiones seleccionadas ya fueron pagadas.");

                DateTime now = DateTime.UtcNow;
                DateTime pay_date = payment_date == default ? now : (payment_date.Kind == DateTimeKind.Utc ? payment_date : payment_date.ToUniversalTime());
                decimal budget = amount_usd;

                foreach (var item in pending)
                {
                    if (budget <= 0) break;

                    decimal alloc = Math.Min(budget, item.pending);
                    decimal alloc_bs;
                    try
                    {
                        alloc_bs = Math.Round(alloc * exchange_rate, 2);
                    }
                    catch (OverflowException)
                    {
                        throw new InvalidOperationException("El calculo del monto en Bs desborda el rango permitido. Revise la tasa BS/USD ingresada.");
                    }
                    var p = new commission_payment(item.commission.id_commission, alloc, alloc_bs, exchange_rate, payment_type ?? "Pago Movil", reference_number, pay_date, bank_name ?? string.Empty, observations ?? string.Empty);
                    db.commission_payments.Add(p);

                    budget -= alloc;

                    decimal total_paid = item.commission.amount_usd - item.pending + alloc;
                    if (total_paid >= item.commission.amount_usd - 0.005m)
                    {
                        item.commission.is_paid = true;
                        item.commission.payout_date = now;
                        db.commissions.Update(item.commission);
                    }
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
