using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class PriceListService : IPriceListService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public PriceListService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        public async Task<List<price_list>> GetAllAsync(bool includeInactive = false)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            var query = db.price_lists.AsNoTracking();

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

        public async Task<price_list> CreateAsync(string name)
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
                is_active = true,
                sort_order = siguiente_orden
            };

            db.price_lists.Add(lista);
            await db.SaveChangesAsync();
            return lista;
        }

        public async Task RenameAsync(int id, string newName)
        {
            string nombre = normalize_name(newName);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            var lista = await db.price_lists.FindAsync(id)
                ?? throw new InvalidOperationException("La lista de precios ya no existe.");

            if (!string.Equals(lista.name, nombre, StringComparison.Ordinal))
                await ensure_name_free_async(db, nombre, excludeId: id);

            lista.name = nombre;
            await db.SaveChangesAsync();
        }

        public async Task SetActiveAsync(int id, bool isActive)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            var lista = await db.price_lists.FindAsync(id);
            if (lista == null) return;

            lista.is_active = isActive;
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

        private static string normalize_name(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("El nombre de la lista de precios no puede estar vacío.");

            return name.Trim();
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
