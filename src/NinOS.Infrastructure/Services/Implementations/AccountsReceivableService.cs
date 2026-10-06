using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Common;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class AccountsReceivableService : IAccountsReceivableService
    {
        /// <summary>
        /// <summary>Observacion que deja el asiento de anulacion de una nota Pro Venta.</summary>
        private const string AnulacionObservation = "Nota anulada";

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
                    .OrderBy(n => n.Year)
                    .ThenBy(n => n.Month)
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
                    .OrderBy(dn => dn.note_number)
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
                    .Include(c => c.zona)
                    .Where(c => customer_ids.Contains(c.id_customer))
                    .ToDictionaryAsync(c => c.id_customer);

                var payment_totals = await db_context.payments
                    .AsNoTracking()
                    .Where(p => p.id_delivery_note != null && note_ids.Contains(p.id_delivery_note.Value))
                    .GroupBy(p => p.id_delivery_note!.Value)
                    .Select(g => new { Id = g.Key, Total = g.Sum(p => p.amount_usd) })
                    .ToDictionaryAsync(x => x.Id, x => x.Total);

                var note_payments = await db_context.payments
                    .AsNoTracking()
                    .Where(p => p.id_delivery_note != null && note_ids.Contains(p.id_delivery_note.Value))
                    .OrderBy(p => p.payment_date)
                    .GroupBy(p => p.id_delivery_note!.Value)
                    .ToDictionaryAsync(g => g.Key, g => g.ToList());

                var result = new List<accounts_receivable_dto>();
                foreach (var dn in notes)
                {
                    decimal paid = payment_totals.TryGetValue(dn.id_delivery_note, out var total) ? total : 0;
                    sellers.TryGetValue(dn.id_seller, out string? seller_name);
                    customers.TryGetValue(dn.id_customer, out var cust);
                    string customer_name = cust?.business_name ?? string.Empty;
                    int? id_zona = cust?.id_zona;
                    string zone_name = cust?.zona?.name ?? "Sin zona";

                    string cxc_obs = dn.cxc_observations ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(cxc_obs) && note_payments.TryGetValue(dn.id_delivery_note, out var pList) && pList.Count > 0)
                    {
                        var pObs = pList.Select(p => p.observations?.Trim()).Where(o => !string.IsNullOrWhiteSpace(o)).Distinct();
                        cxc_obs = string.Join(" | ", pObs);
                    }

                    result.Add(new accounts_receivable_dto
                    {
                        id_delivery_note = dn.id_delivery_note,
                        note_number = dn.note_number,
                        customer_name = customer_name,
                        id_seller = dn.id_seller,
                        seller_name = seller_name ?? string.Empty,
                        id_zona = id_zona,
                        zone_name = zone_name,
                        creation_date = dn.creation_date,
                        dispatch_date = dn.dispatch_date,
                        total_amount_usd = dn.adjusted_total_usd,
                        status = dn.status,
                        paid_amount_usd = paid,
                        balance_due_usd = dn.adjusted_total_usd - paid,
                        cxc_observations = cxc_obs,
                        sales_observations = dn.sales_observations ?? string.Empty
                    });
                }

                return result.OrderByCorrelative(r => r.note_number).ToList();
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
                    .OrderBy(dn => dn.note_number)
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

                    string cxc_obs = dn.cxc_observations ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(cxc_obs) && note_payments.TryGetValue(dn.id_delivery_note, out var pListObs) && pListObs.Count > 0)
                    {
                        var pObs = pListObs.Select(p => p.observations?.Trim()).Where(o => !string.IsNullOrWhiteSpace(o)).Distinct();
                        cxc_obs = string.Join(" | ", pObs);
                    }

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
                        cxc_observations = cxc_obs,
                        sales_observations = dn.sales_observations ?? string.Empty
                    });
                }

                return result.OrderByCorrelative(r => r.note_number).ToList();
            }
        }

        public async Task<IEnumerable<accounts_receivable_dto>> get_all_by_month_and_seller_async(string month_year, int id_seller)
        {
            var all = await get_all_by_month_async(month_year);
            return all.Where(n => n.id_seller == id_seller);
        }

        // Cobranzas y Pagos: sin notas pro venta (MAR ni PVP).
        public async Task<IEnumerable<accounts_receivable_dto>> get_all_notes_async()
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                var excluded_ids = await get_pro_venta_type_ids_async(db_context);
                return await get_notes_internal_async(db_context, excluded_ids);
            }
        }

        // Ventas: todas las notas, incluidas las pro venta (MAR) y las de promocion (PVP).
        public async Task<IEnumerable<accounts_receivable_dto>> get_all_sales_notes_async()
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await get_notes_internal_async(db_context, new List<int>());
            }
        }

        private async Task<IEnumerable<accounts_receivable_dto>> get_notes_internal_async(
            NinOSDbContext db_context, List<int> excluded_type_ids)
        {
            {
                var notes_query = db_context.delivery_notes.AsNoTracking();

                if (excluded_type_ids.Count > 0)
                {
                    notes_query = notes_query
                        .Where(n => n.note_type_id == null || !excluded_type_ids.Contains(n.note_type_id.Value));
                }

                var notes = await notes_query
                    .OrderBy(n => n.note_number)
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
                    .Include(c => c.zona)
                    .Where(c => customer_ids.Contains(c.id_customer))
                    .ToDictionaryAsync(c => c.id_customer);

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

                var note_types = await db_context.note_types
                    .AsNoTracking()
                    .ToDictionaryAsync(t => t.id_note_type);

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
                    customers.TryGetValue(dn.id_customer, out var cust);
                    string customer_name = cust?.business_name ?? string.Empty;
                    int? id_zona = cust?.id_zona;
                    string zone_name = cust?.zona?.name ?? "Sin zona";

                    string note_type_name = string.Empty;
                    string note_type_code = string.Empty;
                    if (dn.note_type_id != null && note_types.TryGetValue(dn.note_type_id.Value, out var note_type_row))
                    {
                        note_type_name = note_type_row.name ?? string.Empty;
                        note_type_code = note_type_row.code ?? string.Empty;
                    }

                    string cxc_obs = dn.cxc_observations ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(cxc_obs) && note_payments.TryGetValue(dn.id_delivery_note, out var pListObs) && pListObs.Count > 0)
                    {
                        var pObs = pListObs.Select(p => p.observations?.Trim()).Where(o => !string.IsNullOrWhiteSpace(o)).Distinct();
                        cxc_obs = string.Join(" | ", pObs);
                    }

                    result.Add(new accounts_receivable_dto
                    {
                        id_delivery_note = dn.id_delivery_note,
                        note_number = dn.note_number,
                        note_type_name = note_type_name,
                        note_type_code = note_type_code,
                        customer_name = customer_name,
                        id_seller = dn.id_seller,
                        seller_name = seller_name ?? string.Empty,
                        id_zona = id_zona,
                        zone_name = zone_name,
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
                        cxc_observations = cxc_obs,
                        sales_observations = dn.sales_observations ?? string.Empty
                    });
                }

                return result.OrderByCorrelative(r => r.note_number).ToList();
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

                var customer = await db_context.customers.AsNoTracking().Include(c => c.zona).FirstOrDefaultAsync(c => c.id_customer == dn.id_customer);
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

                string cxc_obs = dn.cxc_observations ?? string.Empty;
                if (string.IsNullOrWhiteSpace(cxc_obs))
                {
                    var pObs = await db_context.payments
                        .AsNoTracking()
                        .Where(p => p.id_delivery_note == dn.id_delivery_note && !string.IsNullOrWhiteSpace(p.observations))
                        .Select(p => p.observations)
                        .ToListAsync();
                    cxc_obs = string.Join(" | ", pObs.Select(o => o.Trim()).Distinct());
                }

                return new accounts_receivable_dto
                {
                    id_delivery_note = dn.id_delivery_note,
                    note_number = dn.note_number,
                    customer_name = customer?.business_name ?? string.Empty,
                    id_seller = dn.id_seller,
                    seller_name = seller?.full_name ?? string.Empty,
                    id_zona = customer?.id_zona,
                    zone_name = customer?.zona?.name ?? "Sin zona",
                    creation_date = dn.creation_date,
                    total_amount_usd = dn.adjusted_total_usd,
                    gross_total_usd = is_promo_note ? dn.adjusted_total_usd : detail_sum,
                    discount_amount = is_promo_note ? 0 : detail_sum - dn.adjusted_total_usd,
                    discount_percentage = dn.discount_percentage,
                    volume_discount_percentage = dn.volume_discount_percentage,
                    status = dn.status,
                    paid_amount_usd = paid,
                    balance_due_usd = dn.adjusted_total_usd - paid,
                    cxc_observations = cxc_obs,
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
                if (delivery_note == null) throw new ArgumentException("La nota de entrega seleccionada ya no existe.");

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
                if (delivery_note == null) throw new ArgumentException("La nota de entrega seleccionada ya no existe.");

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
                if (delivery_note == null) throw new ArgumentException("La nota de entrega seleccionada ya no existe.");

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
                    if (delivery_note == null) throw new ArgumentException("La nota de entrega seleccionada ya no existe.");
                    if (delivery_note.status == "Anulada") throw new InvalidOperationException("No se puede editar una nota anulada.");
                    if (delivery_note.status == "Devuelta") throw new InvalidOperationException("No se puede editar una nota devuelta.");

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
                        if (delivery_note.status != "Pagada" && delivery_note.status != "Devuelta")
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

        public async Task annul_delivery_note_async(int id_delivery_note, bool registrar_asiento_pro_venta = false)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                using var transaction = await db_context.Database.BeginTransactionAsync();
                
                try
                {
                    var delivery_note = await db_context.delivery_notes
                        .FirstOrDefaultAsync(n => n.id_delivery_note == id_delivery_note);

                    if (delivery_note == null) throw new ArgumentException("La nota de entrega seleccionada ya no existe.");
                    if (delivery_note.status == "Anulada") throw new InvalidOperationException("La nota ya esta anulada.");
                    if (delivery_note.status == "Devuelta") throw new InvalidOperationException("Esta nota fue devuelta en su totalidad; no se puede anular.");

                    var has_payments = await db_context.payments
                        .AnyAsync(p => p.id_delivery_note == id_delivery_note && p.amount_usd > 0);

                    if (has_payments)
                        throw new InvalidOperationException("No se puede anular una nota que ya tiene abonos. Elimine primero los pagos registrados.");

                    // Las notas Pro Venta (MAR) cobran por relacion, y esos pagos se guardan con
                    // id_delivery_note en null. El chequeo de arriba no los ve, asi que sin esto se
                    // podria anular una nota MAR que ya tiene dinero cobrado y quedaria sin respaldo.
                    // La comprobacion es por relacion: el abono es de la relacion semanal completa,
                    // no de una nota suelta, asi que basta con que exista uno para bloquear.
                    var mar_ids_for_note = await get_pro_venta_type_ids_async(db_context);

                    if (delivery_note.note_type_id != null && mar_ids_for_note.Contains(delivery_note.note_type_id.Value))
                    {
                        if (delivery_note.id_relacion == null)
                            throw new InvalidOperationException("No se puede anular esta nota Pro Venta: no tiene relacion asociada.");

                        // No se restringe por vendedor: la tabla Pro Venta filtra por tipo de nota y
                        // hay notas Pro Venta de Anais (3300) ademas de las de Juan Luis (3400) -
                        // las relaciones 4, 5, 6 y 7 son mezcladas. La regla del negocio es que cada
                        // vendedor anula sus propias notas, y la app no tiene login con el que
                        // verificar quien esta operando, asi que el control es de la persona: por eso
                        // la pantalla pide confirmacion mostrando de quien es la nota.
                        bool has_mar_payments = await db_context.payments
                            .AnyAsync(p => p.id_relacion == delivery_note.id_relacion.Value && p.amount_usd > 0);

                        if (has_mar_payments)
                            throw new InvalidOperationException("No se puede anular una nota Pro Venta que ya tiene pagos MAR registrados. Elimine primero los pagos.");
                    }

                    var has_credit_notes = await db_context.credit_notes
                        .AnyAsync(c => c.id_delivery_note == id_delivery_note);

                    if (has_credit_notes)
                        throw new InvalidOperationException("No se puede anular una nota que tiene notas de credito registradas.");

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
                            stock_movement_writer.registrar_entrada(
                                db_context, product, detail.quantity,
                                stock_movement.RazonAnulacion, stock_movement.DocumentoEntrega, delivery_note.note_number,
                                DateTime.UtcNow, detail.unit_price_usd,
                                estado_documento: "Anulada",
                                id_entrega: delivery_note.id_delivery_note,
                                id_vendedor: delivery_note.id_seller,
                                id_cliente: delivery_note.id_customer,
                                vendido_como: stock_movement_writer.VendidoProducto);
                        }
                        else if (detail.id_promotion != null && promotions.TryGetValue(detail.id_promotion.Value, out var promotion))
                        {
                            if (promotion.items != null)
                            {
                                decimal precio_por_unidad = detail.quantity > 0 ? detail.unit_price_usd / detail.quantity : 0m;

                                foreach (var promo_item in promotion.items)
                                {
                                    if (products.TryGetValue(promo_item.id_product, out var promo_product))
                                    {
                                        stock_movement_writer.registrar_entrada(
                                            db_context, promo_product, detail.quantity * promo_item.quantity_required,
                                            stock_movement.RazonAnulacion, stock_movement.DocumentoEntrega, delivery_note.note_number,
                                            DateTime.UtcNow, precio_por_unidad,
                                            estado_documento: "Anulada",
                                            id_entrega: delivery_note.id_delivery_note,
                                            id_vendedor: delivery_note.id_seller,
                                            id_cliente: delivery_note.id_customer,
                                            id_promocion: detail.id_promotion,
                                            unidades_promocion: detail.quantity,
                                            vendido_como: stock_movement_writer.VendidoPromocion,
                                            descripcion_linea: promotion.name);
                                    }
                                }
                            }
                        }
                    }

                    delivery_note.status = "Anulada";

                    if (registrar_asiento_pro_venta && delivery_note.id_relacion != null)
                    {
                        // Asiento de respaldo, solo para Pro Venta. Va con monto NEGATIVO para que
                        // en el historial salga en rojo igual que una nota de credito, y con la fecha
                        // del sistema porque es el momento en que se anulo. No es un pago: por eso
                        // el calculo de saldos de Pro Venta ignora los abonos negativos.
                        var asiento_anulacion = new payment(
                            null,
                            DateTime.Now,
                            -delivery_note.adjusted_total_usd,
                            0,
                            null,
                            payment_dto.AnulacionPaymentType,
                            delivery_note.note_number,
                            "",
                            AnulacionObservation,
                            delivery_note.id_relacion.Value);

                        await db_context.payments.AddAsync(asiento_anulacion);
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
                        // La nota imprime el codigo que tenia el producto cuando se emitio,
                        // no el de hoy: si despues cambio de marca, la nota vieja conserva
                        // el suyo. El codigo actual solo cubre los renglones sin snapshot.
                        code = d.product_code_snapshot ?? prod.product_code;
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
                    promo_banner_text = is_promo ? "PROMOCION" : string.Empty,
                    header_title = string.IsNullOrWhiteSpace(note_type?.header_title) ? "DEFILE_REMBRANT_OLEOS_FLYING_BIOLINE" : note_type.header_title,
                    document_label = note_type != null && NoteTypeCodes.is_pro_venta(note_type.code) ? "NOTA DE DESPACHO" : "NOTA DE ENTREGA",
                    is_pro_venta = note_type != null && NoteTypeCodes.is_pro_venta(note_type.code),
                    is_promo = is_promo,
                    accent_color = note_type != null && NoteTypeCodes.is_pro_venta(note_type.code) ? "#1565C0" : "#1B3A2D",
                    accent_soft_color = note_type != null && NoteTypeCodes.is_pro_venta(note_type.code) ? "#E3F2FD" : "#F0F4EC",
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
                .Where(t => t.code == "MAR" || t.code == "PVP")
                .Select(t => t.id_note_type)
                .ToListAsync();
        }

        public async Task<decimal?> get_sales_goal_async(DateTime year_month, int? id_seller = null)        {
            DateTime month_start = new DateTime(year_month.Year, year_month.Month, 1);
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.sales_goals
                    .AsNoTracking()
                    .Where(g => g.goal_month_start == month_start && g.id_seller == id_seller)
                    .Select(g => (decimal?)g.amount_usd)
                    .FirstOrDefaultAsync();
            }
        }

        public async Task set_sales_goal_async(DateTime year_month, int? id_seller, decimal amount_usd)
        {
            DateTime month_start = new DateTime(year_month.Year, year_month.Month, 1);
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                var existing = await db_context.sales_goals
                    .FirstOrDefaultAsync(g => g.goal_month_start == month_start && g.id_seller == id_seller);

                if (amount_usd <= 0)
                {
                    if (existing != null)
                    {
                        db_context.sales_goals.Remove(existing);
                        await db_context.SaveChangesAsync();
                    }
                    return;
                }

                if (existing != null)
                {
                    existing.amount_usd = amount_usd;
                }
                else
                {
                    db_context.sales_goals.Add(new sales_goal(month_start, amount_usd, id_seller));
                }
                await db_context.SaveChangesAsync();
            }
        }
    }
}
