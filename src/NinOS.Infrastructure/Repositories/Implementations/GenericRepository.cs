using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Repositories.Interfaces;

namespace NinOS.Infrastructure.Repositories.Implementations
{
    public class GenericRepository<t_entity> : IGenericRepository<t_entity> where t_entity : class
    {
        private readonly IServiceScopeFactory? _scope_factory;
        private readonly NinOSDbContext? _db_context;

        public GenericRepository(IServiceScopeFactory scope_factory)
        {
            _scope_factory = scope_factory ?? throw new ArgumentNullException(nameof(scope_factory));
        }

        protected GenericRepository(NinOSDbContext db_context)
        {
            _db_context = db_context ?? throw new ArgumentNullException(nameof(db_context));
        }

        public async Task add_async(t_entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            if (_scope_factory != null)
            {
                using var scope = _scope_factory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                await db.Set<t_entity>().AddAsync(entity);
                await db.SaveChangesAsync();
            }
            else
            {
                await _db_context!.Set<t_entity>().AddAsync(entity);
                await _db_context.SaveChangesAsync();
            }

            AppLog.Info($"INSERT {typeof(t_entity).Name}");
        }

        public async Task<t_entity?> get_by_id_async(int id)
        {
            if (_scope_factory != null)
            {
                using var scope = _scope_factory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                t_entity? entity = await db.Set<t_entity>().FindAsync(id);
                if (entity == null) throw new InvalidOperationException();
                return entity;
            }
            else
            {
                t_entity? entity = await _db_context!.Set<t_entity>().FindAsync(id);
                if (entity == null) throw new InvalidOperationException();
                return entity;
            }
        }

        public async Task<t_entity[]> get_all_async()
        {
            if (_scope_factory != null)
            {
                using var scope = _scope_factory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                return await db.Set<t_entity>().AsNoTracking().ToArrayAsync();
            }
            else
            {
                return await _db_context!.Set<t_entity>().AsNoTracking().ToArrayAsync();
            }
        }

        public async Task update_async(t_entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            if (_scope_factory != null)
            {
                using var scope = _scope_factory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                db.Set<t_entity>().Update(entity);
                await db.SaveChangesAsync();
            }
            else
            {
                _db_context!.Set<t_entity>().Update(entity);
                await _db_context.SaveChangesAsync();
            }

            AppLog.Info($"UPDATE {typeof(t_entity).Name}");
        }

        public async Task delete_async(t_entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            if (_scope_factory != null)
            {
                using var scope = _scope_factory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                db.Set<t_entity>().Remove(entity);
                await db.SaveChangesAsync();
            }
            else
            {
                _db_context!.Set<t_entity>().Remove(entity);
                await _db_context.SaveChangesAsync();
            }

            AppLog.Info($"DELETE {typeof(t_entity).Name}");
        }
    }
}