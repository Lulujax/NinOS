using System;

namespace NinOS.Infrastructure.Data
{
    public static class DbConnectionFactory
    {
        private const string DefaultConnectionString = "Host=localhost;Database=ninos_db;Username=postgres;Password=1234";

        public static string GetConnectionString()
        {
            string? configured = Environment.GetEnvironmentVariable("NINOS_DB_CONNECTION");
            return string.IsNullOrWhiteSpace(configured) ? DefaultConnectionString : configured;
        }
    }
}
