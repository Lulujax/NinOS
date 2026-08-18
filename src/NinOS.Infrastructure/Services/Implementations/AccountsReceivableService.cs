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
                
                var pending_notes = await db_context.delivery_notes
                    .AsNoTracking()
                    .Where(n => n.status == "Pendiente")
                    .Select(n => new { n.creation_date.Year, n.creation_date.Month })
                    .Distinct()
                    .OrderByDescending(n => n.Year)
                    .ThenByDescending(n => n.Month)
                    .ToListAsync();

                return pending_notes
                    .Select(n => new DateTime(n.Year, n.Month, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-VE")))
                    .ToList();
            }
        }

        public async Task<IEnumerable<accounts_receivable_dto>> get_receivables_by_month_async(string month_year)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                
                var target_date = DateTime.ParseExact(month_year, "MMMM yyyy", new System.Globalization.CultureInfo("es-VE"));
                
                var notes = await db_context.delivery_notes
                    .AsNoTracking()
                    .Where(dn => dn.status == "Pendiente"
                              && dn.creation_date.Year == target_date.Year
                              && dn.creation_date.Month == target_date.Month)
                    .ToListAsync();

                if (notes.Count == 0)
                    return Enumerable.Empty<accounts_receivable_dto>();

                var note_ids = notes.Select(n => n.id_delivery_note).ToList();

                var customer_ids = notes.Select(n => n.id_customer).Distinct().ToList();
                var customers = await db_context.customers
                    .AsNoTracking()
                    .Where(c => customer_ids.Contains(c.id_customer))
                    .ToDictionaryAsync(c => c.id_customer, c => c.business_name);

                var payment_totals = await db_context.payments
                    .AsNoTracking()
                    .Where(p => note_ids.Contains(p.id_delivery_note))
                    .GroupBy(p => p.id_delivery_note)
                    .Select(g => new { Id = g.Key, Total = g.Sum(p => p.amount_usd) })
                    .ToDictionaryAsync(x => x.Id, x => x.Total);

                var result = new List<accounts_receivable_dto>();
                foreach (var dn in notes)
                {
                    decimal paid = payment_totals.TryGetValue(dn.id_delivery_note, out var total) ? total : 0;
                    customers.TryGetValue(dn.id_customer, out string? customer_name);
                    result.Add(new accounts_receivable_dto
                    {
                        id_delivery_note = dn.id_delivery_note,
                        note_number = dn.note_number,
                        customer_name = customer_name ?? string.Empty,
                        creation_date = dn.creation_date,
                        total_amount_usd = dn.total_amount_usd,
                        status = dn.status,
                        paid_amount_usd = paid,
                        balance_due_usd = dn.total_amount_usd - paid
                    });
                }

                return result;
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

                    details.Add(new note_detail_print_dto
                    {
                        code = code,
                        name = name,
                        quantity = d.quantity,
                        unit_price_usd = d.unit_price_usd,
                        promo_price_usd = d.subtotal_usd / d.quantity,
                        subtotal_usd = d.subtotal_usd
                    });
                }

                return new note_print_dto
                {
                    id_delivery_note = note.id_delivery_note,
                    note_number = note.note_number,
                    creation_date = note.creation_date,
                    due_date = note.creation_date.AddDays(15),
                    status = note.status,
                    gross_total_usd = details.Sum(d => d.subtotal_usd),
                    discount_percentage = 0,
                    discount_amount = 0,
                    total_amount_usd = note.total_amount_usd,
                    paid_amount_usd = paid,
                    balance_due_usd = note.total_amount_usd - paid,
                    seller_name = seller?.full_name ?? string.Empty,
                    customer_code = customer?.customer_code ?? string.Empty,
                    customer_business_name = customer?.business_name ?? string.Empty,
                    customer_rif = customer?.rif ?? string.Empty,
                    customer_phone = customer?.phone_number ?? string.Empty,
                    customer_delivery_address = customer?.delivery_address ?? string.Empty,
                    fiscal_address = customer?.fiscal_address ?? string.Empty,
                    conditions_text = "DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO",
                    discount_conditions_text = "Descuento 10% SOLO\nCONTADO",
                    details = details
                };
            }
        }
    }
}
