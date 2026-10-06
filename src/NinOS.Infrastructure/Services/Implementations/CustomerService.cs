using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Common;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class CustomerService : ICustomerService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public CustomerService(IServiceScopeFactory scopeFactory)
        {
            if (scopeFactory == null) throw new ArgumentNullException(nameof(scopeFactory));
            _scopeFactory = scopeFactory;
        }

        public async Task<IEnumerable<customer>> GetAllCustomersAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.customers.AsNoTracking()
                    .Include(c => c.zona)
                    .Where(c => c.is_active)
                    .OrderBy(c => c.customer_code)
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<customer>> GetDeletedCustomersAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.customers.AsNoTracking()
                    .Include(c => c.zona)
                    .Where(c => !c.is_active)
                    .OrderByDescending(c => c.deleted_at)
                    .ToListAsync();
            }
        }

        public async Task<customer?> GetCustomerByIdAsync(int id)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.customers.AsNoTracking()
                    .Include(c => c.zona)
                    .FirstOrDefaultAsync(c => c.id_customer == id);
            }
        }

        public async Task<customer?> GetCustomerByCodeAsync(string code)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.customers.AsNoTracking()
                    .Include(c => c.zona)
                    .FirstOrDefaultAsync(c => c.customer_code == code);
            }
        }

        public async Task<string> GetNextCustomerCodeAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                var all_codes = await db_context.customers.AsNoTracking()
                    .Select(c => c.customer_code)
                    .ToListAsync();
                return SeriesCalculator.GetNextCustomerCode(all_codes);
            }
        }

        public async Task AddCustomerAsync(customer newCustomer)
        {
            if (newCustomer == null) throw new ArgumentNullException(nameof(newCustomer));
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                await db_context.customers.AddAsync(newCustomer);
                await db_context.SaveChangesAsync();
                await UpdateSellerLastCustomerNumberAsync(db_context, newCustomer);
            }
        }

        public async Task UpdateCustomerAsync(customer existingCustomer)
        {
            if (existingCustomer == null) throw new ArgumentNullException(nameof(existingCustomer));
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                var dbCust = await db_context.customers.FirstOrDefaultAsync(c => c.id_customer == existingCustomer.id_customer);
                if (dbCust != null)
                {
                    dbCust.customer_code = existingCustomer.customer_code;
                    dbCust.business_name = existingCustomer.business_name;
                    dbCust.rif = existingCustomer.rif;
                    dbCust.contact_name = existingCustomer.contact_name;
                    dbCust.phone_number = existingCustomer.phone_number;
                    dbCust.fiscal_address = existingCustomer.fiscal_address;
                    dbCust.delivery_address = existingCustomer.delivery_address;
                    dbCust.seller_name = existingCustomer.seller_name;
                    dbCust.id_zona = existingCustomer.id_zona;
                    await db_context.SaveChangesAsync();
                    await UpdateSellerLastCustomerNumberAsync(db_context, dbCust);
                }
            }
        }

        private static async Task UpdateSellerLastCustomerNumberAsync(NinOSDbContext db_context, customer changedCustomer)
        {
            if (string.IsNullOrWhiteSpace(changedCustomer.customer_code)) return;

            long full_number = SeriesCalculator.ParseFullNumber(changedCustomer.customer_code);
            if (full_number <= 0) return;

            seller? target_seller = await db_context.sellers
                .FirstOrDefaultAsync(s => s.full_name == changedCustomer.seller_name);
            if (target_seller == null) return;

            if (full_number <= target_seller.last_customer_number) return;

            target_seller.last_customer_number = full_number;
            await db_context.SaveChangesAsync();
        }

        public async Task SoftDeleteCustomerAsync(int id, string? reason)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                customer? customer_to_delete = await db_context.customers.FirstOrDefaultAsync(c => c.id_customer == id);
                if (customer_to_delete == null || !customer_to_delete.is_active) return;

                customer_to_delete.is_active = false;
                customer_to_delete.deleted_at = DateTime.UtcNow;
                customer_to_delete.deleted_reason = string.IsNullOrWhiteSpace(reason) ? "Sin motivo" : reason.Trim();
                await db_context.SaveChangesAsync();
                AppLog.Info($"Cliente {customer_to_delete.customer_code} enviado a la papelera. Motivo: {customer_to_delete.deleted_reason}");
            }
        }

        public async Task RestoreCustomerAsync(int id)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                customer? customer_to_restore = await db_context.customers.FirstOrDefaultAsync(c => c.id_customer == id);
                if (customer_to_restore == null || customer_to_restore.is_active) return;

                customer_to_restore.is_active = true;
                customer_to_restore.deleted_at = null;
                customer_to_restore.deleted_reason = null;
                await db_context.SaveChangesAsync();
                AppLog.Info($"Cliente {customer_to_restore.customer_code} restaurado desde la papelera.");
            }
        }

        public async Task<CustomerHistoryDataDto?> GetCustomerHistoryAsync(int customerId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                var cust = await db_context.customers.AsNoTracking()
                    .Include(c => c.zona)
                    .FirstOrDefaultAsync(c => c.id_customer == customerId);
                if (cust == null) return null;

                // 1. Delivery notes
                var raw_notes = await db_context.delivery_notes.AsNoTracking()
                    .Include(n => n.seller)
                    .Include(n => n.note_type)
                    .Include(n => n.zona)
                    .Where(n => n.id_customer == customerId)
                    .ToListAsync();

                var notes = raw_notes.OrderByCorrelative(n => n.note_number).OrderByDescending(n => n.creation_date).ToList();
                var note_ids = notes.Select(n => n.id_delivery_note).ToList();

                // 2. Payments
                var payments = new List<payment>();
                if (note_ids.Count > 0)
                {
                    payments = await db_context.payments.AsNoTracking()
                        .Where(p => p.id_delivery_note != null && note_ids.Contains(p.id_delivery_note.Value))
                        .OrderByDescending(p => p.payment_date)
                        .ToListAsync();
                }

                var payments_by_note = payments
                    .Where(p => p.id_delivery_note != null)
                    .GroupBy(p => p.id_delivery_note!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Delivery note DTOs
                var deliveryNoteDtos = new List<accounts_receivable_dto>();
                foreach (var note in notes)
                {
                    decimal paid = 0;
                    if (payments_by_note.TryGetValue(note.id_delivery_note, out var notePayments))
                    {
                        paid = notePayments.Where(p => p.amount_usd > 0).Sum(p => p.amount_usd);
                    }

                    decimal total = note.adjusted_total_usd;
                    decimal balance = total - paid;
                    if (balance < 0) balance = 0;

                    var dto = new accounts_receivable_dto
                    {
                        id_delivery_note = note.id_delivery_note,
                        note_number = note.note_number,
                        customer_name = cust.business_name,
                        id_seller = note.id_seller,
                        seller_name = note.seller?.full_name ?? string.Empty,
                        id_zona = note.id_zona,
                        zone_name = note.zona?.name ?? (note.id_zona.HasValue ? $"Zona {note.id_zona}" : "-"),
                        creation_date = note.creation_date,
                        dispatch_date = note.dispatch_date,
                        total_amount_usd = total,
                        gross_total_usd = note.total_amount_usd,
                        paid_amount_usd = paid,
                        balance_due_usd = balance,
                        status = note.status,
                        sales_observations = note.sales_observations ?? string.Empty,
                        observations = note.observations ?? string.Empty
                    };
                    dto.set_note_type(note.note_type?.name ?? string.Empty, note.note_type?.code ?? string.Empty);
                    deliveryNoteDtos.Add(dto);
                }

                // Payment DTOs
                var notes_dict = notes.ToDictionary(n => n.id_delivery_note);
                var paymentDtos = payments.Select(p =>
                {
                    notes_dict.TryGetValue(p.id_delivery_note ?? 0, out var parentNote);
                    return new payment_dto
                    {
                        id_payment = p.id_payment,
                        id_delivery_note = p.id_delivery_note ?? 0,
                        note_number = parentNote?.note_number ?? "-",
                        customer_name = cust.business_name,
                        seller_name = parentNote?.seller?.full_name ?? "-",
                        payment_date = p.payment_date,
                        created_at = p.payment_date,
                        amount_usd = p.amount_usd,
                        amount_bs = p.amount_bs,
                        exchange_rate = p.exchange_rate,
                        payment_type = p.payment_type ?? string.Empty,
                        bank_name = p.bank_name ?? string.Empty,
                        reference_number = p.reference_number ?? string.Empty,
                        notes = p.observations ?? string.Empty
                    };
                }).OrderByDescending(p => p.payment_date).ToList();

                // 3. Credit notes
                var raw_credit_notes = await db_context.credit_notes.AsNoTracking()
                    .Include(c => c.seller)
                    .Where(c => c.id_customer == customerId)
                    .OrderByDescending(c => c.creation_date)
                    .ToListAsync();

                var delivery_note_numbers = new Dictionary<int, string>();
                var credit_deliv_ids = raw_credit_notes.Where(c => c.id_delivery_note != null).Select(c => c.id_delivery_note!.Value).Distinct().ToList();
                if (credit_deliv_ids.Count > 0)
                {
                    delivery_note_numbers = await db_context.delivery_notes.AsNoTracking()
                        .Where(n => credit_deliv_ids.Contains(n.id_delivery_note))
                        .ToDictionaryAsync(n => n.id_delivery_note, n => n.note_number);
                }

                var creditNoteDtos = raw_credit_notes.Select(c => new credit_note_dto
                {
                    id_credit_note = c.id_credit_note,
                    note_number = c.note_number,
                    source_note_number = c.id_delivery_note.HasValue && delivery_note_numbers.TryGetValue(c.id_delivery_note.Value, out var sn) ? sn : (c.category == "Obsequio" ? "OBSEQUIO" : "-"),
                    id_delivery_note = c.id_delivery_note ?? 0,
                    customer_name = cust.business_name,
                    id_seller = c.id_seller,
                    seller_name = c.seller?.full_name ?? "-",
                    creation_date = c.creation_date,
                    total_amount_usd = c.total_amount_usd,
                    status = c.status,
                    category = c.category
                }).ToList();

                // 4. Top purchased products
                var topProducts = new List<CustomerHistoryProductDto>();
                if (note_ids.Count > 0)
                {
                    var details = await db_context.note_details.AsNoTracking()
                        .Include(d => d.product)
                        .Where(d => note_ids.Contains(d.id_delivery_note))
                        .ToListAsync();

                    topProducts = details
                        .Where(d => d.product != null)
                        .GroupBy(d => d.id_product)
                        .Select(g =>
                        {
                            var prod = g.First().product!;
                            var latestDetail = g.OrderByDescending(d => d.id_delivery_note).First();
                            var latestNote = notes.FirstOrDefault(n => n.id_delivery_note == latestDetail.id_delivery_note);
                            return new CustomerHistoryProductDto
                            {
                                ProductCode = prod.code ?? string.Empty,
                                ProductName = prod.name ?? string.Empty,
                                TotalQuantity = g.Sum(d => d.quantity),
                                LastPriceUsd = latestDetail.unit_price,
                                LastPurchaseDate = latestNote?.creation_date ?? DateTime.MinValue,
                                TotalAmountUsd = g.Sum(d => d.quantity * d.unit_price)
                            };
                        })
                        .OrderByDescending(p => p.TotalQuantity)
                        .ToList();
                }

                // 5. KPIs
                var validNotes = deliveryNoteDtos.Where(n => !string.Equals(n.status, "Anulada", StringComparison.OrdinalIgnoreCase)).ToList();
                decimal totalInvoiced = validNotes.Sum(n => n.total_amount_usd);
                decimal totalPaid = paymentDtos.Where(p => p.amount_usd > 0).Sum(p => p.amount_usd);
                decimal balanceDue = validNotes.Sum(n => n.balance_due_usd);
                var validCreditNotes = creditNoteDtos.Where(c => !string.Equals(c.status, "Anulada", StringComparison.OrdinalIgnoreCase)).ToList();

                return new CustomerHistoryDataDto
                {
                    Customer = cust,
                    DeliveryNotes = deliveryNoteDtos,
                    CreditNotes = creditNoteDtos,
                    Payments = paymentDtos,
                    TopProducts = topProducts,
                    TotalInvoicedUsd = totalInvoiced,
                    TotalPaidUsd = totalPaid,
                    BalanceDueUsd = balanceDue,
                    TotalNotesCount = deliveryNoteDtos.Count,
                    TotalCreditNotesCount = creditNoteDtos.Count,
                    TotalCreditNotesUsd = validCreditNotes.Sum(c => c.total_amount_usd)
                };
            }
        }
    }
}