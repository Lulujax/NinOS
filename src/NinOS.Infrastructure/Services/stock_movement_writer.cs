using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;

namespace NinOS.Infrastructure.Services
{
    /// <summary>
    /// Punto unico por donde pasan TODOS los cambios de existencia.
    ///
    /// Spec 003: el stock se calcula EN LA BASE, no en memoria. Antes se leia la cantidad,
    /// se sumaba o restaba en la PC y se guardaba el numero final: con dos personas
    /// despachando el mismo producto a la vez, la ultima pisaba a la primera y se perdian
    /// unidades. Ahora cada movimiento es un UPDATE atomico
    /// (stock_quantity = stock_quantity + delta) dentro de la transaccion de quien llama,
    /// asi que PostgreSQL bloquea la fila y los dos movimientos se suman.
    ///
    /// Importante: NO se le asigna nada a producto.stock_quantity. Si se hiciera, EF Core
    /// escribiria despues el valor viejo que tenia en memoria y se perderia el cambio.
    /// La verdad del stock vive en la base.
    /// </summary>
    public static class stock_movement_writer
    {
        public const string VendidoProducto = "Producto";
        public const string VendidoPromocion = "Promocion";

        /// <summary>
        /// Estado con el que se marcan los ajustes manuales, para que la columna ESTADO del
        /// historial no salga vacia en las filas que no vienen de una nota.
        /// </summary>
        public const string EstadoAjuste = "Ajuste manual";

