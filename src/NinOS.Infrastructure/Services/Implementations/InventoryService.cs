using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class InventoryService : IInventoryService
    {
        private readonly IServiceScopeFactory _scope_factory;

        public InventoryService(IServiceScopeFactory scope_factory)
        {
            if (scope_factory == null) throw new ArgumentNullException(nameof(scope_factory));
            _scope_factory = scope_factory;
        }

        public async Task<IEnumerable<product>> get_all_products_async()
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.products.AsNoTracking()
                    .Where(p => p.is_active)
                    .OrderBy(p => p.product_code)
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<product>> get_deleted_products_async()
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.products.AsNoTracking()
                    .Where(p => !p.is_active)
                    .OrderByDescending(p => p.deleted_at)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Calcula el siguiente codigo correlativo de una marca con el estandar
        /// PREFIJO + 5 digitos. Cuenta tambien los productos que estan en la papelera:
        /// si no, un producto tirado a la basura liberaria su codigo y al restaurarlo
        /// quedaria duplicado.
        /// </summary>
        public async Task<string> get_next_product_code_async(string category)
        {
            if (!product_code_rules.TryGetPrefix(category, out _))
                return product_code_rules.EmptyCode;

            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await calcular_siguiente_codigo_async(db_context, category);
            }
        }

        public async Task add_product_async(product new_product)
        {
            if (new_product == null) throw new ArgumentNullException(nameof(new_product));
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                // El movimiento de carga inicial necesita el id del producto, que la base
                // solo genera al insertar. Por eso se guarda primero y despues se asienta
                // el kardex, dentro de la misma transaccion para que no queden mitades.
                await using var transaction = await db_context.Database.BeginTransactionAsync();
                try
                {
                    await db_context.products.AddAsync(new_product);
                    try
                    {
                        await db_context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (is_unique_product_code_violation(ex))
                    {
                        throw new InvalidOperationException(
                            $"Ya existe un producto con el codigo {new_product.product_code}. " +
                            "Cierre la ventana y vuelva a abrirla para obtener el siguiente codigo disponible.");
                    }

                    stock_movement_writer.registrar_carga_inicial(
                        db_context, new_product, $"ALTA-{new_product.product_code}");

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

        // Ultima red de seguridad del indice unico de product.product_code, para no
        // dejarle al usuario un PostgresException crudo.
        private static bool is_unique_product_code_violation(DbUpdateException ex)
        {
            Exception? base_exception = ex.GetBaseException();
            string message = base_exception?.Message ?? string.Empty;
            return message.Contains("23505")
                || message.Contains("IX_product_product_code")
                || message.Contains("duplicate key");
        }

        /// <summary>
        /// Edita un producto. El codigo y la marca NUNCA se tocan: el codigo lo asigna el
        /// sistema al crear y la marca define el prefijo, asi que dejarlos editables rompe
        /// el estandar. Si la cantidad cambia, queda asentada en el kardex como ajuste con
        /// el motivo que eligio el usuario.
        /// </summary>
        /// <summary>
        /// Edita un producto.
        ///
        /// El codigo solo lo cambia el sistema. Si nueva_categoria viene con una marca
        /// distinta a la actual, se recalcula el codigo con la serie de esa marca
        /// (PREFIJO + maximo + 1) y se asigna aqui, no desde la vista: la pantalla solo
        /// muestra una previsualizacion, y es aqui donde se decide de verdad para que no
        /// dependa de lo que haya mandado la pantalla.
        ///
        /// Si la cantidad cambia, queda asentada en el kardex con el motivo que eligio el
        /// usuario.
        /// </summary>
        public async Task update_product_async(product product_to_update, string? motivo_ajuste = null, string? nueva_categoria = null)
        {
            if (product_to_update == null) throw new ArgumentNullException(nameof(product_to_update));
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                product? actual = await db_context.products
                    .FirstOrDefaultAsync(p => p.id_product == product_to_update.id_product);

                if (actual == null) throw new InvalidOperationException("El producto ya no existe en el inventario.");

                string codigo_anterior = actual.product_code;
                string marca_anterior = actual.category;
                string codigo_ingresado = (product_to_update.product_code ?? string.Empty).Trim().ToUpperInvariant();

                bool cambia_marca = !string.IsNullOrWhiteSpace(nueva_categoria)
                    && !string.Equals(nueva_categoria!.Trim(), actual.category, StringComparison.OrdinalIgnoreCase);

                if (cambia_marca)
                {
                    string marca_destino = nueva_categoria!.Trim();

                    if (!product_code_rules.TryGetPrefix(marca_destino, out _))
                        throw new InvalidOperationException($"La marca {marca_destino} no tiene un prefijo de código configurado.");

                    string codigo_nuevo = await calcular_siguiente_codigo_async(db_context, marca_destino);

                    // El propio producto se excluye del correlativo: al recalcular sobre la
                    // misma marca (o si el codigo viejo caia en la serie destino) podria
                    // devolverse a si mismo y chocar contra el indice unico.
                    if (string.Equals(codigo_nuevo, codigo_anterior, StringComparison.OrdinalIgnoreCase))
                        codigo_nuevo = await calcular_siguiente_codigo_async(db_context, marca_destino, codigo_nuevo);

                    actual.category = marca_destino;
                    actual.product_code = codigo_nuevo;
                }
                else
                {
                    // No cambio de marca, pero el usuario pudo editar el codigo a mano. Nos aseguramos
                    // de que quede en mayusculas, sin espacios, y no choque con otro producto.
                    if (string.IsNullOrWhiteSpace(codigo_ingresado))
                    {
                        throw new InvalidOperationException("El código del producto no puede estar vacío.");
                    }

                    if (!string.Equals(codigo_ingresado, codigo_anterior, StringComparison.OrdinalIgnoreCase))
                    {
                        // Comprobamos que no exista otro producto con ese codigo.
                        var ya_existe = await db_context.products
                            .AsNoTracking()
                            .AnyAsync(p => p.product_code == codigo_ingresado && p.id_product != actual.id_product && p.deleted_at == null);
                        if (ya_existe)
                            throw new InvalidOperationException($"Ya existe un producto con el código {codigo_ingresado}. Por favor usa otro código.");
                    }

                    actual.product_code = codigo_ingresado;
                }

                actual.name = product_to_update.name;
                actual.unit_price_usd = product_to_update.unit_price_usd;

                int diferencia = product_to_update.stock_quantity - actual.stock_quantity;
                actual.stock_quantity = product_to_update.stock_quantity;

                // Sincronizamos los valores finales en el objeto recibido
                product_to_update.product_code = actual.product_code;
                product_to_update.category = actual.category;

                // El producto y su asiento en el kardex van juntos: si el kardex es la fuente
                // de la verdad del stock, un ajuste a medias dejaria la cantidad real
                // distinta de la que suma el historial.
                await using var transaction = await db_context.Database.BeginTransactionAsync();
                try
                {
                    try
                    {
                        await db_context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (is_unique_product_code_violation(ex))
                    {
                        throw new InvalidOperationException(
                            $"El código {actual.product_code} ya pertenece a otro producto.");
                    }

                    // Si el usuario edito el stock a mano, queda asentado en el kardex como ajuste.
                    if (diferencia != 0)
                    {
                        stock_movement_writer.registrar_ajuste(
                            db_context,
                            actual.id_product,
                            diferencia,
                            $"AJUSTE-{actual.product_code}",
                            DateTime.UtcNow,
                            actual.unit_price_usd,
                            motivo_ajuste);

                        await db_context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                if (cambia_marca)
                {
                    AppLog.Info($"Producto {actual.id_product} mudado de marca: " +
                                $"{codigo_anterior} ({marca_anterior}) -> {actual.product_code} ({actual.category}).");
                }
            }
        }

        /// <summary>
        /// Correlativo de una marca: PREFIJO + maximo + 1. Cuenta los productos de la
        /// papelera porque su codigo sigue reservado mientras esten ahi.
        /// Si codigo_reservado viene informado, se salta para no devolverlo.
        /// </summary>
        private static async Task<string> calcular_siguiente_codigo_async(NinOSDbContext db_context, string category, string? codigo_reservado = null)
        {
            if (!product_code_rules.TryGetPrefix(category, out string prefix))
                return product_code_rules.EmptyCode;

            int digits = product_code_rules.DigitsFor(prefix);

            List<string> codes = await db_context.products
                .AsNoTracking()
                .Select(p => p.product_code)
                .ToListAsync();

            int max_number = 0;
            var ya_usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string code in codes)
            {
                if (string.IsNullOrWhiteSpace(code)) continue;

                string limpio = code.Trim();
                ya_usados.Add(limpio);

                int? number = product_code_rules.TryParseNumber(limpio, prefix);
                if (number.HasValue && number.Value > max_number)
                {
                    max_number = number.Value;
                }
            }

            int next_number = max_number + 1;
            while (ya_usados.Contains(product_code_rules.Format(prefix, next_number, digits))
                   || string.Equals(product_code_rules.Format(prefix, next_number, digits), codigo_reservado, StringComparison.OrdinalIgnoreCase))
            {
                next_number++;
            }

            return product_code_rules.Format(prefix, next_number, digits);
        }

        public async Task<IEnumerable<promotion>> get_promotions_using_product_async(int id_product)
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.promotions
                    .AsNoTracking()
                    .Where(p => p.is_active && p.items.Any(i => i.id_product == id_product))
                    .OrderBy(p => p.promotion_code)
                    .ToListAsync();
            }
        }

        public async Task soft_delete_product_async(int id_product, string? reason)
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                product? product_to_delete = await db_context.products.FirstOrDefaultAsync(p => p.id_product == id_product);
                if (product_to_delete == null || !product_to_delete.is_active) return;

                product_to_delete.is_active = false;
                product_to_delete.deleted_at = DateTime.UtcNow;
                product_to_delete.deleted_reason = string.IsNullOrWhiteSpace(reason) ? "Sin motivo" : reason.Trim();

                // Una promoción sin sus productos no tiene sentido, así que se va con el producto.
                List<promotion> affected_promos = await db_context.promotions
                    .Where(p => p.is_active && p.items.Any(i => i.id_product == id_product))
                    .ToListAsync();

                DateTime deleted_at = DateTime.UtcNow;
                foreach (promotion promo in affected_promos)
                {
                    promo.is_active = false;
                    promo.deleted_at = deleted_at;
                    promo.deleted_reason = cascade_delete_reason(product_to_delete.product_code);
                    promo.id_product_deleted_cascade = id_product;
                }

                await db_context.SaveChangesAsync();
                AppLog.Info($"Producto {product_to_delete.product_code} enviado a la papelera. Motivo: {product_to_delete.deleted_reason}");
                if (affected_promos.Count > 0)
                {
                    AppLog.Info($"Promociones eliminadas junto con el producto {product_to_delete.product_code}: " +
                                string.Join(", ", affected_promos.Select(p => p.promotion_code)));
                }
            }
        }

        // Reason con el que se muestran las promociones que caen junto con su producto.
        // Solo es texto para el usuario: la clave para saber si hay que restaurarlas es
        // id_product_deleted_cascade, porque el codigo del producto puede cambiar de marca.
        internal static string cascade_delete_reason(string product_code)
            => $"Eliminado junto con el producto {product_code}";

        public async Task<int> restore_product_async(int id_product)
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                product? product_to_restore = await db_context.products.FirstOrDefaultAsync(p => p.id_product == id_product);
                if (product_to_restore == null || product_to_restore.is_active) return 0;

                product_to_restore.is_active = true;
                product_to_restore.deleted_at = null;
                product_to_restore.deleted_reason = null;

                // Se devuelven las promociones que se fueron con ESE producto. Antes se
                // comparaba el texto del motivo contra el codigo actual, asi que al cambiar
                // el producto de marca estas promociones quedaban atrapadas en la papelera.
                List<promotion> promos_to_restore = await db_context.promotions
                    .Where(p => !p.is_active && p.id_product_deleted_cascade == id_product)
                    .ToListAsync();

                foreach (promotion promo in promos_to_restore)
                {
                    promo.is_active = true;
                    promo.deleted_at = null;
                    promo.deleted_reason = null;
                    promo.id_product_deleted_cascade = null;
                }

                await db_context.SaveChangesAsync();
                AppLog.Info($"Producto {product_to_restore.product_code} restaurado desde la papelera. " +
                            $"Promociones devueltas: {promos_to_restore.Count}.");
                return promos_to_restore.Count;
            }
        }

        public async Task<IEnumerable<promotion>> get_all_promotions_async()
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.promotions
                    .AsNoTracking()
                    .Where(p => p.is_active)
                    .Include(p => p.items)
                    .ThenInclude(i => i.product)
                    .OrderBy(p => p.promotion_code)
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<promotion>> get_deleted_promotions_async()
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db_context.promotions
                    .AsNoTracking()
                    .Where(p => !p.is_active)
                    .Include(p => p.items)
                    .OrderByDescending(p => p.deleted_at)
                    .ToListAsync();
            }
        }

        public async Task add_promotion_async(promotion new_promotion)
        {
            if (new_promotion == null) throw new ArgumentNullException(nameof(new_promotion));
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                await db_context.promotions.AddAsync(new_promotion);
                await db_context.SaveChangesAsync();
            }
        }

        public async Task update_promotion_async(promotion promotion_to_update)
        {
            if (promotion_to_update == null) throw new ArgumentNullException(nameof(promotion_to_update));
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                
                promotion? existing_promo = await db_context.promotions
                    .Include(p => p.items)
                    .FirstOrDefaultAsync(p => p.id_promotion == promotion_to_update.id_promotion);
                
                if (existing_promo != null)
                {
                    existing_promo.name = promotion_to_update.name;
                    existing_promo.unit_price_usd = promotion_to_update.unit_price_usd;
                    existing_promo.category = promotion_to_update.category;
                    
                    db_context.promotion_items.RemoveRange(existing_promo.items);
                    existing_promo.items.Clear();
                    
                    foreach (promotion_item item in promotion_to_update.items)
                    {
                        existing_promo.items.Add(new promotion_item(item.id_product, item.quantity_required));
                    }
                    
                    db_context.promotions.Update(existing_promo);
                    await db_context.SaveChangesAsync();
                }
            }
        }

        public async Task soft_delete_promotion_async(int id_promotion, string? reason)
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                promotion? promo_to_delete = await db_context.promotions
                    .FirstOrDefaultAsync(p => p.id_promotion == id_promotion);
                if (promo_to_delete == null || !promo_to_delete.is_active) return;

                promo_to_delete.is_active = false;
                promo_to_delete.deleted_at = DateTime.UtcNow;
                promo_to_delete.deleted_reason = string.IsNullOrWhiteSpace(reason)
                    ? "Eliminado desde el inventario"
                    : reason;

                await db_context.SaveChangesAsync();

                AppLog.Info($"Promoción {promo_to_delete.promotion_code} eliminada. Motivo: {promo_to_delete.deleted_reason}");
            }
        }

        public async Task restore_promotion_async(int id_promotion)
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                promotion? promo_to_restore = await db_context.promotions
                    .FirstOrDefaultAsync(p => p.id_promotion == id_promotion);
                if (promo_to_restore == null || promo_to_restore.is_active) return;

                promo_to_restore.is_active = true;
                promo_to_restore.deleted_at = null;
                promo_to_restore.deleted_reason = null;
                promo_to_restore.id_product_deleted_cascade = null;

                await db_context.SaveChangesAsync();

                AppLog.Info($"Promoción {promo_to_restore.promotion_code} restaurada.");
            }
        }

        public async Task<IEnumerable<product_sales_history_dto>> get_product_sales_history_async(int id_product)
        {
            return await with_connection_retry(
                () => get_product_sales_history_intento_async(id_product),
                "get_product_sales_history_async");
        }

        private async Task<IEnumerable<product_sales_history_dto>> get_product_sales_history_intento_async(int id_product)
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                // El kardex es la fuente de la verdad: aqui ya estan las ventas, las salidas por
                // obsequio, las entradas por devolucion, las anulaciones y los ajustes manuales.
                var movements = await db.stock_movements
                    .AsNoTracking()
                    .Where(m => m.id_product == id_product)
                    .OrderByDescending(m => m.movement_date)
                    .ThenByDescending(m => m.id_stock_movement)
                    .ToListAsync();

                if (movements.Count == 0) return Enumerable.Empty<product_sales_history_dto>();

                var customer_ids = movements
                    .Where(m => m.id_customer != null)
                    .Select(m => m.id_customer!.Value)
                    .Distinct()
                    .ToList();

                var customers = customer_ids.Count == 0
                    ? new Dictionary<int, customer>()
                    : await db.customers
                        .AsNoTracking()
                        .Where(c => customer_ids.Contains(c.id_customer))
                        .ToDictionaryAsync(c => c.id_customer);

                var seller_ids = movements
                    .Where(m => m.id_seller != null)
                    .Select(m => m.id_seller!.Value)
                    .Distinct()
                    .ToList();

                var sellers = seller_ids.Count == 0
                    ? new Dictionary<int, seller>()
                    : await db.sellers
                        .AsNoTracking()
                        .Where(s => seller_ids.Contains(s.id_seller))
                        .ToDictionaryAsync(s => s.id_seller);

                var result = new List<product_sales_history_dto>();

                foreach (stock_movement m in movements)
                {
                    bool es_entrada = m.movement_type == stock_movement.Entrada;
                    int unidades = m.promotion_units ?? m.quantity;

                    result.Add(new product_sales_history_dto
                    {
                        id_delivery_note = m.id_delivery_note,
                        note_number = m.document_number,
                        creation_date = m.movement_date,
                        customer_name = m.id_customer != null && customers.TryGetValue(m.id_customer.Value, out var c)
                            ? c.business_name
                            : string.Empty,
                        seller_name = m.id_seller != null && sellers.TryGetValue(m.id_seller.Value, out var s)
                            ? s.full_name
                            : string.Empty,
                        line_description = m.line_description ?? string.Empty,
                        sold_as = m.sold_as ?? string.Empty,
                        units_sold = unidades,
                        unit_price_usd = m.unit_price_usd,
                        line_subtotal_usd = m.unit_price_usd * unidades,
                        status = m.document_status ?? string.Empty,
                        movement_type = m.movement_type,
                        movement_reason = m.reason,
                        document_type = m.document_type,
                        is_credit_note = m.id_credit_note != null
                    });
                }

                return result;
            }
        }

        public async Task<IEnumerable<promotion_sales_history_dto>> get_promotion_sales_history_async(int id_promotion)
        {
            return await with_connection_retry(
                () => get_promotion_sales_history_intento_async(id_promotion),
                "get_promotion_sales_history_async");
        }

        private async Task<IEnumerable<promotion_sales_history_dto>> get_promotion_sales_history_intento_async(int id_promotion)
        {
            using (IServiceScope scope = _scope_factory.CreateScope())
            {
                NinOSDbContext db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                // La promocion genera un movimiento por cada producto que la compone;
                // para la grilla se agrupa por documento y se muestra la cantidad de
                // promociones vendidas/devueltas (promotion_units) y su precio unitario.
                var movements = await db.stock_movements
                    .AsNoTracking()
                    .Where(m => m.id_promotion == id_promotion)
                    .OrderByDescending(m => m.movement_date)
                    .ThenByDescending(m => m.id_stock_movement)
                    .ToListAsync();

                if (movements.Count == 0) return Enumerable.Empty<promotion_sales_history_dto>();

                var customer_ids = movements
                    .Where(m => m.id_customer != null)
                    .Select(m => m.id_customer!.Value)
                    .Distinct()
                    .ToList();

                var customers = customer_ids.Count == 0
                    ? new Dictionary<int, customer>()
                    : await db.customers
                        .AsNoTracking()
                        .Where(c => customer_ids.Contains(c.id_customer))
                        .ToDictionaryAsync(c => c.id_customer);

                var seller_ids = movements
                    .Where(m => m.id_seller != null)
                    .Select(m => m.id_seller!.Value)
                    .Distinct()
                    .ToList();

                var sellers = seller_ids.Count == 0
                    ? new Dictionary<int, seller>()
                    : await db.sellers
                        .AsNoTracking()
                        .Where(s => seller_ids.Contains(s.id_seller))
                        .ToDictionaryAsync(s => s.id_seller);

                var result = new List<promotion_sales_history_dto>();

                var groups = movements.GroupBy(m => new
                {
                    m.id_delivery_note,
                    m.id_credit_note,
                    m.document_number,
                    m.movement_type,
                    m.reason,
                    m.document_type,
                    m.document_status,
                    m.id_customer,
                    m.id_seller,
                    m.unit_price_usd,
                    m.movement_date
                });

                foreach (var group in groups)
                {
                    stock_movement first = group.First();
                    int unidades = group.Max(x => x.promotion_units ?? x.quantity);

                    result.Add(new promotion_sales_history_dto
                    {
                        id_delivery_note = first.id_delivery_note,
                        note_number = first.document_number,
                        creation_date = first.movement_date,
                        customer_name = first.id_customer != null && customers.TryGetValue(first.id_customer.Value, out var c)
                            ? c.business_name
                            : string.Empty,
                        seller_name = first.id_seller != null && sellers.TryGetValue(first.id_seller.Value, out var s)
                            ? s.full_name
                            : string.Empty,
                        line_description = first.line_description ?? string.Empty,
                        sold_as = first.sold_as ?? string.Empty,
                        units_sold = unidades,
                        unit_price_usd = first.unit_price_usd,
                        line_subtotal_usd = first.unit_price_usd * unidades,
                        status = first.document_status ?? string.Empty,
                        movement_type = first.movement_type,
                        movement_reason = first.reason,
                        document_type = first.document_type,
                        is_credit_note = first.id_credit_note != null
                    });
                }

                return result
                    .OrderByDescending(r => r.creation_date)
                    .ThenByDescending(r => r.id_delivery_note)
                    .ToList();
            }
        }

        /// <summary>
        /// La base de datos esta en un VPS remoto. Cuando la conexion lleva un rato
        /// sin usarse, el servidor o el firewall pueden cortarla y la primera consulta
        /// falla aunque el internet este bien. En ese caso se reintenta una vez con una
        /// conexion nueva antes de mostrarle el error al usuario.
        /// </summary>
        private static async Task<T> with_connection_retry<T>(Func<Task<T>> operacion, string origen)
        {
            for (int intento = 1; ; intento++)
            {
                try
                {
                    return await operacion();
                }
                catch (Exception ex) when (intento == 1 && is_error_de_conexion(ex))
                {
                    AppLog.Warn($"Conexion interrumpida en {origen}, se reintenta una vez. " +
                                $"Detalle: {ex.GetType().Name}: {ex.Message}");
                    await Task.Delay(700);
                }
            }
        }

        private static bool is_error_de_conexion(Exception exception)
        {
            Exception? actual = exception;
            int depth = 0;

            while (actual != null && depth < 8)
            {
                string tipo = actual.GetType().Name;
                if (tipo.Contains("Npgsql") || tipo.Contains("Socket") ||
                    tipo.Contains("Timeout") || tipo.Contains("IOException"))
                    return true;

                string mensaje = (actual.Message ?? string.Empty).ToLowerInvariant();
                if (mensaje.Contains("connection") || mensaje.Contains("conexión") ||
                    mensaje.Contains("conexion") || mensaje.Contains("timeout") ||
                    mensaje.Contains("broken pipe") || mensaje.Contains("no such host"))
                    return true;

                actual = actual.InnerException;
                depth++;
            }

            return false;
        }
    }
}