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
    public class PriceListService : IPriceListService
    {
        private static readonly SemaphoreSlim _principal_lock = new SemaphoreSlim(1, 1);
        private readonly IServiceScopeFactory _scopeFactory;

        public PriceListService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        public async Task<List<price_list>> GetAllAsync(bool includeInactive = false)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            var query = db.price_lists.AsNoTracking().Where(l => !l.is_principal);

            if (!includeInactive)
                query = query.Where(l => l.is_active);

            return await query.OrderBy(l => l.sort_order).ThenBy(l => l.name).ToListAsync();
        }

        public async Task<List<price_list>> GetActiveAsync()
        {
            return await GetAllAsync(includeInactive: false);
        }

        public async Task<price_list?> GetByIdAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.price_lists.FindAsync(id);
        }

        public async Task<price_list> GetPrincipalAsync()
        {
            await _principal_lock.WaitAsync();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                var principal = await db.price_lists.FirstOrDefaultAsync(l => l.is_principal);
                if (principal != null) return principal;

                // Bases creadas antes de existir la fila Principal. Si ya hay una lista
                // llamada "Principal" se reutiliza para no chocar con el nombre unico.
                principal = await db.price_lists.FirstOrDefaultAsync(l => l.name == "Principal");
                if (principal != null)
                {
                    principal.is_principal = true;
                    await db.SaveChangesAsync();
                    return principal;
                }

                principal = new price_list
                {
                    name = "Principal",
                    description = "Precios base del inventario",
                    is_active = true,
                    is_principal = true,
                    sort_order = 0
                };

                db.price_lists.Add(principal);

                try
                {
                    await db.SaveChangesAsync();
                    return principal;
                }
                catch (DbUpdateException)
                {
                    // Otra instancia de la aplicacion pudo crearla al mismo tiempo.
                    db.Entry(principal).State = EntityState.Detached;

                    var existente = await db.price_lists.AsNoTracking()
                        .FirstOrDefaultAsync(l => l.is_principal || l.name == "Principal");

                    if (existente != null) return existente;
                    throw;
                }
            }
            finally
            {
                _principal_lock.Release();
            }
        }

        public async Task<price_list> CreateAsync(string name, string? description = null)
        {
            string nombre = normalize_name(name);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            await ensure_name_free_async(db, nombre, excludeId: null);

            int siguiente_orden = await db.price_lists.AnyAsync()
                ? await db.price_lists.MaxAsync(l => l.sort_order) + 1
                : 1;

            var lista = new price_list
            {
                name = nombre,
                description = normalize_description(description),
                is_active = true,
                is_principal = false,
                sort_order = siguiente_orden
            };

            db.price_lists.Add(lista);
            await db.SaveChangesAsync();
            return lista;
        }

        public async Task UpdateAsync(int id, string name, string? description)
        {
            string nombre = normalize_name(name);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            var lista = await db.price_lists.FindAsync(id)
                ?? throw new InvalidOperationException("La lista de precios ya no existe.");

            if (!string.Equals(lista.name, nombre, StringComparison.Ordinal))
                await ensure_name_free_async(db, nombre, excludeId: id);

            lista.name = nombre;
            lista.description = normalize_description(description);
            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            var lista = await db.price_lists.FindAsync(id);
            if (lista == null) return;

            if (lista.is_principal)
                throw new InvalidOperationException("La lista Principal no se puede eliminar.");

            // Si la lista ya aparece en notas emitidas no se borra: se desactiva para
            // conservar el historial. En ambos casos deja de ofrecerse al usuario.
            bool usada_en_notas = await db.note_details.AnyAsync(d => d.id_price_list == id);
            if (usada_en_notas)
            {
                lista.is_active = false;
            }
            else
            {
                db.price_lists.Remove(lista);
            }

            await db.SaveChangesAsync();
        }

        public async Task SavePrincipalPriceAsync(int idProduct, decimal price)
        {
            if (price < 0)
                throw new InvalidOperationException("El precio no puede ser negativo.");

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            var producto = await db.products.FindAsync(idProduct)
                ?? throw new InvalidOperationException("El producto ya no existe.");

            producto.unit_price_usd = Money.round(price);
            await db.SaveChangesAsync();
        }

        public async Task<Dictionary<int, decimal>> GetProductOwnPricesAsync(int idProduct)
        {
            var todos = await GetAllOwnPricesAsync();
            return todos.TryGetValue(idProduct, out var precios)
                ? precios
                : new Dictionary<int, decimal>();
        }

        public async Task<Dictionary<int, Dictionary<int, decimal>>> GetAllOwnPricesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var filas = await db.product_prices
                .AsNoTracking()
                .Select(p => new { p.id_product, p.id_price_list, p.unit_price_usd })
                .ToListAsync();

            return filas
                .GroupBy(f => f.id_product)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(f => f.id_price_list, f => f.unit_price_usd));
        }

        public async Task SaveProductPriceAsync(int idProduct, int idPriceList, decimal? price)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var fila = await db.product_prices
                .FirstOrDefaultAsync(p => p.id_product == idProduct && p.id_price_list == idPriceList);

            if (price.HasValue)
            {
                if (price.Value < 0)
                    throw new InvalidOperationException("El precio no puede ser negativo.");

                if (fila == null)
                {
                    db.product_prices.Add(new product_price
                    {
                        id_product = idProduct,
                        id_price_list = idPriceList,
                        unit_price_usd = Money.round(price.Value)
                    });
                }
                else
                {
                    fila.unit_price_usd = Money.round(price.Value);
                }
            }
            else if (fila != null)
            {
                // Sin precio propio la fila sobra: la ausencia ES la herencia.
                db.product_prices.Remove(fila);
            }

            await db.SaveChangesAsync();
        }

        public async Task<List<product_price_dto>> GetProductPricesForListAsync(int? idPriceList)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var productsQuery = db.products
                .AsNoTracking()
                .Where(p => p.is_active);

            if (!idPriceList.HasValue)
            {
                return await productsQuery
                    .OrderBy(p => p.product_code)
                    .Select(p => new product_price_dto
                    {
                        id_product = p.id_product,
                        product_code = p.product_code,
                        product_name = p.name,
                        category = p.category,
                        original_price_usd = p.unit_price_usd,
                        custom_price_usd = null
                    })
                    .ToListAsync();
            }

            int listId = idPriceList.Value;
            var listPricesQuery = db.product_prices
                .AsNoTracking()
                .Where(pp => pp.id_price_list == listId);

            var query = from p in productsQuery
                        join pp in listPricesQuery on p.id_product equals pp.id_product into ppJoin
                        from subPp in ppJoin.DefaultIfEmpty()
                        orderby p.product_code
                        select new product_price_dto
                        {
                            id_product = p.id_product,
                            product_code = p.product_code,
                            product_name = p.name,
                            category = p.category,
                            original_price_usd = p.unit_price_usd,
                            custom_price_usd = subPp != null ? (decimal?)subPp.unit_price_usd : null
                        };

            return await query.ToListAsync();
        }

        public async Task SaveProductPricesBulkAsync(int idPriceList, IEnumerable<(int idProduct, decimal? price)> priceUpdates)
        {
            if (priceUpdates == null) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var updateList = priceUpdates.ToList();
            if (updateList.Count == 0) return;

            var productIds = updateList.Select(u => u.idProduct).Distinct().ToList();
            var existingPrices = await db.product_prices
                .Where(pp => pp.id_price_list == idPriceList && productIds.Contains(pp.id_product))
                .ToListAsync();

            var existingMap = existingPrices.ToDictionary(pp => pp.id_product);

            foreach (var (idProduct, price) in updateList)
            {
                if (price.HasValue)
                {
                    if (price.Value < 0)
                        throw new InvalidOperationException("El precio no puede ser negativo.");

                    decimal roundedPrice = Money.round(price.Value);
                    if (existingMap.TryGetValue(idProduct, out var existing))
                    {
                        existing.unit_price_usd = roundedPrice;
                    }
                    else
                    {
                        db.product_prices.Add(new product_price
                        {
                            id_product = idProduct,
                            id_price_list = idPriceList,
                            unit_price_usd = roundedPrice
                        });
                    }
                }
                else
                {
                    if (existingMap.TryGetValue(idProduct, out var existing))
                    {
                        db.product_prices.Remove(existing);
                    }
                }
            }

            await db.SaveChangesAsync();
        }

        private static string normalize_name(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("El nombre de la lista de precios no puede estar vacío.");

            return name.Trim();
        }

        private static string? normalize_description(string? description)
        {
            return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        }

        private static async Task ensure_name_free_async(NinOSDbContext db, string name, int? excludeId)
        {
            bool ocupado = await db.price_lists
                .Where(l => excludeId == null || l.id_price_list != excludeId)
                .AnyAsync(l => l.name.ToLower() == name.ToLower());

            if (ocupado)
                throw new InvalidOperationException($"Ya existe una lista de precios con el nombre '{name}'.");
        }
    }
}
