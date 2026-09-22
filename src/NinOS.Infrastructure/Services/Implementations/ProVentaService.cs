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
    public class ProVentaService : IProVentaService
    {
        private readonly IServiceScopeFactory _scope_factory;

        public ProVentaService(IServiceScopeFactory scope_factory)
        {
            _scope_factory = scope_factory ?? throw new ArgumentNullException(nameof(scope_factory));
        }

        public async Task<List<pro_venta_month_option>> get_available_months_async()
        {
            using var scope = _scope_factory.CreateScope();
            var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var mar_ids = await db_context.note_types
                .Where(t => t.code == "MAR")
                .Select(t => t.id_note_type)
                .ToListAsync();

            var now = DateTime.Now;

            var months = await db_context.delivery_notes
                .AsNoTracking()
                .Where(n => n.note_type_id != null && mar_ids.Contains(n.note_type_id.Value))
                .Select(n => new { n.creation_date.Year, n.creation_date.Month })
                .Distinct()
                .ToListAsync();

            var culture = new System.Globalization.CultureInfo("es-VE");

            return months
                .Select(m => new DateTime(m.Year, m.Month, 1))
                .Concat(new[] { new DateTime(now.Year, now.Month, 1) })
                .Distinct()
                .OrderByDescending(d => d)
                .Select(d => new pro_venta_month_option
                {
                    value = d,
                    label = d.ToString("MMMM yyyy", culture)
                })
                .ToList();
        }

        public List<pro_venta_week_info> build_weeks(int year, int month)
        {
            var first_day = new DateTime(year, month, 1);
            var last_day = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            int offset = (((int)first_day.DayOfWeek) + 6) % 7;
            var monday = first_day.AddDays(-offset);

            var culture = new System.Globalization.CultureInfo("es-VE");
            var weeks = new List<pro_venta_week_info>();
            int index = 1;

            for (var start = monday; start <= last_day; start = start.AddDays(7))
            {
                var end = start.AddDays(6);
                weeks.Add(new pro_venta_week_info
                {
                    week_index = index++,
                    start = start,
                    end = end,
                    label = $"{start:dd} AL {end:dd} {culture.DateTimeFormat.GetMonthName(end.Month)}"
                });
            }

            return weeks;
        }

        public async Task<pro_venta_weekly_dto> get_weekly_report_async(pro_venta_week_info week)
        {
            using var scope = _scope_factory.CreateScope();
            var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var mar_ids = await db_context.note_types
                .Where(t => t.code == "MAR")
                .Select(t => t.id_note_type)
                .ToListAsync();

            var notes = await db_context.delivery_notes
                .AsNoTracking()
                .Where(n => n.note_type_id != null && mar_ids.Contains(n.note_type_id.Value))
                .ToListAsync();

            var in_week = notes
                .Where(n => n.creation_date.Date >= week.start.Date && n.creation_date.Date <= week.end.Date)
                .OrderBy(n => n.creation_date)
                .ToList();

            var customer_ids = in_week.Select(n => n.id_customer).Distinct().ToList();

            var customers = await db_context.customers
                .AsNoTracking()
                .Where(c => customer_ids.Contains(c.id_customer))
                .ToDictionaryAsync(c => c.id_customer, c => c.business_name);

            var rows = in_week.Select(n =>
            {
                customers.TryGetValue(n.id_customer, out string? customer_name);
                decimal amount = n.adjusted_total_usd;
                return new pro_venta_weekly_row
                {
                    id_delivery_note = n.id_delivery_note,
                    note_number = n.note_number,
                    customer_name = customer_name ?? string.Empty,
                    amount = amount,
                    commission_luis = Math.Round(amount * 0.10m, 2),
                    gastos_25 = Math.Round(amount * 0.25m, 2),
                    gastos_15 = Math.Round(amount * 0.15m, 2)
                };
            }).ToList();

            relacion? relation = await db_context.relaciones
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.week_start == week.start.Date);

            if (relation == null && in_week.Count > 0)
            {
                int next_number = (await db_context.relaciones.MaxAsync(r => (int?)r.relation_number) ?? 0) + 1;
                relacion new_relation = new relacion(next_number, week.start.Date, week.start.Date.AddDays(6));
                db_context.relaciones.Add(new_relation);
                await db_context.SaveChangesAsync();
                relation = new_relation;
            }

            return new pro_venta_weekly_dto
            {
                relation_number = relation?.relation_number ?? 0,
                week_start = week.start,
                week_end = week.end,
                city = "MARACAY",
                rows = rows,
                total_amount = rows.Sum(r => r.amount),
                total_commission_luis = rows.Sum(r => r.commission_luis),
                total_gastos_25 = rows.Sum(r => r.gastos_25),
                total_gastos_15 = rows.Sum(r => r.gastos_15)
            };
        }

        public Task<List<pro_venta_relation_row>> get_pending_relations_async()
        {
            return build_relation_rows_async(only_pending: true);
        }

        public async Task<List<pro_venta_weekly_row>> get_relation_notes_async(int id_relacion)
        {
            using var scope = _scope_factory.CreateScope();
            var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var notes = await db_context.delivery_notes
                .AsNoTracking()
                .Where(n => n.id_relacion == id_relacion && n.status != "Anulada")
                .OrderBy(n => n.note_number)
                .ToListAsync();

            if (notes.Count == 0)
                return new List<pro_venta_weekly_row>();

            var customer_ids = notes.Select(n => n.id_customer).Distinct().ToList();
            var customers = await db_context.customers
                .AsNoTracking()
                .Where(c => customer_ids.Contains(c.id_customer))
                .ToDictionaryAsync(c => c.id_customer, c => c.business_name);

            return notes.Select(n =>
            {
                customers.TryGetValue(n.id_customer, out string? customer_name);
                decimal amount = n.adjusted_total_usd;
                return new pro_venta_weekly_row
                {
                    id_delivery_note = n.id_delivery_note,
                    note_number = n.note_number,
                    customer_name = customer_name ?? string.Empty,
                    amount = amount,
                    commission_luis = Math.Round(amount * 0.10m, 2),
                    gastos_25 = Math.Round(amount * 0.25m, 2),
                    gastos_15 = Math.Round(amount * 0.15m, 2)
                };
            }).ToList();
        }

        public Task<List<pro_venta_relation_row>> get_paid_relations_async()
        {
            return build_relation_rows_async(only_pending: false);
        }

        private async Task<List<pro_venta_relation_row>> build_relation_rows_async(bool only_pending)
        {
            using var scope = _scope_factory.CreateScope();
            var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var mar_ids = await db_context.note_types
                .Where(t => t.code == "MAR")
                .Select(t => t.id_note_type)
                .ToListAsync();

            if (mar_ids.Count == 0)
                return new List<pro_venta_relation_row>();

            var notes = await db_context.delivery_notes
                .AsNoTracking()
                .Where(n => n.note_type_id != null && mar_ids.Contains(n.note_type_id.Value)
                         && n.status != "Anulada" && n.id_relacion != null)
                .ToListAsync();

            if (notes.Count == 0)
                return new List<pro_venta_relation_row>();

            var relation_ids = notes.Select(n => n.id_relacion!.Value).Distinct().ToList();

            var payment_totals = await db_context.payments
                .AsNoTracking()
                .Where(p => p.id_relacion != null && relation_ids.Contains(p.id_relacion.Value))
                .GroupBy(p => p.id_relacion!.Value)
                .Select(g => new { Id = g.Key, Total = g.Sum(p => p.amount_usd) })
                .ToDictionaryAsync(x => x.Id, x => x.Total);

            var relations = await db_context.relaciones
                .AsNoTracking()
                .Where(r => relation_ids.Contains(r.id_relacion))
                .ToDictionaryAsync(r => r.id_relacion);

            var rows = new List<pro_venta_relation_row>();
            foreach (var group in notes.GroupBy(n => n.id_relacion!.Value))
            {
                if (!relations.TryGetValue(group.Key, out var relation)) continue;

                decimal amount = group.Sum(n => n.adjusted_total_usd);
                decimal paid = payment_totals.TryGetValue(group.Key, out var total) ? total : 0;
                decimal balance = amount - paid;
                bool is_fully_paid = balance <= 0.005m;

                if (only_pending == is_fully_paid) continue;

                rows.Add(new pro_venta_relation_row
                {
                    id_relacion = relation.id_relacion,
                    relation_number = relation.relation_number,
                    week_start = relation.week_start,
                    week_end = relation.week_end,
                    amount = amount,
                    paid_amount_usd = paid,
                    balance_due_usd = balance < 0 ? 0 : balance,
                    status = is_fully_paid ? "Pagada" : "Pendiente",
                    note_count = group.Count()
                });
            }

            return rows.OrderBy(r => r.relation_number).ToList();
        }
    }
}