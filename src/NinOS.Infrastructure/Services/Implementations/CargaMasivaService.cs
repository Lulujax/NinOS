using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain;
using NinOS.Domain.CargaMasiva;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class CargaMasivaService : ICargaMasivaService
    {
        public const string MensajeListaNoActiva = "La lista de precios de este archivo ya no está activa. Descargue una plantilla nueva.";
        public const string MensajeSinCambiosParaAplicar = "El archivo tiene errores o no tiene cambios para aplicar. Corríjalo y vuelva a cargarlo.";

        private readonly IServiceScopeFactory _scope_factory;

        public CargaMasivaService(IServiceScopeFactory scope_factory)
        {
            _scope_factory = scope_factory ?? throw new ArgumentNullException(nameof(scope_factory));
        }

        public async Task<CabeceraPlantilla> obtener_cabecera_async(TipoPlantilla tipo, int? id_price_list)
        {
            using IServiceScope scope = _scope_factory.CreateScope();
            NinOSDbContext db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await construir_cabecera_async(db, tipo, id_price_list, DateTime.UtcNow);
        }

        public async Task<IReadOnlyList<ProductoPlantilla>> obtener_productos_async(TipoPlantilla tipo, int? id_price_list)
        {
            using IServiceScope scope = _scope_factory.CreateScope();
            NinOSDbContext db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            CabeceraPlantilla cabecera = await construir_cabecera_async(db, tipo, id_price_list, DateTime.UtcNow);
            List<product> productos = await db.products.AsNoTracking()
                .Where(p => p.is_active)
                .OrderBy(p => p.category)
                .ThenBy(p => p.product_code)
                .ToListAsync();

            Dictionary<int, decimal> propios = await precios_propios_de_la_lista_async(db, cabecera, null);

            List<ProductoPlantilla> resultado = productos.Select(p => new ProductoPlantilla(
                p.id_product,
                p.product_code,
                p.category,
                p.name,
                p.stock_quantity,
                p.unit_price_usd,
                propios.TryGetValue(p.id_product, out decimal propio) ? propio : p.unit_price_usd)).ToList();

            AppLog.Info("Productos para plantilla de inventario. tipo=" + tipo + ", productos=" + resultado.Count);
            return resultado;
        }

        public async Task<VistaPreviaCarga> generar_vista_previa_async(LecturaPlantilla lectura, TipoCargaCantidad? tipo_carga)
        {
            if (lectura == null) throw new ArgumentNullException(nameof(lectura));

            using IServiceScope scope = _scope_factory.CreateScope();
            NinOSDbContext db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            CabeceraPlantilla cabecera = lectura.cabecera;
            await validar_cabecera_async(db, cabecera);

            List<int> ids = lectura.filas
                .Where(f => f.id_product.HasValue)
                .Select(f => f.id_product!.Value)
                .Distinct()
                .ToList();

            IReadOnlyList<ProductoActual> actuales = await leer_valores_actuales_async(db, cabecera, ids);
            IReadOnlyDictionary<int, ProductoActual> productos_por_id = actuales.ToDictionary(p => p.id_product);

            VistaPreviaCarga base_vista = ArmadorVistaPrevia.armar(cabecera, tipo_carga, lectura.filas, productos_por_id, 0);

            int productos_en_otras_listas = cabecera.tipo == TipoPlantilla.Precio && cabecera.es_principal
                ? await contar_productos_con_precio_heredado_async(db, ids_que_cambian(base_vista))
                : 0;

            VistaPreviaCarga vista = new VistaPreviaCarga(cabecera, tipo_carga, base_vista.resultados, productos_en_otras_listas);
            AppLog.Info("Vista previa de carga masiva. tipo=" + cabecera.tipo
                + ", cambian=" + vista.cambian + ", avisos=" + vista.avisos
                + ", errores=" + vista.errores + ", iguales=" + vista.iguales);
            return vista;
        }

        public async Task<int> aplicar_async(VistaPreviaCarga vista, bool incluir_avisos, string? motivo, string? nota)
        {
            if (vista == null) throw new ArgumentNullException(nameof(vista));
            if (!vista.puede_aplicar) throw new InvalidOperationException(MensajeSinCambiosParaAplicar);
            if (vista.cabecera.tipo == TipoPlantilla.Cantidad && string.IsNullOrWhiteSpace(motivo))
            {
                throw new ArgumentException("El motivo es obligatorio para cambiar cantidades.", nameof(motivo));
            }

            List<ResultadoFila> seleccion = vista.resultados
                .Where(r => r.estado == EstadoFila.Cambia || (incluir_avisos && r.estado == EstadoFila.Aviso))
                .ToList();
            if (seleccion.Count == 0) throw new InvalidOperationException("No hay productos seleccionados para aplicar.");

            AppLog.Info("Aplicando carga masiva. tipo=" + vista.cabecera.tipo + ", a_aplicar=" + seleccion.Count
                + ", avisos_incluidos=" + incluir_avisos);

            using IServiceScope scope = _scope_factory.CreateScope();
            NinOSDbContext db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            await using IDbContextTransaction transaccion = await db.Database.BeginTransactionAsync();
            try
            {
                List<int> ids_vista = vista.resultados
                    .Where(r => r.id_product.HasValue && r.valor_actual.HasValue)
                    .Select(r => r.id_product!.Value)
                    .Distinct()
                    .ToList();

                await stock_movement_writer.reservar_productos_async(db, ids_vista);

                IReadOnlyList<ProductoActual> actuales = await leer_valores_actuales_async(db, vista.cabecera, ids_vista);
                verificar_sin_cambios_desde_la_vista(vista, actuales);

                int aplicados = vista.cabecera.tipo == TipoPlantilla.Cantidad
                    ? await aplicar_cantidad_async(db, vista, seleccion, motivo!, nota)
                    : await aplicar_precio_async(db, vista.cabecera, seleccion);

                await db.SaveChangesAsync();
                await transaccion.CommitAsync();

                AppLog.Info("Carga masiva aplicada. productos=" + aplicados);
                return aplicados;
            }
            catch (CargaCambiadaException)
            {
                AppLog.Warn("Carga masiva cancelada: los datos cambiaron desde la vista previa. No se aplicó nada.");
                throw;
            }
            catch (Exception ex)
            {
                AppLog.Error("Carga masiva cancelada. No se aplicó nada.", ex);
                throw;
            }
        }

        private static async Task<CabeceraPlantilla> construir_cabecera_async(
            NinOSDbContext db,
            TipoPlantilla tipo,
            int? id_price_list,
            DateTime fecha)
        {
            if (tipo == TipoPlantilla.Cantidad)
            {
                return new CabeceraPlantilla(tipo, null, string.Empty, false, fecha);
            }

            price_list lista = await validar_lista_async(db, id_price_list);
            return new CabeceraPlantilla(tipo, lista.id_price_list, lista.name, lista.is_principal, fecha);
        }

        private static async Task validar_cabecera_async(NinOSDbContext db, CabeceraPlantilla cabecera)
        {
            if (cabecera.tipo == TipoPlantilla.Precio)
            {
                await validar_lista_async(db, cabecera.id_price_list);
            }
        }

        private static async Task<price_list> validar_lista_async(NinOSDbContext db, int? id_price_list)
        {
            price_list? lista = id_price_list.HasValue
                ? await db.price_lists.AsNoTracking().FirstOrDefaultAsync(l => l.id_price_list == id_price_list.Value)
                : await db.price_lists.AsNoTracking().FirstOrDefaultAsync(l => l.is_principal);

            if (lista == null || (!lista.is_principal && !lista.is_active))
            {
                AppLog.Warn("Lista de precios no disponible para la plantilla.");
                throw new InvalidOperationException(MensajeListaNoActiva);
            }

            return lista;
        }

        private static async Task<IReadOnlyList<ProductoActual>> leer_valores_actuales_async(
            NinOSDbContext db,
            CabeceraPlantilla cabecera,
            IReadOnlyList<int> ids)
        {
            List<int> lista_ids = ids.ToList();
            if (lista_ids.Count == 0) return new List<ProductoActual>();

            List<product> productos = await db.products.AsNoTracking()
                .Where(p => lista_ids.Contains(p.id_product))
                .ToListAsync();

            Dictionary<int, decimal> propios = await precios_propios_de_la_lista_async(db, cabecera, lista_ids);

            return productos.Select(p => new ProductoActual(
                p.id_product,
                p.product_code,
                p.is_active,
                valor_actual(cabecera, p, propios))).ToList();
        }

        private static decimal valor_actual(CabeceraPlantilla cabecera, product producto, Dictionary<int, decimal> propios)
        {
            if (cabecera.tipo == TipoPlantilla.Cantidad) return producto.stock_quantity;
            if (cabecera.es_principal) return producto.unit_price_usd;
            return propios.TryGetValue(producto.id_product, out decimal propio) ? propio : producto.unit_price_usd;
        }

        private static async Task<Dictionary<int, decimal>> precios_propios_de_la_lista_async(
            NinOSDbContext db,
            CabeceraPlantilla cabecera,
            List<int>? ids)
        {
            if (cabecera.tipo == TipoPlantilla.Cantidad || cabecera.es_principal || !cabecera.id_price_list.HasValue)
            {
                return new Dictionary<int, decimal>();
            }

            int id_lista = cabecera.id_price_list.Value;
            IQueryable<product_price> consulta = db.product_prices.AsNoTracking()
                .Where(pp => pp.id_price_list == id_lista);

            if (ids != null)
            {
                consulta = consulta.Where(pp => ids.Contains(pp.id_product));
            }

            return await consulta.ToDictionaryAsync(pp => pp.id_product, pp => pp.unit_price_usd);
        }

        private static List<int> ids_que_cambian(VistaPreviaCarga vista)
        {
            return vista.resultados
                .Where(r => (r.estado == EstadoFila.Cambia || r.estado == EstadoFila.Aviso) && r.id_product.HasValue)
                .Select(r => r.id_product!.Value)
                .Distinct()
                .ToList();
        }

        private static async Task<int> contar_productos_con_precio_heredado_async(NinOSDbContext db, List<int> ids)
        {
            if (ids.Count == 0) return 0;

            List<int> listas = await db.price_lists.AsNoTracking()
                .Where(l => !l.is_principal && l.is_active)
                .Select(l => l.id_price_list)
                .ToListAsync();
            if (listas.Count == 0) return 0;

            var filas_propias = await db.product_prices.AsNoTracking()
                .Where(pp => listas.Contains(pp.id_price_list) && ids.Contains(pp.id_product))
                .Select(pp => new { pp.id_product, pp.id_price_list })
                .ToListAsync();

            HashSet<(int, int)> propios = filas_propias.Select(f => (f.id_product, f.id_price_list)).ToHashSet();

            return ids.Count(id => listas.Any(lista => !propios.Contains((id, lista))));
        }

        private static void verificar_sin_cambios_desde_la_vista(VistaPreviaCarga vista, IReadOnlyList<ProductoActual> actuales)
        {
            Dictionary<int, decimal> valores = actuales.ToDictionary(p => p.id_product, p => p.valor_actual);

            foreach (ResultadoFila resultado in vista.resultados)
            {
                if (!resultado.id_product.HasValue || !resultado.valor_actual.HasValue) continue;

                bool cambio = !valores.TryGetValue(resultado.id_product.Value, out decimal ahora)
                    || ahora != resultado.valor_actual.Value;
                if (cambio)
                {
                    throw new CargaCambiadaException(
                        "Los datos cambiaron desde la vista previa. No se aplicó nada: vuelva a cargar el archivo.");
                }
            }
        }

        private static async Task<int> aplicar_cantidad_async(
            NinOSDbContext db,
            VistaPreviaCarga vista,
            IReadOnlyList<ResultadoFila> seleccion,
            string motivo,
            string? nota)
        {
            List<int> ids = seleccion.Select(r => r.id_product!.Value).ToList();
            Dictionary<int, product> productos = await db.products.AsNoTracking()
                .Where(p => ids.Contains(p.id_product))
                .ToDictionaryAsync(p => p.id_product);

            string nota_final = construir_nota(motivo, nota);
            bool es_conteo = vista.tipo_carga == TipoCargaCantidad.Conteo;

            foreach (ResultadoFila resultado in seleccion)
            {
                if (!productos.TryGetValue(resultado.id_product!.Value, out product? producto))
                {
                    throw new CargaCambiadaException("Un producto ya no existe. No se aplicó nada: vuelva a cargar el archivo.");
                }

                if (es_conteo)
                {
                    await stock_movement_writer.fijar_stock_async(
                        db,
                        producto.id_product,
                        (int)resultado.valor_final!.Value,
                        (int)resultado.valor_actual!.Value,
                        "CONTEO-" + producto.product_code,
                        producto.unit_price_usd,
                        stock_movement.RazonCorreccionConteo,
                        nota_final);
                }
                else
                {
                    await stock_movement_writer.ajustar_stock_async(
                        db,
                        producto.id_product,
                        (int)resultado.diferencia!.Value,
                        "AJUSTE-" + producto.product_code,
                        producto.unit_price_usd,
                        stock_movement.RazonAjuste,
                        nota_final);
                }
            }

            return seleccion.Count;
        }

        private static async Task<int> aplicar_precio_async(
            NinOSDbContext db,
            CabeceraPlantilla cabecera,
            IReadOnlyList<ResultadoFila> seleccion)
        {
            if (cabecera.es_principal)
            {
                List<int> ids = seleccion.Select(r => r.id_product!.Value).ToList();
                Dictionary<int, product> productos = await db.products
                    .Where(p => ids.Contains(p.id_product))
                    .ToDictionaryAsync(p => p.id_product);

                foreach (ResultadoFila resultado in seleccion)
                {
                    if (!productos.TryGetValue(resultado.id_product!.Value, out product? producto))
                    {
                        throw new CargaCambiadaException("Un producto ya no existe. No se aplicó nada: vuelva a cargar el archivo.");
                    }
                    producto.unit_price_usd = Money.round(resultado.valor_final!.Value);
                }
            }
            else
            {
                foreach (ResultadoFila resultado in seleccion)
                {
                    await price_writer.guardar_precio_async(
                        db,
                        resultado.id_product!.Value,
                        cabecera.id_price_list!.Value,
                        resultado.valor_final!.Value);
                }
            }

            return seleccion.Count;
        }

        private static string construir_nota(string motivo, string? nota)
        {
            string texto = "Carga masiva - " + motivo.Trim();
            return string.IsNullOrWhiteSpace(nota) ? texto : texto + " - " + nota.Trim();
        }
    }
}
