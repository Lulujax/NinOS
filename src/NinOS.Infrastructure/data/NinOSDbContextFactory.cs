using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NinOS.Infrastructure.Data
{
    public class NinOSDbContextFactory : IDesignTimeDbContextFactory<NinOSDbContext>
    {
        public NinOSDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<NinOSDbContext> options_builder = new DbContextOptionsBuilder<NinOSDbContext>();
            options_builder.UseNpgsql(DbConnectionFactory.GetConnectionString());

            return new NinOSDbContext(options_builder.Options);
        }
    }
}