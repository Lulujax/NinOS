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
using NinOS.Infrastructure.Logging;
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
                return await repository.get_next_credit_correlative_async();
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

        public async Task<IEnumerable<string>> get_delivery_note_months_for_seller_async(int id_seller)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                // Se convierte a hora de Venezuela antes de tomar el anio y el mes, igual que en el
                // filtro por mes de esta misma vista, para que el combo y el filtro coincidan.
                // No se usa TimeZoneInfo.Local: el sistema opera en Caracas (UTC-4), no en la
                // zona de la maquina, y una nota creada a medianoche se reportaba en el mes anterior.
                double local_offset_hours = AppTimeZone.offset_hours;

                var dates = await db_context.delivery_notes
                    .AsNoTracking()
                    .Where(dn => dn.id_seller == id_seller && dn.status != "Anulada" && dn.status != "Devuelta")
                    .Select(dn => new
                    {
                        dn.creation_date.AddHours(-local_offset_hours).Year,
                        dn.creation_date.AddHours(-local_offset_hours).Month
                    })
                    .Distinct()
                    .OrderBy(d => d.Year)
                    .ThenBy(d => d.Month)
                    .ToListAsync();

                return dates
                    .Select(d => new DateTime(d.Year, d.Month, 1).ToString("MMMM yyyy", new CultureInfo("es-VE")))
                    .ToList();
            }
        }

        public async Task<IEnumerable<accounts_receivable_dto>> get_delivery_notes_for_credit_async(int id_seller, string? month_year = null)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                var query = db_context.delivery_notes
                    .AsNoTracking()
                    .Where(dn => dn.id_seller == id_seller && dn.status != "Anulada" && dn.status != "Devuelta");

                if (!string.IsNullOrWhiteSpace(month_year))
                {
                    if (DateTime.TryParseExact(month_year.Trim(), "MMMM yyyy", new CultureInfo("es-VE"), DateTimeStyles.None, out var target_date))
                    {
                        // Se compara contra un rango en UTC, no con Year/Month. La columna guarda
                        // UTC, asi que extraer el mes de la columna cruda corre el riesgo de poner
                        // en el mes equivocado un documento creado de noche (ya va en el mes
                        // siguiente al guardarlo). El rango se arma desde la hora local del
                        // primer dia del mes y del siguiente, y se convierte a UTC.
                        var month_start = new DateTime(target_date.Year, target_date.Month, 1, 0, 0, 0, DateTimeKind.Local);
                        var month_end = month_start.AddMonths(1);

                        query = query.Where(dn => dn.creation_date >= month_start.ToUniversalTime()
                                              && dn.creation_date < month_end.ToUniversalTime());
                    }
                }

                var notes = await query
                    .OrderBy(n => n.note_number)
                    .ToListAsync();

                if (notes.Count == 0)
                    return Enumerable.Empty<accounts_receivable_dto>();

                var note_ids = notes.Select(n => n.id_delivery_note).ToList();
                var customer_ids = notes.Select(n => n.id_customer).Distinct().ToList();

                var customers = await db_context.customers
                    .AsNoTracking()
                    .Where(c => customer_ids.Contains(c.id_customer))
                    .ToDictionaryAsync(c => c.id_customer, c => c.business_name);

                var seller = await db_context.sellers.AsNoTracking().FirstOrDefaultAsync(s => s.id_seller == id_seller);
                string seller_name = seller?.full_name ?? string.Empty;

                var note_types = await db_context.note_types.AsNoTracking().ToDictionaryAsync(t => t.id_note_type, t => t.name);

                // Una NC se registra como pago NEGATIVO contra la nota de entrega (payment_type
                // "NOTA DE CREDITO" y reference_number = numero de NC). Si esa NC despues se
                // anula, su descuento sigue descontando y la nota original pareceria pagada
                // cuando en realidad todavia tiene saldo, haciendo que desaparezca del
                // buscador cuando si se le podria hacer una NC.
                var annulled_credit_numbers = await db_context.credit_notes
                    .AsNoTracking()
                    .Where(c => c.id_delivery_note != null
                             && note_ids.Contains(c.id_delivery_note.Value)
                             && c.status == "Anulada")
                    .Select(c => c.note_number)
                    .ToListAsync();

                var payment_query = db_context.payments
                    .AsNoTracking()
                    .Where(p => p.id_delivery_note != null && note_ids.Contains(p.id_delivery_note.Value));

                if (annulled_credit_numbers.Count > 0)
                {
                    var anuladas = annulled_credit_numbers;
                    payment_query = payment_query.Where(p => p.payment_type != "NOTA DE CREDITO"
                                                       || p.reference_number == null
                                                       || !anuladas.Contains(p.reference_number));
                }

                var payment_totals = await payment_query
                    .GroupBy(p => p.id_delivery_note!.Value)
                    .Select(g => new { Id = g.Key, Total = g.Sum(p => p.amount_usd) })
                    .ToDictionaryAsync(x => x.Id, x => x.Total);

                var results = new List<accounts_receivable_dto>();
                foreach (var dn in notes)
                {
                    decimal paid = payment_totals.TryGetValue(dn.id_delivery_note, out var total) ? total : 0;
                    customers.TryGetValue(dn.id_customer, out string? customer_name);

                    string type_name = string.Empty;
                    if (dn.note_type_id != null && note_types.TryGetValue(dn.note_type_id.Value, out var tn))
                    {
                        type_name = tn;
                    }
                    else if (dn.promo_discount_percentage != null && dn.promo_discount_percentage > 0)
                    {
                        type_name = "Promocion";
                    }
                    else if (!string.IsNullOrWhiteSpace(dn.sales_observations))
                    {
                        type_name = dn.sales_observations;
                    }
                    else
                    {
                        type_name = "General";
                    }

                    // Una nota totalmente pagada no admite nota de credito: no queda saldo por
                    // devolver. Se filtra por el saldo real (adjusted_total - pagado) y no solo por
                    // el status, porque el status es un valor cacheado que puede quedar desfasado
                    // si se edito la nota o se registro un pago despues.
                    decimal balance_due = dn.adjusted_total_usd - paid;

                    if (balance_due <= 0) continue;

                    results.Add(new accounts_receivable_dto
                    {
                        id_delivery_note = dn.id_delivery_note,
                        note_number = dn.note_number,
                        customer_name = customer_name ?? string.Empty,
                        id_seller = dn.id_seller,
                        seller_name = seller_name,
                        creation_date = dn.creation_date,
                        dispatch_date = dn.dispatch_date,
                        total_amount_usd = dn.adjusted_total_usd,
                        status = dn.status,
                        paid_amount_usd = paid,
                        balance_due_usd = balance_due,
                        note_type_name = type_name,
                        sales_observations = type_name
                    });
                }

                return results.OrderByCorrelative(r => r.note_number).ToList();
            }
        }

        public async Task<IEnumerable<string>> get_credit_note_months_async()
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

