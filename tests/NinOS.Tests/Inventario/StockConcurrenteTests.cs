using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Services;

namespace NinOS.Tests.Inventario;

/// <summary>
/// Spec 003: el stock no se pisa entre usuarios.
///
/// Esto NO se puede probar con logica pura: hace falta PostgreSQL de verdad y dos conexiones
/// a la vez. Corre contra la base de testing y limpia lo que crea.
/// </summary>
public class StockConcurrenteTests
{
    private const int StockInicial = 100;

    /// <summary>Crea un producto de prueba y devuelve su id. El codigo lleva marca ZZTEST.</summary>
    private static async Task<int> crear_producto_de_prueba(NinOSDbContext db)
    {
        string codigo = "ZZTEST" + DateTime.UtcNow.Ticks.ToString().Substring(10);

        product producto = new product(codigo, "PRODUCTO DE PRUEBA AUTOMATICA", "Otros", 1.00m, StockInicial);
        db.products.Add(producto);
        await db.SaveChangesAsync();

        return producto.id_product;
    }

    private static async Task borrar_producto_de_prueba(int id_product)
    {
        using NinOSDbContext? db = BaseDeTesting.crear_contexto_o_null();
        if (db == null) return;

        // Los movimientos van primero: la nota de stock apunta al producto con FK Restrict.
        await db.stock_movements.Where(m => m.id_product == id_product).ExecuteDeleteAsync();
        await db.products.Where(p => p.id_product == id_product).ExecuteDeleteAsync();
    }

    /// <summary>Saca unidades usando una conexion propia, como lo haria otra PC.</summary>
    private static async Task sacar_en_su_propia_conexion(int id_product, int cantidad)
    {
        using NinOSDbContext db = BaseDeTesting.crear_contexto_o_null()!;
        product producto = await db.products.FirstAsync(p => p.id_product == id_product);

        await using var transaccion = await db.Database.BeginTransactionAsync();
        await stock_movement_writer.registrar_salida_async(
            db, producto, cantidad,
            stock_movement.RazonVenta, stock_movement.DocumentoEntrega, "PRUEBA-CONCURRENCIA",
            DateTime.UtcNow, 1.00m);
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
    }

    private static async Task<int> leer_stock(int id_product)
    {
        using NinOSDbContext db = BaseDeTesting.crear_contexto_o_null()!;
        return await db.products.AsNoTracking().Where(p => p.id_product == id_product)
            .Select(p => p.stock_quantity).FirstAsync();
    }

    // ---------- Caso feliz: el bug que se arreglo ----------

    [Fact]
    public async Task dos_salidas_al_mismo_tiempo_del_mismo_producto_se_suman_las_dos()
    {
        using NinOSDbContext? db = BaseDeTesting.crear_contexto_o_null();
        if (db == null) return;

        int id_product = await crear_producto_de_prueba(db);
        try
        {
            // Dos "PCs" sacando a la vez. Antes, la ultima pisaba a la primera y el stock
            // quedaba en 70 o en 80 en vez de 50.
            await Task.WhenAll(
                sacar_en_su_propia_conexion(id_product, 30),
                sacar_en_su_propia_conexion(id_product, 20));

            Assert.Equal(StockInicial - 50, await leer_stock(id_product));
        }
        finally
        {
            await borrar_producto_de_prueba(id_product);
        }
    }

    [Fact]
    public async Task diez_salidas_al_mismo_tiempo_no_pierden_ni_una_unidad()
    {
        using NinOSDbContext? db = BaseDeTesting.crear_contexto_o_null();
        if (db == null) return;

        int id_product = await crear_producto_de_prueba(db);
        try
        {
            Task[] ventas = Enumerable.Range(0, 10)
                .Select(_ => sacar_en_su_propia_conexion(id_product, 5))
                .ToArray();

            await Task.WhenAll(ventas);

            Assert.Equal(StockInicial - 50, await leer_stock(id_product));
        }
        finally
        {
            await borrar_producto_de_prueba(id_product);
        }
    }

    [Fact]
    public async Task el_stock_queda_siempre_igual_a_la_suma_del_kardex()
    {
        using NinOSDbContext? db = BaseDeTesting.crear_contexto_o_null();
        if (db == null) return;

        int id_product = await crear_producto_de_prueba(db);
        try
        {
            await Task.WhenAll(
                sacar_en_su_propia_conexion(id_product, 7),
                sacar_en_su_propia_conexion(id_product, 13),
                sacar_en_su_propia_conexion(id_product, 3));

            using NinOSDbContext revision = BaseDeTesting.crear_contexto_o_null()!;
            var movimientos = await revision.stock_movements.AsNoTracking()
                .Where(m => m.id_product == id_product)
                .Select(m => new { m.movement_type, m.quantity })
                .ToListAsync();

            int suma_kardex = movimientos.Sum(m =>
                m.movement_type == stock_movement.Entrada ? m.quantity : -m.quantity);

            // La carga inicial no pasa por el writer, asi que al stock le sumamos el inicial.
            Assert.Equal(StockInicial + suma_kardex - StockInicial, await leer_stock(id_product) - StockInicial);
            Assert.Equal(-23, suma_kardex);
        }
        finally
        {
            await borrar_producto_de_prueba(id_product);
        }
    }

