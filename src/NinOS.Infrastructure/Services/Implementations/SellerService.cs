using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain;
using NinOS.Infrastructure.Common;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class SellerService : ISellerService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SellerService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        public async Task<IEnumerable<seller>> GetAllActiveAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.sellers
                .AsNoTracking()
                .Include(s => s.seller_zones)
                    .ThenInclude(sz => sz.zona)
                .Where(s => s.is_active)
                .OrderBy(s => s.seller_code)
                .ToListAsync();
        }

        public async Task<IEnumerable<seller>> GetAllDeletedAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.sellers
                .AsNoTracking()
                .Include(s => s.seller_zones)
                    .ThenInclude(sz => sz.zona)
                .Where(s => !s.is_active)
                .OrderByDescending(s => s.deleted_at)
                .ToListAsync();
        }

        public async Task<seller?> GetByIdAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.sellers
                .Include(s => s.seller_zones)
                    .ThenInclude(sz => sz.zona)
                .FirstOrDefaultAsync(s => s.id_seller == id);
        }

        public async Task<seller?> GetByCodeAsync(string code)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            return await db.sellers
                .Include(s => s.seller_zones)
                    .ThenInclude(sz => sz.zona)
                .FirstOrDefaultAsync(s => s.seller_code == code);
        }

        public async Task<string> GetNextSellerCodeAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
            var allCodes = await db.sellers
                .AsNoTracking()
                .Select(s => s.seller_code)
                .ToListAsync();

            return SeriesCalculator.GetNextSellerCode(allCodes);
        }

        public async Task<seller> CreateAsync(seller newSeller, IEnumerable<int> assignedZonaIds)
        {
            if (newSeller == null) throw new ArgumentNullException(nameof(newSeller));
            if (string.IsNullOrWhiteSpace(newSeller.full_name))
                throw new ArgumentException("El nombre del vendedor es obligatorio.");

            if (string.IsNullOrWhiteSpace(newSeller.seller_code))
            {
                newSeller.seller_code = await GetNextSellerCodeAsync();
            }

            if (string.IsNullOrWhiteSpace(newSeller.customer_code_prefix))
            {
                newSeller.customer_code_prefix = newSeller.seller_code;
            }

            var assignedList = assignedZonaIds?.Distinct().ToList() ?? new List<int>();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            // Validar exclusividad de Maracay (06) para Juan Luis (3400)
            await ValidateMaracayExclusivityAsync(db, newSeller, assignedList);

            newSeller.is_active = true;
            newSeller.deleted_at = null;
            newSeller.deleted_reason = null;

            db.sellers.Add(newSeller);
            await db.SaveChangesAsync();

            foreach (var zonaId in assignedList)
            {
                db.seller_zones.Add(new seller_zone
                {
                    id_seller = newSeller.id_seller,
                    id_zona = zonaId
                });
            }

            if (assignedList.Count > 0)
            {
                await db.SaveChangesAsync();
            }

            AppLog.Info($"Vendedor creado: {newSeller.full_name} ({newSeller.seller_code}) con {assignedList.Count} zonas.");
            return newSeller;
        }

        public async Task<seller> UpdateAsync(seller updatedSeller, IEnumerable<int> assignedZonaIds)
        {
            if (updatedSeller == null) throw new ArgumentNullException(nameof(updatedSeller));

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var existing = await db.sellers
                .Include(s => s.seller_zones)
                .FirstOrDefaultAsync(s => s.id_seller == updatedSeller.id_seller);

            if (existing == null)
                throw new InvalidOperationException($"No se encontró el vendedor con ID {updatedSeller.id_seller}.");

            var assignedList = assignedZonaIds?.Distinct().ToList() ?? new List<int>();

            // Validar exclusividad de Maracay (06) para Juan Luis (3400)
            await ValidateMaracayExclusivityAsync(db, existing, assignedList);

            existing.full_name = updatedSeller.full_name;
            if (!string.IsNullOrWhiteSpace(updatedSeller.seller_code))
            {
                existing.seller_code = updatedSeller.seller_code;
            }
            if (!string.IsNullOrWhiteSpace(updatedSeller.customer_code_prefix))
            {
                existing.customer_code_prefix = updatedSeller.customer_code_prefix;
            }

            // Actualizar zonas asignadas
            db.seller_zones.RemoveRange(existing.seller_zones);

            foreach (var zonaId in assignedList)
            {
                db.seller_zones.Add(new seller_zone
                {
                    id_seller = existing.id_seller,
                    id_zona = zonaId
                });
            }

            await db.SaveChangesAsync();
            AppLog.Info($"Vendedor actualizado: {existing.full_name} ({existing.seller_code}) con {assignedList.Count} zonas.");
            return existing;
        }

        public async Task SoftDeleteAsync(int id, string? reason)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var existing = await db.sellers.FirstOrDefaultAsync(s => s.id_seller == id);
            if (existing == null || !existing.is_active) return;

            existing.is_active = false;
            existing.deleted_at = DateTime.UtcNow;
            existing.deleted_reason = string.IsNullOrWhiteSpace(reason) ? "Sin motivo" : reason.Trim();

            await db.SaveChangesAsync();
            AppLog.Info($"Vendedor {existing.full_name} ({existing.seller_code}) enviado a la papelera. Motivo: {existing.deleted_reason}");
        }

        public async Task RestoreAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            var existing = await db.sellers.FirstOrDefaultAsync(s => s.id_seller == id);
            if (existing == null || existing.is_active) return;

            existing.is_active = true;
            existing.deleted_at = null;
            existing.deleted_reason = null;

            await db.SaveChangesAsync();
            AppLog.Info($"Vendedor {existing.full_name} ({existing.seller_code}) restaurado de la papelera.");
        }

        public async Task<IEnumerable<zona>> GetAssignedZonasAsync(int sellerId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

            return await db.seller_zones
                .AsNoTracking()
                .Where(sz => sz.id_seller == sellerId)
                .Include(sz => sz.zona)
                .Select(sz => sz.zona!)
                .Where(z => z != null && z.is_active)
                .OrderBy(z => z.code)
                .ToListAsync();
        }

        private async Task ValidateMaracayExclusivityAsync(NinOSDbContext db, seller sellerEntity, List<int> assignedZonaIds)
        {
            if (assignedZonaIds == null || assignedZonaIds.Count == 0) return;

            bool isJuanLuis = string.Equals(sellerEntity.seller_code?.Trim(), "3400", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(sellerEntity.full_name?.Trim(), "Juan Luis", StringComparison.OrdinalIgnoreCase);

            if (isJuanLuis) return;

            // Buscar si alguna de las zonas asignadas es Maracay (código "06" o nombre "Maracay")
            var maracayZonas = await db.zonas
                .Where(z => assignedZonaIds.Contains(z.id_zona) && (z.code == "06" || z.name.ToLower().Contains("maracay")))
                .ToListAsync();

            if (maracayZonas.Any())
            {
                throw new InvalidOperationException("La zona Maracay (06) es exclusiva y solo puede asignarse al vendedor Juan Luis (3400).");
            }
        }
    }
}

