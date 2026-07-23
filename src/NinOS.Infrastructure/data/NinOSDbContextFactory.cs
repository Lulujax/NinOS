using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NinOS.Infrastructure.Data
{
    public class NinOSDbContextFactory : IDesignTimeDbContextFactory<NinOSDbContext>
    {
        public NinOSDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<NinOSDbContext> options_builder = new DbContextOptionsBuilder<NinOSDbContext>();
            string connection_string = "Host=localhost;Database=ninos_db;Username=postgres;Password=1234";
            
            options_builder.UseNpgsql(connection_string);

            return new NinOSDbContext(options_builder.Options);
        }
    }
}