using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Common
{
    public static class AdminSecurity
    {
        private static readonly AdminLoginLimiter login_limiter = new AdminLoginLimiter();

        public static int MinimumPasswordLength => AdminPasswordHasher.MinimumLength;

        public static bool is_configured()
        {
            return load_key() != null;
        }

        public static bool verify(string password)
        {
            if (login_limiter.is_locked(DateTime.Now)) return false;
            if (string.IsNullOrEmpty(password)) return false;

            admin_key? stored = load_key();
            if (stored == null)
            {
                AppLog.Warn("Panel Admin sin clave configurada: acceso bloqueado");
                return false;
            }

            if (AdminPasswordHasher.matches(password, stored))
            {
                login_limiter.reset();
                AppLog.Info("Acceso al Panel Admin correcto");
                return true;
            }

            login_limiter.register_failure(DateTime.Now);
            AppLog.Error("Intento fallido de acceso al Panel Admin");
            return false;
        }

        public static bool change_password(string new_password)
        {
            if (!AdminPasswordHasher.is_long_enough(new_password))
            {
                AppLog.Warn($"Clave del Panel Admin rechazada: minimo {MinimumPasswordLength} caracteres");
                return false;
            }

            try
            {
                using NinOSDbContext db_context = create_context();
                db_context.Database.Migrate();

                admin_key fresh_key = AdminPasswordHasher.create(new_password, DateTime.UtcNow);
                admin_key? existing = db_context.admin_keys.FirstOrDefault(k => k.id_key == admin_key.SingleRowId);
                if (existing == null)
                {
                    db_context.admin_keys.Add(fresh_key);
                }
                else
                {
                    existing.password_salt = fresh_key.password_salt;
                    existing.password_hash = fresh_key.password_hash;
                    existing.iterations = fresh_key.iterations;
                    existing.updated_at_utc = fresh_key.updated_at_utc;
                }

                db_context.SaveChanges();
                login_limiter.reset();
                AppLog.Info("Clave del Panel Admin actualizada en la base");
                return true;
            }
            catch (Exception exception)
            {
                AppLog.Error("No se pudo guardar la clave del Panel Admin", exception);
                return false;
            }
        }

        public static int remaining_delay_seconds()
        {
            return login_limiter.remaining_delay_seconds(DateTime.Now);
        }

        private static admin_key? load_key()
        {
            try
            {
                using NinOSDbContext db_context = create_context();
                return db_context.admin_keys
                    .AsNoTracking()
                    .FirstOrDefault(k => k.id_key == admin_key.SingleRowId);
            }
            catch (Exception exception)
            {
                AppLog.Error("No se pudo leer la clave del Panel Admin de la base", exception);
                return null;
            }
        }

        private static NinOSDbContext create_context()
        {
            DbContextOptionsBuilder<NinOSDbContext> options_builder = new DbContextOptionsBuilder<NinOSDbContext>();
            options_builder.UseNpgsql(DbConnectionFactory.GetConnectionString());

            return new NinOSDbContext(options_builder.Options);
        }
    }
}
