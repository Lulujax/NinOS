using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly NinOSDbContext _db_context;

        public PaymentService(NinOSDbContext db_context)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));
            _db_context = db_context;
        }

        public async Task register_payment_async(payment new_payment)
        {
            if (new_payment == null) throw new ArgumentNullException(nameof(new_payment));

            using var transaction = await _db_context.Database.BeginTransactionAsync();
            try
            {
                delivery_note target_note = await _db_context.delivery_notes.FindAsync(new_payment.id_delivery_note);
                if (target_note == null) throw new InvalidOperationException("La nota de entrega no existe.");
                if (target_note.status == "Anulada") throw new InvalidOperationException("No se puede abonar una nota anulada.");
                if (target_note.status == "Pagada") throw new InvalidOperationException("No se puede abonar una nota ya pagada.");

                decimal previously_paid_usd = await _db_context.payments
                    .Where(p => p.id_delivery_note == target_note.id_delivery_note)
                    .SumAsync(p => p.amount_usd);

                if (new_payment.amount_usd > target_note.total_amount_usd - previously_paid_usd)
                {
                    throw new InvalidOperationException("El monto del abono supera el saldo pendiente de la nota.");
                }

                new_payment.amount_bs = new_payment.amount_usd * new_payment.exchange_rate;
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

                if (total_paid_usd >= target_note.total_amount_usd)
                {
                    target_note.status = "Pagada";
                    _db_context.delivery_notes.Update(target_note);
                    
                    decimal generated_amount_usd = target_note.total_amount_usd * 0.05m;
                    commission new_commission = new commission(
                        target_note.id_seller,
                        target_note.id_delivery_note,
                        0.05m,
                        generated_amount_usd,
                        false,
                        null);

                    await _db_context.commissions.AddAsync(new_commission);
                }

                await _db_context.SaveChangesAsync();
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