        /// <summary>
        /// Toma el bloqueo de varios productos en orden de id, antes de empezar a moverlos.
        ///
        /// Sin esto, dos notas con los mismos productos en orden distinto pueden quedar
        /// trabadas una esperando a la otra (deadlock) y PostgreSQL aborta una.
        /// </summary>
        public static async Task reservar_productos_async(NinOSDbContext db_context, IEnumerable<int> ids_de_producto)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));
            if (ids_de_producto == null) return;

            int[] ids = ids_de_producto.Distinct().OrderBy(id => id).ToArray();
            if (ids.Length == 0) return;

            await ejecutar_async(
                db_context,
                "SELECT id_product FROM product WHERE id_product = ANY(@ids) ORDER BY id_product FOR UPDATE",
                new Dictionary<string, object> { ["@ids"] = ids },
                espera_resultado: false);
        }

        // Descuenta del stock y registra la SALIDA.
        public static Task<stock_movement> registrar_salida_async(
            NinOSDbContext db_context,
            product producto,
            int cantidad,
            string razon,
            string tipo_documento,
            string numero_documento,
            DateTime fecha,
            decimal precio_unitario,
            string? estado_documento = null,
            int? id_entrega = null,
            int? id_credito = null,
            int? id_vendedor = null,
            int? id_cliente = null,
            int? id_promocion = null,
            int? unidades_promocion = null,
            string vendido_como = VendidoProducto,
            string? descripcion_linea = null)
        {
            return aplicar_async(db_context, producto, cantidad, stock_movement.Salida, razon, tipo_documento,
                numero_documento, fecha, precio_unitario, estado_documento, id_entrega, id_credito,
                id_vendedor, id_cliente, id_promocion, unidades_promocion, vendido_como, descripcion_linea);
        }

        // Suma al stock y registra la ENTRADA.
        public static Task<stock_movement> registrar_entrada_async(
            NinOSDbContext db_context,
            product producto,
            int cantidad,
            string razon,
            string tipo_documento,
            string numero_documento,
            DateTime fecha,
            decimal precio_unitario,
            string? estado_documento = null,
            int? id_entrega = null,
            int? id_credito = null,
            int? id_vendedor = null,
            int? id_cliente = null,
            int? id_promocion = null,
            int? unidades_promocion = null,
            string vendido_como = VendidoProducto,
            string? descripcion_linea = null)
        {
            return aplicar_async(db_context, producto, cantidad, stock_movement.Entrada, razon, tipo_documento,
                numero_documento, fecha, precio_unitario, estado_documento, id_entrega, id_credito,
                id_vendedor, id_cliente, id_promocion, unidades_promocion, vendido_como, descripcion_linea);
        }

        /// <summary>
        /// Suma o resta una cantidad al stock actual de la base y lo asienta como ajuste.
        /// delta positivo agrega, negativo quita. Devuelve el stock que quedo.
        ///
        /// Se calcula sobre el stock REAL del momento, no sobre el que tenia la pantalla:
        /// si otra PC vendio mientras el formulario estaba abierto, ese movimiento no se pierde.
        /// </summary>
        public static async Task<int> ajustar_stock_async(
            NinOSDbContext db_context,
            int id_product,
            int delta,
            string numero_documento,
            decimal precio_unitario,
            string? motivo_ajuste = null,
            string? nota = null)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));
            if (id_product <= 0) throw new ArgumentException("El producto del ajuste es invalido.", nameof(id_product));

            if (delta == 0) return await leer_stock_async(db_context, id_product);

            int? resultante = await mover_stock_async(db_context, id_product, delta, permite_negativo: false);

            if (resultante == null)
            {
                int disponible = await leer_stock_async(db_context, id_product);
                throw new InvalidOperationException(
                    $"No se puede quitar {Math.Abs(delta)} unidad(es): el stock disponible es {disponible}.");
            }

            asentar_ajuste(db_context, id_product, delta, numero_documento, precio_unitario, motivo_ajuste, nota);
            return resultante.Value;
        }

        /// <summary>
        /// Deja el stock exactamente en cantidad_contada (conteo manual) y asienta la
        /// diferencia contra el stock REAL del momento, no contra el que vio la pantalla.
        ///
        /// Devuelve la diferencia asentada. Si stock_visto se informa y ya no coincide con la
        /// base, lanza excepcion: la pantalla tiene que avisar y pedir confirmacion antes de
        /// pisar un movimiento que hizo otra persona.
        /// </summary>
        public static async Task<int> fijar_stock_async(
            NinOSDbContext db_context,
            int id_product,
            int cantidad_contada,
            int? stock_visto,
            string numero_documento,
            decimal precio_unitario,
            string? motivo_ajuste = null,
            string? nota = null)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));
            if (id_product <= 0) throw new ArgumentException("El producto del conteo es invalido.", nameof(id_product));

            await reservar_productos_async(db_context, new[] { id_product });

            int stock_real = await leer_stock_async(db_context, id_product);

            if (stock_visto.HasValue && stock_visto.Value != stock_real)
            {
                throw new stock_cambiado_exception(stock_visto.Value, stock_real);
            }

            int diferencia = cantidad_contada - stock_real;
            if (diferencia == 0) return 0;

            int? resultante = await mover_stock_async(db_context, id_product, diferencia, permite_negativo: true);
            if (resultante == null)
                throw new InvalidOperationException("El producto del conteo ya no existe en el inventario.");

            asentar_ajuste(db_context, id_product, diferencia, numero_documento, precio_unitario, motivo_ajuste, nota);
            return diferencia;
        }

        /// <summary>
        /// Alta de un producto: el stock inicial ya viene en el producto que se acaba de
        /// insertar, asi que aqui no se vuelve a sumar, solo queda el movimiento de entrada.
        /// </summary>
        public static void registrar_carga_inicial(
            NinOSDbContext db_context,
            product producto,
            string numero_documento)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));
            if (producto == null) throw new ArgumentNullException(nameof(producto));
            if (producto.stock_quantity <= 0) return;

            var movimiento = new stock_movement(
                DateTime.UtcNow,
                producto.id_product,
                producto.stock_quantity,
                stock_movement.Entrada,
                stock_movement.RazonCargaInicial,
                stock_movement.DocumentoInventario,
                numero_documento,
                VendidoProducto,
                producto.unit_price_usd);

            db_context.stock_movements.Add(movimiento);
        }

        public static async Task<int> leer_stock_async(NinOSDbContext db_context, int id_product)
        {
            object? valor = await ejecutar_async(
                db_context,
                "SELECT stock_quantity FROM product WHERE id_product = @id",
                new Dictionary<string, object> { ["@id"] = id_product },
                espera_resultado: true);

            return valor == null || valor is DBNull ? 0 : Convert.ToInt32(valor);
        }

        private static async Task<stock_movement> aplicar_async(
            NinOSDbContext db_context,
            product producto,
            int cantidad,
            string movement_type,
            string razon,
            string tipo_documento,
            string numero_documento,
            DateTime fecha,
            decimal precio_unitario,
            string? estado_documento,
            int? id_entrega,
            int? id_credito,
            int? id_vendedor,
            int? id_cliente,
            int? id_promocion,
            int? unidades_promocion,
            string vendido_como,
            string? descripcion_linea)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));
            if (producto == null) throw new ArgumentNullException(nameof(producto));
            if (cantidad <= 0) throw new InvalidOperationException("La cantidad del movimiento debe ser mayor que cero.");

            // Las notas de entrega pueden dejar el stock en negativo (decision del cliente).
            // El resto de documentos no: ahi un negativo es un error de captura.
            bool permite_negativo = tipo_documento == stock_movement.DocumentoEntrega;
            int delta = movement_type == stock_movement.Salida ? -cantidad : cantidad;

            int? resultante = await mover_stock_async(db_context, producto.id_product, delta, permite_negativo);

            if (resultante == null)
            {
                int disponible = await leer_stock_async(db_context, producto.id_product);
                throw new InvalidOperationException(
                    $"Stock insuficiente para {producto.name} (disponible: {disponible}, se necesitan: {cantidad}).");
            }

            var movimiento = new stock_movement(
                fecha,
                producto.id_product,
                cantidad,
                movement_type,
                razon,
                tipo_documento,
                numero_documento,
                vendido_como,
                precio_unitario)
            {
                document_status = estado_documento,
                id_delivery_note = id_entrega,
                id_credit_note = id_credito,
                id_seller = id_vendedor,
                id_customer = id_cliente,
                id_promotion = id_promocion,
                promotion_units = unidades_promocion,
                line_description = descripcion_linea
            };

            db_context.stock_movements.Add(movimiento);

            return movimiento;
        }

        public static async Task<List<stock_movement>> mover_promocion_async(
            NinOSDbContext db_context,
            string? items_snapshot_json,
            promotion? promocion_actual,
            int cantidad_lineas,
            bool es_salida,
            string razon,
            string tipo_documento,
            string numero_documento,
            DateTime fecha,
            decimal precio_por_unidad,
            string? estado_documento,
            int? id_entrega,
            int? id_credito,
            int? id_vendedor,
            int? id_cliente,
            int? id_promocion,
            string? descripcion_linea)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));
            if (cantidad_lineas <= 0) throw new InvalidOperationException("La cantidad de la linea de promocion debe ser mayor que cero.");

            List<promo_item_snapshot>? items = promo_items_snapshot_store.deserialize(items_snapshot_json);

            if (items == null)
            {
                if (promocion_actual?.items == null || promocion_actual.items.Count == 0)
                    throw new InvalidOperationException(
                        $"No se puede revertir el stock de '{descripcion_linea}': no tiene composicion guardada y la promocion ya no existe o no tiene productos.");

                AppLog.Warn($"Linea de promocion sin foto guardada ({descripcion_linea}): se usa la composicion actual de la promocion.");
                items = promocion_actual.items
                    .Select(i => new promo_item_snapshot { id_product = i.id_product, quantity_required = i.quantity_required })
                    .ToList();
            }

            if (items.Count == 0)
                throw new InvalidOperationException($"La promocion '{descripcion_linea}' no tiene productos en su composicion.");

            var product_ids = items.Select(i => i.id_product).Distinct().ToList();
            var products = await db_context.products
                .Where(p => product_ids.Contains(p.id_product))
                .ToDictionaryAsync(p => p.id_product);

            foreach (var item in items)
            {
                if (!products.ContainsKey(item.id_product))
                    throw new InvalidOperationException(
                        $"No se puede revertir el stock de '{descripcion_linea}': el producto {item.id_product} de su composicion ya no existe.");
            }

            var movimientos = new List<stock_movement>();
            foreach (var item in items)
            {
                int cantidad = cantidad_lineas * item.quantity_required;
                product producto = products[item.id_product];

                stock_movement movimiento = es_salida
                    ? await registrar_salida_async(db_context, producto, cantidad, razon, tipo_documento, numero_documento,
                        fecha, precio_por_unidad, estado_documento, id_entrega, id_credito, id_vendedor, id_cliente,
                        id_promocion, cantidad_lineas, VendidoPromocion, descripcion_linea)
                    : await registrar_entrada_async(db_context, producto, cantidad, razon, tipo_documento, numero_documento,
                        fecha, precio_por_unidad, estado_documento, id_entrega, id_credito, id_vendedor, id_cliente,
                        id_promocion, cantidad_lineas, VendidoPromocion, descripcion_linea);

                movimientos.Add(movimiento);
            }

            return movimientos;
        }

        /// <summary>
        /// El UPDATE atomico. Devuelve el stock que quedo, o null si no se pudo (producto
        /// inexistente, o quedaria negativo sin permitirlo).
        /// </summary>
        private static async Task<int?> mover_stock_async(
            NinOSDbContext db_context, int id_product, int delta, bool permite_negativo)
        {
            object? valor = await ejecutar_async(
                db_context,
                "UPDATE product SET stock_quantity = stock_quantity + @delta " +
                "WHERE id_product = @id AND (@permite OR stock_quantity + @delta >= 0) " +
                "RETURNING stock_quantity",
                new Dictionary<string, object>
                {
                    ["@delta"] = delta,
                    ["@id"] = id_product,
                    ["@permite"] = permite_negativo
                },
                espera_resultado: true);

            return valor == null || valor is DBNull ? null : Convert.ToInt32(valor);
        }

        private static void asentar_ajuste(
            NinOSDbContext db_context,
            int id_product,
            int diferencia,
            string numero_documento,
            decimal precio_unitario,
            string? motivo_ajuste,
            string? nota)
        {
            string razon = stock_movement.IsKnownAdjustmentReason(motivo_ajuste)
                ? motivo_ajuste!.Trim().ToUpperInvariant()
                : stock_movement.RazonAjuste;

            // La columna es varchar(200): se recorta aqui para no reventar el insert.
            string? nota_limpia = string.IsNullOrWhiteSpace(nota) ? null : nota!.Trim();
            if (nota_limpia != null && nota_limpia.Length > 200)
            {
                nota_limpia = nota_limpia.Substring(0, 200);
            }

            var movimiento = new stock_movement(
                DateTime.UtcNow,
                id_product,
                Math.Abs(diferencia),
                diferencia > 0 ? stock_movement.Entrada : stock_movement.Salida,
                razon,
                stock_movement.DocumentoInventario,
                numero_documento,
                VendidoProducto,
                precio_unitario)
            {
                document_status = EstadoAjuste,
                line_description = nota_limpia
            };

            db_context.stock_movements.Add(movimiento);
        }

        /// <summary>
        /// Ejecuta SQL en la MISMA conexion y la MISMA transaccion que abrio quien llama.
        /// Si no se enlistara en la transaccion, el movimiento de stock quedaria confirmado
        /// aunque el resto de la operacion se deshiciera.
        /// </summary>
        private static async Task<object?> ejecutar_async(
            NinOSDbContext db_context,
            string sql,
            Dictionary<string, object> parametros,
            bool espera_resultado)
        {
            DbConnection connection = db_context.Database.GetDbConnection();
            bool la_abrimos_nosotros = connection.State != ConnectionState.Open;
            if (la_abrimos_nosotros) await connection.OpenAsync();

            try
            {
                using DbCommand comando = connection.CreateCommand();
                comando.CommandText = sql;
                comando.Transaction = db_context.Database.CurrentTransaction?.GetDbTransaction();

                foreach (KeyValuePair<string, object> parametro in parametros)
                {
                    DbParameter nuevo = comando.CreateParameter();
                    nuevo.ParameterName = parametro.Key;
                    nuevo.Value = parametro.Value;
                    comando.Parameters.Add(nuevo);
                }

                if (espera_resultado) return await comando.ExecuteScalarAsync();

                await comando.ExecuteNonQueryAsync();
                return null;
            }
            finally
            {
                if (la_abrimos_nosotros) await connection.CloseAsync();
            }
        }
    }

    /// <summary>
    /// El stock cambio entre que la pantalla lo mostro y el usuario guardo el conteo.
    /// La pantalla usa los dos valores para avisar y pedir confirmacion.
    /// </summary>
    public class stock_cambiado_exception : InvalidOperationException
    {
        public int stock_visto { get; }
        public int stock_real { get; }

        public stock_cambiado_exception(int stock_visto, int stock_real)
            : base($"El stock cambio mientras llenabas el formulario: viste {stock_visto} y ahora hay {stock_real}.")
        {
            this.stock_visto = stock_visto;
            this.stock_real = stock_real;
        }
    }
}
