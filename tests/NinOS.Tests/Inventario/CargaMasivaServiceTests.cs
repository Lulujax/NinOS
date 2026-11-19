using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain;
using NinOS.Domain.CargaMasiva;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Services.Implementations;
using Xunit;

namespace NinOS.Tests.Inventario;

public class CargaMasivaServiceTests
{
    private static IServiceScopeFactory? fabrica_de_scopes()
    {
        if (!DbConnectionFactory.TryGetConnectionString(out string? cadena, out _)) return null;

        if (!BaseDeTesting.es_base_de_testing(cadena))
        {
            throw new InvalidOperationException("PRUEBAS DETENIDAS: la conexión no apunta a una base de testing.");
        }

        var services = new ServiceCollection();
        services.AddDbContext<NinOSDbContext>(opciones => opciones.UseNpgsql(cadena));
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static CabeceraPlantilla cabecera_cantidad() => new CabeceraPlantilla(TipoPlantilla.Cantidad, null, string.Empty, false, DateTime.UtcNow);

    private static async Task<product> crear_producto(NinOSDbContext db, decimal precio, int stock)
    {
        string codigo = "ZZTEST" + Guid.NewGuid().ToString("N").Substring(0, 10);
        var producto = new product(codigo, "PRODUCTO DE PRUEBA CARGA MASIVA", "ZZTEST", precio, stock);
        db.products.Add(producto);
        await db.SaveChangesAsync();
        return producto;
    }

    private static async Task limpiar(List<int> ids_productos, List<int> ids_listas)
    {
        using NinOSDbContext? db = BaseDeTesting.crear_contexto_o_null();
        if (db == null) return;

        await db.stock_movements.Where(m => ids_productos.Contains(m.id_product)).ExecuteDeleteAsync();
        await db.product_prices.Where(pp => ids_productos.Contains(pp.id_product)).ExecuteDeleteAsync();
        await db.products.Where(p => ids_productos.Contains(p.id_product)).ExecuteDeleteAsync();
        await db.price_lists.Where(l => ids_listas.Contains(l.id_price_list)).ExecuteDeleteAsync();
    }

    private static async Task<int> leer_stock(int id_product)
    {
        using NinOSDbContext db = BaseDeTesting.crear_contexto_o_null()!;
        return await db.products.AsNoTracking().Where(p => p.id_product == id_product)
            .Select(p => p.stock_quantity).FirstAsync();
    }

    private static async Task<price_list> crear_lista(NinOSDbContext db, bool activa)
    {
        var lista = new price_list
        {
            name = "ZZTEST-LISTA-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            is_active = activa,
            is_principal = false,
            sort_order = 9999
        };
        db.price_lists.Add(lista);
        await db.SaveChangesAsync();
        return lista;
    }

    [Fact]
    public async Task Vista_previa_de_conteo_no_modifica_el_stock()
    {
        IServiceScopeFactory? scopes = fabrica_de_scopes();
        if (scopes == null) return;

        int id_producto = 0;
        try
        {
            using NinOSDbContext db = BaseDeTesting.crear_contexto_o_null()!;
            product producto = await crear_producto(db, 1m, 10);
            id_producto = producto.id_product;

            var servicio = new CargaMasivaService(scopes);
            var lectura = new LecturaPlantilla(cabecera_cantidad(), new[] { new FilaPlantilla(5, id_producto, producto.product_code, 10m, 25m, null) });

            VistaPreviaCarga vista = await servicio.generar_vista_previa_async(lectura, TipoCargaCantidad.Conteo);

            Assert.Equal(1, vista.cambian);
            Assert.Equal(10, await leer_stock(id_producto));
        }
        finally
        {
            await limpiar(new List<int> { id_producto }, new List<int>());
        }
    }

    [Fact]
    public async Task Aplicar_conteo_deja_el_stock_escrito_y_queda_en_el_kardex()
    {
        IServiceScopeFactory? scopes = fabrica_de_scopes();
        if (scopes == null) return;

        int id_producto = 0;
        try
        {
            using NinOSDbContext db = BaseDeTesting.crear_contexto_o_null()!;
            product producto = await crear_producto(db, 1m, 10);
            id_producto = producto.id_product;

            var servicio = new CargaMasivaService(scopes);
            var lectura = new LecturaPlantilla(cabecera_cantidad(), new[] { new FilaPlantilla(5, id_producto, producto.product_code, 10m, 25m, null) });
            VistaPreviaCarga vista = await servicio.generar_vista_previa_async(lectura, TipoCargaCantidad.Conteo);

            int aplicados = await servicio.aplicar_async(vista, false, "Conteo fin de mes", null);

            Assert.Equal(1, aplicados);
            Assert.Equal(25, await leer_stock(id_producto));

            using NinOSDbContext verificacion = BaseDeTesting.crear_contexto_o_null()!;
            stock_movement movimiento = await verificacion.stock_movements
                .Where(m => m.id_product == id_producto).OrderByDescending(m => m.id_stock_movement).FirstAsync();
            Assert.Contains("Carga masiva", movimiento.line_description);
            Assert.StartsWith("CONTEO-", movimiento.document_number);
        }
        finally
        {
            await limpiar(new List<int> { id_producto }, new List<int>());
        }
    }

    [Fact]
    public async Task Si_el_stock_cambio_desde_la_vista_no_se_aplica_nada()
    {
        IServiceScopeFactory? scopes = fabrica_de_scopes();
        if (scopes == null) return;

        int id_uno = 0;
        int id_dos = 0;
        try
        {
            using NinOSDbContext db = BaseDeTesting.crear_contexto_o_null()!;
            product uno = await crear_producto(db, 1m, 10);
            product dos = await crear_producto(db, 1m, 10);
            id_uno = uno.id_product;
            id_dos = dos.id_product;

            var servicio = new CargaMasivaService(scopes);
            var lectura = new LecturaPlantilla(cabecera_cantidad(), new[]
            {
                new FilaPlantilla(5, id_uno, uno.product_code, 10m, 25m, null),
                new FilaPlantilla(6, id_dos, dos.product_code, 10m, 30m, null)
            });
            VistaPreviaCarga vista = await servicio.generar_vista_previa_async(lectura, TipoCargaCantidad.Conteo);

            await db.products.Where(p => p.id_product == id_dos).ExecuteUpdateAsync(s => s.SetProperty(p => p.stock_quantity, 7));

            await Assert.ThrowsAsync<CargaCambiadaException>(() => servicio.aplicar_async(vista, false, "Conteo", null));

            Assert.Equal(10, await leer_stock(id_uno));
            Assert.Equal(7, await leer_stock(id_dos));
        }
        finally
        {
            await limpiar(new List<int> { id_uno, id_dos }, new List<int>());
        }
    }

    [Fact]
    public async Task Motivo_vacio_en_cantidad_se_rechaza()
    {
        IServiceScopeFactory? scopes = fabrica_de_scopes();
        if (scopes == null) return;

        var servicio = new CargaMasivaService(scopes);
        var cabecera = cabecera_cantidad();
        var resultado = new ResultadoFila(new FilaPlantilla(5, 1, "A-1", 10m, 12m, null), 10m, 12m, EstadoFila.Cambia, null);
        var vista = new VistaPreviaCarga(cabecera, TipoCargaCantidad.Conteo, new[] { resultado }, 0);

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.aplicar_async(vista, false, " ", null));
    }

