using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain;
using NinOS.Infrastructure.Common;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Repositories.Interfaces;
using System.Data;
using System.Data.Common;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class DeliveryNoteService : IDeliveryNoteService
    {
        public const long CorrelativeLockKey = 9070002L;
        public const long RelationLockKey = 9070004L;

        private readonly IServiceScopeFactory _scope_factory;

        public DeliveryNoteService(IServiceScopeFactory scope_factory)
        {
            if (scope_factory == null) throw new ArgumentNullException(nameof(scope_factory));
            _scope_factory = scope_factory;
        }

        public async Task<IEnumerable<delivery_note>> get_all_notes_async()
        {
            using var scope = _scope_factory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDeliveryNoteRepository>();
            var all = await repo.get_all_async();
            return all.OrderBy(n => n.creation_date).ThenBy(n => n.id_delivery_note).ToList();
        }

        public async Task<string> generate_correlative_async(int id_seller)
        {
            if (id_seller <= 0) throw new ArgumentException(nameof(id_seller));
            using var scope = _scope_factory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDeliveryNoteRepository>();
            return await repo.get_next_correlative_async();
        }

        public async Task create_delivery_note_async(delivery_note new_note, IEnumerable<note_detail> details, NinOS.Domain.ViewModels.note_print_dto? print_dto = null, bool confirmar_relacion_pagada = false)
        {
            if (new_note == null) throw new ArgumentNullException(nameof(new_note));
            if (details == null) throw new InvalidOperationException("Los detalles no pueden ser nulos.");

            using var scope = _scope_factory.CreateScope();
            var _db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            using (var transaction = await _db_context.Database.BeginTransactionAsync())
            {
                try
                {
                    DbConnection conn = _db_context.Database.GetDbConnection();
                    if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                    using DbCommand lockCmd = conn.CreateCommand();
                    lockCmd.Transaction = transaction.GetDbTransaction();
                    lockCmd.CommandText = $"SELECT pg_advisory_xact_lock({CorrelativeLockKey})";
                    await lockCmd.ExecuteNonQueryAsync();

                    var all_numbers = await _db_context.delivery_notes.AsNoTracking().Select(n => n.note_number).ToListAsync();
                    string correlativo_definitivo = NinOS.Infrastructure.Common.SeriesCalculator.GetNextGlobal6(all_numbers);
                    if (correlativo_definitivo != new_note.note_number)
                    {
                        AppLog.Info($"Correlativo cambiado de {new_note.note_number} a {correlativo_definitivo} (otra PC lo tomo primero).");
                        new_note.note_number = correlativo_definitivo;
                    }

                    var details_list = details.ToList();

                    NinOS.Domain.nota_limite_productos.validar(details_list.Count);

                    var direct_product_ids = new HashSet<int>();
                    var promotion_ids = new HashSet<int>();

                    foreach (note_detail detail in details_list)
                    {
                        if (detail.id_product != null) direct_product_ids.Add(detail.id_product.Value);
                        if (detail.id_promotion != null) promotion_ids.Add(detail.id_promotion.Value);
                    }

                    var promotions = await _db_context.promotions
                        .Include(pr => pr.items)
                        .Where(p => promotion_ids.Contains(p.id_promotion))
                        .ToDictionaryAsync(p => p.id_promotion);

                    var all_product_ids = new HashSet<int>(direct_product_ids);
                    foreach (var promo in promotions.Values)
                    {
                        if (promo.items != null)
                        {
                            foreach (var item in promo.items)
                            {
                                all_product_ids.Add(item.id_product);
                            }
                        }
                    }

                    var products = await _db_context.products
                        .Where(p => all_product_ids.Contains(p.id_product))
                        .ToDictionaryAsync(p => p.id_product);

                    // Los movimientos se_guardan aqui porque recien despues de insertar la nota
                    // se conoce su id, y es el que queda como FK en el kardex.
                    var stock_movimientos = new List<stock_movement>();

                    foreach (note_detail detail in details_list)
                    {
                        if (detail.id_product != null)
                        {
                            if (!products.TryGetValue(detail.id_product.Value, out var p))
                                throw new InvalidOperationException("Uno de los productos seleccionados ya no existe. Actualice el detalle de la nota.");

                            // Se congela el codigo del momento. Si el producto despues cambia de
                            // marca y el sistema le reasigna otro, esta nota sigue imprimiendo
                            // el codigo con el que se emitio.
                            detail.product_code_snapshot = p.product_code;

                            stock_movimientos.Add(await stock_movement_writer.registrar_salida_async(
                                _db_context, p, detail.quantity,
                                stock_movement.RazonVenta, stock_movement.DocumentoEntrega, new_note.note_number,
                                new_note.creation_date, detail.unit_price_usd,
                                estado_documento: new_note.status,
                                id_vendedor: new_note.id_seller,
                                id_cliente: new_note.id_customer,
                                vendido_como: stock_movement_writer.VendidoProducto));
                        }
                        else if (detail.id_promotion != null)
                        {
                            if (!promotions.TryGetValue(detail.id_promotion.Value, out var promo))
                                throw new InvalidOperationException("La promoción seleccionada ya no existe. Actualice el detalle de la nota.");
                            if (promo.items == null || promo.items.Count == 0)
                                throw new InvalidOperationException($"La promocion {promo.name} no tiene productos asignados.");

                            decimal precio_por_unidad = detail.quantity > 0 ? detail.unit_price_usd / detail.quantity : 0m;

                            detail.promo_items_snapshot = NinOS.Domain.promo_items_snapshot_store.serialize(
                                promo.items.Select(i => new NinOS.Domain.promo_item_snapshot
                                {
                                    id_product = i.id_product,
                                    quantity_required = i.quantity_required
                                }).ToList());

                            var movimientos_promo = await stock_movement_writer.mover_promocion_async(
                                _db_context, detail.promo_items_snapshot, promo, detail.quantity, es_salida: true,
                                stock_movement.RazonVenta, stock_movement.DocumentoEntrega, new_note.note_number,
                                new_note.creation_date, precio_por_unidad,
                                estado_documento: new_note.status, id_entrega: null, id_credito: null,
                                id_vendedor: new_note.id_seller, id_cliente: new_note.id_customer,
                                id_promocion: detail.id_promotion, descripcion_linea: promo.name);

                            stock_movimientos.AddRange(movimientos_promo);
                        }
                    }

                    if (new_note.note_type_id != null)
                    {
                        bool is_mar = await _db_context.note_types
                            .AsNoTracking()
                            .AnyAsync(t => t.id_note_type == new_note.note_type_id.Value && NoteTypeCodes.pro_venta_codes.Contains(t.code));

                        if (is_mar)
                        {
                            relacion relation = await get_or_create_week_relation_async(_db_context, new_note.creation_date);
                            new_note.id_relacion = relation.id_relacion;
                            new_note.adjusted_total_usd = original_amount_calculator.monto_al_emitir(
                                details_list.Select(d => (d.quantity, d.unit_price_usd)).ToList());
                            resumen_relacion resumen_previo = await ProVentaService.resumen_relacion_async(_db_context, relation.id_relacion);
                            if (relacion_pagada_aviso.debe_pedir_confirmacion(resumen_previo, confirmar_relacion_pagada))
                            {
                                AppLog.Warn($"Nota Pro Venta para la relacion pagada {relation.id_relacion}: se pide confirmacion.");
                                throw new relacion_pagada_exception(relation.id_relacion);
                            }
                            if (resumen_previo.esta_pagada && resumen_previo.notas_vigentes > 0)
                                AppLog.Warn($"Relacion {relation.id_relacion} estaba pagada y se reabre con la nota confirmada.");
                        }
                    }

                    await _db_context.delivery_notes.AddAsync(new_note);
                    try
                    {
                        await _db_context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (is_unique_note_number_violation(ex))
                    {
                        throw new InvalidOperationException("Ya existe una nota con ese correlativo. Verifique el número e intente de nuevo.");
                    }

                    foreach (stock_movement movimiento in stock_movimientos)
                    {
                        movimiento.id_delivery_note = new_note.id_delivery_note;
                    }

                    foreach (note_detail detail in details_list)
                    {
                        detail.id_delivery_note = new_note.id_delivery_note;
                        await _db_context.note_details.AddAsync(detail);
                    }

                    if (print_dto != null)
                    {
                        print_dto.note_number = new_note.note_number;
                        new_note.print_snapshot = NinOS.Domain.note_snapshot_store.serialize(print_dto);
                        new_note.print_snapshot_version = NinOS.Domain.note_snapshot_store.CurrentVersion;
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

        private static bool is_unique_note_number_violation(DbUpdateException ex)
        {
            Exception? base_exception = ex.GetBaseException();
            string message = base_exception?.Message ?? string.Empty;
            return message.Contains("23505")
                || message.Contains("IX_delivery_note_note_number")
                || message.Contains("duplicate key");
        }

        private static async Task<relacion> get_or_create_week_relation_async(NinOSDbContext db_context, DateTime creation_date_utc)
        {
            DateTime week_start = pro_venta_week.lunes_de(creation_date_utc);

            await db_context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({RelationLockKey})");

            relacion? existing = await db_context.relaciones
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.week_start == week_start && !r.es_hueca);
            if (existing != null)
            {
                AppLog.Info($"Nota asignada a la relacion {existing.id_relacion} de la semana {week_start:yyyy-MM-dd}.");
                return existing;
            }

            int next_number = (await db_context.relaciones.MaxAsync(r => (int?)r.relation_number) ?? 0) + 1;
            relacion relation = new relacion(next_number, week_start, week_start.AddDays(6));
            db_context.relaciones.Add(relation);
            await db_context.SaveChangesAsync();

            AppLog.Info($"Relacion {relation.id_relacion} creada para la semana {week_start:yyyy-MM-dd}.");
            return relation;
        }
    }
}
