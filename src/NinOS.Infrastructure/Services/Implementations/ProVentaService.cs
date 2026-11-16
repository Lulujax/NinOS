using System;

using System.Collections.Generic;

using System.Linq;

using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.DependencyInjection;

using NinOS.Domain;

using NinOS.Domain.ViewModels;

using NinOS.Infrastructure.Common;
using NinOS.Infrastructure.Logging;

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

                // La fecha se pasa a hora de Venezuela antes de ubicar la semana: una nota

                // creada un domingo por la noche caia en la semana siguiente.

                .Where(n => AppTimeZone.to_local(n.creation_date).Date >= week.start.Date

                         && AppTimeZone.to_local(n.creation_date).Date <= week.end.Date)

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



            Dictionary<int, decimal> montos_originales = await cargar_montos_originales_async(
                db_context, in_week.Select(n => n.id_delivery_note).ToList());

            var rows = in_week.Select(n =>

            {

                customers.TryGetValue(n.id_customer, out string? customer_name);

                sellers.TryGetValue(n.id_seller, out var seller);

                // En el reporte semanal las notas van sin descuento como tal (total_amount_usd)

                decimal amount = n.total_amount_usd;
                decimal monto_original = montos_originales.TryGetValue(n.id_delivery_note, out decimal mo)
                    ? mo
                    : n.total_amount_usd;

                return new pro_venta_weekly_row

                {

                    id_delivery_note = n.id_delivery_note,

                    note_number = n.note_number,

                    customer_name = customer_name ?? string.Empty,

                    seller_name = seller?.full_name ?? string.Empty,

                    seller_code = seller?.seller_code ?? string.Empty,

                    gross_total_usd = n.total_amount_usd,

                    discount_percentage = n.discount_percentage,

                    discount_percentage_text = (n.discount_percentage.HasValue && n.discount_percentage.Value > 0)

                        ? $"{n.discount_percentage.Value:0.##}%"

                        : "0%",

                    amount = amount,

                    monto_original = monto_original,

                    commission_luis = pro_venta_calculator.comision(monto_original),

                    gastos_25 = pro_venta_calculator.gastos_operativos(monto_original),

                    gastos_15 = pro_venta_calculator.gastos_administrativos(monto_original),

                    status = n.status ?? string.Empty

                };

            }).ToList();



            // Se separan las anuladas de las vigentes para que los totales de la semana se calculen

            // solo con las que cuentan. Las anuladas siguen en rows, para que el usuario las vea.

            var valid_rows = rows.Where(r => !r.esta_anulada).ToList();

            var annulled_rows = rows.Where(r => r.esta_anulada).ToList();



            relacion? relation = await db_context.relaciones

                .AsNoTracking()

                .FirstOrDefaultAsync(r => r.week_start == week.start.Date && !r.es_hueca);



            if (relation == null && in_week.Count > 0)

            {

                AppLog.Warn($"Semana {week.start:yyyy-MM-dd} con {in_week.Count} notas sin relacion. Se muestra sin numero.");

            }



            return new pro_venta_weekly_dto

            {

                id_relacion = relation?.id_relacion ?? 0,

                relation_number = relation?.relation_number ?? 0,

                week_start = week.start,

                week_end = week.end,

                city = pro_venta_calculator.Ciudad,

                rows = rows,

                // La fila anulada se queda en la tabla para que se vea que existio, pero los

                // totales salen solo de las vigentes. Si se sumara, la semana regalaria comision

                // y gastos por una nota que ya no esta.

                total_monto_original = Money.round(valid_rows.Sum(r => r.monto_original)),
                total_amount = Money.round(valid_rows.Sum(r => r.amount)),

                total_commission_luis = Money.round(valid_rows.Sum(r => r.commission_luis)),

                total_gastos_25 = Money.round(valid_rows.Sum(r => r.gastos_25)),

                total_gastos_15 = Money.round(valid_rows.Sum(r => r.gastos_15)),

                annulled_amount = Money.round(annulled_rows.Sum(r => r.amount)),

                annulled_count = annulled_rows.Count

            };

        }



        public async Task<pro_venta_weekly_dto> get_relation_report_async(int id_relacion)

        {

            using var scope = _scope_factory.CreateScope();

            var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();



            relacion? relation = await db_context.relaciones

                .AsNoTracking()

                .FirstOrDefaultAsync(r => r.id_relacion == id_relacion);



            if (relation == null)

                throw new InvalidOperationException($"No se encontró la relación con ID {id_relacion}.");



            var mar_ids = await db_context.note_types

                .Where(t => NoteTypeCodes.pro_venta_codes.Contains(t.code))

                .Select(t => t.id_note_type)

                .ToListAsync();



            var raw_notes = await db_context.delivery_notes

                .AsNoTracking()

                .Where(n => n.id_relacion == id_relacion && (n.note_type_id == null || mar_ids.Contains(n.note_type_id.Value)))

                .ToListAsync();



            var notes = raw_notes.OrderByCorrelative(n => n.note_number).ToList();



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



            Dictionary<int, decimal> montos_originales = await cargar_montos_originales_async(
                db_context, notes.Select(n => n.id_delivery_note).ToList());

            var rows = notes.Select(n =>

            {

                customers.TryGetValue(n.id_customer, out string? customer_name);

                sellers.TryGetValue(n.id_seller, out var seller);

                // En el reporte semanal las notas van sin descuento como tal (total_amount_usd)

                decimal amount = n.total_amount_usd;
                decimal monto_original = montos_originales.TryGetValue(n.id_delivery_note, out decimal mo)
                    ? mo
                    : n.total_amount_usd;

                return new pro_venta_weekly_row

                {

                    id_delivery_note = n.id_delivery_note,

                    note_number = n.note_number,

                    customer_name = customer_name ?? string.Empty,

                    seller_name = seller?.full_name ?? string.Empty,

                    seller_code = seller?.seller_code ?? string.Empty,

                    gross_total_usd = n.total_amount_usd,

                    discount_percentage = n.discount_percentage,

                    discount_percentage_text = (n.discount_percentage.HasValue && n.discount_percentage.Value > 0)

                        ? $"{n.discount_percentage.Value:0.##}%"

                        : "0%",

                    amount = amount,

                    monto_original = monto_original,

                    commission_luis = pro_venta_calculator.comision(monto_original),

                    gastos_25 = pro_venta_calculator.gastos_operativos(monto_original),

                    gastos_15 = pro_venta_calculator.gastos_administrativos(monto_original),

                    status = n.status ?? string.Empty

                };

            }).ToList();



            var valid_rows = rows.Where(r => !r.esta_anulada).ToList();

            var annulled_rows = rows.Where(r => r.esta_anulada).ToList();



            decimal total_amount = valid_rows.Count > 0

                ? Money.round(valid_rows.Sum(r => r.amount))

                : (relation.es_hueca ? relation.saldo : 0m);



            decimal comm_luis = valid_rows.Count > 0 ? Money.round(valid_rows.Sum(r => r.commission_luis)) : 0m;

            decimal g25 = valid_rows.Count > 0 ? Money.round(valid_rows.Sum(r => r.gastos_25)) : 0m;

            decimal g15 = valid_rows.Count > 0 ? Money.round(valid_rows.Sum(r => r.gastos_15)) : 0m;



            return new pro_venta_weekly_dto

            {

                id_relacion = relation.id_relacion,

                relation_number = relation.relation_number,

                week_start = relation.week_start,

                week_end = relation.week_end,

                city = pro_venta_calculator.Ciudad,

                rows = rows,

                total_monto_original = Money.round(valid_rows.Sum(r => r.monto_original)),
                total_amount = total_amount,

                total_commission_luis = comm_luis,

                total_gastos_25 = g25,

                total_gastos_15 = g15,

                annulled_amount = Money.round(annulled_rows.Sum(r => r.amount)),

                annulled_count = annulled_rows.Count

            };

        }



        /// <summary>

        /// Unica forma de decidir si una nota esta anulada en este servicio. Comparar contra

        /// pro_venta_weekly_row.esta_anulada, que es lo que usa la pantalla para pintar la fila

        /// en rojo: si las dos no coinciden, la fila se pinta de un color y el total dice otra cosa.

        /// </summary>



        /// <summary>

        /// Monto original de cada nota: suma de los renglones a precio de lista, menos las

        /// unidades devueltas con notas de credito VIGENTES (las anuladas no cuentan).

        /// </summary>

        private static async Task<Dictionary<int, decimal>> cargar_montos_originales_async(
            NinOSDbContext db_context, List<int> ids_de_nota)
        {
            if (ids_de_nota == null || ids_de_nota.Count == 0) return new Dictionary<int, decimal>();

            var lineas = await db_context.note_details
                .AsNoTracking()
                .Where(d => ids_de_nota.Contains(d.id_delivery_note))
                .Select(d => new linea_nota(d.id_delivery_note, d.id_product, d.id_promotion, d.quantity, d.unit_price_usd))
                .ToListAsync();

            var ids_creditos_vigentes = await db_context.credit_notes
                .AsNoTracking()
                .Where(c => c.id_delivery_note != null
                         && ids_de_nota.Contains(c.id_delivery_note.Value)
                         && c.status != "Anulada")
                .Select(c => new { c.id_credit_note, id_nota = c.id_delivery_note!.Value })
                .ToListAsync();

            var devoluciones = ids_creditos_vigentes.Count == 0
                ? new List<devolucion_nota>()
                : (await db_context.credit_note_details
                    .AsNoTracking()
                    .Where(d => ids_creditos_vigentes.Select(c => c.id_credit_note).Contains(d.id_credit_note))
                    .Select(d => new { d.id_credit_note, d.id_product, d.id_promotion, d.quantity })
                    .ToListAsync())
                  .Join(ids_creditos_vigentes, d => d.id_credit_note, c => c.id_credit_note,
                        (d, c) => new devolucion_nota(c.id_nota, d.id_product, d.id_promotion, d.quantity))
                  .ToList();

            return original_amount_calculator.calcular(lineas, devoluciones);
        }



        private static bool is_annulled(string? status)

            => string.Equals(status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase);



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



            Dictionary<int, decimal> montos_originales = await cargar_montos_originales_async(
                db_context, notes.Select(n => n.id_delivery_note).ToList());

            return notes.Select(n =>

            {

                customers.TryGetValue(n.id_customer, out string? customer_name);

                sellers.TryGetValue(n.id_seller, out var seller);

                decimal amount = n.adjusted_total_usd;
                decimal monto_original = montos_originales.TryGetValue(n.id_delivery_note, out decimal mo)
                    ? mo
                    : n.total_amount_usd;

                return new pro_venta_weekly_row

                {

                    id_delivery_note = n.id_delivery_note,

                    note_number = n.note_number,

                    customer_name = customer_name ?? string.Empty,

                    seller_name = seller?.full_name ?? string.Empty,

                    seller_code = seller?.seller_code ?? string.Empty,

                    gross_total_usd = n.total_amount_usd,

                    discount_percentage = n.discount_percentage,
                    emission_discount_percentage = n.original_discount_percentage,

                    discount_percentage_text = (n.discount_percentage.HasValue && n.discount_percentage.Value > 0)

                        ? $"{n.discount_percentage.Value:0.##}%"

                        : "0%",

                    amount = amount,

                    monto_original = monto_original,

                    commission_luis = pro_venta_calculator.comision(monto_original),

                    gastos_25 = pro_venta_calculator.gastos_operativos(monto_original),

                    gastos_15 = pro_venta_calculator.gastos_administrativos(monto_original),

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
                if (relation.es_hueca) continue;

                var pending_notes = group.Where(n => !is_annulled(n.status)).ToList();
                var annulled_notes = group.Where(n => is_annulled(n.status)).ToList();

                decimal paid_total = payment_totals.TryGetValue(group.Key, out var total) ? total : 0m;
                var resumen = relation_amounts.calcular(
                    pending_notes.Select(n => n.adjusted_total_usd).ToList(),
                    new[] { paid_total },
                    false,
                    0m);

                if (only_pending == resumen.esta_pagada) continue;

                rows.Add(new pro_venta_relation_row
                {
                    id_relacion = relation.id_relacion,
                    relation_number = relation.relation_number,
                    week_start = relation.week_start,
                    week_end = relation.week_end,
                    amount = resumen.monto_por_pagar,
                    paid_amount_usd = resumen.abonado,
                    balance_due_usd = resumen.saldo,
                    status = resumen.estado,
                    note_count = pending_notes.Count,
                    annulled_amount = Money.round(annulled_notes.Sum(n => n.adjusted_total_usd)),
                    annulled_count = annulled_notes.Count
                });
            }

            foreach (var relation in hollow_relations)
            {
                decimal paid_total = payment_totals.TryGetValue(relation.id_relacion, out var total) ? total : 0m;
                var resumen = relation_amounts.calcular(
                    Array.Empty<decimal>(),
                    new[] { paid_total },
                    true,
                    Math.Max(0m, relation.saldo));

                if (only_pending == resumen.esta_pagada) continue;

                int notes_in_relation = notes.Count(n => n.id_relacion == relation.id_relacion);

                rows.Add(new pro_venta_relation_row
                {
                    id_relacion = relation.id_relacion,
                    relation_number = relation.relation_number,
                    week_start = relation.week_start,
                    week_end = relation.week_end,
                    amount = resumen.monto_por_pagar,
                    paid_amount_usd = resumen.abonado,
                    balance_due_usd = resumen.saldo,
                    status = resumen.estado,
                    note_count = notes_in_relation,
                    annulled_amount = 0,
                    annulled_count = 0
                });
            }

            return rows.OrderBy(r => r.relation_number).ToList();

        }



        public async Task update_note_amount_async(int id_delivery_note, decimal monto_a_cobrar, decimal monto_esperado)
        {
            if (id_delivery_note <= 0) throw new ArgumentException("El id de la nota no es valido.", nameof(id_delivery_note));

            using var scope = _scope_factory.CreateScope();
            var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            using var transaction = await db_context.Database.BeginTransactionAsync();
            int? id_relacion_leida = null;
            decimal descuento_antes = 0m;
            try
            {
                id_relacion_leida = await db_context.delivery_notes
                    .AsNoTracking()
                    .Where(n => n.id_delivery_note == id_delivery_note)
                    .Select(n => n.id_relacion)
                    .FirstOrDefaultAsync();

                if (id_relacion_leida == null)
                    throw new InvalidOperationException("La nota no existe o no pertenece a una relacion Pro Venta.");

                int id_relacion = id_relacion_leida.Value;
                await lock_relation_for_update_async(db_context, id_relacion);
                await lock_note_for_update_async(db_context, id_delivery_note);

                delivery_note? nota = await db_context.delivery_notes
                    .FirstOrDefaultAsync(n => n.id_delivery_note == id_delivery_note);
                if (nota == null)
                    throw new InvalidOperationException("La nota no existe.");

                descuento_antes = nota.discount_percentage ?? 0m;
                resumen_relacion resumen = await resumen_relacion_async(db_context, id_relacion);
                decimal monto_original = (await cargar_montos_originales_async(db_context, new List<int> { id_delivery_note }))
                    .TryGetValue(id_delivery_note, out decimal mo) ? mo : 0m;

                resultado_edicion resultado = pro_venta_note_edit.evaluar_edicion(
                    nota.status, resumen.esta_pagada, monto_original, nota.adjusted_total_usd, monto_esperado, monto_a_cobrar);
                if (!resultado.aceptada)
                    throw new InvalidOperationException(resultado.motivo);

                decimal descuento_nuevo = pro_venta_note_edit.descuento_desde_monto(monto_original, monto_a_cobrar);
                nota.adjusted_total_usd = Money.round(monto_a_cobrar);
                nota.discount_percentage = descuento_nuevo;

                await db_context.SaveChangesAsync();
                await transaction.CommitAsync();

                AppLog.Info($"Edicion Pro Venta guardada: nota {id_delivery_note}, relacion {id_relacion}, descuento {descuento_antes:0.##} -> {descuento_nuevo:0.##}.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                AppLog.Warn($"Edicion Pro Venta rechazada: nota {id_delivery_note}, relacion {id_relacion_leida?.ToString() ?? "desconocida"}, descuento antes {descuento_antes:0.##}. Motivo: {ex.Message}");
                throw;
            }
        }

        private static Task lock_relation_for_update_async(NinOSDbContext db_context, int id_relacion)
            => db_context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM relacion WHERE id_relacion = {id_relacion} FOR UPDATE");

        private static Task lock_note_for_update_async(NinOSDbContext db_context, int id_delivery_note)
            => db_context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM delivery_note WHERE id_delivery_note = {id_delivery_note} FOR UPDATE");

        public static async Task<resumen_relacion> resumen_relacion_async(NinOSDbContext db_context, int id_relacion)
        {
            var notas = await db_context.delivery_notes
                .AsNoTracking()
                .Where(n => n.id_relacion == id_relacion)
                .Select(n => new { n.id_delivery_note, n.status, n.adjusted_total_usd })
                .ToListAsync();

            var anuladas_ids = notas.Where(n => is_annulled(n.status)).Select(n => n.id_delivery_note).ToHashSet();

            var abonos = await db_context.payments
                .AsNoTracking()
                .Where(p => p.id_relacion == id_relacion && p.amount_usd > 0)
                .Select(p => new { p.id_delivery_note, p.amount_usd })
                .ToListAsync();

            var abonos_vigentes = abonos
                .Where(p => p.id_delivery_note == null || !anuladas_ids.Contains(p.id_delivery_note.Value))
                .Select(p => p.amount_usd)
                .ToList();

            var montos_vigentes = notas
                .Where(n => !is_annulled(n.status))
                .Select(n => n.adjusted_total_usd)
                .ToList();

            return relation_amounts.calcular(montos_vigentes, abonos_vigentes, false, 0m);
        }

        public async Task<List<pro_venta_relation_row>> get_relations_at_cutoff_async(DateTime fecha_corte)
        {
            if (fecha_corte == default)
                throw new ArgumentException("La fecha de corte no puede estar vacia.", nameof(fecha_corte));

            using var scope = _scope_factory.CreateScope();
            var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var mar_ids = await db_context.note_types
                .AsNoTracking()
                .Where(t => NoteTypeCodes.pro_venta_codes.Contains(t.code))
                .Select(t => t.id_note_type)
                .ToListAsync();

            var relaciones = await db_context.relaciones
                .AsNoTracking()
                .OrderBy(r => r.relation_number)
                .ToListAsync();

            var notas = await db_context.delivery_notes
                .AsNoTracking()
                .Where(n => n.note_type_id != null && mar_ids.Contains(n.note_type_id.Value) && n.id_relacion != null)
                .Select(n => new { n.id_delivery_note, n.id_relacion, n.status, n.creation_date, n.adjusted_total_usd })
                .ToListAsync();

            var abonos = await db_context.payments
                .AsNoTracking()
                .Where(p => p.id_relacion != null && p.amount_usd > 0)
                .Select(p => new { p.id_relacion, p.id_delivery_note, p.payment_date, p.amount_usd })
                .ToListAsync();

            var notas_anuladas = notas.Where(n => is_annulled(n.status)).Select(n => n.id_delivery_note).ToHashSet();

            var filas = new List<pro_venta_relation_row>();
            foreach (var relacion_actual in relaciones.Where(r => r.week_start.Date <= fecha_corte.Date))
            {
                var montos_notas = notas
                    .Where(n => n.id_relacion == relacion_actual.id_relacion && !is_annulled(n.status))
                    .Select(n => new movimiento_con_fecha(n.creation_date, n.adjusted_total_usd))
                    .ToList();

                var movimientos_abonos = abonos
                    .Where(p => p.id_relacion == relacion_actual.id_relacion
                             && (p.id_delivery_note == null || !notas_anuladas.Contains(p.id_delivery_note.Value)))
                    .Select(p => new movimiento_con_fecha(p.payment_date, p.amount_usd))
                    .ToList();

                resumen_relacion resumen = relation_balance_at.calcular(
                    fecha_corte, montos_notas, movimientos_abonos, relacion_actual.es_hueca, Math.Max(0m, relacion_actual.saldo));

                filas.Add(new pro_venta_relation_row
                {
                    id_relacion = relacion_actual.id_relacion,
                    relation_number = relacion_actual.relation_number,
                    week_start = relacion_actual.week_start,
                    week_end = relacion_actual.week_end,
                    amount = resumen.monto_por_pagar,
                    paid_amount_usd = resumen.abonado,
                    balance_due_usd = resumen.saldo,
                    status = resumen.estado,
                    note_count = resumen.notas_vigentes
                });
            }

            AppLog.Info($"Reporte con corte {fecha_corte:yyyy-MM-dd}: {filas.Count} relaciones calculadas (solo lectura).");
            return filas;
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