    [Fact]
    public async Task Precio_de_la_principal_se_cambia_en_el_precio_base()
    {
        IServiceScopeFactory? scopes = fabrica_de_scopes();
        if (scopes == null) return;

        int id_producto = 0;
        try
        {
            using NinOSDbContext db = BaseDeTesting.crear_contexto_o_null()!;
            price_list? principal = await db.price_lists.FirstOrDefaultAsync(l => l.is_principal);
            if (principal == null) return;

            product producto = await crear_producto(db, 5m, 1);
            id_producto = producto.id_product;

            var servicio = new CargaMasivaService(scopes);
            var cabecera = new CabeceraPlantilla(TipoPlantilla.Precio, principal.id_price_list, principal.name, true, DateTime.UtcNow);
            var lectura = new LecturaPlantilla(cabecera, new[] { new FilaPlantilla(5, id_producto, producto.product_code, 5m, 6m, null) });
            VistaPreviaCarga vista = await servicio.generar_vista_previa_async(lectura, null);

            await servicio.aplicar_async(vista, false, null, null);

            using NinOSDbContext verificacion = BaseDeTesting.crear_contexto_o_null()!;
            decimal precio = await verificacion.products.AsNoTracking()
                .Where(p => p.id_product == id_producto).Select(p => p.unit_price_usd).FirstAsync();
            Assert.Equal(6m, precio);
        }
        finally
        {
            await limpiar(new List<int> { id_producto }, new List<int>());
        }
    }

