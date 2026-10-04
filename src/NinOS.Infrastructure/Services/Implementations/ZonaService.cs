using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class ZonaService : IZonaService
    {
        private readonly NinOSDbContext _dbContext;

        public ZonaService(NinOSDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<zona>> GetAllAsync(bool includeInactive = false)
        {
            var query = _dbContext.zonas.AsNoTracking();

            if (!includeInactive)
                query = query.Where(z => z.is_active);

            return await query.OrderBy(z => z.sort_order).ThenBy(z => z.code).ToListAsync();
        }

        public async Task<List<zona>> GetActiveAsync()
        {
            return await _dbContext.zonas
                .AsNoTracking()
                .Where(z => z.is_active)
                .OrderBy(z => z.sort_order)
                .ThenBy(z => z.code)
                .ToListAsync();
        }

        public async Task<List<zona>> GetDeletedAsync()
        {
            return await _dbContext.zonas
                .AsNoTracking()
                .Where(z => !z.is_active)
                .OrderByDescending(z => z.deleted_at)
                .ToListAsync();
        }

        public async Task<zona?> GetByIdAsync(int id)
        {
            return await _dbContext.zonas.FindAsync(id);
        }

        public async Task<zona?> GetByCodeAsync(string code)
        {
            return await _dbContext.zonas
                .FirstOrDefaultAsync(z => z.code == code && z.is_active);
        }

        public async Task CreateAsync(zona zona)
        {
            if (string.IsNullOrWhiteSpace(zona.code))
                zona.code = await GetNextCodeAsync();

            _dbContext.zonas.Add(zona);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(zona zona)
        {
            _dbContext.zonas.Update(zona);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id, string reason)
        {
            var zona = await _dbContext.zonas.FindAsync(id);
            if (zona == null || !zona.is_active) return;

            zona.is_active = false;
            zona.deleted_at = DateTime.UtcNow;
            zona.deleted_reason = string.IsNullOrWhiteSpace(reason) ? "Sin motivo" : reason.Trim();

            await _dbContext.SaveChangesAsync();
        }

        public async Task RestoreAsync(int id)
        {
            var zona = await _dbContext.zonas.FindAsync(id);
            if (zona == null || zona.is_active) return;

            zona.is_active = true;
            zona.deleted_at = null;
            zona.deleted_reason = null;

            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> ExistsActiveAsync(string name, string code, int? excludeId = null)
        {
            var query = _dbContext.zonas
                .Where(z => z.is_active &&
                            (z.name.ToLower() == name.ToLower() ||
                             z.code == code));

            if (excludeId.HasValue)
                query = query.Where(z => z.id_zona != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<string> GetNextCodeAsync()
        {
            var codes = await _dbContext.zonas
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