    // ---------- Casos limite ----------

    [Fact]
    public async Task una_nota_de_entrega_si_puede_dejar_el_stock_en_negativo()
    {
        using NinOSDbContext? db = BaseDeTesting.crear_contexto_o_null();
        if (db == null) return;

        int id_product = await crear_producto_de_prueba(db);
        try
        {
            // Decision del cliente: solo las notas de entrega pueden pasarse del stock.
            await sacar_en_su_propia_conexion(id_product, StockInicial + 5);

            Assert.Equal(-5, await leer_stock(id_product));
        }
        finally
        {
            await borrar_producto_de_prueba(id_product);
        }
    }

    [Fact]
    public async Task el_conteo_manual_deja_el_stock_exacto_y_asienta_la_diferencia()
    {
        using NinOSDbContext? db = BaseDeTesting.crear_contexto_o_null();
        if (db == null) return;

        int id_product = await crear_producto_de_prueba(db);
        try
        {
            using NinOSDbContext contexto = BaseDeTesting.crear_contexto_o_null()!;
            await using var transaccion = await contexto.Database.BeginTransactionAsync();

            int diferencia = await stock_movement_writer.fijar_stock_async(
                contexto, id_product, 88, StockInicial, "CONTEO-PRUEBA", 1.00m, "CORRECCION DE CONTEO");

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            Assert.Equal(-12, diferencia);
            Assert.Equal(88, await leer_stock(id_product));
        }
        finally
        {
            await borrar_producto_de_prueba(id_product);
        }
    }

    // ---------- Casos de error ----------

    [Fact]
    public async Task un_obsequio_no_puede_dejar_el_stock_en_negativo()
    {
        using NinOSDbContext? db = BaseDeTesting.crear_contexto_o_null();
        if (db == null) return;

        int id_product = await crear_producto_de_prueba(db);
        try
        {
            using NinOSDbContext contexto = BaseDeTesting.crear_contexto_o_null()!;
            product producto = await contexto.products.FirstAsync(p => p.id_product == id_product);

            await using var transaccion = await contexto.Database.BeginTransactionAsync();

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await stock_movement_writer.registrar_salida_async(
                    contexto, producto, StockInicial + 1,
                    stock_movement.RazonObsequio, stock_movement.DocumentoCredito, "PRUEBA-OBSEQUIO",
                    DateTime.UtcNow, 1.00m));

            Assert.Contains("Stock insuficiente", error.Message);
            await transaccion.RollbackAsync();

            Assert.Equal(StockInicial, await leer_stock(id_product));
        }
        finally
        {
            await borrar_producto_de_prueba(id_product);
        }
    }

    [Fact]
    public async Task el_conteo_avisa_si_el_stock_cambio_mientras_llenaban_el_formulario()
    {
        using NinOSDbContext? db = BaseDeTesting.crear_contexto_o_null();
        if (db == null) return;

        int id_product = await crear_producto_de_prueba(db);
        try
        {
            // Otra PC vende 10 mientras el formulario estaba abierto mostrando 100.
            await sacar_en_su_propia_conexion(id_product, 10);

            using NinOSDbContext contexto = BaseDeTesting.crear_contexto_o_null()!;
            await using var transaccion = await contexto.Database.BeginTransactionAsync();

            stock_cambiado_exception error = await Assert.ThrowsAsync<stock_cambiado_exception>(async () =>
                await stock_movement_writer.fijar_stock_async(
                    contexto, id_product, 95, StockInicial, "CONTEO-PRUEBA", 1.00m, "CORRECCION DE CONTEO"));

            Assert.Equal(StockInicial, error.stock_visto);
            Assert.Equal(StockInicial - 10, error.stock_real);

            await transaccion.RollbackAsync();
            Assert.Equal(StockInicial - 10, await leer_stock(id_product));
        }
        finally
        {
            await borrar_producto_de_prueba(id_product);
        }
    }

    [Fact]
    public async Task si_la_transaccion_se_deshace_el_stock_vuelve_atras()
    {
        using NinOSDbContext? db = BaseDeTesting.crear_contexto_o_null();
        if (db == null) return;

        int id_product = await crear_producto_de_prueba(db);
        try
        {
            using NinOSDbContext contexto = BaseDeTesting.crear_contexto_o_null()!;
            product producto = await contexto.products.FirstAsync(p => p.id_product == id_product);

            await using (var transaccion = await contexto.Database.BeginTransactionAsync())
            {
                await stock_movement_writer.registrar_salida_async(
                    contexto, producto, 40,
                    stock_movement.RazonVenta, stock_movement.DocumentoEntrega, "PRUEBA-ROLLBACK",
                    DateTime.UtcNow, 1.00m);
                await contexto.SaveChangesAsync();
                await transaccion.RollbackAsync();
            }

            // El UPDATE va en la misma transaccion, asi que deshacerla devuelve el stock.
            Assert.Equal(StockInicial, await leer_stock(id_product));
        }
        finally
        {
            await borrar_producto_de_prueba(id_product);
        }
    }
}
