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
using NinOS.Infrastructure.Repositories.Interfaces;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class CreditNoteService : ICreditNoteService
    {
        private readonly IServiceScopeFactory _scope_factory;

        public CreditNoteService(IServiceScopeFactory scope_factory)
        {
            if (scope_factory == null) throw new ArgumentNullException(nameof(scope_factory));
            _scope_factory = scope_factory;
        }

        public async Task<string> generate_credit_correlative_async(int id_seller)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<ICreditNoteRepository>();
                return await repository.get_next_credit_correlative_async(id_seller);
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

        public async Task<IEnumerable<string>> get_credit_note_months_async()
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                var months = await db_context.credit_notes
                    .AsNoTracking()
                    .Select(c => new { c.creation_date.Year, c.creation_date.Month })
                    .Distinct()
                    .OrderByDescending(c => c.Year)
                    .ThenByDescending(c => c.Month)
                    .ToListAsync();

                return months
                    .Select(c => new DateTime(c.Year, c.Month, 1).ToString("MMMM yyyy", new CultureInfo("es-VE")))
                    .ToList();
            }
        }

        public async Task<IEnumerable<credit_note_dto>> get_all_credit_notes_async()
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await build_list_dtos_async(db_context, null);
            }
        }

        public async Task<IEnumerable<credit_note_dto>> get_credit_notes_by_month_async(string month_year)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                var target_date = DateTime.ParseExact(month_year, "MMMM yyyy", new CultureInfo("es-VE"));
                return await build_list_dtos_async(db_context, target_date);
            }
        }

        public async Task<IEnumerable<credit_note_dto>> get_credit_notes_by_month_and_seller_async(string month_year, int id_seller)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                var target_date = DateTime.ParseExact(month_year, "MMMM yyyy", new CultureInfo("es-VE"));
                return await build_list_dtos_async(db_context, target_date, id_seller);
            }
        }

        private static async Task<IEnumerable<credit_note_dto>> build_list_dtos_async(NinOSDbContext db_context, DateTime? target_date, int? id_seller = null)
        {
            var query = db_context.credit_notes.AsNoTracking();

            if (target_date != null)
            {
                query = query.Where(c => c.creation_date.Year == target_date.Value.Year && c.creation_date.Month == target_date.Value.Month);
            }

            if (id_seller != null)
            {
                query = query.Where(c => c.id_seller == id_seller.Value);
            }

            var notes = await query.ToListAsync();

            var customer_ids = notes.Select(c => c.id_customer).Distinct().ToList();
            var seller_ids = notes.Select(c => c.id_seller).Distinct().ToList();
            var delivery_ids = notes.Select(c => c.id_delivery_note).Distinct().ToList();

            var customers = await db_context.customers
                .AsNoTracking()
                .Where(c => customer_ids.Contains(c.id_customer))
                .ToDictionaryAsync(c => c.id_customer);
            var sellers = await db_context.sellers
                .AsNoTracking()
                .Where(s => seller_ids.Contains(s.id_seller))
                .ToDictionaryAsync(s => s.id_seller);
            var originals = await db_context.delivery_notes
                .AsNoTracking()
                .Where(n => delivery_ids.Contains(n.id_delivery_note))
                .ToDictionaryAsync(n => n.id_delivery_note);

            return notes
                .OrderByDescending(c => c.creation_date)
                .ThenByDescending(c => c.id_credit_note)
                .Select(c => new credit_note_dto
                {
                    id_credit_note = c.id_credit_note,
                    note_number = c.note_number,
                    id_delivery_note = c.id_delivery_note,
                    source_note_number = originals.TryGetValue(c.id_delivery_note, out var o) ? o.note_number : string.Empty,
                    customer_name = customers.TryGetValue(c.id_customer, out var cu) ? cu.business_name : string.Empty,
                    id_seller = c.id_seller,
                    seller_name = sellers.TryGetValue(c.id_seller, out var se) ? se.full_name : string.Empty,
                    creation_date = c.creation_date,
                    total_amount_usd = c.total_amount_usd,
                    status = c.status
                })
                .ToList();
        }

        public async Task<credit_note_source_dto?> get_credit_source_by_note_number_async(string note_number)
        {
            if (string.IsNullOrWhiteSpace(note_number)) throw new ArgumentException("Debe indicar el numero de la nota de entrega.");

            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                var note = await db_context.delivery_notes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.note_number == note_number);

                if (note == null) return null;

                var customer = await db_context.customers.AsNoTracking().FirstOrDefaultAsync(c => c.id_customer == note.id_customer);
                var seller = await db_context.sellers.AsNoTracking().FirstOrDefaultAsync(s => s.id_seller == note.id_seller);

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

                var source_dto = new credit_note_source_dto
                {
                    id_delivery_note = note.id_delivery_note,
                    note_number = note.note_number,
                    creation_date = note.creation_date,
                    id_customer = note.id_customer,
                    customer_code = customer?.customer_code ?? string.Empty,
                    customer_name = customer?.business_name ?? string.Empty,
                    id_seller = note.id_seller,
                    seller_name = seller?.full_name ?? string.Empty,
                    adjusted_total_usd = note.adjusted_total_usd,
                    status = note.status
                };

                var existing_credit_ids = await db_context.credit_notes
                    .AsNoTracking()
                    .Where(c => c.id_delivery_note == note.id_delivery_note)
                    .Select(c => c.id_credit_note)
                    .ToListAsync();

                source_dto.already_returned_usd = existing_credit_ids.Count == 0
                    ? 0
                    : await db_context.credit_notes
                        .AsNoTracking()
                        .Where(c => existing_credit_ids.Contains(c.id_credit_note))
                        .SumAsync(c => (decimal?)c.total_amount_usd) ?? 0;

                if (existing_credit_ids.Count > 0)
                {
                    var returned_rows = await db_context.credit_note_details
                        .AsNoTracking()
                        .Where(d => existing_credit_ids.Contains(d.id_credit_note))
                        .ToListAsync();

                    foreach (var d in raw_details)
                    {
                        int already_returned = returned_rows
                            .Where(r => r.id_product == d.id_product && r.id_promotion == d.id_promotion)
                            .Sum(r => r.quantity);

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

                        source_dto.lines.Add(new credit_note_source_line_dto
                        {
                            id_product = d.id_product,
                            id_promotion = d.id_promotion,
                            code = code,
                            name = name,
                            unit_price_usd = d.unit_price_usd,
                            delivered_quantity = d.quantity,
                            already_returned_quantity = already_returned,
                            remaining_quantity = Math.Max(d.quantity - already_returned, 0)
                        });
                    }
                }
                else
                {
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

                        source_dto.lines.Add(new credit_note_source_line_dto
                        {
                            id_product = d.id_product,
                            id_promotion = d.id_promotion,
                            code = code,
                            name = name,
                            unit_price_usd = d.unit_price_usd,
                            delivered_quantity = d.quantity,
                            already_returned_quantity = 0,
                            remaining_quantity = d.quantity
                        });
                    }
                }

                return source_dto;
            }
        }

        public async Task<credit_note_dto> create_credit_note_async(credit_note new_note, IEnumerable<credit_note_detail> details)
        {
            if (new_note == null) throw new ArgumentNullException(nameof(new_note));
            if (details == null) throw new InvalidOperationException("Los detalles no pueden ser nulos.");

            var detail_list = details.ToList();
            if (detail_list.Count == 0) throw new InvalidOperationException("Debe devolver al menos un producto.");

            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                using var transaction = await db_context.Database.BeginTransactionAsync();
                try
                {
                    var original_note = await db_context.delivery_notes
                        .FirstOrDefaultAsync(n => n.id_delivery_note == new_note.id_delivery_note);
                    if (original_note == null) throw new ArgumentException("La nota de entrega seleccionada ya no existe.");
                    if (original_note.status == "Anulada") throw new InvalidOperationException("No se puede crear una nota de credito sobre una nota anulada.");

                    var direct_product_ids = new HashSet<int>();
                    var promotion_ids = new HashSet<int>();
                    foreach (var detail in detail_list)
                    {
                        if (detail.id_product != null) direct_product_ids.Add(detail.id_product.Value);
                        if (detail.id_promotion != null) promotion_ids.Add(detail.id_promotion.Value);
                        if (detail.id_product == null && detail.id_promotion == null)
                            throw new InvalidOperationException("Cada linea debe tener un producto o una promocion.");
                    }

                    var promotions = await db_context.promotions
                        .Include(p => p.items)
                        .Where(p => promotion_ids.Contains(p.id_promotion))
                        .ToDictionaryAsync(p => p.id_promotion);

                    // Los productos que arman cada promocion tambien se cargan: asi se puede
                    // devolver stock de promociones sin devolver a la vez sus productos sueltos.
                    var all_product_ids = new HashSet<int>(direct_product_ids);
                    foreach (var promotion in promotions.Values)
                    {
                        if (promotion.items == null) continue;
                        foreach (var item in promotion.items)
                        {
                            all_product_ids.Add(item.id_product);
                        }
                    }

                    var products = await db_context.products
                        .Where(p => all_product_ids.Contains(p.id_product))
                        .ToDictionaryAsync(p => p.id_product);

                    var original_details = await db_context.note_details
                        .AsNoTracking()
                        .Where(d => d.id_delivery_note == original_note.id_delivery_note)
                        .ToListAsync();

                    var original_map = new Dictionary<(int?, int?), note_detail>();
                    foreach (var od in original_details) original_map[(od.id_product, od.id_promotion)] = od;

                    var existing_credit_ids = await db_context.credit_notes
                        .AsNoTracking()
                        .Where(c => c.id_delivery_note == original_note.id_delivery_note)
                        .Select(c => c.id_credit_note)
                        .ToListAsync();

                    var existing_returns = existing_credit_ids.Count == 0
                        ? new List<credit_note_detail>()
                        : await db_context.credit_note_details
                            .AsNoTracking()
                            .Where(d => existing_credit_ids.Contains(d.id_credit_note))
                            .ToListAsync();

                    decimal subtotal_check = 0;

                    foreach (var detail in detail_list)
                    {
                        var key = (detail.id_product, detail.id_promotion);
                        if (!original_map.TryGetValue(key, out var original_detail))
                            throw new InvalidOperationException("Una de las lineas devueltas no pertenece al detalle de la nota.");

                        int already_returned = existing_returns
                            .Where(r => r.id_product == detail.id_product && r.id_promotion == detail.id_promotion)
                            .Sum(r => r.quantity);

                        int remaining = original_detail.quantity - already_returned;
                        if (detail.quantity > remaining)
                        {
                            string item_name = describe_item(detail, products, promotions);
                            throw new InvalidOperationException(
                                $"La cantidad devuelta de {item_name} ({detail.quantity}) supera lo entregado en la nota ({original_detail.quantity}).");
                        }

                        if (detail.id_product != null)
                        {
                            if (!products.TryGetValue(detail.id_product.Value, out var product))
                                throw new InvalidOperationException("Uno de los productos devueltos ya no existe.");
                            product.stock_quantity += detail.quantity;
                        }
                        else if (detail.id_promotion != null)
                        {
                            if (!promotions.TryGetValue(detail.id_promotion.Value, out var promotion))
                                throw new InvalidOperationException("Una de las promociones devueltas ya no existe.");
                            if (promotion.items == null || promotion.items.Count == 0)
                                throw new InvalidOperationException($"La promocion {promotion.name} no tiene productos asignados.");
                            foreach (var promo_item in promotion.items)
                            {
                                if (!products.TryGetValue(promo_item.id_product, out var promo_product))
                                    throw new InvalidOperationException("Uno de los productos de la promocion devuelta ya no existe.");
                                promo_product.stock_quantity += detail.quantity * promo_item.quantity_required;
                            }
                        }

                        subtotal_check += detail.subtotal_usd;
                    }

                    await db_context.credit_notes.AddAsync(new_note);

                    try
                    {
                        await db_context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex)
                    {
                        if (is_unique_note_number_violation(ex))
                            throw new InvalidOperationException("Ya existe una nota de credito con ese correlativo. Verifique el numero e intente de nuevo.");
                        throw;
                    }

                    foreach (var detail in detail_list)
                    {
                        detail.id_credit_note = new_note.id_credit_note;
                        await db_context.credit_note_details.AddAsync(detail);
                    }

                    // La NC se registra como pago NEGATIVO para que reste en el historial y en el saldo.
                    payment nc_payment = new payment(
                        original_note.id_delivery_note,
                        new_note.creation_date,
                        -new_note.total_amount_usd,
                        0m,
                        null,
                        "NOTA DE CREDITO",
                        new_note.note_number,
                        string.Empty,
                        string.IsNullOrWhiteSpace(new_note.observations)
                            ? $"Devolucion registrada por nota de credito {new_note.note_number}"
                            : new_note.observations,
                        original_note.id_relacion);

                    await db_context.payments.AddAsync(nc_payment);

                    if (original_note.status == "Pagada")
                    {
                        decimal existing_paid = await db_context.payments
                            .AsNoTracking()
                            .Where(p => p.id_delivery_note == original_note.id_delivery_note)
                            .SumAsync(p => (decimal?)p.amount_usd) ?? 0;

                        decimal total_paid_after = existing_paid + nc_payment.amount_usd;
                        if (total_paid_after < original_note.adjusted_total_usd)
                        {
                            original_note.status = "Pendiente";
                        }
                    }

                    await db_context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (await get_all_credit_notes_async())
                        .First(c => c.id_credit_note == new_note.id_credit_note);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<IEnumerable<credit_note_detail_dto>> get_credit_note_details_async(int id_credit_note)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                var details = await db_context.credit_note_details
                    .AsNoTracking()
                    .Where(d => d.id_credit_note == id_credit_note)
                    .ToListAsync();

                var product_ids = details.Where(d => d.id_product != null).Select(d => d.id_product!.Value).Distinct().ToList();
                var promo_ids = details.Where(d => d.id_promotion != null).Select(d => d.id_promotion!.Value).Distinct().ToList();

                var products = await db_context.products
                    .AsNoTracking()
                    .Where(p => product_ids.Contains(p.id_product))
                    .ToDictionaryAsync(p => p.id_product);
                var promotions = await db_context.promotions
                    .AsNoTracking()
                    .Where(p => promo_ids.Contains(p.id_promotion))
                    .ToDictionaryAsync(p => p.id_promotion);

                return details
                    .OrderBy(d => d.id_credit_note_detail)
                    .Select(d =>
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

                        return new credit_note_detail_dto
                        {
                            id_credit_note_detail = d.id_credit_note_detail,
                            id_product = d.id_product,
                            id_promotion = d.id_promotion,
                            code = code,
                            name = name,
                            quantity = d.quantity,
                            unit_price_usd = d.unit_price_usd,
                            subtotal_usd = d.subtotal_usd
                        };
                    })
                    .ToList();
            }
        }

        public async Task<note_print_dto> get_printable_credit_note_async(int id_credit_note)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                var credit = await db_context.credit_notes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.id_credit_note == id_credit_note);
                if (credit == null) throw new ArgumentException("Nota de credito no encontrada.");

                var original = await db_context.delivery_notes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.id_delivery_note == credit.id_delivery_note);

                var customer = await db_context.customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.id_customer == credit.id_customer);
                var seller = await db_context.sellers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.id_seller == credit.id_seller);

                string header_title = "DEFILE_REMBRANT_OLEOS_FLYING_BIOLINE";
                if (original != null && original.note_type_id != null)
                {
                    var note_type = await db_context.note_types
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.id_note_type == original.note_type_id);
                    if (note_type != null && !string.IsNullOrWhiteSpace(note_type.header_title))
                        header_title = note_type.header_title;
                }

                var details = await get_credit_note_details_async(id_credit_note);

                var print_details = details.Select(d => new note_detail_print_dto
                {
                    code = d.code,
                    name = d.name,
                    quantity = d.quantity,
                    unit_price_usd = d.unit_price_usd,
                    discount_usd = 0,
                    promo_price_usd = d.unit_price_usd,
                    subtotal_usd = d.subtotal_usd
                }).ToList();

                decimal total = credit.total_amount_usd;
                string source_text = original != null ? original.note_number : string.Empty;

                return new note_print_dto
                {
                    id_delivery_note = credit.id_delivery_note,
                    note_number = credit.note_number,
                    company_name = "DEFILE_REMBRANT_OLEOS_FLYING_BIOLINE",
                    promo_banner_text = string.Empty,
                    header_title = header_title,
                    document_label = "NOTA DE CREDITO",
                    is_pro_venta = false,
                    is_promo = false,
                    accent_color = "#C62828",
                    accent_soft_color = "#FDECEA",
                    promo_discount_percentage = null,
                    promo_discount_amount = 0,
                    volume_discount_percentage = 0,
                    volume_discount_amount = 0,
                    discounted_total_usd = total,
                    creation_date = credit.creation_date,
                    due_date = credit.creation_date,
                    status = credit.status,
                    gross_total_usd = total,
                    discount_percentage = 0,
                    discount_amount = 0,
                    total_amount_usd = -total,
                    paid_amount_usd = 0,
                    balance_due_usd = 0,
                    seller_name = seller?.full_name ?? string.Empty,
                    customer_code = customer?.customer_code ?? string.Empty,
                    customer_business_name = customer?.business_name ?? string.Empty,
                    customer_rif = customer?.rif ?? string.Empty,
                    customer_phone = customer?.phone_number ?? string.Empty,
                    customer_contact = customer?.contact_name ?? string.Empty,
                    customer_delivery_address = customer?.effective_delivery_address ?? string.Empty,
                    fiscal_address = customer?.fiscal_address ?? string.Empty,
                    conditions_text = string.IsNullOrWhiteSpace(source_text)
                        ? "NOTA DE CREDITO POR DEVOLUCION DE PRODUCTOS."
                        : $"DEVOLUCION DE PRODUCTOS DE LA NOTA {source_text}. ESTA NOTA RESTA DEL MONTO A COBRAR.",
                    discount_conditions_text = string.Empty,
                    details = print_details
                };
            }
        }

        private static string describe_item(credit_note_detail detail, Dictionary<int, product> products, Dictionary<int, promotion> promotions)
        {
            if (detail.id_product != null && products.TryGetValue(detail.id_product.Value, out var prod)) return prod.name;
            if (detail.id_promotion != null && promotions.TryGetValue(detail.id_promotion.Value, out var promo)) return promo.name;
            return "la linea seleccionada";
        }

        private static bool is_unique_note_number_violation(DbUpdateException ex)
        {
            var message = ex.GetBaseException()?.Message ?? string.Empty;
            return message.Contains("23505")
                || message.Contains("IX_credit_note_note_number")
                || message.Contains("duplicate key");
        }
    }
}