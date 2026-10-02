using System;
using NinOS.Domain;
using NinOS.Infrastructure.Data;

namespace NinOS.Infrastructure.Services
{
    // Punto unico por donde deben pasar TODAS las mudanÃ§as de existencia.
    // Cada llamada descuenta o suma el stock y deja el movimiento registrado en
    // stock_movement, en la misma transaccion y con el mismo DbContext de quien la abrio
    // (nota de entrega, nota de credito, anulacion, ajuste manual, carga inicial).
    public static class stock_movement_writer
    {
        public const string VendidoProducto = "Producto";
        public const string VendidoPromocion = "Promocion";

        // Estado con el que se marcan los ajustes manuales, para que la columna ESTADO
        // del historial no salga vacia en las filas que no vienen de una nota.
        public const string EstadoAjuste = "Ajuste manual";

        // Descuenta del stock y registra la SALIDA.

        public static stock_movement registrar_salida(
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
            return aplicar(db_context, producto, cantidad, stock_movement.Salida, razon, tipo_documento,
                numero_documento, fecha, precio_unitario, estado_documento, id_entrega, id_credito,
                id_vendedor, id_cliente, id_promocion, unidades_promocion, vendido_como, descripcion_linea);
        }

        // Suma al stock y registra la ENTRADA.
        public static stock_movement registrar_entrada(
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
            return aplicar(db_context, producto, cantidad, stock_movement.Entrada, razon, tipo_documento,
                numero_documento, fecha, precio_unitario, estado_documento, id_entrega, id_credito,
                id_vendedor, id_cliente, id_promocion, unidades_promocion, vendido_como, descripcion_linea);
        }

        // Registra el movimiento de un ajuste manual: el stock ya viene cambiado en el
        // producto (por eso no se toca aqui), solo queda dejar constancia de la diferencia.
        // motivo_ajuste es lo que eligio el usuario en el formulario (reposicion, merma,
        // correccion de conteo...); si no se reconoce, queda el generico "AJUSTE".
        public static void registrar_ajuste(
            NinOSDbContext db_context,
            int id_product,
            int diferencia,
            string numero_documento,
            DateTime fecha,
            decimal precio_unitario,
            string? motivo_ajuste = null)
        {
            if (diferencia == 0) return;

            string razon = stock_movement.IsKnownAdjustmentReason(motivo_ajuste)
                ? motivo_ajuste!.Trim().ToUpperInvariant()
                : stock_movement.RazonAjuste;

            var movimiento = new stock_movement(
                fecha,
                id_product,
                Math.Abs(diferencia),
                diferencia > 0 ? stock_movement.Entrada : stock_movement.Salida,
                razon,
                stock_movement.DocumentoInventario,
                numero_documento,
                VendidoProducto,
                precio_unitario)
            {
                document_status = EstadoAjuste
            };

            db_context.stock_movements.Add(movimiento);
        }

        // Alta de un producto en el inventario: el stock inicial ya viene en el producto,
        // asi que aqui no se vuelve a sumar, solo queda registrado como movimiento de entrada.
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


        private static stock_movement aplicar(
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

            int resultante = movement_type == stock_movement.Salida
                ? producto.stock_quantity - cantidad
                : producto.stock_quantity + cantidad;

            if (resultante < 0)
                throw new InvalidOperationException($"Stock insuficiente para {producto.name} (disponible: {producto.stock_quantity}).");

            producto.stock_quantity = resultante;

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
    }
}