    [Fact]
    public async Task Precio_de_lista_secundaria_no_cambia_la_principal()
    {
        IServiceScopeFactory? scopes = fabrica_de_scopes();
        if (scopes == null) return;

        int id_producto = 0;
        int id_lista = 0;
        try
        {
            using NinOSDbContext db = BaseDeTesting.crear_contexto_o_null()!;
            product producto = await crear_producto(db, 5m, 1);
            id_producto = producto.id_product;
            price_list lista = await crear_lista(db, activa: true);
            id_lista = lista.id_price_list;

            var servicio = new CargaMasivaService(scopes);
            var cabecera = new CabeceraPlantilla(TipoPlantilla.Precio, id_lista, lista.name, false, DateTime.UtcNow);
            var lectura = new LecturaPlantilla(cabecera, new[] { new FilaPlantilla(5, id_producto, producto.product_code, 5m, 6m, null) });
            VistaPreviaCarga vista = await servicio.generar_vista_previa_async(lectura, null);

            await servicio.aplicar_async(vista, false, null, null);

            using NinOSDbContext verificacion = BaseDeTesting.crear_contexto_o_null()!;
            decimal precio_lista = await verificacion.product_prices.AsNoTracking()
                .Where(pp => pp.id_product == id_producto && pp.id_price_list == id_lista)
                .Select(pp => pp.unit_price_usd).FirstAsync();
            decimal precio_principal = await verificacion.products.AsNoTracking()
                .Where(p => p.id_product == id_producto).Select(p => p.unit_price_usd).FirstAsync();

            Assert.Equal(6m, precio_lista);
            Assert.Equal(5m, precio_principal);
        }
        finally
        {
            await limpiar(new List<int> { id_producto }, new List<int> { id_lista });
        }
    }

    [Fact]
    public async Task Lista_inactiva_se_rechaza_al_generar_la_vista_previa()
    {
        IServiceScopeFactory? scopes = fabrica_de_scopes();
        if (scopes == null) return;

        int id_lista = 0;
        try
        {
            using NinOSDbContext db = BaseDeTesting.crear_contexto_o_null()!;
            price_list lista = await crear_lista(db, activa: false);
            id_lista = lista.id_price_list;

            var servicio = new CargaMasivaService(scopes);
            var cabecera = new CabeceraPlantilla(TipoPlantilla.Precio, id_lista, lista.name, false, DateTime.UtcNow);
            var lectura = new LecturaPlantilla(cabecera, Array.Empty<FilaPlantilla>());

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => servicio.generar_vista_previa_async(lectura, null));
            Assert.Equal(CargaMasivaService.MensajeListaNoActiva, error.Message);
        }
        finally
        {
            await limpiar(new List<int>(), new List<int> { id_lista });
        }
    }
}
