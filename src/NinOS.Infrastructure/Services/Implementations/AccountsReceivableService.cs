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
                
                var query = from dn in db_context.delivery_notes
                            join c in db_context.customers on dn.id_customer equals c.id_customer
                            where dn.status == "Pendiente" 
                               && dn.creation_date.Year == target_date.Year
                               && dn.creation_date.Month == target_date.Month
                            select new accounts_receivable_dto
                            {
                                id_delivery_note = dn.id_delivery_note,
                                note_number = dn.note_number,
                                customer_name = c.business_name,
                                creation_date = dn.creation_date,
                                total_amount_usd = dn.total_amount_usd,
                                status = dn.status,
                                paid_amount_usd = db_context.payments
                                    .Where(p => p.id_delivery_note == dn.id_delivery_note)
                                    .Sum(p => (decimal?)p.amount_usd) ?? 0,
                                balance_due_usd = dn.total_amount_usd - (db_context.payments
                                    .Where(p => p.id_delivery_note == dn.id_delivery_note)
                                    .Sum(p => (decimal?)p.amount_usd) ?? 0)
                            };

                return await query.ToListAsync();
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

                    if (delivery_note == null) throw new ArgumentException();
                    if (delivery_note.status == "Anulada") throw new InvalidOperationException();

                    var details = await db_context.note_details
                        .Where(d => d.id_delivery_note == id_delivery_note)
                        .ToListAsync();

                    foreach (var detail in details)
                    {
                        if (detail.id_product != null)
                        {
                            var product = await db_context.products.FindAsync(detail.id_product);
                            if (product != null)
                            {
                                product.stock_quantity += detail.quantity;
                                db_context.products.Update(product);
                            }
                        }
                        else if (detail.id_promotion != null)
                        {
                            var promotion = await db_context.promotions
                                .Include(p => p.items)
                                .FirstOrDefaultAsync(p => p.id_promotion == detail.id_promotion);

                            if (promotion != null && promotion.items != null)
                            {
                                foreach (var promo_item in promotion.items)
                                {
                                    var product = await db_context.products.FindAsync(promo_item.id_product);
                                    if (product != null)
                                    {
                                        product.stock_quantity += (detail.quantity * promo_item.quantity_required);
                                        db_context.products.Update(product);
                                    }
                                }
                            }
                        }
                    }

                    delivery_note.status = "Anulada";
                    db_context.delivery_notes.Update(delivery_note);
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

        public async Task add_payment_async(payment new_payment)
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                using var transaction = await db_context.Database.BeginTransactionAsync();
                
                try
                {
                    var delivery_note = await db_context.delivery_notes
                        .FirstOrDefaultAsync(n => n.id_delivery_note == new_payment.id_delivery_note);

                    if (delivery_note == null) throw new ArgumentException();
                    if (delivery_note.status == "Anulada" || delivery_note.status == "Pagada") throw new InvalidOperationException();

                    await db_context.payments.AddAsync(new_payment);
                    await db_context.SaveChangesAsync();

                    var total_paid = await db_context.payments
                        .Where(p => p.id_delivery_note == new_payment.id_delivery_note)
                        .SumAsync(p => p.amount_usd);

                    if (total_paid >= delivery_note.total_amount_usd)
                    {
                        delivery_note.status = "Pagada";
                        db_context.delivery_notes.Update(delivery_note);
                        await db_context.SaveChangesAsync();
                    }

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
}