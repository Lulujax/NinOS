using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Repositories.Interfaces;

namespace NinOS.Infrastructure.Repositories.Implementations
{
    public class GenericRepository<t_entity> : IGenericRepository<t_entity> where t_entity : class
    {
        private readonly NinOSDbContext _db_context;
        private readonly DbSet<t_entity> _db_set;

        public GenericRepository(NinOSDbContext db_context)
        {
            if (db_context == null)
            {
                throw new ArgumentNullException(nameof(db_context));
            }
            
            _db_context = db_context;
            _db_set = _db_context.Set<t_entity>();
        }

        public async Task add_async(t_entity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            
            await _db_set.AddAsync(entity);
            await _db_context.SaveChangesAsync();
        }

        public async Task<t_entity?> get_by_id_async(int id)
        {
            t_entity? entity = await _db_set.FindAsync(id);
            
            if (entity == null)
            {
                throw new InvalidOperationException();
            }
            
            return entity;
        }

        public async Task<t_entity[]> get_all_async()
        {
            return await _db_set.ToArrayAsync();
        }

        public async Task update_async(t_entity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            
            _db_set.Update(entity);
            await _db_context.SaveChangesAsync();
        }

        public async Task delete_async(t_entity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            
            _db_set.Remove(entity);
            await _db_context.SaveChangesAsync();
        }
    }
}