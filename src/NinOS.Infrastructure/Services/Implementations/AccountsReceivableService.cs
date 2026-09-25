using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class AccountsReceivableService : IAccountsReceivableService
    {
        private readonly IServiceScopeFactory _scope_factory;

        public AccountsReceivableService(IServiceScopeFactory scope_factory)
        {
            if (scope_factory == null) throw new ArgumentNullException(nameof(scope_factory));
            _scope_factory = scope_factory;
        }

        public async Task<IEnumerable<string>> get_pending_months_async()
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                
                var mar_ids = await get_pro_venta_type_ids_async(db_context);

                var pending_notes = await db_context.delivery_notes
                    .AsNoTracking()
                    .Where(n => n.status == "Pendiente"
                             && (n.note_type_id == null || !mar_ids.Contains(n.note_type_id.Value)))
                    .Select(n => new { n.creation_date.Year, n.creation_date.Month })
                    .Distinct()
                    .OrderByDescending(n => n.Year)
                    .ThenByDescending(n => n.Month)
                    .ToListAsync();

                return pending_notes
                    .Select(n => new DateTime(n.Year, n.Month, 1).ToString("MMMM yyyy", new CultureInfo("es-VE")))
                    .ToList();
            }
        }

        public async Task<IEnumerable<string>> get_all_months_async()
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                var mar_ids = await get_pro_venta_type_ids_async(db_context);

                var all_notes = await db_context.delivery_notes
                    .AsNoTracking()
                    .Where(n => n.note_type_id == null || !mar_ids.Contains(n.note_type_id.Value))
                    .Select(n => new { n.creation_date.Year, n.creation_date.Month })
                    .Distinct()
                    .OrderBy(n => n.Year)
                    .ThenBy(n => n.Month)
                    .ToListAsync();

                return all_notes
                    .Select(n => new DateTime(n.Year, n.Month, 1).ToString("MMMM yyyy", new CultureInfo("es-VE")))
                    .ToList();
            }
        }

        public async Task<IEnumerable<accounts_receivable_dto>> get_receivables_by_month_async(string month_year)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                
                var target_date = DateTime.ParseExact(month_year, "MMMM yyyy", new System.Globalization.CultureInfo("es-VE"));
                
                var mar_ids = await get_pro_venta_type_ids_async(db_context);

                var notes = await db_context.delivery_notes
                    .AsNoTracking()
                    .Where(dn => dn.status == "Pendiente"
                              && (dn.note_type_id == null || !mar_ids.Contains(dn.note_type_id.Value))
                              && dn.creation_date.Year == target_date.Year
                              && dn.creation_date.Month == target_date.Month)
                    .ToListAsync();

                if (notes.Count == 0)
                    return Enumerable.Empty<accounts_receivable_dto>();

                var note_ids = notes.Select(n => n.id_delivery_note).ToList();

                var seller_ids = notes.Select(n => n.id_seller).Distinct().ToList();
                var customer_ids = notes.Select(n => n.id_customer).Distinct().ToList();

                var sellers = await db_context.sellers
                    .AsNoTracking()
                    .Where(s => seller_ids.Contains(s.id_seller))
                    .ToDictionaryAsync(s => s.id_seller, s => s.full_name);
                var customers = await db_context.customers
                    .AsNoTracking()
                    .Where(c => customer_ids.Contains(c.id_customer))
                    .ToDictionaryAsync(c => c.id_customer, c => c.business_name);

                var payment_totals = await db_context.payments
                    .AsNoTracking()
                    .Where(p => p.id_delivery_note != null && note_ids.Contains(p.id_delivery_note.Value))
                    .GroupBy(p => p.id_delivery_note!.Value)
                    .Select(g => new { Id = g.Key, Total = g.Sum(p => p.amount_usd) })
                    .ToDictionaryAsync(x => x.Id, x => x.Total);

                var result = new List<accounts_receivable_dto>();
                foreach (var dn in notes)
                {
                    decimal paid = payment_totals.TryGetValue(dn.id_delivery_note, out var total) ? total : 0;
                    sellers.TryGetValue(dn.id_seller, out string? seller_name);
                    customers.TryGetValue(dn.id_customer, out string? customer_name);
                    result.Add(new accounts_receivable_dto
                    {
                        id_delivery_note = dn.id_delivery_note,
                        note_number = dn.note_number,
                        customer_name = customer_name ?? string.Empty,
                        id_seller = dn.id_seller,
                        seller_name = seller_name ?? string.Empty,
                        creation_date = dn.creation_date,
                        dispatch_date = dn.dispatch_date,
                        total_amount_usd = dn.adjusted_total_usd,
                        status = dn.status,
                        paid_amount_usd = paid,
                        balance_due_usd = dn.adjusted_total_usd - paid,
                        cxc_observations = dn.cxc_observations ?? string.Empty,
                        sales_observations = dn.sales_observations ?? string.Empty
                    });
                }

                return result;
            }
        }

        public async Task<IEnumerable<accounts_receivable_dto>> get_receivables_by_month_and_seller_async(string month_year, int id_seller)
        {
            var all = await get_receivables_by_month_async(month_year);
            return all.Where(n => n.id_seller == id_seller);
        }

        public async Task<IEnumerable<accounts_receivable_dto>> get_all_by_month_async(string month_year)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                
                var target_date = DateTime.ParseExact(month_year, "MMMM yyyy", new CultureInfo("es-VE"));
                
                var mar_ids = await get_pro_venta_type_ids_async(db_context);

                var notes = await db_context.delivery_notes
                    .AsNoTracking()
                    .Where(dn => (dn.note_type_id == null || !mar_ids.Contains(dn.note_type_id.Value))
                              && dn.creation_date.Year == target_date.Year
                              && dn.creation_date.Month == target_date.Month)
                    .ToListAsync();

                if (notes.Count == 0)
                    return Enumerable.Empty<accounts_receivable_dto>();

                var note_ids = notes.Select(n => n.id_delivery_note).ToList();
                var seller_ids = notes.Select(n => n.id_seller).Distinct().ToList();
                var customer_ids = notes.Select(n => n.id_customer).Distinct().ToList();

                var sellers = await db_context.sellers
                    .AsNoTracking()
                    .Where(s => seller_ids.Contains(s.id_seller))
                    .ToDictionaryAsync(s => s.id_seller, s => s.full_name);
                var customers = await db_context.customers
                    .AsNoTracking()
                    .Where(c => customer_ids.Contains(c.id_customer))
                    .ToDictionaryAsync(c => c.id_customer, c => c.business_name);

                var payment_totals = await db_context.payments
                    .AsNoTracking()
                    .Where(p => p.id_delivery_note != null && note_ids.Contains(p.id_delivery_note.Value))
                    .GroupBy(p => p.id_delivery_note!.Value)
                    .Select(g => new { Id = g.Key, Total = g.Sum(p => p.amount_usd) })
                    .ToDictionaryAsync(x => x.Id, x => x.Total);

                var note_ids_with_payments = payment_totals.Keys.ToList();
                var last_payments = await db_context.payments
                    .AsNoTracking()
                    .Where(p => p.id_delivery_note != null && note_ids_with_payments.Contains(p.id_delivery_note.Value))
                    .GroupBy(p => p.id_delivery_note!.Value)
                    .Select(g => new { Id = g.Key, MaxDate = g.Max(p => p.payment_date) })
                    .ToDictionaryAsync(x => x.Id, x => x.MaxDate);

                var note_payments = await db_context.payments
                    .AsNoTracking()
                    .Where(p => p.id_delivery_note != null && note_ids.Contains(p.id_delivery_note.Value))
                    .OrderBy(p => p.payment_date)
                    .GroupBy(p => p.id_delivery_note!.Value)
                    .ToDictionaryAsync(g => g.Key, g => g.ToList());

                var gross_totals = new Dictionary<int, decimal>();
                var note_detail_map = await db_context.note_details
                    .AsNoTracking()
                    .Where(d => note_ids.Contains(d.id_delivery_note))
                    .GroupBy(d => d.id_delivery_note)
                    .ToDictionaryAsync(g => g.Key, g => g.Sum(d => d.subtotal_usd));

                var result = new List<accounts_receivable_dto>();
                foreach (var dn in notes)
                {
                    decimal paid = payment_totals.TryGetValue(dn.id_delivery_note, out var total) ? total : 0;
                    bool is_promo_note = dn.promo_discount_percentage != null && dn.promo_discount_percentage > 0;
                    decimal gross = is_promo_note
                        ? dn.adjusted_total_usd
                        : (note_detail_map.TryGetValue(dn.id_delivery_note, out var gt) ? gt : dn.adjusted_total_usd);
                    decimal discount = is_promo_note ? 0 : gross - dn.adjusted_total_usd;

                    DateTime? lastDate = last_payments.TryGetValue(dn.id_delivery_note, out var ld) ? ld : null;

                    string paymentMethod = "";
                    string bankText = "";
                    if (note_payments.TryGetValue(dn.id_delivery_note, out var pList) && pList.Count > 0)
                    {
                        paymentMethod = string.Join("/", pList.Select(p => p.reference_number));
                        bankText = string.Join("/", pList.Select(p => p.bank_name).Where(b => !string.IsNullOrEmpty(b)).Distinct());
                    }

                    sellers.TryGetValue(dn.id_seller, out string? seller_name);
                    customers.TryGetValue(dn.id_customer, out string? customer_name);
                    result.Add(new accounts_receivable_dto
                    {
                        id_delivery_note = dn.id_delivery_note,
                        note_number = dn.note_number,
                        customer_name = customer_name ?? string.Empty,
                        id_seller = dn.id_seller,
                        seller_name = seller_name ?? string.Empty,
                        creation_date = dn.creation_date,
                        dispatch_date = dn.dispatch_date,
                        total_amount_usd = dn.adjusted_total_usd,
                        gross_total_usd = gross,
                        discount_amount = discount,
                        discount_percentage = dn.discount_percentage,
                        volume_discount_percentage = dn.volume_discount_percentage,
                        status = dn.status,
                        paid_amount_usd = paid,
                        balance_due_usd = dn.adjusted_total_usd - paid,
                        last_payment_date = lastDate,
                        payment_method_text = paymentMethod,
                        bank_name_text = bankText,
                        cxc_observations = dn.cxc_observations ?? string.Empty,
                        sales_observations = dn.sales_observations ?? string.Empty
                    });
                }

                return result;
            }
        }

        public async Task<IEnumerable<accounts_receivable_dto>> get_all_by_month_and_seller_async(string month_year, int id_seller)
        {
            var all = await get_all_by_month_async(month_year);
            return all.Where(n => n.id_seller == id_seller);
        }

        public async Task<IEnumerable<accounts_receivable_dto>> get_all_notes_async()
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                var mar_ids = await get_pro_venta_type_ids_async(db_context);

                var notes = await db_context.delivery_notes
                    .AsNoTracking()
                    .Where(n => n.note_type_id == null || !mar_ids.Contains(n.note_type_id.Value))
                    .ToListAsync();

                if (notes.Count == 0)
                    return Enumerable.Empty<accounts_receivable_dto>();

                var note_ids = notes.Select(n => n.id_delivery_note).ToList();
                var seller_ids = notes.Select(n => n.id_seller).Distinct().ToList();
                var customer_ids = notes.Select(n => n.id_customer).Distinct().ToList();

                var sellers = await db_context.sellers
                    .AsNoTracking()
                    .Where(s => seller_ids.Contains(s.id_seller))
                    .ToDictionaryAsync(s => s.id_seller, s => s.full_name);
                var customers = await db_context.customers
                    .AsNoTracking()
                    .Where(c => customer_ids.Contains(c.id_customer))
                    .ToDictionaryAsync(c => c.id_customer, c => c.business_name);

                var payment_totals = await db_context.payments
                    .AsNoTracking()
                    .Where(p => p.id_delivery_note != null && note_ids.Contains(p.id_delivery_note.Value))
                    .GroupBy(p => p.id_delivery_note!.Value)
                    .Select(g => new { Id = g.Key, Total = g.Sum(p => p.amount_usd) })
                    .ToDictionaryAsync(x => x.Id, x => x.Total);

                var note_ids_with_payments = payment_totals.Keys.ToList();
                var last_payments = await db_context.payments
                    .AsNoTracking()
                    .Where(p => p.id_delivery_note != null && note_ids_with_payments.Contains(p.id_delivery_note.Value))
                    .GroupBy(p => p.id_delivery_note!.Value)
                    .Select(g => new { Id = g.Key, MaxDate = g.Max(p => p.payment_date) })
                    .ToDictionaryAsync(x => x.Id, x => x.MaxDate);

                var note_payments = await db_context.payments
                    .AsNoTracking()
                    .Where(p => p.id_delivery_note != null && note_ids.Contains(p.id_delivery_note.Value))
                    .OrderBy(p => p.payment_date)
                    .GroupBy(p => p.id_delivery_note!.Value)
                    .ToDictionaryAsync(g => g.Key, g => g.ToList());

                var note_detail_map = await db_context.note_details
                    .AsNoTracking()
                    .Where(d => note_ids.Contains(d.id_delivery_note))
                    .GroupBy(d => d.id_delivery_note)
                    .ToDictionaryAsync(g => g.Key, g => g.Sum(d => d.subtotal_usd));

                var result = new List<accounts_receivable_dto>();
                foreach (var dn in notes)
                {
                    decimal paid = payment_totals.TryGetValue(dn.id_delivery_note, out var total) ? total : 0;
                    bool is_promo_note = dn.promo_discount_percentage != null && dn.promo_discount_percentage > 0;
                    decimal gross = is_promo_note
                        ? dn.adjusted_total_usd
                        : (note_detail_map.TryGetValue(dn.id_delivery_note, out var gt) ? gt : dn.adjusted_total_usd);
                    decimal discount = is_promo_note ? 0 : gross - dn.adjusted_total_usd;

                    DateTime? lastDate = last_payments.TryGetValue(dn.id_delivery_note, out var ld) ? ld : null;

                    string paymentMethod = "";
                    string bankText = "";
                    if (note_payments.TryGetValue(dn.id_delivery_note, out var pList) && pList.Count > 0)
                    {
                        paymentMethod = string.Join("/", pList.Select(p => p.reference_number));
                        bankText = string.Join("/", pList.Select(p => p.bank_name).Where(b => !string.IsNullOrEmpty(b)).Distinct());
                    }

                    sellers.TryGetValue(dn.id_seller, out string? seller_name);
                    customers.TryGetValue(dn.id_customer, out string? customer_name);
                    result.Add(new accounts_receivable_dto
                    {
                        id_delivery_note = dn.id_delivery_note,
                        note_number = dn.note_number,
                        customer_name = customer_name ?? string.Empty,
                        id_seller = dn.id_seller,
                        seller_name = seller_name ?? string.Empty,
                        creation_date = dn.creation_date,
                        dispatch_date = dn.dispatch_date,
                        total_amount_usd = dn.adjusted_total_usd,
                        gross_total_usd = gross,
                        discount_amount = discount,
                        discount_percentage = dn.discount_percentage,
                        volume_discount_percentage = dn.volume_discount_percentage,
                        status = dn.status,
                        paid_amount_usd = paid,
                        balance_due_usd = dn.adjusted_total_usd - paid,
                        last_payment_date = lastDate,
                        payment_method_text = paymentMethod,
                        bank_name_text = bankText,
                        cxc_observations = dn.cxc_observations ?? string.Empty,
                        sales_observations = dn.sales_observations ?? string.Empty
                    });
                }

                return result;
            }
        }

        public async Task<accounts_receivable_dto?> search_note_by_number_async(string note_number)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                var mar_ids = await get_pro_venta_type_ids_async(db_context);

                var dn = await db_context.delivery_notes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.note_number == note_number
                                           && (n.note_type_id == null || !mar_ids.Contains(n.note_type_id.Value)));

                if (dn == null) return null;

                var customer = await db_context.customers.AsNoTracking().FirstOrDefaultAsync(c => c.id_customer == dn.id_customer);
                var seller = await db_context.sellers.AsNoTracking().FirstOrDefaultAsync(s => s.id_seller == dn.id_seller);

                decimal paid = await db_context.payments
                    .AsNoTracking()
                    .Where(p => p.id_delivery_note == dn.id_delivery_note)
                    .SumAsync(p => (decimal?)p.amount_usd) ?? 0;

                var detail_sum = await db_context.note_details
                    .AsNoTracking()
                    .Where(d => d.id_delivery_note == dn.id_delivery_note)
                    .SumAsync(d => (decimal?)d.subtotal_usd) ?? dn.total_amount_usd;

                bool is_promo_note = dn.promo_discount_percentage != null && dn.promo_discount_percentage > 0;

                return new accounts_receivable_dto
                {
                    id_delivery_note = dn.id_delivery_note,
                    note_number = dn.note_number,
                    customer_name = customer?.business_name ?? string.Empty,
                    id_seller = dn.id_seller,
                    seller_name = seller?.full_name ?? string.Empty,
                    creation_date = dn.creation_date,
                    total_amount_usd = dn.adjusted_total_usd,
                    gross_total_usd = is_promo_note ? dn.adjusted_total_usd : detail_sum,
                    discount_amount = is_promo_note ? 0 : detail_sum - dn.adjusted_total_usd,
                    discount_percentage = dn.discount_percentage,
                    volume_discount_percentage = dn.volume_discount_percentage,
                    status = dn.status,
                    paid_amount_usd = paid,
                    balance_due_usd = dn.adjusted_total_usd - paid,
                    cxc_observations = dn.cxc_observations ?? string.Empty,
                    sales_observations = dn.sales_observations ?? string.Empty
                };
            }
        }

        public async Task update_note_cxc_observations_async(int id_delivery_note, string? observations)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                var delivery_note = await db_context.delivery_notes
                    .FirstOrDefaultAsync(n => n.id_delivery_note == id_delivery_note);
                if (delivery_note == null) throw new ArgumentException($"Nota de entrega con ID {id_delivery_note} no encontrada.");

                delivery_note.cxc_observations = string.IsNullOrWhiteSpace(observations) ? null : observations.Trim();
                await db_context.SaveChangesAsync();
            }
        }

        public async Task update_note_dispatch_date_async(int id_delivery_note, DateTime? dispatch_date)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                var delivery_note = await db_context.delivery_notes
                    .FirstOrDefaultAsync(n => n.id_delivery_note == id_delivery_note);
                if (delivery_note == null) throw new ArgumentException($"Nota de entrega con ID {id_delivery_note} no encontrada.");

                delivery_note.dispatch_date = dispatch_date?.Kind == DateTimeKind.Utc
                    ? dispatch_date
                    : dispatch_date?.ToUniversalTime();
                await db_context.SaveChangesAsync();
            }
        }

        public async Task update_note_sales_observations_async(int id_delivery_note, string? observations)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                var delivery_note = await db_context.delivery_notes
                    .FirstOrDefaultAsync(n => n.id_delivery_note == id_delivery_note);
                if (delivery_note == null) throw new ArgumentException($"Nota de entrega con ID {id_delivery_note} no encontrada.");

                delivery_note.sales_observations = string.IsNullOrWhiteSpace(observations) ? null : observations.Trim();
                await db_context.SaveChangesAsync();
            }
        }

        public async Task update_note_total_async(int id_delivery_note, decimal adjusted_total_usd, decimal? discount_percentage = null, decimal? volume_discount_percentage = null)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                using var transaction = await db_context.Database.BeginTransactionAsync();

                try
                {
                    var delivery_note = await db_context.delivery_notes
                        .FirstOrDefaultAsync(n => n.id_delivery_note == id_delivery_note);
                    if (delivery_note == null) throw new ArgumentException($"Nota de entrega con ID {id_delivery_note} no encontrada.");
                    if (delivery_note.status == "Anulada") throw new InvalidOperationException("No se puede editar una nota anulada.");

                    var mar_ids = await get_pro_venta_type_ids_async(db_context);
                    bool is_pro_venta = delivery_note.note_type_id != null && mar_ids.Contains(delivery_note.note_type_id.Value);
                    bool is_promo_note = delivery_note.promo_discount_percentage != null && delivery_note.promo_discount_percentage > 0;

                    decimal gross = is_promo_note
                        ? await db_context.note_details
                            .AsNoTracking()
                            .Where(d => d.id_delivery_note == id_delivery_note)
                            .SumAsync(d => (decimal?)(d.quantity * d.unit_price_usd)) ?? delivery_note.total_amount_usd
                        : await db_context.note_details
                            .AsNoTracking()
                            .Where(d => d.id_delivery_note == id_delivery_note)
                            .SumAsync(d => (decimal?)d.subtotal_usd) ?? delivery_note.adjusted_total_usd;

                    decimal adjusted = adjusted_total_usd < 0 ? 0 : adjusted_total_usd;
                    if (adjusted > gross) adjusted = gross;

                    delivery_note.adjusted_total_usd = adjusted;

                    // CxC trabaja un único DCTO (discount_percentage). El volumen de trabajo se limpia;
                    // el detalle original queda congelado en original_discount_percentage/original_volume_discount_percentage.
                    delivery_note.discount_percentage = discount_percentage;
                    delivery_note.volume_discount_percentage = volume_discount_percentage;

                    decimal total_paid = await db_context.payments
                        .AsNoTracking()
                        .Where(p => p.id_delivery_note == id_delivery_note)
                        .SumAsync(p => (decimal?)p.amount_usd) ?? 0;

                    var existing_commission = await db_context.commissions
                        .FirstOrDefaultAsync(c => c.id_delivery_note == id_delivery_note);

                    if (total_paid >= adjusted)
                    {
                        if (delivery_note.status != "Pagada")
                        {
                            delivery_note.status = "Pagada";

                            if (existing_commission == null && !is_pro_venta)
                            {
                                existing_commission = new commission(
                                    delivery_note.id_seller,
                                    delivery_note.id_delivery_note,
                                    0.10m,
                                    Math.Round(adjusted * 0.10m, 2),
                                    false,
                                    null);
                                await db_context.commissions.AddAsync(existing_commission);
                            }
                        }
                    }
                    else
                    {
                        if (delivery_note.status == "Pagada") delivery_note.status = "Pendiente";
                    }

                    if (existing_commission != null)
                    {
                        existing_commission.amount_usd = Math.Round(adjusted * existing_commission.commission_percentage, 2);
                    }

                    await db_context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<IEnumerable<seller>> get_sellers_async()
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.sellers.AsNoTracking().ToListAsync();
            }
        }

        public async Task annul_delivery_note_async(int id_delivery_note)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                using var transaction = await db_context.Database.BeginTransactionAsync();
                
                try
                {
                    var delivery_note = await db_context.delivery_notes
                        .FirstOrDefaultAsync(n => n.id_delivery_note == id_delivery_note);

                    if (delivery_note == null) throw new ArgumentException($"Nota de entrega con ID {id_delivery_note} no encontrada.");
                    if (delivery_note.status == "Anulada") throw new InvalidOperationException("La nota ya esta anulada.");

                    var has_payments = await db_context.payments
                        .AnyAsync(p => p.id_delivery_note == id_delivery_note && p.amount_usd > 0);

                    if (has_payments)
                        throw new InvalidOperationException("No se puede anular una nota que ya tiene abonos. Elimine primero los pagos registrados.");

                    var details = await db_context.note_details
                        .Where(d => d.id_delivery_note == id_delivery_note)
                        .ToListAsync();

                    var product_ids = new HashSet<int>();
                    var promotion_ids = new HashSet<int>();

                    foreach (var detail in details)
                    {
                        if (detail.id_product != null) product_ids.Add(detail.id_product.Value);
                        if (detail.id_promotion != null) promotion_ids.Add(detail.id_promotion.Value);
                    }

                    var products = await db_context.products
                        .Where(p => product_ids.Contains(p.id_product))
                        .ToDictionaryAsync(p => p.id_product);

                    var promotions = await db_context.promotions
                        .Include(p => p.items)
                        .Where(p => promotion_ids.Contains(p.id_promotion))
                        .ToDictionaryAsync(p => p.id_promotion);

                    foreach (var detail in details)
                    {
                        if (detail.id_product != null && products.TryGetValue(detail.id_product.Value, out var product))
                        {
                            product.stock_quantity += detail.quantity;
                        }
                        else if (detail.id_promotion != null && promotions.TryGetValue(detail.id_promotion.Value, out var promotion))
                        {
                            if (promotion.items != null)
                            {
                                foreach (var promo_item in promotion.items)
                                {
                                    if (products.TryGetValue(promo_item.id_product, out var promo_product))
                                    {
                                        promo_product.stock_quantity += detail.quantity * promo_item.quantity_required;
                                    }
                                }
                            }
                        }
                    }

                    delivery_note.status = "Anulada";
                    await db_context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<note_print_dto> get_printable_note_async(int id_delivery_note)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                var note = await db_context.delivery_notes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.id_delivery_note == id_delivery_note);
                if (note == null) throw new ArgumentException("Nota no encontrada.");

                var customer = await db_context.customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.id_customer == note.id_customer);
                var seller = await db_context.sellers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.id_seller == note.id_seller);

                note_type? note_type = null;
                if (note.note_type_id != null)
                {
                    note_type = await db_context.note_types
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.id_note_type == note.note_type_id);
                }

                bool is_promo = note_type != null && string.Equals(note_type.calculation_type, "promo", StringComparison.OrdinalIgnoreCase);

                var raw_details = await db_context.note_details
                    .AsNoTracking()
                    .Where(d => d.id_delivery_note == note.id_delivery_note)
                    .ToListAsync();

                var product_ids = raw_details.Where(d => d.id_product != null).Select(d => d.id_product!.Value).Distinct().ToList();
                var promo_ids = raw_details.Where(d => d.id_promotion != null).Select(d => d.id_promotion!.Value).Distinct().ToList();

                var products = await db_context.products
                    .AsNoTracking()
                    .Where(p => product_ids.Contains(p.id_product))
                    .ToDictionaryAsync(p => p.id_product);
                var promotions = await db_context.promotions
                    .AsNoTracking()
                    .Where(p => promo_ids.Contains(p.id_promotion))
                    .ToDictionaryAsync(p => p.id_promotion);

                decimal paid = await db_context.payments
                    .AsNoTracking()
                    .Where(p => p.id_delivery_note == note.id_delivery_note)
                    .SumAsync(p => (decimal?)p.amount_usd) ?? 0;

                var details = new List<note_detail_print_dto>();
                foreach (var d in raw_details)
                {
                    string code = string.Empty;
                    string name = string.Empty;

                    if (d.id_product != null && products.TryGetValue(d.id_product.Value, out var prod))
                    {
                        code = prod.product_code;
                        name = prod.name;
                    }
                    else if (d.id_promotion != null && promotions.TryGetValue(d.id_promotion.Value, out var promo))
                    {
                        code = promo.promotion_code;
                        name = promo.name;
                    }

                    decimal net_unit = d.quantity > 0 ? d.subtotal_usd / d.quantity : 0;

                    details.Add(new note_detail_print_dto
                    {
                        code = code,
                        name = name,
                        quantity = d.quantity,
                        unit_price_usd = d.unit_price_usd,
                        discount_usd = is_promo ? (d.unit_price_usd - net_unit) : 0,
                        promo_price_usd = net_unit,
                        subtotal_usd = d.subtotal_usd
                    });
                }

                decimal gross = details.Sum(d => d.subtotal_usd);
                if (is_promo) gross = details.Sum(d => d.quantity * d.unit_price_usd);
                decimal? promo_pct = is_promo ? note.promo_discount_percentage : null;
                decimal promo_amt = is_promo && promo_pct != null ? (gross * promo_pct.Value / 100m) : 0;
                decimal after_promo = gross - promo_amt;

                // El PDF/vista previa usan los valores originales congelados, no los editados en CxC.
                decimal discount_pct = note.original_discount_percentage ?? note.discount_percentage ?? 0;
                decimal discount_amt = discount_pct > 0 ? (after_promo * discount_pct / 100m) : 0;
                decimal after_discount = after_promo - discount_amt;

                decimal? volume_pct = note.original_volume_discount_percentage ?? note.volume_discount_percentage;
                decimal volume_amt = volume_pct != null ? (after_discount * volume_pct.Value / 100m) : 0;
                decimal total_calc = after_discount - volume_amt;

                return new note_print_dto
                {
                    id_delivery_note = note.id_delivery_note,
                    note_number = note.note_number,
                    company_name = "DEFILE_REMBRANT_OLEOS_FLYING_BIOLINE",
                    promo_banner_text = is_promo ? (string.IsNullOrWhiteSpace(note.promo_banner) ? "PROMOCION OLEOS MAYO Y JUNIO" : note.promo_banner) : string.Empty,
                    header_title = string.IsNullOrWhiteSpace(note_type?.header_title) ? "DEFILE_REMBRANT_OLEOS_FLYING_BIOLINE" : note_type.header_title,
                    document_label = note_type != null && note_type.code == "MAR" ? "NOTA DE DESPACHO" : "NOTA DE ENTREGA",
                    is_pro_venta = note_type != null && note_type.code == "MAR",
                    is_promo = is_promo,
                    accent_color = note_type != null && note_type.code == "MAR" ? "#1565C0" : "#1B3A2D",
                    accent_soft_color = note_type != null && note_type.code == "MAR" ? "#E3F2FD" : "#F0F4EC",
                    promo_discount_percentage = is_promo ? promo_pct : null,
                    promo_discount_amount = promo_amt,
                    volume_discount_percentage = volume_pct ?? 0,
                    volume_discount_amount = volume_amt,
                    discounted_total_usd = after_discount,
                    creation_date = note.creation_date,
                    due_date = note.creation_date.AddDays(15),
                    status = note.status,
                    gross_total_usd = gross,
                    discount_percentage = discount_pct,
                    discount_amount = discount_amt,
                    total_amount_usd = total_calc,
                    paid_amount_usd = paid,
                    balance_due_usd = note.total_amount_usd - paid,
                    seller_name = seller?.full_name ?? string.Empty,
                    customer_code = customer?.customer_code ?? string.Empty,
                    customer_business_name = customer?.business_name ?? string.Empty,
                    customer_rif = customer?.rif ?? string.Empty,
                    customer_phone = customer?.phone_number ?? string.Empty,
                    customer_contact = customer?.contact_name ?? string.Empty,
                    customer_delivery_address = customer?.effective_delivery_address ?? string.Empty,
                    fiscal_address = customer?.fiscal_address ?? string.Empty,
                    conditions_text = is_promo ? "DIAS CREDITO 21 DIAS SIN DESCUENTO" : "DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO",
                    discount_conditions_text = is_promo ? string.Empty : "Descuento 10% SOLO\nCONTADO",
                    details = details
                };
            }
        }

        private static Task<List<int>> get_pro_venta_type_ids_async(NinOSDbContext db_context)
        {
            return db_context.note_types
                .AsNoTracking()
                .Where(t => t.code == "MAR")
                .Select(t => t.id_note_type)
                .ToListAsync();
        }

        public async Task<decimal?> get_sales_goal_async(DateTime year_month)
        {
            DateTime month_start = new DateTime(year_month.Year, year_month.Month, 1);
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.sales_goals
                    .AsNoTracking()
                    .Where(g => g.goal_month_start == month_start)
                    .Select(g => (decimal?)g.amount_usd)
                    .FirstOrDefaultAsync();
            }
        }

        public async Task set_sales_goal_async(DateTime year_month, decimal amount_usd)
        {
            DateTime month_start = new DateTime(year_month.Year, year_month.Month, 1);
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                var existing = await db_context.sales_goals
                    .FirstOrDefaultAsync(g => g.goal_month_start == month_start);

                if (existing != null)
                {
                    existing.amount_usd = amount_usd;
                }
                else
                {
                    db_context.sales_goals.Add(new sales_goal(month_start, amount_usd));
                }
                await db_context.SaveChangesAsync();
            }
        }
    }
}
