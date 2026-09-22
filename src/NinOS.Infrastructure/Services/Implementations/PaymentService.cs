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

        public async Task register_payment_async(payment new_payment, bool is_pro_venta = false)
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

                var mar_ids = await get_pro_venta_type_ids_async(_db_context);
                if (!is_pro_venta && target_note.note_type_id != null && mar_ids.Contains(target_note.note_type_id.Value))
                    throw new InvalidOperationException("Los pagos de notas Pro Venta (MAR) se gestionan en el modulo Pro Venta.");

                new_payment.amount_bs = new_payment.amount_bs < 0 ? 0 : new_payment.amount_bs;
                if (string.IsNullOrEmpty(new_payment.bank_name)) new_payment.bank_name = "";
                if (string.IsNullOrEmpty(new_payment.observations)) new_payment.observations = "";
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

                if (total_paid_usd >= target_note.adjusted_total_usd && target_note.status != "Pagada")
                {
                    target_note.status = "Pagada";

                    if (!is_pro_venta)
                    {
                        decimal generated_amount_usd = target_note.adjusted_total_usd * 0.10m;
                        commission new_commission = new commission(
                            target_note.id_seller,
                            target_note.id_delivery_note,
                            0.10m,
                            generated_amount_usd,
                            false,
                            null);

                        await _db_context.commissions.AddAsync(new_commission);
                    }
                }

                await _db_context.SaveChangesAsync();
                await transaction.CommitAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"[PAYMENT] Note={target_note.note_number} | Total={target_note.total_amount_usd} | Paid={total_paid_usd} | Status={target_note.status}");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task register_relation_payment_async(int id_relacion, decimal amount_usd, DateTime payment_date, string observations = "")
        {
            if (id_relacion <= 0) throw new ArgumentException(nameof(id_relacion));
            if (amount_usd <= 0) throw new InvalidOperationException("El monto debe ser mayor a 0.");

            using var scope = _scope_factory.CreateScope();
            var _db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            bool relation_exists = await _db_context.relaciones.AnyAsync(r => r.id_relacion == id_relacion);
            if (!relation_exists) throw new InvalidOperationException("La relacion no existe.");

            payment new_payment = new payment(
                null, payment_date, amount_usd, 0, null,
                "Efectivo", $"REL-{id_relacion}", "", observations ?? "", id_relacion);

            await _db_context.payments.AddAsync(new_payment);
            await _db_context.SaveChangesAsync();
        }

        public async Task update_relation_payment_async(int id_payment, decimal amount_usd, DateTime payment_date, string observations)
        {
            if (amount_usd <= 0) throw new InvalidOperationException("El monto debe ser mayor a 0.");

            using var scope = _scope_factory.CreateScope();
            var _db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            payment? existing = await _db_context.payments.FindAsync(id_payment);
            if (existing == null) throw new InvalidOperationException("El pago no existe.");
            if (existing.id_relacion == null) throw new InvalidOperationException("El pago no pertenece a una relacion.");

            existing.amount_usd = amount_usd;
            existing.payment_date = payment_date;
            existing.observations = observations ?? "";
            existing.updated_at = DateTime.UtcNow;

            await _db_context.SaveChangesAsync();
        }

        public async Task update_payment_async(payment updated_payment, bool is_pro_venta = false)
        {
            if (updated_payment == null) throw new ArgumentNullException(nameof(updated_payment));

            using var scope = _scope_factory.CreateScope();
            var _db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            using var transaction = await _db_context.Database.BeginTransactionAsync();
            try
            {
                payment existing = await _db_context.payments.FindAsync(updated_payment.id_payment);
                if (existing == null) throw new InvalidOperationException("El pago no existe.");

                delivery_note target_note = await _db_context.delivery_notes.FindAsync(existing.id_delivery_note);
                if (target_note == null) throw new InvalidOperationException("La nota de entrega no existe.");

                var mar_ids = await get_pro_venta_type_ids_async(_db_context);
                if (!is_pro_venta && target_note.note_type_id != null && mar_ids.Contains(target_note.note_type_id.Value))
                    throw new InvalidOperationException("Los pagos de notas Pro Venta (MAR) se gestionan en el modulo Pro Venta.");

                existing.id_delivery_note = updated_payment.id_delivery_note;
                existing.payment_date = updated_payment.payment_date;
                existing.amount_usd = updated_payment.amount_usd;
                existing.amount_bs = updated_payment.amount_bs < 0 ? 0 : updated_payment.amount_bs;
                existing.exchange_rate = updated_payment.exchange_rate;
                existing.payment_type = updated_payment.payment_type;
                existing.reference_number = updated_payment.reference_number;
                existing.bank_name = updated_payment.bank_name ?? "";
                existing.observations = updated_payment.observations ?? "";
                existing.updated_at = DateTime.UtcNow;

                await _db_context.SaveChangesAsync();

                payment[] all_payments = await _db_context.payments
                    .Where(p => p.id_delivery_note == existing.id_delivery_note)
                    .ToArrayAsync();

                decimal total_paid_usd = all_payments.Sum(p => p.amount_usd);

                bool is_fully_paid = total_paid_usd >= target_note.adjusted_total_usd;

                var existing_commission = await _db_context.commissions
                    .FirstOrDefaultAsync(c => c.id_delivery_note == target_note.id_delivery_note);

                if (is_fully_paid)
                {
                    if (target_note.status != "Pagada")
                    {
                        target_note.status = "Pagada";

                        if (!is_pro_venta && existing_commission == null)
                        {
                            decimal generated_amount_usd = target_note.adjusted_total_usd * 0.10m;
                            commission new_commission = new commission(
                                target_note.id_seller,
                                target_note.id_delivery_note,
                                0.10m,
                                generated_amount_usd,
                                false,
                                null);
                            await _db_context.commissions.AddAsync(new_commission);
                        }
                    }
                }
                else
                {
                    if (target_note.status == "Pagada") target_note.status = "Pendiente";

                    if (!is_pro_venta && existing_commission != null)
                    {
                        _db_context.commissions.Remove(existing_commission);
                    }
                }

                await _db_context.SaveChangesAsync();
                await transaction.CommitAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"[PAYMENT-UPDATE] Note={target_note.note_number} | Paid={total_paid_usd} | Status={target_note.status}");
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
                id_delivery_note = p.id_delivery_note ?? 0,
                note_number = note?.note_number ?? string.Empty,
                customer_name = customer?.business_name ?? string.Empty,
                seller_name = seller?.full_name ?? string.Empty,
                id_seller = note?.id_seller ?? 0,
                payment_date = p.payment_date,
                created_at = p.created_at,
                updated_at = p.updated_at,
                amount_usd = p.amount_usd,
                amount_bs = p.amount_bs,
                exchange_rate = p.exchange_rate,
                payment_type = p.payment_type,
                bank_name = p.bank_name,
                reference_number = p.reference_number,
                notes = p.observations,
                total_note_usd = note?.adjusted_total_usd ?? 0,
                balance_due_usd = (note?.adjusted_total_usd ?? 0) - payments.Sum(x => x.amount_usd)
            }).ToList();
        }

        public async Task<IEnumerable<payment_dto>> get_payments_by_relation_async(int id_relacion)
        {
            using var scope = _scope_factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var payments = await db.payments
                .AsNoTracking()
                .Where(p => p.id_relacion == id_relacion)
                .OrderByDescending(p => p.payment_date)
                .ToListAsync();

            return payments.Select(p => new payment_dto
            {
                id_payment = p.id_payment,
                id_delivery_note = p.id_delivery_note ?? 0,
                id_relacion = p.id_relacion,
                payment_date = p.payment_date,
                created_at = p.created_at,
                updated_at = p.updated_at,
                amount_usd = p.amount_usd,
                amount_bs = p.amount_bs,
                exchange_rate = p.exchange_rate,
                payment_type = p.payment_type,
                bank_name = p.bank_name,
                reference_number = p.reference_number,
                notes = p.observations
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

            var mar_ids = await get_pro_venta_type_ids_async(db);
            var mar_note_ids = notes.Values
                .Where(n => n.note_type_id != null && mar_ids.Contains(n.note_type_id.Value))
                .Select(n => n.id_delivery_note)
                .ToHashSet();

            if (mar_note_ids.Count > 0)
            {
                payments = payments.Where(p => p.id_delivery_note == null || !mar_note_ids.Contains(p.id_delivery_note.Value)).ToList();
                if (payments.Count == 0) return Enumerable.Empty<payment_dto>();

                note_ids = payments.Select(p => p.id_delivery_note).Distinct().ToList();
                notes = await db.delivery_notes
                    .AsNoTracking()
                    .Where(n => note_ids.Contains(n.id_delivery_note))
                    .ToDictionaryAsync(n => n.id_delivery_note);
            }

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
                delivery_note? note = p.id_delivery_note != null && notes.TryGetValue(p.id_delivery_note.Value, out var n) ? n : null;
                string seller_name = note != null && sellers.TryGetValue(note.id_seller, out var sn) ? sn : string.Empty;
                string customer_name = note != null && customers.TryGetValue(note.id_customer, out var cn) ? cn : string.Empty;
                return new payment_dto
                {
                    id_payment = p.id_payment,
                    id_delivery_note = p.id_delivery_note ?? 0,
                    id_relacion = p.id_relacion,
                    note_number = note?.note_number ?? string.Empty,
                    customer_name = customer_name,
                    seller_name = seller_name,
                    id_seller = note?.id_seller ?? 0,
                    payment_date = p.payment_date,
                    created_at = p.created_at,
                    updated_at = p.updated_at,
                    amount_usd = p.amount_usd,
                    amount_bs = p.amount_bs,
                    exchange_rate = p.exchange_rate,
                    payment_type = p.payment_type,
                    bank_name = p.bank_name,
                    reference_number = p.reference_number,
                    notes = p.observations,
                    total_note_usd = note?.adjusted_total_usd ?? 0
                };
            }).ToList();
        }

        public async Task<IEnumerable<payment_dto>> get_payments_by_month_and_seller_async(string month_year, int id_seller)
        {
            var all = await get_payments_by_month_async(month_year);
            return all.Where(p => p.id_seller == id_seller);
        }

        private static Task<List<int>> get_pro_venta_type_ids_async(NinOSDbContext db_context)
        {
            return db_context.note_types
                .AsNoTracking()
                .Where(t => t.code == "MAR")
                .Select(t => t.id_note_type)
                .ToListAsync();
        }
    }
}
