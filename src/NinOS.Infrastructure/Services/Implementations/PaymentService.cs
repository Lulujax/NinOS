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
    public class PaymentService : IPaymentService
    {
        private readonly IServiceScopeFactory _scope_factory;

        public PaymentService(IServiceScopeFactory scope_factory)
        {
            if (scope_factory == null) throw new ArgumentNullException(nameof(scope_factory));
            _scope_factory = scope_factory;
        }

        public async Task register_payment_async(payment new_payment)
        {
            if (new_payment == null) throw new ArgumentNullException(nameof(new_payment));

            using var scope = _scope_factory.CreateScope();
            var _db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            using var transaction = await _db_context.Database.BeginTransactionAsync();
            try
            {
                delivery_note target_note = await _db_context.delivery_notes.FindAsync(new_payment.id_delivery_note);
                if (target_note == null) throw new InvalidOperationException("La nota de entrega no existe.");
                if (target_note.status == "Anulada") throw new InvalidOperationException("No se puede abonar una nota anulada.");
                if (target_note.status == "Pagada") throw new InvalidOperationException("No se puede abonar una nota ya pagada.");

                new_payment.amount_bs = new_payment.exchange_rate > 0 ? new_payment.amount_usd * new_payment.exchange_rate.Value : 0;
                await _db_context.payments.AddAsync(new_payment);
                await _db_context.SaveChangesAsync();

                payment[] all_payments = await _db_context.payments
                    .Where(p => p.id_delivery_note == target_note.id_delivery_note)
                    .ToArrayAsync();

                decimal total_paid_usd = 0;
                for (int i = 0; i < all_payments.Length; i++)
                {
                    total_paid_usd += all_payments[i].amount_usd;
                }

                if (total_paid_usd >= target_note.total_amount_usd)
                {
                    target_note.status = "Pagada";
                    _db_context.delivery_notes.Update(target_note);
                    
                    decimal generated_amount_usd = target_note.total_amount_usd * 0.10m;
                    commission new_commission = new commission(
                        target_note.id_seller,
                        target_note.id_delivery_note,
                        0.10m,
                        generated_amount_usd,
                        false,
                        null);

                    await _db_context.commissions.AddAsync(new_commission);
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

        public async Task<IEnumerable<payment_dto>> get_payments_by_note_async(int id_delivery_note)
        {
            using var scope = _scope_factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var payments = await db.payments
                .AsNoTracking()
                .Where(p => p.id_delivery_note == id_delivery_note)
                .OrderByDescending(p => p.payment_date)
                .ToListAsync();

            var note = await db.delivery_notes.FindAsync(id_delivery_note);
            var customer = note != null ? await db.customers.FindAsync(note.id_customer) : null;
            var seller = note != null ? await db.sellers.FindAsync(note.id_seller) : null;

            return payments.Select(p => new payment_dto
            {
                id_payment = p.id_payment,
                id_delivery_note = p.id_delivery_note,
                note_number = note?.note_number ?? string.Empty,
                customer_name = customer?.business_name ?? string.Empty,
                seller_name = seller?.full_name ?? string.Empty,
                id_seller = note?.id_seller ?? 0,
                payment_date = p.payment_date,
                amount_usd = p.amount_usd,
                amount_bs = p.amount_bs,
                exchange_rate = p.exchange_rate,
                payment_type = p.payment_type,
                reference_number = p.reference_number,
                total_note_usd = note?.total_amount_usd ?? 0,
                balance_due_usd = (note?.total_amount_usd ?? 0) - payments.Sum(x => x.amount_usd)
            }).ToList();
        }

        public async Task<IEnumerable<payment_dto>> get_payments_by_month_async(string month_year)
        {
            using var scope = _scope_factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var target_date = DateTime.ParseExact(month_year, "MMMM yyyy", new System.Globalization.CultureInfo("es-VE"));

            var payments = await db.payments
                .AsNoTracking()
                .Where(p => p.payment_date.Year == target_date.Year && p.payment_date.Month == target_date.Month)
                .OrderByDescending(p => p.payment_date)
                .ToListAsync();

            if (payments.Count == 0) return Enumerable.Empty<payment_dto>();

            var note_ids = payments.Select(p => p.id_delivery_note).Distinct().ToList();
            var notes = await db.delivery_notes
                .AsNoTracking()
                .Where(n => note_ids.Contains(n.id_delivery_note))
                .ToDictionaryAsync(n => n.id_delivery_note);

            var seller_ids = notes.Values.Select(n => n.id_seller).Distinct().ToList();
            var customer_ids = notes.Values.Select(n => n.id_customer).Distinct().ToList();

            var sellers = await db.sellers
                .AsNoTracking()
                .Where(s => seller_ids.Contains(s.id_seller))
                .ToDictionaryAsync(s => s.id_seller, s => s.full_name);
            var customers = await db.customers
                .AsNoTracking()
                .Where(c => customer_ids.Contains(c.id_customer))
                .ToDictionaryAsync(c => c.id_customer, c => c.business_name);

            return payments.Select(p =>
            {
                notes.TryGetValue(p.id_delivery_note, out var note);
                string seller_name = note != null && sellers.TryGetValue(note.id_seller, out var sn) ? sn : string.Empty;
                string customer_name = note != null && customers.TryGetValue(note.id_customer, out var cn) ? cn : string.Empty;
                return new payment_dto
                {
                    id_payment = p.id_payment,
                    id_delivery_note = p.id_delivery_note,
                    note_number = note?.note_number ?? string.Empty,
                    customer_name = customer_name,
                    seller_name = seller_name,
                    id_seller = note?.id_seller ?? 0,
                    payment_date = p.payment_date,
                    amount_usd = p.amount_usd,
                    amount_bs = p.amount_bs,
                    exchange_rate = p.exchange_rate,
                    payment_type = p.payment_type,
                    reference_number = p.reference_number,
                    total_note_usd = note?.total_amount_usd ?? 0
                };
            }).ToList();
        }

        public async Task<IEnumerable<payment_dto>> get_payments_by_month_and_seller_async(string month_year, int id_seller)
        {
            var all = await get_payments_by_month_async(month_year);
            return all.Where(p => p.id_seller == id_seller);
        }
    }
}