// Se convierte a hora de Venezuela antes de tomar el anio y el mes. Sin esto el combo
                // ofrece un mes distinto del que realmente usa el filtro por mes, y una nota
                // creada de noche queda en un mes que el combo no lista.
                double local_offset_hours = AppTimeZone.offset_hours;

                var months = await db_context.credit_notes
                    .AsNoTracking()
                    .Select(c => new
                    {
                        c.creation_date.AddHours(-local_offset_hours).Year,
                        c.creation_date.AddHours(-local_offset_hours).Month
                    })
                    .Distinct()
                    .OrderBy(c => c.Year)
                    .ThenBy(c => c.Month)
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
                // Rango en UTC en vez de Year/Month: la columna guarda UTC y una NC emitida de
                // noche ya cae en el mes siguiente al guardarse, con Year/Month se iba al mes
                // equivocado. El rango va desde el primer dia del mes local hasta el siguiente.
                var month_start = new DateTime(target_date.Value.Year, target_date.Value.Month, 1, 0, 0, 0, DateTimeKind.Local);
                var month_end = month_start.AddMonths(1);

                query = query.Where(c => c.creation_date >= month_start.ToUniversalTime()
                                      && c.creation_date < month_end.ToUniversalTime());
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
            var zonas = await db_context.zonas
                .AsNoTracking()
                .ToDictionaryAsync(z => z.id_zona, z => z.name);

            // La columna TIPO NOTA de la NC se hereda de la nota de entrega que se esta revirtiendo:
            // una devolucion no tiene tipo propio, hereda el de la nota que la origina (general,
            // promocion, pro venta). Asi se puede leer de un vistazo si la devolucion viene de una
            // nota de promocion, que es el caso que cambia como se revierte el dinero.
            var note_types = await db_context.note_types
                .AsNoTracking()
                .ToDictionaryAsync(t => t.id_note_type, t => t.name);

            return notes
                .OrderByCorrelative(c => c.note_number)
                .Select(c =>
                {
                    string type_name = string.Empty;
                    if (c.id_delivery_note.HasValue
                        && originals.TryGetValue(c.id_delivery_note.Value, out var source_note)
                        && source_note.note_type_id != null
                        && note_types.TryGetValue(source_note.note_type_id.Value, out var resolved))
                    {
                        type_name = resolved;
                    }

                    // El obsequio no cuelga de ninguna nota de entrega, asi que no hereda tipo.
                    if (string.IsNullOrWhiteSpace(type_name) && string.Equals(c.category, "Obsequio", StringComparison.OrdinalIgnoreCase))
                    {
                        type_name = "Obsequio";
                    }

                    // Las notas de entrega anteriores a que existiera note_type_id lo tienen en null,
                    // asi que no hay tipo que heredar y la columna quedaba vacia. Esas notas son
                    // General por definicion, asi que se cae ahi en vez de dejar un hueco.
                    if (string.IsNullOrWhiteSpace(type_name) && c.id_delivery_note.HasValue)
                    {
                        type_name = "General";
                    }

                    bool isGift = string.Equals(c.category, "Obsequio", StringComparison.OrdinalIgnoreCase);
                    customers.TryGetValue(c.id_customer, out var cu);
                    int? zona_id = cu?.id_zona;
                    string zona_name = zona_id.HasValue && zonas.TryGetValue(zona_id.Value, out var zn) ? zn : string.Empty;

                    return new credit_note_dto
                    {
                        id_credit_note = c.id_credit_note,
                        note_number = c.note_number,
                        id_delivery_note = c.id_delivery_note ?? 0,
                        source_note_number = c.id_delivery_note.HasValue && originals.TryGetValue(c.id_delivery_note.Value, out var o) ? o.note_number : string.Empty,
                        customer_name = cu?.business_name ?? string.Empty,
                        id_seller = c.id_seller,
                        seller_name = isGift ? "-" : (sellers.TryGetValue(c.id_seller, out var se) ? se.full_name : string.Empty),
                        id_zona = zona_id,
                        zone_name = zona_name,
                        creation_date = c.creation_date,
                        total_amount_usd = c.total_amount_usd,
                        status = c.status,
                        category = c.category,
                        note_type_name = type_name
                    };
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

                // Descuento que la nota le aplico al cliente. En CxC se trabaja con un unico DCTO
                // (condicion + volumen ya compactados en discount_percentage), pero se suman los dos
                // por si alguna nota antigua todavia tiene el volumen separado.
                decimal note_discount = (note.discount_percentage ?? 0m) + (note.volume_discount_percentage ?? 0m);
                if (note_discount < 0m) note_discount = 0m;
                if (note_discount > 100m) note_discount = 100m;
                decimal net_factor = resolve_net_factor(note_discount);

                // Tipo de la nota de origen (General, Promocion, Pro Venta...). La NC lo hereda
                // para que se vea de que tipo de nota vino la devolucion.
                string source_note_type = note.note_type_id == null
                    ? string.Empty
                    : await db_context.note_types
                        .AsNoTracking()
                        .Where(t => t.id_note_type == note.note_type_id.Value)
                        .Select(t => t.name)
                        .FirstOrDefaultAsync() ?? string.Empty;

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
                    discount_percentage = note_discount,
                    note_type = source_note_type,
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
                            // Se imprime el codigo congelado al emitir la nota; el codigo
                            // actual solo se usa en renglones viejos sin snapshot.
                            code = d.product_code_snapshot ?? prod.product_code;
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
                            net_unit_price_usd = resolve_net_unit_price(d, net_factor),
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
                            // Codigo congelado al emitir la nota; el actual es solo el respaldo
                            // para los renglones anteriores a esta columna.
                            code = d.product_code_snapshot ?? prod.product_code;
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
                            net_unit_price_usd = resolve_net_unit_price(d, net_factor),
                            delivered_quantity = d.quantity,
                            already_returned_quantity = 0,
                            remaining_quantity = d.quantity
                        });
                    }
                }

                return source_dto;
            }
        }

        /// <summary>
        /// Precio unitario NETO de un renglon de la nota de entrega: lo que el cliente realmente
        /// pago por esa unidad, con el descuento de la nota ya descontado.
        ///
        /// Se parte de subtotal_usd y no de unit_price_usd a proposito. En una nota de promocion
        /// el subtotal ya viene con el precio de promocion resuelto, asi que usar el unit_price
        /// (precio de lista) devolveria dinero de mas. Encima se aplica el descuento de cabecera,
        /// que es donde vive el descuento del cliente.
        ///
        /// El factor llega ya recortado al rango 0..1 por quien lo arma.
        /// </summary>
        private static decimal resolve_net_unit_price(note_detail detail, decimal net_factor)
        {
            decimal line_net = detail.quantity > 0
                ? detail.subtotal_usd / detail.quantity
                : detail.unit_price_usd;

            return Money.round(line_net * net_factor);
        }

        /// <summary>
        /// Factor 1 - descuento/100 de la nota de entrega, acotado a un rango util. Centraliza el
        /// calculo para que la pantalla y el guardado no puedan discrepar entre si.
        /// </summary>
        private static decimal resolve_net_factor(decimal discount_percentage)
        {
            if (discount_percentage < 0m) discount_percentage = 0m;
            if (discount_percentage > 100m) discount_percentage = 100m;
            return 1m - discount_percentage / 100m;
        }

        /// <summary>
        /// Recorta las lineas de una devolucion para que el total no pase del saldo disponible.
        ///
        /// No se toca la cantidad devuelta: el cliente devuelve los articulos que devolvio, lo que
        /// se ajusta es cuanto dinero se acredita por cada uno. Se reparte el mismo factor a todas
        /// las lineas para que el recorte caiga parejo y no castigue a un solo renglon.
        ///
        /// El redondeo no siempre cuadra al centimo. En vez de ajustar linea por linea y terminar
        /// con un total descuadrado, el residuo se absorbe entero en la ultima linea con cantidad,
        /// que es la que ya venia movida por el redondeo. Asi la suma final es exactamente el saldo.
        /// </summary>
        private static void apply_available_cap(List<credit_note_detail> details, decimal available_usd)
        {
            if (details == null || details.Count == 0) return;

            if (available_usd <= 0m)
            {
                foreach (var detail in details)
                {
                    detail.unit_price_usd = 0m;
                    detail.subtotal_usd = 0m;
                }
                return;
            }

            var weighted = details
                .Select((detail, index) => (detail, index, weight: detail.subtotal_usd))
                .Where(x => x.weight > 0m && x.detail.quantity > 0)
                .ToList();

            if (weighted.Count == 0) return;

            decimal total = weighted.Sum(x => x.weight);
            if (total <= 0m) return;

            decimal factor = available_usd / total;

            foreach (var item in weighted)
            {
                item.detail.unit_price_usd = Money.round(item.detail.unit_price_usd * factor);
                item.detail.subtotal_usd = Money.round(item.detail.unit_price_usd * item.detail.quantity);
            }

            decimal residue = Money.round(available_usd - details.Sum(d => d.subtotal_usd));
            if (residue == 0m) return;

            var absorber = weighted.OrderByDescending(x => x.index).First();
            absorber.detail.subtotal_usd = Money.round(absorber.detail.subtotal_usd + residue);
            if (absorber.detail.quantity > 0)
            {
                absorber.detail.unit_price_usd = Money.round(absorber.detail.subtotal_usd / absorber.detail.quantity);
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

                        if (new_note.id_seller <= 0)
                        {
                            var adminSeller = await db_context.sellers
                                .FirstOrDefaultAsync(s => s.seller_code == "001" || s.full_name.Contains("Sandra"));
                            if (adminSeller != null)
                            {
                                new_note.id_seller = adminSeller.id_seller;
                            }
                        }

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
                        if (original_note.status == "Devuelta") throw new InvalidOperationException("Esta nota de entrega ya fue devuelta en su totalidad.");

                        // Una nota de entrega totalmente pagada no admite nota de credito: no queda
                        // saldo por devolver. Se valida aca para que no se pueda saltar saltandose
                        // el buscador, ya que el saldo pudo cambiar despues de armar la lista.
                        // El saldo sale de la suma de pagos y notas de credito aplicadas. Las NC
                        // anuladas se excluyen porque ya no devuelven nada.
                        var annulled_numbers_of_note = await db_context.credit_notes
                            .AsNoTracking()
                            .Where(c => c.id_delivery_note == original_note.id_delivery_note && c.status == "Anulada")
                            .Select(c => c.note_number)
                            .ToListAsync();

                        var note_payments = await db_context.payments
                            .AsNoTracking()
                            .Where(p => p.id_delivery_note == original_note.id_delivery_note)
                            .ToListAsync();

                        decimal balance_of_note = original_note.adjusted_total_usd;

                        foreach (var p in note_payments)
                        {
                            bool is_annulled_credit = p.payment_type == "NOTA DE CREDITO"
                                                      && p.reference_number != null
                                                      && annulled_numbers_of_note.Contains(p.reference_number);

                            if (!is_annulled_credit) balance_of_note -= p.amount_usd;
                        }

                        if (balance_of_note <= 0)
                            throw new InvalidOperationException("La nota de entrega no tiene saldo pendiente por devolver.");
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
                                }
                            }
                        }

                        // El importe de la nota de credito se RECALCULA aqui, sin confiar en lo que
                        // mando la pantalla, y se recorta al saldo que le queda a la nota.
                        // Nada de esto bloquea al usuario: si hay que recortar, se recorta y queda
                        // registrado en el log y en las observaciones de la NC.
                        decimal applied_net_factor = resolve_net_factor(
                            (original_note.discount_percentage ?? 0m) + (original_note.volume_discount_percentage ?? 0m));

                        foreach (var detail in detail_list)
                        {
                            var original_detail = original_map[(detail.id_product, detail.id_promotion)];
                            decimal net_unit = resolve_net_unit_price(original_detail, applied_net_factor);

                            detail.unit_price_usd = net_unit;
                            detail.subtotal_usd = Money.round(net_unit * detail.quantity);
                        }

                        decimal computed_total = Money.round(detail_list.Sum(d => d.subtotal_usd));

                        decimal already_returned_usd = existing_credit_ids.Count == 0
                            ? 0m
                            : await db_context.credit_notes
                                .AsNoTracking()
                                .Where(c => existing_credit_ids.Contains(c.id_credit_note))
                                .SumAsync(c => (decimal?)c.total_amount_usd) ?? 0m;

                        decimal available_usd = Math.Max(original_note.adjusted_total_usd - already_returned_usd, 0m);

                        if (computed_total > available_usd + 0.005m)
                        {
                            apply_available_cap(detail_list, available_usd);

                            AppLog.Warn(
                                $"Nota de credito sobre {original_note.note_number}: el importe calculado " +
                                $"({computed_total:N2}) supera el saldo disponible ({available_usd:N2}) " +
                                $"y se recortó en {detail_list.Count} linea(s).");

                            string adjustment_note =
                                $"Ajuste por saldo disponible de la nota {original_note.note_number}: " +
                                $"{computed_total:N2} -> {available_usd:N2} USD.";

                            new_note.observations = string.IsNullOrWhiteSpace(new_note.observations)
                                ? adjustment_note
                                : new_note.observations.Trim() + " " + adjustment_note;
                        }

                        new_note.total_amount_usd = Money.round(detail_list.Sum(d => d.subtotal_usd));
                    }

                    // El correlativo es por vendedor e independiente del numero de la nota de entrega.
                    if (string.IsNullOrWhiteSpace(new_note.note_number))
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<ICreditNoteRepository>();
                        new_note.note_number = await repository.get_next_credit_correlative_async();
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

                    // Kardex: el obsequio descuenta stock (SALIDA) y la devolucion lo repone (ENTRADA).
                    // Se registra aqui para que el movimiento quede ligado a la nota ya insertada.
                    foreach (var detail in detail_list)
                    {
                        if (detail.id_product != null)
                        {
                            var producto = products[detail.id_product.Value];

                            // Codigo congelado al emitir la nota de credito, por si el
                            // producto despues cambia de marca y le reasignan otro.
                            detail.product_code_snapshot = producto.product_code;

                            if (is_gift)
                            {
                                stock_movement_writer.registrar_salida(
                                    db_context, producto, detail.quantity,
                                    stock_movement.RazonObsequio, stock_movement.DocumentoCredito, new_note.note_number,
                                    new_note.creation_date, detail.unit_price_usd,
                                    estado_documento: new_note.status,
                                    id_entrega: original_note?.id_delivery_note,
                                    id_credito: new_note.id_credit_note,
                                    id_vendedor: new_note.id_seller,
                                    id_cliente: new_note.id_customer,
                                    vendido_como: stock_movement_writer.VendidoProducto);
                            }
                            else
                            {
                                stock_movement_writer.registrar_entrada(
                                    db_context, producto, detail.quantity,
                                    stock_movement.RazonDevolucion, stock_movement.DocumentoCredito, new_note.note_number,
                                    new_note.creation_date, detail.unit_price_usd,
                                    estado_documento: new_note.status,
                                    id_entrega: original_note?.id_delivery_note,
                                    id_credito: new_note.id_credit_note,
                                    id_vendedor: new_note.id_seller,
                                    id_cliente: new_note.id_customer,
                                    vendido_como: stock_movement_writer.VendidoProducto);
                            }
                        }
                        else if (detail.id_promotion != null)
                        {
                            var promocion = promotions[detail.id_promotion.Value];
                            decimal precio_por_unidad = detail.quantity > 0 ? detail.unit_price_usd / detail.quantity : 0m;

                            foreach (var promo_item in promocion.items!)
                            {
                                var promo_producto = products[promo_item.id_product];
                                int cantidad = detail.quantity * promo_item.quantity_required;

                                if (is_gift)
                                {
                                    stock_movement_writer.registrar_salida(
                                        db_context, promo_producto, cantidad,
                                        stock_movement.RazonObsequio, stock_movement.DocumentoCredito, new_note.note_number,
                                        new_note.creation_date, precio_por_unidad,
                                        estado_documento: new_note.status,
                                        id_entrega: original_note?.id_delivery_note,
                                        id_credito: new_note.id_credit_note,
                                        id_vendedor: new_note.id_seller,
                                        id_cliente: new_note.id_customer,
                                        id_promocion: detail.id_promotion,
                                        unidades_promocion: detail.quantity,
                                        vendido_como: stock_movement_writer.VendidoPromocion,
                                        descripcion_linea: promocion.name);
                                }
                                else
                                {
                                    stock_movement_writer.registrar_entrada(
                                        db_context, promo_producto, cantidad,
                                        stock_movement.RazonDevolucion, stock_movement.DocumentoCredito, new_note.note_number,
                                        new_note.creation_date, precio_por_unidad,
                                        estado_documento: new_note.status,
                                        id_entrega: original_note?.id_delivery_note,
                                        id_credito: new_note.id_credit_note,
                                        id_vendedor: new_note.id_seller,
                                        id_cliente: new_note.id_customer,
                                        id_promocion: detail.id_promotion,
                                        unidades_promocion: detail.quantity,
                                        vendido_como: stock_movement_writer.VendidoPromocion,
                                        descripcion_linea: promocion.name);
                                }
                            }
                        }
                    }

                    // La devolucion se registra como abono para que compense la deuda en el historial y en el saldo;
                    // el obsequio no es deuda y solo afecta el inventario.
                    if (!is_gift && original_note != null)
                    {
                        string pay_observations = $"Nota de credito {new_note.note_number} - {new_note.total_amount_usd:N2}";
                        if (!string.IsNullOrWhiteSpace(new_note.observations))
                            pay_observations += $". {new_note.observations}";

                        payment nc_payment = new payment(
                            original_note.id_delivery_note,
                            new_note.creation_date,
                            new_note.total_amount_usd,
                            0m,
                            null,
                            "NOTA DE CREDITO",
                            new_note.note_number,
                            string.Empty,
                            pay_observations,
                            original_note.id_relacion);

                        await db_context.payments.AddAsync(nc_payment);

                        // Comprobar si se devolvieron todos los renglones de la nota de entrega
                        var all_original_details = await db_context.note_details
                            .AsNoTracking()
                            .Where(d => d.id_delivery_note == original_note.id_delivery_note)
                            .ToListAsync();

                        var all_credit_ids = await db_context.credit_notes
                            .AsNoTracking()
                            .Where(c => c.id_delivery_note == original_note.id_delivery_note && c.id_credit_note != new_note.id_credit_note)
                            .Select(c => c.id_credit_note)
                            .ToListAsync();

                        var past_returns = all_credit_ids.Count == 0
                            ? new List<credit_note_detail>()
                            : await db_context.credit_note_details
                                .AsNoTracking()
                                .Where(d => all_credit_ids.Contains(d.id_credit_note))
                                .ToListAsync();

                        bool is_fully_returned = all_original_details.Count > 0 && all_original_details.All(od =>
                        {
                            int already = past_returns
                                .Where(r => r.id_product == od.id_product && r.id_promotion == od.id_promotion)
                                .Sum(r => r.quantity);
                            int in_this_nc = detail_list
                                .Where(d => d.id_product == od.id_product && d.id_promotion == od.id_promotion)
                                .Sum(d => d.quantity);
                            return (already + in_this_nc) >= od.quantity;
                        });

                        if (is_fully_returned)
                        {
                            original_note.status = "Devuelta";

                            // Si existia comision pendiente de liquidar generada previamente, se cancela/elimina
                            var existing_comm = await db_context.commissions
                                .FirstOrDefaultAsync(c => c.id_delivery_note == original_note.id_delivery_note);
                            if (existing_comm != null && !existing_comm.is_paid)
                            {
                                db_context.commissions.Remove(existing_comm);
                            }
                        }
                        else
                        {
                            // Devolución parcial: comprobar si el saldo restante ya quedo cubierto
                            decimal existing_paid = await db_context.payments
                                .AsNoTracking()
                                .Where(p => p.id_delivery_note == original_note.id_delivery_note)
                                .SumAsync(p => (decimal?)p.amount_usd) ?? 0;

                            decimal total_paid_after = existing_paid + nc_payment.amount_usd;
                            if (total_paid_after >= original_note.adjusted_total_usd)
                            {
                                original_note.status = "Pagada";
                            }
                            else
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

        /// <summary>
        /// Anula una nota de credito, de devolucion o de obsequio. Hace lo contrario de lo que
        /// hizo al crearse, en una sola transaccion:
        ///   - Devolucion: la nota habia ingresado stock y habia descontado un abono de la nota
        ///     de entrega. Se saca el stock y se devuelve el abono.
        ///   - Obsequio: solo habia sacado stock del inventario. Se devuelve ese stock.
        /// La nota no se borra: queda con status "Anulada", asi el historial sigue mostrandola.
        /// </summary>
        public async Task annul_credit_note_async(int id_credit_note)
        {
            using var scope = _scope_factory.CreateScope();
            var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var nota = await db_context.credit_notes
                .FirstOrDefaultAsync(c => c.id_credit_note == id_credit_note);

            if (nota == null) throw new ArgumentException("La nota de credito ya no existe.");

            if (string.Equals(nota.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La nota de credito ya esta anulada.");

            bool es_obsequio = string.Equals(nota.category?.Trim(), "Obsequio", StringComparison.OrdinalIgnoreCase);

            var detalles = await db_context.credit_note_details
                .Where(d => d.id_credit_note == id_credit_note)
                .ToListAsync();

            var product_ids = new HashSet<int>();
            var promocion_ids = new HashSet<int>();

            foreach (var d in detalles)
            {
                if (d.id_product != null) product_ids.Add(d.id_product.Value);
                if (d.id_promotion != null) promocion_ids.Add(d.id_promotion.Value);
            }

            var products = await db_context.products
                .Where(p => product_ids.Contains(p.id_product))
                .ToDictionaryAsync(p => p.id_product);

            var promotions = await db_context.promotions
                .Include(p => p.items)
                .Where(p => promocion_ids.Contains(p.id_promotion))
                .ToDictionaryAsync(p => p.id_promotion);

            var original_note = nota.id_delivery_note != null
                ? await db_context.delivery_notes.FirstOrDefaultAsync(n => n.id_delivery_note == nota.id_delivery_note)
                : null;

            await using var transaction = await db_context.Database.BeginTransactionAsync();
            try
            {
                foreach (var detalle in detalles)
                {
                    // Al crear, el obsequio SACO stock (registrar_salida) y la devolucion lo
                    // devolvio (registrar_entrada). Anular es exactamente lo contrario.
                    if (detalle.id_product != null && products.TryGetValue(detalle.id_product.Value, out var producto))
                    {
                        if (es_obsequio)
                        {
                            stock_movement_writer.registrar_entrada(
                                db_context, producto, detalle.quantity,
                                stock_movement.RazonAnulacion, stock_movement.DocumentoCredito, nota.note_number,
                                DateTime.UtcNow, detalle.unit_price_usd,
                                estado_documento: "Anulada",
                                id_entrega: original_note?.id_delivery_note,
                                id_credito: nota.id_credit_note,
                                id_vendedor: nota.id_seller,
                                id_cliente: nota.id_customer,
                                vendido_como: stock_movement_writer.VendidoProducto);
                        }
                        else
                        {
                            stock_movement_writer.registrar_salida(
                                db_context, producto, detalle.quantity,
                                stock_movement.RazonAnulacion, stock_movement.DocumentoCredito, nota.note_number,
                                DateTime.UtcNow, detalle.unit_price_usd,
                                estado_documento: "Anulada",
                                id_entrega: original_note?.id_delivery_note,
                                id_credito: nota.id_credit_note,
                                id_vendedor: nota.id_seller,
                                id_cliente: nota.id_customer,
                                vendido_como: stock_movement_writer.VendidoProducto);
                        }
                    }
                    else if (detalle.id_promotion != null && promotions.TryGetValue(detalle.id_promotion.Value, out var promocion))
                    {
                        if (promocion.items == null) continue;

                        decimal precio_por_unidad = detalle.quantity > 0 ? detalle.unit_price_usd / detalle.quantity : 0m;

                        foreach (var promo_item in promocion.items)
                        {
                            if (!products.TryGetValue(promo_item.id_product, out var promo_producto)) continue;

                            int cantidad = detalle.quantity * promo_item.quantity_required;

                            if (es_obsequio)
                            {
                                stock_movement_writer.registrar_entrada(
                                    db_context, promo_producto, cantidad,
                                    stock_movement.RazonAnulacion, stock_movement.DocumentoCredito, nota.note_number,
                                    DateTime.UtcNow, precio_por_unidad,
                                    estado_documento: "Anulada",
                                    id_entrega: original_note?.id_delivery_note,
                                    id_credito: nota.id_credit_note,
                                    id_vendedor: nota.id_seller,
                                    id_cliente: nota.id_customer,
                                    id_promocion: detalle.id_promotion,
                                    unidades_promocion: detalle.quantity,
                                    vendido_como: stock_movement_writer.VendidoPromocion,
                                    descripcion_linea: promocion.name);
                            }
                            else
                            {
                                stock_movement_writer.registrar_salida(
                                    db_context, promo_producto, cantidad,
                                    stock_movement.RazonAnulacion, stock_movement.DocumentoCredito, nota.note_number,
                                    DateTime.UtcNow, precio_por_unidad,
                                    estado_documento: "Anulada",
                                    id_entrega: original_note?.id_delivery_note,
                                    id_credito: nota.id_credit_note,
                                    id_vendedor: nota.id_seller,
                                    id_cliente: nota.id_customer,
                                    id_promocion: detalle.id_promotion,
                                    unidades_promocion: detalle.quantity,
                                    vendido_como: stock_movement_writer.VendidoPromocion,
                                    descripcion_linea: promocion.name);
                            }
                        }
                    }
                }

                // La devolucion habia descontado un abono (pago negativo) de la nota de entrega.
                // Al anular hay que devolverlo, o el saldo de la nota queda inflado a menor.
                if (!es_obsequio && original_note != null)
                {
                    var abono = new payment(
                        original_note.id_delivery_note,
                        DateTime.UtcNow,
                        nota.total_amount_usd,
                        0m,
                        null,
                        payment_dto.AnulacionPaymentType,
                        nota.note_number,
                        string.Empty,
                        $"Anulacion de la nota de credito {nota.note_number}",
                        original_note.id_relacion);

                    db_context.payments.Add(abono);

                    // El estado de la nota de entrega se recalcula: sin la devolucion, la nota
                    // vuelve a Pendiente o queda Pagada segun lo que ya se le haya abonado.
                    decimal pagado = await db_context.payments
                        .Where(p => p.id_delivery_note == original_note.id_delivery_note)
                        .SumAsync(p => (decimal?)p.amount_usd) ?? 0;

                    if (pagado >= original_note.adjusted_total_usd)
                        original_note.status = "Pagada";
                    else
                        original_note.status = "Pendiente";
                }

                nota.status = "Anulada";

                await db_context.SaveChangesAsync();
                await transaction.CommitAsync();

                AppLog.Info($"Nota de credito {nota.note_number} anulada. Categoria: {nota.category}. "
                            + $"Stock revertido: {detalles.Count} linea(s).");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
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
                            code = d.product_code_snapshot ?? prod.product_code;
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

                // El comparativo del periodo anterior tambien va sin las anuladas: si el periodo viejo las
                // suma y el nuevo no, la variacion que muestra el PDF seria falsa.
                var previous_vigentes = previous_notes
                    .Where(c => !string.Equals(c.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var report = new credit_note_report_dto
                {
                    from_date = from_date.Date,
                    to_date = to_date.Date,
                    category_label = string.IsNullOrWhiteSpace(category) ? "Todas" : category.Trim(),
                    seller_label = "Todos",
                    period_label = build_period_label(from_date, to_date),
                    previous_period_label = build_period_label(previous_start, previous_end_exclusive.AddDays(-1)),
                    previous_total_usd = previous_vigentes.Sum(c => c.total_amount_usd),
                    previous_total_notes = previous_vigentes.Count
                };

                if (id_seller != null)
                {
                    var seller = await db_context.sellers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.id_seller == id_seller.Value);
                    report.seller_label = seller?.full_name ?? "Todos";
                }

                // Las anuladas van aparte: NO entran en el total ni en el promedio del reporte, asi que se
                // calculan sobre las vigentes. Antes total_usd las sumaba y el encabezado del
                // PDF mostraba un monto que no era el real.
                var vigentes = rows.Where(r => !r.esta_anulada).ToList();
                var anuladas = rows.Where(r => r.esta_anulada).ToList();

                report.total_notes = vigentes.Count;
                report.total_usd = vigentes.Sum(r => r.total_amount_usd);
                report.gift_notes = vigentes.Count(r => r.es_obsequio);
                report.gift_usd = vigentes.Where(r => r.es_obsequio).Sum(r => r.total_amount_usd);
                report.return_notes = report.total_notes - report.gift_notes;
                report.return_usd = report.total_usd - report.gift_usd;
                report.voided_notes = anuladas.Count;
                report.voided_usd = anuladas.Sum(r => r.total_amount_usd);
                report.affected_customers = vigentes
                    .Select(r => r.customer_name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .Count();
                report.average_note_usd = report.total_notes == 0 ? 0 : report.total_usd / report.total_notes;

                report.rows = rows
                    .OrderByCorrelative(r => r.note_number)
                    .ToList();

                report.by_seller = build_seller_bars(vigentes);

                // Las barras por dia solo tienen sentido dentro de un mes natural: en un rango
                // de meses o anios el grafico seria ilegible, asi que se omite.
                bool is_single_month = from_date.Year == to_date.Year && from_date.Month == to_date.Month;
                if (is_single_month) report.by_day = build_day_bars(vigentes, DateTime.DaysInMonth(from_date.Year, from_date.Month));

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
            var zonas = await db_context.zonas
                .AsNoTracking()
                .ToDictionaryAsync(z => z.id_zona, z => z.name);

            return notes
                .Select(c =>
                {
                    customers.TryGetValue(c.id_customer, out var cu);
                    int? zona_id = cu?.id_zona;
                    string zona_name = zona_id.HasValue && zonas.TryGetValue(zona_id.Value, out var zn) ? zn : string.Empty;

                    return new credit_note_report_row_dto
                    {
                        id_credit_note = c.id_credit_note,
                        note_number = c.note_number,
                        source_note_number = c.id_delivery_note.HasValue && originals.TryGetValue(c.id_delivery_note.Value, out var o) ? o.note_number : string.Empty,
                        category = c.category,
                        customer_name = cu?.business_name ?? string.Empty,
                        id_seller = c.id_seller,
                        seller_name = sellers.TryGetValue(c.id_seller, out var se) ? se.full_name : string.Empty,
                        id_zona = zona_id,
                        zone_name = zona_name,
                        status = c.status,
                        creation_date = c.creation_date,
                        total_amount_usd = c.total_amount_usd
                    };
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