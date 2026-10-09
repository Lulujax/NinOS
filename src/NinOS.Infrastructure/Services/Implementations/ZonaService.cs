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
    public class ZonaService : IZonaService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ZonaService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        public async Task<List<zona>> GetAllAsync(bool includeInactive = false)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            var query = db.zonas.AsNoTracking();

            if (!includeInactive)
                query = query.Where(z => z.is_active);

            return await query.OrderBy(z => z.code).ToListAsync();
        }

        public async Task<List<zona>> GetActiveAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.zonas
                .AsNoTracking()
                .Where(z => z.is_active)
                .OrderBy(z => z.code)
                .ToListAsync();
        }

        public async Task<List<zona>> GetDeletedAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.zonas
                .AsNoTracking()
                .Where(z => !z.is_active && z.name != "-")
                .OrderByDescending(z => z.deleted_at)
                .ToListAsync();
        }

        public async Task<zona?> GetByIdAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.zonas.FindAsync(id);
        }

        public async Task<zona?> GetByCodeAsync(string code)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.zonas
                .FirstOrDefaultAsync(z => z.code == code && z.is_active);
        }

        public async Task CreateAsync(zona zona)
        {
            if (string.IsNullOrWhiteSpace(zona.code))
                zona.code = await GetNextCodeAsync();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            db.zonas.Add(zona);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAsync(zona zona)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            db.zonas.Update(zona);
            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id, string reason)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            var zona = await db.zonas.FindAsync(id);
            if (zona == null || !zona.is_active) return;

            zona.is_active = false;
            zona.deleted_at = DateTime.UtcNow;
            zona.deleted_reason = string.IsNullOrWhiteSpace(reason) ? "Sin motivo" : reason.Trim();

            await db.SaveChangesAsync();
        }

        public async Task RestoreAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            var zona = await db.zonas.FindAsync(id);
            if (zona == null || zona.is_active) return;

            zona.is_active = true;
            zona.deleted_at = null;
            zona.deleted_reason = null;

            await db.SaveChangesAsync();
        }

        public async Task<bool> ExistsActiveAsync(string name, string code, int? excludeId = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            var query = db.zonas
                .Where(z => z.is_active &&
                            (z.name.ToLower() == name.ToLower() ||
                             z.code == code));

            if (excludeId.HasValue)
                query = query.Where(z => z.id_zona != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<string> GetNextCodeAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            var codes = await db.zonas
                .AsNoTracking()
                .Select(z => z.code)
                .ToListAsync();

            int maxNum = 0;
            foreach (var c in codes)
            {
                if (int.TryParse(c.Trim(), out int n))
                {
                    if (n > maxNum) maxNum = n;
                }
            }

            return (maxNum + 1).ToString("00");
        }
    }
}

