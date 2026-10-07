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
    }
}
