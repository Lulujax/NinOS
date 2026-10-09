using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class ProductLineService : IProductLineService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ProductLineService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<List<product_line>> GetAllAsync(bool includeInactive = false)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            
            var query = db.product_lines.Where(x => x.deleted_at == null);
            if (!includeInactive)
                query = query.Where(x => x.is_active);
                
            return await query.OrderBy(x => x.sort_order).ToListAsync();
        }

        public async Task<List<product_line>> GetActiveAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.product_lines
                .Where(x => x.is_active && x.deleted_at == null)
                .OrderBy(x => x.sort_order)
                .ToListAsync();
        }

        public async Task<List<product_line>> GetDeletedAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.product_lines
                .Where(x => x.deleted_at != null)
                .OrderByDescending(x => x.deleted_at)
                .ToListAsync();
        }

        public async Task<product_line?> GetByIdAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.product_lines.FindAsync(id);
        }

        public async Task<product_line?> GetByCodePrefixAsync(string codePrefix)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.product_lines
                .FirstOrDefaultAsync(x => x.code_prefix.ToLower() == codePrefix.ToLower() && x.is_active && x.deleted_at == null);
        }

        public async Task CreateAsync(product_line productLine)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            
            productLine.name = productLine.name.Trim().ToUpperInvariant();
            productLine.code_prefix = productLine.code_prefix.Trim().ToUpperInvariant();
            
            db.product_lines.Add(productLine);
            await db.SaveChangesAsync();

            product_code_rules.RegisterPrefix(productLine.name, productLine.code_prefix);
        }

        public async Task UpdateAsync(product_line productLine)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            
            productLine.name = productLine.name.Trim().ToUpperInvariant();
            productLine.code_prefix = productLine.code_prefix.Trim().ToUpperInvariant();
            
            db.product_lines.Update(productLine);
            await db.SaveChangesAsync();

            product_code_rules.RegisterPrefix(productLine.name, productLine.code_prefix);
        }

        public async Task DeleteAsync(int id, string reason)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            
            var line = await db.product_lines.FindAsync(id);
            if (line != null)
            {
                line.is_active = false;
                line.deleted_at = DateTime.UtcNow;
                line.deleted_reason = reason;
                await db.SaveChangesAsync();
            }
        }

        public async Task RestoreAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            
            var line = await db.product_lines.FindAsync(id);
            if (line != null)
            {
                line.is_active = true;
                line.deleted_at = null;
                line.deleted_reason = null;
                await db.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsActiveAsync(string name, string codePrefix, int? excludeId = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            
            var query = db.product_lines.Where(x => x.deleted_at == null);
            if (excludeId.HasValue)
                query = query.Where(x => x.id_product_line != excludeId.Value);
                
            return await query.AnyAsync(x => 
                x.name.ToLower() == name.ToLower() || 
                x.code_prefix.ToLower() == codePrefix.ToLower());
        }

        public async Task<bool> HasProductsAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            
            var line = await db.product_lines.FindAsync(id);
            if (line == null) return false;
            
            return await db.products.AnyAsync(p => 
                p.is_active && 
                p.category.ToLower() == line.name.ToLower());
        }

        /// <summary>
        /// Arma el lista de productos de la linea y el codigo que cada uno tendria con el
        /// prefijo nuevo. Solo se usa para mostrar el alcance: no escribe nada.
        /// </summary>
        private static async Task<List<product_code_migration_item>> build_migration_items_async(
            NinOSDbContext db,
            string line_name,
            string old_prefix,
            string new_prefix,
            int exclude_product_id = 0)
        {
            var products = await db.products
                .AsNoTracking()
                .Where(p => p.deleted_at == null
                         && p.category != null
                         && p.category.ToLower() == line_name.ToLower())
                .ToListAsync();

            // Codigos ya tomados por otros productos: si el nuevo prefijo choca con uno, ese
            // producto no se toca en vez de romper el indice unico.
            var occupied = new HashSet<string>(
                products
                    .Where(p => p.id_product != exclude_product_id)
                    .Select(p => p.product_code)
                    .Where(c => !string.IsNullOrWhiteSpace(c)),
                StringComparer.OrdinalIgnoreCase);

            var items = new List<product_code_migration_item>();

            foreach (var p in products)
            {
                var item = new product_code_migration_item
                {
                    id_product = p.id_product,
                    old_code = p.product_code,
                    product_name = p.name
                };

                int? correlativo = product_code_rules.TryParseNumber(p.product_code, old_prefix);

                if (correlativo == null)
                {
                    item.new_code = p.product_code;
                    item.queda_igual = true;
                    item.motivo = $"No sigue el formato del prefijo {old_prefix}, se deja como está.";
                }
                else
                {
                    string candidato = product_code_rules.Format(new_prefix, correlativo.Value, product_code_rules.DigitsFor(new_prefix));

                    if (occupied.Contains(candidato))
                    {
                        item.new_code = p.product_code;
                        item.queda_igual = true;
                        item.motivo = $"El código {candidato} ya lo usa otro producto, se deja como está.";
                    }
                    else
                    {
                        item.new_code = candidato;
                    }
                }

                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// Un cambio de codigo ya resuelto: producto, codigo viejo y codigo nuevo.
        /// </summary>
        private sealed class code_change
        {
            public int id_product { get; init; }
            public string old_code { get; init; } = string.Empty;
            public string new_code { get; init; } = string.Empty;
        }

        public async Task<product_code_migration_preview> preview_prefix_change_async(
            int id_product_line,
            string old_prefix,
            string new_prefix)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var line = await db.product_lines.FindAsync(id_product_line);
            if (line == null) return new product_code_migration_preview();

            old_prefix = (old_prefix ?? string.Empty).Trim().ToUpperInvariant();
            new_prefix = (new_prefix ?? string.Empty).Trim().ToUpperInvariant();

            var items = await build_migration_items_async(db, line.name, old_prefix, new_prefix);

            var preview = new product_code_migration_preview
            {
                old_prefix = old_prefix,
                new_prefix = new_prefix,
                items = items
            };

            var changes = items
                .Where(i => !i.queda_igual)
                .Select(i => new code_change { id_product = i.id_product, old_code = i.old_code, new_code = i.new_code })
                .ToList();

            if (changes.Count == 0) return preview;

            var ids = changes.Select(c => c.id_product).ToList();

            // Cuanto historial quedaria mostrando un codigo que ya no existe si solo se tocaran
            // los productos. Por eso la migracion tambien reescribe los snapshots.
            var mapa = changes.ToDictionary(c => c.id_product, c => (c.old_code, c.new_code));

            var snapshots_notas = await db.note_details
                .AsNoTracking()
                .Where(d => d.id_product != null && ids.Contains(d.id_product.Value) && d.product_code_snapshot != null)
                .Select(d => new { d.id_product, d.product_code_snapshot })
                .ToListAsync();

            preview.detalles_de_notas_afectados = snapshots_notas.Count(d =>
                mapa.TryGetValue(d.id_product!.Value, out var m) && m.old_code == d.product_code_snapshot);

            var snapshots_nc = await db.credit_note_details
                .AsNoTracking()
                .Where(d => d.id_product != null && ids.Contains(d.id_product.Value) && d.product_code_snapshot != null)
                .Select(d => new { d.id_product, d.product_code_snapshot })
                .ToListAsync();

            preview.detalles_de_notas_de_credito_afectados = snapshots_nc.Count(d =>
                mapa.TryGetValue(d.id_product!.Value, out var m) && m.old_code == d.product_code_snapshot);

            var docs = await db.stock_movements
                .AsNoTracking()
                .Where(m => ids.Contains(m.id_product) && m.document_number != null)
                .Select(m => new { m.id_product, m.document_number })
                .ToListAsync();

            preview.ajustes_de_kardex_afectados = docs.Count(m =>
                mapa.TryGetValue(m.id_product, out var c)
                && m.document_number == stock_movement.RazonAjuste + "-" + c.old_code);

            return preview;
        }

        public async Task<product_code_migration_result> migrate_prefix_async(
            int id_product_line,
            string old_prefix,
            string new_prefix)
        {
            var result = new product_code_migration_result();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var line = await db.product_lines.FindAsync(id_product_line);
            if (line == null)
            {
                result.mensaje = "La línea de productos ya no existe.";
                return result;
            }

            old_prefix = (old_prefix ?? string.Empty).Trim().ToUpperInvariant();
            new_prefix = (new_prefix ?? string.Empty).Trim().ToUpperInvariant();

            if (old_prefix == new_prefix)
            {
                result.mensaje = "El prefijo no cambió.";
                return result;
            }

            // El prefijo y el historial van juntos en una transaccion: si se corta a la mitad
            // quedan notas mostrando un codigo que ya no corresponde a su producto.
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var items = await build_migration_items_async(db, line.name, old_prefix, new_prefix);
                var cambios = items.Where(i => !i.queda_igual).ToList();

                foreach (var cambio in cambios)
                {
                    var product = await db.products.FindAsync(cambio.id_product);
                    if (product == null) continue;

                    product.product_code = cambio.new_code;
                }

                line.code_prefix = new_prefix;
                await db.SaveChangesAsync();

                // Historial: se reescriben los snapshots de las notas y notas de credito, y los
                // documentos de ajuste del kardex que tenian el codigo embebido ("AJUSTE-DEF30508").
                foreach (var cambio in cambios)
                {
                    await db.note_details
                        .Where(d => d.id_product == cambio.id_product && d.product_code_snapshot == cambio.old_code)
                        .ExecuteUpdateAsync(s => s.SetProperty(d => d.product_code_snapshot, cambio.new_code));

                    await db.credit_note_details
                        .Where(d => d.id_product == cambio.id_product && d.product_code_snapshot == cambio.old_code)
                        .ExecuteUpdateAsync(s => s.SetProperty(d => d.product_code_snapshot, cambio.new_code));

                    string ajuste_viejo = $"{stock_movement.RazonAjuste}-{cambio.old_code}";
                    string ajuste_nuevo = $"{stock_movement.RazonAjuste}-{cambio.new_code}";

                    await db.stock_movements
                        .Where(m => m.id_product == cambio.id_product && m.document_number == ajuste_viejo)
                        .ExecuteUpdateAsync(s => s.SetProperty(m => m.document_number, ajuste_nuevo));
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                product_code_rules.RegisterPrefix(line.name, new_prefix);

                result.sucesso = true;
                result.productos_actualizados = cambios.Count;
                result.mensaje = cambios.Count == 0
                    ? "No había productos con el prefijo anterior."
                    : $"{cambios.Count} producto(s) actualizados.";

                AppLog.Info($"Prefijo de la linea {line.name}: {old_prefix} -> {new_prefix}. "
                            + $"Productos migrados: {cambios.Count}.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                result.sucesso = false;
                result.mensaje = $"No se pudo migrar el prefijo: {ex.Message}";

                AppLog.Error($"Error migrando el prefijo de la linea {line.name} "
                             + $"de {old_prefix} a {new_prefix}: {ex}");
            }

            return result;
        }
    }
}
