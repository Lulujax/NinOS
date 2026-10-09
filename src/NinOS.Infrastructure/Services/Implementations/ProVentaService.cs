using System;
using System.Collections.Generic;
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
                .Where(t => NoteTypeCodes.pro_venta_codes.Contains(t.code))
                .Select(t => t.id_note_type)
                .ToListAsync();

            var now = DateTime.Now;

            double tz_offset = AppTimeZone.offset_hours;

            var months = await db_context.delivery_notes
                .AsNoTracking()
                .Where(n => n.note_type_id != null && mar_ids.Contains(n.note_type_id.Value))
                .Select(n => new {
                    Year = n.creation_date.AddHours(tz_offset).Year,
                    Month = n.creation_date.AddHours(tz_offset).Month })
                .Distinct()
                .ToListAsync();

            var culture = new System.Globalization.CultureInfo("es-VE");

            return months
                .Select(m => new DateTime(m.Year, m.Month, 1))
                .Concat(new[] { new DateTime(now.Year, now.Month, 1) })
                .Distinct()
                .OrderBy(d => d)
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
                .Where(t => NoteTypeCodes.pro_venta_codes.Contains(t.code))
                .Select(t => t.id_note_type)
                .ToListAsync();

            var notes = await db_context.delivery_notes
                .AsNoTracking()
                .Where(n => n.note_type_id != null && mar_ids.Contains(n.note_type_id.Value))
                .ToListAsync();

            var in_week = notes
                .Where(n => n.creation_date.Date >= week.start.Date && n.creation_date.Date <= week.end.Date)
                .OrderByCorrelative(n => n.note_number)
                .ToList();

            var customer_ids = in_week.Select(n => n.id_customer).Distinct().ToList();

            var customers = await db_context.customers
                .AsNoTracking()
                .Where(c => customer_ids.Contains(c.id_customer))
                .ToDictionaryAsync(c => c.id_customer, c => c.business_name);

            var seller_ids = in_week.Select(n => n.id_seller).Distinct().ToList();
            var sellers = await db_context.sellers
                .AsNoTracking()
                .Where(s => seller_ids.Contains(s.id_seller))
                .ToDictionaryAsync(s => s.id_seller, s => new { s.full_name, s.seller_code });

            var rows = in_week.Select(n =>
            {
                customers.TryGetValue(n.id_customer, out string? customer_name);
                sellers.TryGetValue(n.id_seller, out var seller);
                decimal amount = n.adjusted_total_usd;
                return new pro_venta_weekly_row
                {
                    id_delivery_note = n.id_delivery_note,
                    note_number = n.note_number,
                    customer_name = customer_name ?? string.Empty,
                    seller_name = seller?.full_name ?? string.Empty,
                    seller_code = seller?.seller_code ?? string.Empty,
                    amount = amount,
                    commission_luis = Money.round(amount * 0.10m),
                    gastos_25 = Money.round(amount * 0.25m),
                    gastos_15 = Money.round(amount * 0.15m),
                    status = n.status ?? string.Empty
                };
            }).ToList();

            // Se separan las anuladas de las vigentes para que los totales de la semana se calculen
            // solo con las que cuentan. Las anuladas siguen en rows, para que el usuario las vea.
            var valid_rows = rows.Where(r => !r.esta_anulada).ToList();
            var annulled_rows = rows.Where(r => r.esta_anulada).ToList();

            relacion? relation = await db_context.relaciones
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.week_start == week.start.Date);

            if (relation == null && in_week.Count > 0)
            {
                relation = await get_or_create_week_relation_async(db_context, week.start.Date);
            }

            return new pro_venta_weekly_dto
            {
                id_relacion = relation?.id_relacion ?? 0,
                relation_number = relation?.relation_number ?? 0,
                week_start = week.start,
                week_end = week.end,
                city = "MARACAY",
                rows = rows,
                // La fila anulada se queda en la tabla para que se vea que existio, pero los
                // totales salen solo de las vigentes. Si se sumara, la semana regalaria comision
                // y gastos por una nota que ya no esta.
                total_amount = Money.round(valid_rows.Sum(r => r.amount)),
                total_commission_luis = Money.round(valid_rows.Sum(r => r.commission_luis)),
                total_gastos_25 = Money.round(valid_rows.Sum(r => r.gastos_25)),
                total_gastos_15 = Money.round(valid_rows.Sum(r => r.gastos_15)),
                annulled_amount = Money.round(annulled_rows.Sum(r => r.amount)),
                annulled_count = annulled_rows.Count
            };
        }

        /// <summary>
        /// Unica forma de decidir si una nota esta anulada en este servicio. Comparar contra
        /// pro_venta_weekly_row.esta_anulada, que es lo que usa la pantalla para pintar la fila
        /// en rojo: si las dos no coinciden, la fila se pinta de un color y el total dice otra cosa.
        /// </summary>
        private static bool is_annulled(string? status)
            => string.Equals(status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase);

        private static async Task<relacion> get_or_create_week_relation_async(NinOSDbContext db_context, DateTime date)
        {
            int offset = ((int)date.Date.DayOfWeek + 6) % 7;
            DateTime week_start = date.Date.AddDays(-offset);

            relacion? existing = await db_context.relaciones
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.week_start == week_start && !r.es_hueca);
            if (existing != null) return existing;

            int next_number = (await db_context.relaciones.MaxAsync(r => (int?)r.relation_number) ?? 0) + 1;

            relacion relation = new relacion(next_number, week_start, week_start.AddDays(6));
            db_context.relaciones.Add(relation);

            try
            {
                await db_context.SaveChangesAsync();
                await renumber_relations_chronologically_async(db_context);
                return await db_context.relaciones.AsNoTracking().FirstAsync(r => r.id_relacion == relation.id_relacion);
            }
            catch (DbUpdateException)
            {
                // Otra transaccion pudo crear la relacion de esta semana (o disputar el numero):
                // se reintenta leyendo.
                db_context.Entry(relation).State = EntityState.Detached;

                relacion? created = await db_context.relaciones
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.week_start == week_start && !r.es_hueca);
                if (created != null) return created;
                throw;
            }
        }

        private static async Task renumber_relations_chronologically_async(NinOSDbContext db_context)
        {
            // Las relaciones huecas conservan el numero que se les asigno al migrarlas:
            // renumerarlas por week_start las mezclaria entre si, porque varias
            // comparten la misma semana.
            var all = await db_context.relaciones
                .Where(r => !r.es_hueca)
                .OrderBy(r => r.week_start)
                .ToListAsync();

            // Las relaciones normales se numeran a partir de la mas alta que ya existe, para no
            // chocar con IX_relacion_relation_number (unico) ni invadir el rango de las
            // huecas. Con la cartera heredada en 149..179, la siguiente normal es 180.
            int max_number = await db_context.relaciones.MaxAsync(r => (int?)r.relation_number) ?? 0;

            bool needs_renumber = false;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].relation_number != max_number - all.Count + 1 + i)
                {
                    needs_renumber = true;
                    break;
                }
            }
            if (needs_renumber)
            {
                int temp_base = 1000000;
                for (int i = 0; i < all.Count; i++)
                {
                    all[i].relation_number = temp_base + i + 1;
                }
                await db_context.SaveChangesAsync();

                for (int i = 0; i < all.Count; i++)
                {
                    all[i].relation_number = max_number - all.Count + 1 + i;
                }
                await db_context.SaveChangesAsync();
            }
        }

        public Task<List<pro_venta_relation_row>> get_pending_relations_async()
        {
            return build_relation_rows_async(only_pending: true);
        }

        public async Task<List<pro_venta_weekly_row>> get_relation_notes_async(int id_relacion)
        {
            using var scope = _scope_factory.CreateScope();
            var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var raw_notes = await db_context.delivery_notes
                .AsNoTracking()
                .Where(n => n.id_relacion == id_relacion)
                .OrderBy(n => n.note_number)
                .ToListAsync();

            var notes = raw_notes.OrderByCorrelative(n => n.note_number).ToList();

            if (notes.Count == 0)
                return new List<pro_venta_weekly_row>();

            var customer_ids = notes.Select(n => n.id_customer).Distinct().ToList();
            var customers = await db_context.customers
                .AsNoTracking()
                .Where(c => customer_ids.Contains(c.id_customer))
                .ToDictionaryAsync(c => c.id_customer, c => c.business_name);

            var seller_ids = notes.Select(n => n.id_seller).Distinct().ToList();
            var sellers = await db_context.sellers
                .AsNoTracking()
                .Where(s => seller_ids.Contains(s.id_seller))
                .ToDictionaryAsync(s => s.id_seller, s => new { s.full_name, s.seller_code });

            return notes.Select(n =>
            {
                customers.TryGetValue(n.id_customer, out string? customer_name);
                sellers.TryGetValue(n.id_seller, out var seller);
                decimal amount = n.adjusted_total_usd;
                return new pro_venta_weekly_row
                {
                    id_delivery_note = n.id_delivery_note,
                    note_number = n.note_number,
                    customer_name = customer_name ?? string.Empty,
                    seller_name = seller?.full_name ?? string.Empty,
                    seller_code = seller?.seller_code ?? string.Empty,
                    amount = amount,
                    commission_luis = Money.round(amount * 0.10m),
                    gastos_25 = Money.round(amount * 0.25m),
                    gastos_15 = Money.round(amount * 0.15m),
                    status = n.status ?? string.Empty
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
                .Where(t => NoteTypeCodes.pro_venta_codes.Contains(t.code))
                .Select(t => t.id_note_type)
                .ToListAsync();

            if (mar_ids.Count == 0)
                return new List<pro_venta_relation_row>();

            var notes = await db_context.delivery_notes
                .AsNoTracking()
                .Where(n => n.note_type_id != null && mar_ids.Contains(n.note_type_id.Value)
                         && n.id_relacion != null)
                .ToListAsync();

            // Las relaciones huecas llevan su propio saldo en la tabla relacion (cartera
            // heredada del Excel) y pueden no tener ninguna nota. Se agregan aparte para
            // que aparezcan en Pendientes y se abonen una por una.
            var hollow_relations = await db_context.relaciones
                .AsNoTracking()
                .Where(r => r.es_hueca)
                .ToListAsync();

            if (notes.Count == 0 && hollow_relations.Count == 0)
                return new List<pro_venta_relation_row>();

            var relation_ids = notes.Select(n => n.id_relacion!.Value).Distinct().ToList();
            if (hollow_relations.Count > 0)
                relation_ids.AddRange(hollow_relations.Select(r => r.id_relacion));

            // Solo cuentan los abonos positivos. Los asientos de anulacion van con monto negativo
            // y existen para dejar rastro en el historial, no para saldo: si se sumaran, una nota
            // anulada inflaria el saldo pendiente en vez de liberarlo.
            //
            // Tampoco cuenta lo que se le pago a una nota que despues se anulo. Si se dejara, el
            // dinero de esa nota taparia el saldo de las notas vigentes que conviven con ella en
            // la misma relacion, y la relacion cerraria como pagada con facturas pendientes.
            var annulled_note_ids = notes
                .Where(n => is_annulled(n.status))
                .Select(n => n.id_delivery_note)
                .ToList();

            var payment_query = db_context.payments
                .AsNoTracking()
                .Where(p => p.id_relacion != null && relation_ids.Contains(p.id_relacion.Value) && p.amount_usd > 0);

            if (annulled_note_ids.Count > 0)
            {
                var anuladas = annulled_note_ids;
                // Los abonos sin nota asociada (pagos a nivel de relacion) se respetan siempre:
                // son plata que entro de verdad y no pertenece a ninguna nota en particular.
                payment_query = payment_query.Where(p => p.id_delivery_note == null || !anuladas.Contains(p.id_delivery_note.Value));
            }

            var payment_totals = await payment_query
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
                if (relation.es_hueca) continue;   // se agrega mas abajo, con su saldo propio

                // Una nota anulada deja de ser exigible, asi que sale del monto y del conteo de
                // notas por cobrar, pero no se borra: si era la unica que habia, la relacion
                // tiene que aparecer en Pagadas y no desaparecer de las dos tablas.
                var pending_notes = group.Where(n => !is_annulled(n.status)).ToList();
                var annulled_notes = group.Where(n => is_annulled(n.status)).ToList();

                decimal amount = pending_notes.Sum(n => n.adjusted_total_usd);
                decimal paid = payment_totals.TryGetValue(group.Key, out var total) ? total : 0;
                decimal balance = amount - paid;
                bool is_fully_paid = pending_notes.Count == 0 || balance <= 0.005m;

                if (only_pending == is_fully_paid) continue;

                rows.Add(new pro_venta_relation_row
                {
                    id_relacion = relation.id_relacion,
                    relation_number = relation.relation_number,
                    week_start = relation.week_start,
                    week_end = relation.week_end,
                    // El monto es solo de las notas vigentes. Las anuladas se reportan aparte para
                    // que se vean sin reentrar en el total ni en el saldo a pagar.
                    amount = amount,
                    paid_amount_usd = paid,
                    balance_due_usd = balance < 0 ? 0 : balance,
                    status = is_fully_paid ? "Pagada" : "Pendiente",
                    note_count = pending_notes.Count,
                    annulled_amount = Money.round(annulled_notes.Sum(n => n.adjusted_total_usd)),
                    annulled_count = annulled_notes.Count
                });
            }

            // Relacion hueca: el saldo viene de la propia relacion, no de la suma de notas.
            foreach (var relation in hollow_relations)
            {
                decimal amount = relation.saldo;
                decimal paid = payment_totals.TryGetValue(relation.id_relacion, out var total) ? total : 0;
                decimal balance = amount - paid;
                bool is_fully_paid = balance <= 0.005m;

                if (only_pending == is_fully_paid) continue;

                int notes_in_relation = notes.Count(n => n.id_relacion == relation.id_relacion);

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
                    note_count = notes_in_relation,
                    annulled_amount = 0,
                    annulled_count = 0
                });
            }

            return rows.OrderBy(r => r.relation_number).ToList();
        }

        public async Task<List<pro_venta_relation_option>> get_all_relations_async()
        {
            using var scope = _scope_factory.CreateScope();
            var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var list = await db_context.relaciones
                .AsNoTracking()
                .OrderBy(r => r.relation_number)
                .ToListAsync();

            return list.Select(r => new pro_venta_relation_option
            {
                id_relacion = r.id_relacion,
                relation_number = r.relation_number,
                week_start = r.week_start,
                week_end = r.week_end,
                label = $"Relacion nro {r.relation_number}"
            }).ToList();
        }
    }
}