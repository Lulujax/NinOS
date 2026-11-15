using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Logging;

namespace NinOS.Infrastructure.Data
{
    /// <summary>
    /// Lo unico que corre al abrir la app.
    ///
    /// Spec 007: una base CON DATOS nunca se toca al arrancar. No se ejecuta ningun INSERT,
    /// UPDATE ni DELETE de datos de negocio. Antes este arranque borraba pagos, renumeraba
    /// notas y clientes, restauraba zonas eliminadas y reasignaba zonas a vendedoras, todo
    /// sin avisar y en cada apertura. Eso vive ahora en DbSeeder (solo base vacia) o se hace
    /// con un script supervisado y respaldo previo.
    ///
    /// Lo que si cambia es la ESTRUCTURA: las migraciones pendientes se aplican aqui.
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Numero del candado de PostgreSQL que serializa las migraciones entre PCs.
        /// Es publico para que las pruebas puedan comprobar que queda liberado.
        /// </summary>
        public const long MigrationLockKey = 9070001L;

        public static void initialize(NinOSDbContext db_context)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));

            apply_pending_migrations(db_context);
            load_product_code_prefixes(db_context);
            DbDiagnostics.log(DbDiagnostics.inspect(db_context));
        }

        private static void apply_pending_migrations(NinOSDbContext db_context)
        {
            if (!db_context.Database.GetPendingMigrations().Any())
            {
                AppLog.Info("Base de datos al dia: sin migraciones pendientes");
                return;
            }

            DbConnection connection = db_context.Database.GetDbConnection();
            bool la_abrimos_nosotros = connection.State != ConnectionState.Open;

            // La conexion se mantiene abierta a mano porque el candado de PostgreSQL vive en
            // la sesion: si la conexion se cierra en medio, el candado se suelta solo y dos
            // PCs podrian migrar a la vez.
            if (la_abrimos_nosotros) connection.Open();

            try
            {
                ejecutar(connection, $"SELECT pg_advisory_lock({MigrationLockKey})");
                AppLog.Info("Turno tomado para aplicar migraciones");

                try
                {
                    // Se vuelve a preguntar: otra PC pudo aplicarlas mientras esperabamos el
                    // turno. Sin esto, la segunda PC reventaba con clave duplicada en
                    // __EFMigrationsHistory y el usuario veia "verifique su internet".
                    string[] pendientes = db_context.Database.GetPendingMigrations().ToArray();

                    if (pendientes.Length == 0)
                    {
                        AppLog.Info("Otra PC ya aplico las migraciones mientras esperabamos el turno");
                        return;
                    }

                    AppLog.Info($"Aplicando {pendientes.Length} migracion(es): {string.Join(", ", pendientes)}");
                    db_context.Database.Migrate();
                    AppLog.Info("Migraciones aplicadas correctamente");
                }
                finally
                {
                    ejecutar(connection, $"SELECT pg_advisory_unlock({MigrationLockKey})");
                    AppLog.Info("Turno de migraciones liberado");
                }
            }
            finally
            {
                if (la_abrimos_nosotros) connection.Close();
            }
        }

        private static void ejecutar(DbConnection connection, string sql)
        {
            using DbCommand comando = connection.CreateCommand();
            comando.CommandText = sql;
            comando.ExecuteNonQuery();
        }

        /// <summary>
        /// Carga en memoria los prefijos de codigo de cada linea de producto. Es solo lectura:
        /// no crea ni modifica lineas. Si falta alguna, el diagnostico lo avisa.
        /// </summary>
        private static void load_product_code_prefixes(NinOSDbContext db_context)
        {
            var lineas_activas = db_context.product_lines
                .AsNoTracking()
                .Where(l => l.is_active)
                .Select(l => new { l.name, l.code_prefix })
                .ToList();

            foreach (var linea in lineas_activas)
            {
                product_code_rules.RegisterPrefix(linea.name, linea.code_prefix);
            }

            AppLog.Info($"Prefijos de codigo cargados: {lineas_activas.Count} linea(s) de producto");
        }
    }
}
