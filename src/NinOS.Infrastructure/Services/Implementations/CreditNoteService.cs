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

        public async Task<IEnumerable<accounts_receivable_dto>> get_delivery_notes_for_credit_async(int id_seller, string? month_year = null)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var cxc = scope.ServiceProvider.GetRequiredService<IAccountsReceivableService>();
                if (!string.IsNullOrWhiteSpace(month_year))
                {
                    return await cxc.get_all_by_month_and_seller_async(month_year, id_seller);
                }
                var all_months = await cxc.get_all_months_async();
                var results = new List<accounts_receivable_dto>();
                foreach (var month in all_months)
                {
                    var notes = await cxc.get_all_by_month_and_seller_async(month, id_seller);
                    results.AddRange(notes);
                }
                return results;
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
            var delivery_ids = notes
                .Where(c => c.id_delivery_note.HasValue)
                .Select(c => c.id_delivery_note!.Value)
                .Distinct()
                .ToList();

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
                    id_delivery_note = c.id_delivery_note ?? 0,
                    source_note_number = c.id_delivery_note.HasValue && originals.TryGetValue(c.id_delivery_note.Value, out var o) ? o.note_number : string.Empty,
                    customer_name = customers.TryGetValue(c.id_customer, out var cu) ? cu.business_name : string.Empty,
                    id_seller = c.id_seller,
                    seller_name = sellers.TryGetValue(c.id_seller, out var se) ? se.full_name : string.Empty,
                    creation_date = c.creation_date,
                    total_amount_usd = c.total_amount_usd,
                    status = c.status,
                    category = c.category
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

            bool is_gift = string.Equals(new_note.category, "Obsequio", StringComparison.OrdinalIgnoreCase);

            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                using var transaction = await db_context.Database.BeginTransactionAsync();
                try
                {
                    delivery_note? original_note = null;

                    if (is_gift)
                    {
                        if (new_note.id_delivery_note.HasValue)
                            throw new InvalidOperationException("La nota de credito por obsequio no va anclada a una nota de entrega.");

                        bool customer_exists = await db_context.customers
                            .AsNoTracking()
                            .AnyAsync(c => c.id_customer == new_note.id_customer);
                        if (!customer_exists) throw new ArgumentException("El cliente seleccionado ya no existe.");
                    }
                    else
                    {
                        if (!new_note.id_delivery_note.HasValue)
                            throw new InvalidOperationException("La nota de credito por devolucion requiere una nota de entrega.");

                        original_note = await db_context.delivery_notes
                            .FirstOrDefaultAsync(n => n.id_delivery_note == new_note.id_delivery_note);
                        if (original_note == null) throw new ArgumentException("La nota de entrega seleccionada ya no existe.");
                        if (original_note.status == "Anulada") throw new InvalidOperationException("No se puede crear una nota de credito sobre una nota anulada.");
                    }

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
                    // mover stock de promociones sin mover a la vez sus productos sueltos.
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

                    if (is_gift)
                    {
                        // El obsequio regala producto: valida stock disponible y lo resta.
                        foreach (var detail in detail_list)
                        {
                            if (detail.id_product != null)
                            {
                                if (!products.TryGetValue(detail.id_product.Value, out var product))
                                    throw new InvalidOperationException("Uno de los productos obsequiados ya no existe.");
                                if (product.stock_quantity < detail.quantity)
                                    throw new InvalidOperationException(
                                        $"La cantidad obsequiada de {product.name} ({detail.quantity}) supera el stock disponible ({product.stock_quantity}).");
                            }
                            else if (detail.id_promotion != null)
                            {
                                if (!promotions.TryGetValue(detail.id_promotion.Value, out var promotion))
                                    throw new InvalidOperationException("Una de las promociones obsequiadas ya no existe.");
                                if (promotion.items == null || promotion.items.Count == 0)
                                    throw new InvalidOperationException($"La promocion {promotion.name} no tiene productos asignados.");
                                foreach (var promo_item in promotion.items)
                                {
                                    if (!products.TryGetValue(promo_item.id_product, out var promo_product))
                                        throw new InvalidOperationException("Uno de los productos de la promocion obsequiada ya no existe.");
                                    if (promo_product.stock_quantity < detail.quantity * promo_item.quantity_required)
                                        throw new InvalidOperationException(
                                            $"La cantidad obsequiada de {promotion.name} ({detail.quantity}) supera el stock de {promo_product.name} ({promo_product.stock_quantity}).");
                                }
                            }
                        }

                        foreach (var detail in detail_list)
                        {
                            if (detail.id_product != null)
                            {
                                products[detail.id_product.Value].stock_quantity -= detail.quantity;
                            }
                            else if (detail.id_promotion != null)
                            {
                                var promotion = promotions[detail.id_promotion.Value];
                                foreach (var promo_item in promotion.items!)
                                {
                                    products[promo_item.id_product].stock_quantity -= detail.quantity * promo_item.quantity_required;
                                }
                            }
                        }
                    }
                    else
                    {
                        var original_details = await db_context.note_details
                            .AsNoTracking()
                            .Where(d => d.id_delivery_note == original_note!.id_delivery_note)
                            .ToListAsync();

                        var original_map = new Dictionary<(int?, int?), note_detail>();
                        foreach (var od in original_details) original_map[(od.id_product, od.id_promotion)] = od;

                        var existing_credit_ids = await db_context.credit_notes
                            .AsNoTracking()
                            .Where(c => c.id_delivery_note == original_note!.id_delivery_note)
                            .Select(c => c.id_credit_note)
                            .ToListAsync();

                        var existing_returns = existing_credit_ids.Count == 0
                            ? new List<credit_note_detail>()
                            : await db_context.credit_note_details
                                .AsNoTracking()
                                .Where(d => existing_credit_ids.Contains(d.id_credit_note))
                                .ToListAsync();

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
                        }
                    }

                    // El correlativo es por vendedor e independiente del numero de la nota de entrega.
                    if (string.IsNullOrWhiteSpace(new_note.note_number))
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<ICreditNoteRepository>();
                        new_note.note_number = await repository.get_next_credit_correlative_async(new_note.id_seller);
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

                    // Solo la devolucion se registra como pago NEGATIVO para que reste en el historial y en el saldo;
                    // el obsequio no es deuda y solo afecta el inventario.
                    if (!is_gift && original_note != null)
                    {
                        string pay_observations = $"Nota de credito {new_note.note_number} - {new_note.total_amount_usd:N2}";
                        if (!string.IsNullOrWhiteSpace(new_note.observations))
                            pay_observations += $". {new_note.observations}";

                        payment nc_payment = new payment(
                            original_note.id_delivery_note,
                            new_note.creation_date,
                            -new_note.total_amount_usd,
                            0m,
                            null,
                            "NOTA DE CREDITO",
                            new_note.note_number,
                            string.Empty,
                            pay_observations,
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
                bool is_gift = string.Equals(credit.category, "Obsequio", StringComparison.OrdinalIgnoreCase);

                return new note_print_dto
                {
                    id_delivery_note = credit.id_delivery_note ?? 0,
                    note_number = credit.note_number,
                    company_name = "DEFILE_REMBRANT_OLEOS_FLYING_BIOLINE",
                    promo_banner_text = string.Empty,
                    header_title = header_title,
                    document_label = "NOTA DE CREDITO",
                    is_pro_venta = false,
                    is_promo = false,
                    accent_color = "#2E7D32",
                    accent_soft_color = "#F1F8E9",
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
                    total_amount_usd = total,
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
                    conditions_text = is_gift
                        ? "NOTA DE CREDITO POR OBSEQUIO DE PRODUCTOS."
                        : string.IsNullOrWhiteSpace(source_text)
                            ? "NOTA DE CREDITO POR DEVOLUCION DE PRODUCTOS."
                            : $"DEVOLUCION DE PRODUCTOS DE LA NOTA {source_text}. ESTA NOTA RESTA DEL MONTO A COBRAR.",
                    discount_conditions_text = string.Empty,
                    details = print_details
                };
            }
        }

        public async Task<credit_note_report_dto> get_credit_note_report_async(DateTime from_date, DateTime to_date, string? category, int? id_seller)
        {
            if (from_date.Date > to_date.Date)
                throw new ArgumentException("La fecha inicial del reporte no puede ser posterior a la fecha final.");

            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                var period_start = to_utc(from_date);
                var period_end_exclusive = to_utc(to_date).AddDays(1);
                int period_days = (to_date.Date - from_date.Date).Days + 1;
                var previous_start = period_start.AddDays(-period_days);
                var previous_end_exclusive = period_start;

                var rows = await load_report_rows_async(db_context, period_start, period_end_exclusive, category, id_seller);

                var previous_notes = await apply_report_filters(
                        db_context.credit_notes.AsNoTracking(),
                        previous_start,
                        previous_end_exclusive,
                        category,
                        id_seller)
                    .ToListAsync();

                var report = new credit_note_report_dto
                {
                    from_date = from_date.Date,
                    to_date = to_date.Date,
                    category_label = string.IsNullOrWhiteSpace(category) ? "Todas" : category.Trim(),
                    seller_label = "Todos",
                    period_label = build_period_label(from_date, to_date),
                    previous_period_label = build_period_label(previous_start, previous_end_exclusive.AddDays(-1)),
                    previous_total_usd = previous_notes.Sum(c => c.total_amount_usd),
                    previous_total_notes = previous_notes.Count
                };

                if (id_seller != null)
                {
                    var seller = await db_context.sellers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.id_seller == id_seller.Value);
                    report.seller_label = seller?.full_name ?? "Todos";
                }

                report.total_notes = rows.Count;
                report.total_usd = rows.Sum(r => r.total_amount_usd);
                report.gift_notes = rows.Count(r => r.es_obsequio);
                report.gift_usd = rows.Where(r => r.es_obsequio).Sum(r => r.total_amount_usd);
                report.return_notes = rows.Count - report.gift_notes;
                report.return_usd = report.total_usd - report.gift_usd;
                report.voided_notes = rows.Count(r => r.esta_anulada);
                report.voided_usd = rows.Where(r => r.esta_anulada).Sum(r => r.total_amount_usd);
                report.affected_customers = rows
                    .Select(r => r.customer_name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .Count();
                report.average_note_usd = report.total_notes == 0 ? 0 : report.total_usd / report.total_notes;

                report.rows = rows
                    .OrderByDescending(r => r.creation_date)
                    .ThenByDescending(r => r.id_credit_note)
                    .ToList();

                report.by_seller = build_seller_bars(rows);

                // Las barras por dia solo tienen sentido dentro de un mes natural: en un rango
                // de meses o anios el grafico seria ilegible, asi que se omite.
                bool is_single_month = from_date.Year == to_date.Year && from_date.Month == to_date.Month;
                if (is_single_month) report.by_day = build_day_bars(rows, DateTime.DaysInMonth(from_date.Year, from_date.Month));

                return report;
            }
        }

        private static async Task<List<credit_note_report_row_dto>> load_report_rows_async(
            NinOSDbContext db_context,
            DateTime from_exclusive_or_inclusive_start,
            DateTime to_exclusive,
            string? category,
            int? id_seller)
        {
            var notes = await apply_report_filters(
                    db_context.credit_notes.AsNoTracking(),
                    from_exclusive_or_inclusive_start,
                    to_exclusive,
                    category,
                    id_seller)
                .ToListAsync();

            var customer_ids = notes.Select(c => c.id_customer).Distinct().ToList();
            var seller_ids = notes.Select(c => c.id_seller).Distinct().ToList();
            var delivery_ids = notes
                .Where(c => c.id_delivery_note.HasValue)
                .Select(c => c.id_delivery_note!.Value)
                .Distinct()
                .ToList();

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
                .Select(c => new credit_note_report_row_dto
                {
                    id_credit_note = c.id_credit_note,
                    note_number = c.note_number,
                    source_note_number = c.id_delivery_note.HasValue && originals.TryGetValue(c.id_delivery_note.Value, out var o) ? o.note_number : string.Empty,
                    category = c.category,
                    customer_name = customers.TryGetValue(c.id_customer, out var cu) ? cu.business_name : string.Empty,
                    seller_name = sellers.TryGetValue(c.id_seller, out var se) ? se.full_name : string.Empty,
                    status = c.status,
                    creation_date = c.creation_date,
                    total_amount_usd = c.total_amount_usd
                })
                .ToList();
        }

        private static IQueryable<credit_note> apply_report_filters(
            IQueryable<credit_note> query,
            DateTime from_inclusive,
            DateTime to_exclusive,
            string? category,
            int? id_seller)
        {
            query = query.Where(c => c.creation_date >= from_inclusive && c.creation_date < to_exclusive);

            if (!string.IsNullOrWhiteSpace(category))
            {
                string wanted = category.Trim();
                query = query.Where(c => c.category == wanted);
            }

            if (id_seller != null)
            {
                query = query.Where(c => c.id_seller == id_seller.Value);
            }

            return query;
        }

        private static List<credit_note_report_seller_dto> build_seller_bars(List<credit_note_report_row_dto> rows)
        {
            var bars = rows
                .GroupBy(r => string.IsNullOrWhiteSpace(r.seller_name) ? "Sin vendedor" : r.seller_name)
                .Select(g => new credit_note_report_seller_dto
                {
                    seller_name = g.Key,
                    notes_count = g.Count(),
                    total_usd = g.Sum(r => r.total_amount_usd),
                    gift_usd = g.Where(r => r.es_obsequio).Sum(r => r.total_amount_usd),
                    return_usd = g.Where(r => !r.es_obsequio).Sum(r => r.total_amount_usd)
                })
                .OrderByDescending(s => s.total_usd)
                .ThenBy(s => s.seller_name)
                .ToList();

            decimal max = bars.Count == 0 ? 0 : bars.Max(s => s.total_usd);
            if (max > 0)
            {
                foreach (var bar in bars)
                {
                    bar.total_ratio = ratio(bar.total_usd, max);
                    bar.gift_ratio = ratio(bar.gift_usd, max);
                    bar.return_ratio = ratio(bar.return_usd, max);
                }
            }

            return bars;
        }

        private static List<credit_note_report_day_dto> build_day_bars(List<credit_note_report_row_dto> rows, int period_days)
        {
            var totals = new Dictionary<int, (int notes, decimal total)>();
            foreach (var row in rows)
            {
                int day = row.creation_date.Day;
                totals.TryGetValue(day, out var current);
                totals[day] = (current.notes + 1, current.total + row.total_amount_usd);
            }

            var bars = new List<credit_note_report_day_dto>();
            for (int day = 1; day <= period_days; day++)
            {
                totals.TryGetValue(day, out var current);
                bars.Add(new credit_note_report_day_dto
                {
                    day = day,
                    notes_count = current.notes,
                    total_usd = current.total
                });
            }

            decimal max = bars.Count == 0 ? 0 : bars.Max(b => b.total_usd);
            if (max > 0)
            {
                foreach (var bar in bars) bar.total_ratio = ratio(bar.total_usd, max);
            }

            return bars;
        }

        private static double ratio(decimal value, decimal max) => max <= 0 ? 0 : (double)(value / max);

        private static DateTime to_utc(DateTime date)
            => new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);

        private static string build_period_label(DateTime from_date, DateTime to_date)
        {
            var culture = new CultureInfo("es-VE");
            string from = from_date.Date.ToString("dd/MM/yyyy", culture);
            string to = to_date.Date.ToString("dd/MM/yyyy", culture);

            if (from_date.Year == to_date.Year && from_date.Month == to_date.Month)
                return Capitalize(from_date.Date.ToString("MMMM yyyy", culture));

            return $"{from} - {to}";
        }

        private static string Capitalize(string text)
            => string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0], CultureInfo.InvariantCulture) + text.Substring(1);

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