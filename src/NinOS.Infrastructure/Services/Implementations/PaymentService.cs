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
        /// <summary>
        /// Excedente maximo permitido sobre el saldo de la nota.
        ///
        /// Se tolera una pequena diferencia por redondeo o por pagos que entran con un poco
        /// de más, pero mas alla de esto es un error de captura y no se deja pasar: si se
        /// aceptara sin limite, un monto mal tecleado dejaria la nota con saldo negativo
        /// (dinero que la empresa no debe) y eso nadie lo revisa despues.
        /// </summary>
        private const decimal EXCESO_MAXIMO_PERMITIDO = 3.00m;

        private readonly IServiceScopeFactory _scope_factory;

        public PaymentService(IServiceScopeFactory scope_factory)
        {
            if (scope_factory == null) throw new ArgumentNullException(nameof(scope_factory));
            _scope_factory = scope_factory;
        }

        /// <summary>
        /// Verifica que el abono no exceda el saldo de la nota mas alla del tolerado.
        ///
        /// El saldo se calcula con lo ya abonado, y al editar se excluye el pago que se esta
        /// cambiando para no contarlo dos veces.
        /// </summary>
        private static async Task verificar_exceso_permitido_async(
            NinOSDbContext db_context,
            delivery_note target_note,
            decimal amount_usd,
            int? id_payment_excluido = null)
        {
            var query = db_context.payments
                .Where(p => p.id_delivery_note == target_note.id_delivery_note && p.payment_type != "NOTA DE CREDITO");

            if (id_payment_excluido.HasValue)
            {
                query = query.Where(p => p.id_payment != id_payment_excluido.Value);
            }

            decimal[] abonos = await query.Select(p => p.amount_usd).ToArrayAsync();
            decimal saldo = target_note.adjusted_total_usd - abonos.Sum();

            decimal maximo_a_pagar = saldo + EXCESO_MAXIMO_PERMITIDO;
            if (amount_usd <= maximo_a_pagar) return;

            throw new InvalidOperationException(
                $"El monto excede el saldo de la nota {target_note.note_number}.\n\n" +
                $"Saldo pendiente: {saldo:N2}\n" +
                $"Maximo a pagar (saldo + {EXCESO_MAXIMO_PERMITIDO:N2}): {maximo_a_pagar:N2}\n" +
                $"Monto ingresado: {amount_usd:N2}\n\n" +
                $"El pago no fue registrado.");
        }

        private static Task lock_relation_for_update_async(NinOSDbContext db_context, int id_relacion)
        {
            return db_context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM relacion WHERE id_relacion = {id_relacion} FOR UPDATE");
        }

        private static async Task<(decimal amount, decimal paid)> get_relation_amount_and_paid_async(NinOSDbContext db_context, relacion relation)
        {
            decimal amount;

            if (relation.es_hueca)
            {
                amount = relation.saldo;
            }
            else
            {
                var relation_notes = await db_context.delivery_notes
                    .AsNoTracking()
                    .Where(n => n.id_relacion == relation.id_relacion)
                    .ToListAsync();

                amount = relation_notes
                    .Where(n => !string.Equals(n.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase))
                    .Sum(n => n.adjusted_total_usd);
            }

            var relation_payments = await db_context.payments
                .AsNoTracking()
                .Where(p => p.id_relacion == relation.id_relacion && p.amount_usd > 0)
                .ToListAsync();

            decimal paid = relation_payments.Sum(p => p.amount_usd);

            return (amount, paid);
        }

        private static void verificar_exceso_permitido_relacion(int id_relacion, decimal relation_amount, decimal paid_sin_este_abono, decimal amount_usd)
        {
            decimal saldo = RelationBalance.raw_balance(relation_amount, paid_sin_este_abono < 0 ? 0 : paid_sin_este_abono);
            decimal maximo_a_pagar = saldo + EXCESO_MAXIMO_PERMITIDO;
            if (amount_usd <= maximo_a_pagar) return;

            throw new InvalidOperationException(
                $"El monto excede el saldo de la relacion {id_relacion}.\n\n" +
                $"Saldo pendiente: {saldo:N2}\n" +
                $"Maximo a pagar (saldo + {EXCESO_MAXIMO_PERMITIDO:N2}): {maximo_a_pagar:N2}\n" +
                $"Monto ingresado: {amount_usd:N2}\n\n" +
                $"El abono no fue registrado.");
        }

        public async Task register_payment_async(payment new_payment, bool is_pro_venta = false)
        {
            if (new_payment == null) throw new ArgumentNullException(nameof(new_payment));

            using var scope = _scope_factory.CreateScope();
            var _db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            using var transaction = await _db_context.Database.BeginTransactionAsync();
            try
            {
                if (new_payment.amount_usd < 0)
                    throw new InvalidOperationException("El monto del abono no puede ser negativo.");

                delivery_note target_note = await _db_context.delivery_notes.FindAsync(new_payment.id_delivery_note);
                if (target_note == null) throw new InvalidOperationException("La nota de entrega no existe.");

                await lock_note_for_update_async(_db_context, target_note.id_delivery_note);
                await _db_context.Entry(target_note).ReloadAsync();

                if (target_note.status == "Anulada") throw new InvalidOperationException("No se puede abonar una nota anulada.");
                if (target_note.status == "Devuelta") throw new InvalidOperationException("No se puede abonar una nota devuelta en su totalidad.");
                if (target_note.status == "Pagada") throw new InvalidOperationException("No se puede abonar una nota ya pagada.");

                var mar_ids = await get_pro_venta_type_ids_async(_db_context);
                if (!is_pro_venta && target_note.note_type_id != null && mar_ids.Contains(target_note.note_type_id.Value))
                    throw new InvalidOperationException("Los pagos de notas Pro Venta se gestionan en el modulo Pro Venta.");
                if (await is_vesupronta_note_async(_db_context, target_note))
                    throw new InvalidOperationException("Los pagos de notas Vesupronta se gestionan en el modulo Vesupronta.");

                await verificar_exceso_permitido_async(_db_context, target_note, new_payment.amount_usd);

                new_payment.amount_bs = new_payment.amount_bs < 0 ? 0 : new_payment.amount_bs;
                if (string.IsNullOrEmpty(new_payment.bank_name)) new_payment.bank_name = "";
                if (string.IsNullOrEmpty(new_payment.observations)) new_payment.observations = "";
                await _db_context.payments.AddAsync(new_payment);

                if (!string.IsNullOrWhiteSpace(new_payment.observations))
                {
                    string obsTrim = new_payment.observations.Trim();
                    if (string.IsNullOrWhiteSpace(target_note.cxc_observations))
                    {
                        target_note.cxc_observations = obsTrim;
                    }
                    else if (!target_note.cxc_observations.Contains(obsTrim, StringComparison.OrdinalIgnoreCase))
                    {
                        target_note.cxc_observations = $"{target_note.cxc_observations} | {obsTrim}";
                    }
                }

                await _db_context.SaveChangesAsync();

                decimal total_paid_usd = await _db_context.payments
                    .Where(p => p.id_delivery_note == target_note.id_delivery_note && p.payment_type != "NOTA DE CREDITO")
                    .SumAsync(p => (decimal?)p.amount_usd) ?? 0m;

                await apply_settlement_async(_db_context, target_note, total_paid_usd, is_pro_venta);

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

            using var transaction = await _db_context.Database.BeginTransactionAsync();
            try
            {
                await lock_relation_for_update_async(_db_context, id_relacion);

                relacion? relation = await _db_context.relaciones.AsNoTracking().FirstOrDefaultAsync(r => r.id_relacion == id_relacion);
                if (relation == null) throw new InvalidOperationException("La relacion no existe.");

                var (relation_amount, relation_paid) = await get_relation_amount_and_paid_async(_db_context, relation);
                verificar_exceso_permitido_relacion(id_relacion, relation_amount, relation_paid, amount_usd);

                payment new_payment = new payment(
                    null, payment_date, amount_usd, 0, null,
                    "Efectivo", $"REL-{id_relacion}", "", observations ?? "", id_relacion);

                await _db_context.payments.AddAsync(new_payment);
                await _db_context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task update_relation_payment_async(int id_payment, decimal amount_usd, DateTime payment_date, string observations)
        {
            if (amount_usd <= 0) throw new InvalidOperationException("El monto debe ser mayor a 0.");

            using var scope = _scope_factory.CreateScope();
            var _db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            using var transaction = await _db_context.Database.BeginTransactionAsync();
            try
            {
                payment? existing = await _db_context.payments.FindAsync(id_payment);
                if (existing == null) throw new InvalidOperationException("El pago no existe.");
                if (existing.id_relacion == null) throw new InvalidOperationException("El pago no pertenece a una relacion.");

                int id_relacion = existing.id_relacion.Value;
                await lock_relation_for_update_async(_db_context, id_relacion);

                relacion? relation = await _db_context.relaciones.AsNoTracking().FirstOrDefaultAsync(r => r.id_relacion == id_relacion);
                if (relation == null) throw new InvalidOperationException("La relacion no existe.");

                var (relation_amount, relation_paid_before) = await get_relation_amount_and_paid_async(_db_context, relation);
                decimal relation_paid_sin_este = relation_paid_before - existing.amount_usd;
                verificar_exceso_permitido_relacion(id_relacion, relation_amount, relation_paid_sin_este, amount_usd);

                existing.amount_usd = amount_usd;
                existing.payment_date = payment_date;
                existing.observations = observations ?? "";
                existing.updated_at = DateTime.UtcNow;

                await _db_context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task update_payment_async(payment updated_payment, bool is_pro_venta = false)
        {
            if (updated_payment == null) throw new ArgumentNullException(nameof(updated_payment));

            using var scope = _scope_factory.CreateScope();
            var _db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            using var transaction = await _db_context.Database.BeginTransactionAsync();
            try
            {
                if (updated_payment.amount_usd < 0)
                    throw new InvalidOperationException("El monto del abono no puede ser negativo.");

                payment existing = await _db_context.payments.FindAsync(updated_payment.id_payment);
                if (existing == null) throw new InvalidOperationException("El pago no existe.");

                delivery_note target_note = await _db_context.delivery_notes.FindAsync(existing.id_delivery_note);
                if (target_note == null) throw new InvalidOperationException("La nota de entrega no existe.");

                await lock_note_for_update_async(_db_context, target_note.id_delivery_note);
                await _db_context.Entry(target_note).ReloadAsync();

                var mar_ids = await get_pro_venta_type_ids_async(_db_context);
                if (!is_pro_venta && target_note.note_type_id != null && mar_ids.Contains(target_note.note_type_id.Value))
                    throw new InvalidOperationException("Los pagos de notas Pro Venta se gestionan en el modulo Pro Venta.");
                if (await is_vesupronta_note_async(_db_context, target_note))
                    throw new InvalidOperationException("Los pagos de notas Vesupronta se gestionan en el modulo Vesupronta.");

                await verificar_exceso_permitido_async(_db_context, target_note, updated_payment.amount_usd, existing.id_payment);

                var commission_before_edit = await _db_context.commissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.id_delivery_note == target_note.id_delivery_note);

                if (commission_before_edit != null && commission_before_edit.is_paid)
                {
                    decimal other_payments_usd = await _db_context.payments
                        .AsNoTracking()
                        .Where(p => p.id_delivery_note == target_note.id_delivery_note
                                 && p.payment_type != "NOTA DE CREDITO"
                                 && p.id_payment != existing.id_payment)
                        .SumAsync(p => (decimal?)p.amount_usd) ?? 0m;
                    decimal would_be_total_paid = other_payments_usd + updated_payment.amount_usd;
                    bool would_remain_fully_paid = would_be_total_paid > 0 && (target_note.adjusted_total_usd - would_be_total_paid) <= 0.01m;

                    if (!would_remain_fully_paid)
                        throw new InvalidOperationException(
                            $"No se puede editar este pago: la comisión de la nota {target_note.note_number} ya fue pagada y este cambio la dejaría sin saldar.");
                }

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

                if (!string.IsNullOrWhiteSpace(updated_payment.observations))
                {
                    string obsTrim = updated_payment.observations.Trim();
                    if (string.IsNullOrWhiteSpace(target_note.cxc_observations))
                    {
                        target_note.cxc_observations = obsTrim;
                    }
                    else if (!target_note.cxc_observations.Contains(obsTrim, StringComparison.OrdinalIgnoreCase))
                    {
                        target_note.cxc_observations = $"{target_note.cxc_observations} | {obsTrim}";
                    }
                }

                await _db_context.SaveChangesAsync();

                decimal total_paid_usd = await _db_context.payments
                    .Where(p => p.id_delivery_note == existing.id_delivery_note && p.payment_type != "NOTA DE CREDITO")
                    .SumAsync(p => (decimal?)p.amount_usd) ?? 0m;

                await apply_settlement_async(_db_context, target_note, total_paid_usd, is_pro_venta);

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
                .Where(p => p.id_delivery_note == id_delivery_note && p.payment_type != "NOTA DE CREDITO")
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

        public Task<IEnumerable<payment_dto>> get_payments_by_month_async(string month_year)
        {
            if (string.IsNullOrWhiteSpace(month_year) || string.Equals(month_year, "Todos", StringComparison.OrdinalIgnoreCase))
            {
                return get_payments_by_period_async(DateTime.UtcNow.Year, null);
            }

            if (DateTime.TryParseExact(month_year, "MMMM yyyy", new System.Globalization.CultureInfo("es-VE"), System.Globalization.DateTimeStyles.None, out var target_date))
            {
                return get_payments_by_period_async(target_date.Year, target_date.Month);
            }

            return Task.FromResult(Enumerable.Empty<payment_dto>());
        }

        public async Task<IEnumerable<payment_dto>> get_payments_by_period_async(int year, int? month = null)
        {
            using var scope = _scope_factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            double tz_offset = AppTimeZone.offset_hours;

            var query = db.payments
                .AsNoTracking()
                .Where(p => p.payment_date.AddHours(tz_offset).Year == year
                         && p.payment_type != "NOTA DE CREDITO"
                         && p.id_delivery_note != null);

            if (month.HasValue)
            {
                query = query.Where(p => p.payment_date.AddHours(tz_offset).Month == month.Value);
            }

            var payments = await query
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
                .Include(c => c.zona)
                .Where(c => customer_ids.Contains(c.id_customer))
                .ToDictionaryAsync(c => c.id_customer, c => c.business_name);

            // La zona del cliente se carga aparte para que el reporte de pagos pueda agrupar por
            // zona igual que el de ventas.
            var zonas_por_cliente = await db.customers
                .AsNoTracking()
                .Include(c => c.zona)
                .Where(c => customer_ids.Contains(c.id_customer))
                .ToDictionaryAsync(
                    c => c.id_customer,
                    c => c.zona == null || string.IsNullOrWhiteSpace(c.zona.name) ? "Sin zona" : c.zona.name.Trim());

            return payments.Select(p =>
            {
                delivery_note? note = p.id_delivery_note != null && notes.TryGetValue(p.id_delivery_note.Value, out var n) ? n : null;
                string seller_name = note != null && sellers.TryGetValue(note.id_seller, out var sn) ? sn : string.Empty;
                string customer_name = note != null && customers.TryGetValue(note.id_customer, out var cn) ? cn : string.Empty;
                string zone_name = note != null && zonas_por_cliente.TryGetValue(note.id_customer, out var zn) ? zn : "Sin zona";
                return new payment_dto
                {
                    id_payment = p.id_payment,
                    id_delivery_note = p.id_delivery_note ?? 0,
                    id_relacion = p.id_relacion,
                    note_number = note?.note_number ?? string.Empty,
                    customer_name = customer_name,
                    seller_name = seller_name,
                    id_seller = note?.id_seller ?? 0,
                    zone_name = zone_name,
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

        internal static Task lock_note_for_update_async(NinOSDbContext db_context, int id_delivery_note)
        {
            return db_context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM delivery_note WHERE id_delivery_note = {id_delivery_note} FOR UPDATE");
        }

        internal static async Task apply_settlement_async(NinOSDbContext db_context, delivery_note target_note, decimal total_paid_usd, bool is_pro_venta, bool is_fully_returned = false, bool allow_transition_from_returned = false)
        {
            if (target_note.status == "Devuelta" && !is_fully_returned && !allow_transition_from_returned) return;

            var existing_commission = await db_context.commissions
                .FirstOrDefaultAsync(c => c.id_delivery_note == target_note.id_delivery_note);

            bool eligible_for_commission = false;
            decimal commission_percentage = 0m;

            bool is_vesupronta = await is_vesupronta_note_async(db_context, target_note);

            if (!is_pro_venta && !is_vesupronta)
            {
                var note_seller = await db_context.sellers
                    .AsNoTracking()
                    .Include(s => s.seller_zones)
                        .ThenInclude(sz => sz.zona)
                    .FirstOrDefaultAsync(s => s.id_seller == target_note.id_seller);

                if (note_seller != null && note_seller.can_receive_commissions && !note_seller.is_pro_venta_only && note_seller.commission_percentage > 0)
                {
                    eligible_for_commission = true;
                    commission_percentage = note_seller.commission_percentage;
                }
            }

            var result = NoteSettlement.evaluate(
                target_note.adjusted_total_usd,
                total_paid_usd,
                existing_commission?.is_paid,
                eligible_for_commission,
                commission_percentage,
                is_fully_returned);

            target_note.status = result.status;

            switch (result.commission_action)
            {
                case CommissionAction.Create:
                    var new_commission = new commission(
                        target_note.id_seller,
                        target_note.id_delivery_note,
                        commission_percentage,
                        result.commission_amount_usd ?? 0m,
                        false,
                        null);
                    await db_context.commissions.AddAsync(new_commission);
                    break;
                case CommissionAction.Remove:
                    if (existing_commission != null) db_context.commissions.Remove(existing_commission);
                    break;
                case CommissionAction.UpdateAmount:
                    if (existing_commission != null) existing_commission.amount_usd = result.commission_amount_usd ?? existing_commission.amount_usd;
                    break;
            }
        }

        private static async Task<bool> is_vesupronta_note_async(NinOSDbContext db_context, delivery_note target_note)
        {
            if (target_note.note_type_id == null) return false;

            var vesupronta_codes = BusinessUnitRules.codes_of_unit(business_unit.Vesupronta);
            var vesupronta_type_ids = await db_context.note_types
                .AsNoTracking()
                .Where(t => vesupronta_codes.Contains(t.code))
                .Select(t => t.id_note_type)
                .ToListAsync();

            return vesupronta_type_ids.Contains(target_note.note_type_id.Value);
        }

        private static Task<List<int>> get_pro_venta_type_ids_async(NinOSDbContext db_context)
        {
            return db_context.note_types
                .AsNoTracking()
                .Where(t => NoteTypeCodes.pro_venta_codes.Contains(t.code))
                .Select(t => t.id_note_type)
                .ToListAsync();
        }
    }
}
