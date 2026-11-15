using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using NinOS.Infrastructure.Data;

namespace NinOS.Tests.Arranque;

/// <summary>
/// La prueba que de verdad respalda la spec 007: abrir la app contra una base CON DATOS no
/// cambia ni una fila. Corre contra la base de testing real, porque es lo unico que puede
/// demostrarlo.
///
/// Si no hay base configurada, no hace nada. Si la base no es de testing, falla a proposito.
/// </summary>
public class ArranqueContraBaseTests
{
    /// <summary>Foto de la base: cuantas filas y cuanto dinero hay en cada tabla sensible.</summary>
    private static Dictionary<string, string> tomar_foto(NinOSDbContext db)
    {
        return new Dictionary<string, string>
        {
            ["notas_de_entrega"] = db.delivery_notes.AsNoTracking().Count().ToString(),
            ["detalles_de_nota"] = db.note_details.AsNoTracking().Count().ToString(),
            ["pagos"] = db.payments.AsNoTracking().Count().ToString(),
            ["total_pagado"] = db.payments.AsNoTracking().Sum(p => p.amount_usd).ToString("F2"),
            ["total_notas"] = db.delivery_notes.AsNoTracking().Sum(n => n.adjusted_total_usd).ToString("F2"),
            ["comisiones"] = db.commissions.AsNoTracking().Count().ToString(),
            ["pagos_de_comision"] = db.commission_payments.AsNoTracking().Count().ToString(),
            ["notas_de_credito"] = db.credit_notes.AsNoTracking().Count().ToString(),
            ["clientes"] = db.customers.AsNoTracking().Count().ToString(),
            ["clientes_sin_zona"] = db.customers.AsNoTracking().Count(c => c.id_zona == null).ToString(),
            ["vendedoras"] = db.sellers.AsNoTracking().Count().ToString(),
            ["asignaciones_de_zona"] = db.seller_zones.AsNoTracking().Count().ToString(),
            ["zonas"] = db.zonas.AsNoTracking().Count().ToString(),
            ["zonas_activas"] = db.zonas.AsNoTracking().Count(z => z.is_active).ToString(),
            ["tipos_de_nota"] = db.note_types.AsNoTracking().Count().ToString(),
            ["relaciones"] = db.relaciones.AsNoTracking().Count().ToString(),
            ["productos"] = db.products.AsNoTracking().Count().ToString(),
            ["lineas_de_producto"] = db.product_lines.AsNoTracking().Count().ToString(),
            ["movimientos_de_stock"] = db.stock_movements.AsNoTracking().Count().ToString()
        };
    }

    [Fact]
    public void el_arranque_no_modifica_ni_una_fila_de_una_base_con_datos()
    {
        using NinOSDbContext? antes = BaseDeTesting.crear_contexto_o_null();
        if (antes == null) return;

        Dictionary<string, string> foto_antes = tomar_foto(antes);

        // Se arranca dos veces seguidas: antes, la segunda apertura podia renumerar o borrar
        // cosas que la primera habia dejado a medias.
        for (int apertura = 1; apertura <= 2; apertura++)
        {
            using NinOSDbContext arranque = BaseDeTesting.crear_contexto_o_null()!;
            DbInitializer.initialize(arranque);

            Assert.False(arranque.ChangeTracker.HasChanges(),
                $"La apertura {apertura} dejo cambios pendientes de guardar: el arranque no debe escribir nada.");
        }

        using NinOSDbContext despues = BaseDeTesting.crear_contexto_o_null()!;
        Dictionary<string, string> foto_despues = tomar_foto(despues);

        List<string> diferencias = new List<string>();
        foreach (KeyValuePair<string, string> fila in foto_antes)
        {
            string valor_despues = foto_despues[fila.Key];
            if (fila.Value != valor_despues)
                diferencias.Add($"{fila.Key}: antes {fila.Value}, despues {valor_despues}");
        }

        Assert.True(diferencias.Count == 0,
            "Abrir la app cambio la base. Diferencias: " + string.Join(" | ", diferencias));
    }

    [Fact]
    public void el_arranque_no_deja_tomado_el_candado_de_migraciones()
    {
        using NinOSDbContext? db_context = BaseDeTesting.crear_contexto_o_null();
        if (db_context == null) return;

        DbInitializer.initialize(db_context);

        Assert.Equal(0, candados_tomados(db_context));
    }

    /// <summary>Cuantas sesiones tienen tomado el candado de migraciones en este momento.</summary>
    private static int candados_tomados(NinOSDbContext db_context)
    {
        DbConnection connection = db_context.Database.GetDbConnection();
        bool la_abrimos_nosotros = connection.State != System.Data.ConnectionState.Open;
        if (la_abrimos_nosotros) connection.Open();

        try
        {
            using DbCommand comando = connection.CreateCommand();
            comando.CommandText =
                "SELECT count(*) FROM pg_locks WHERE locktype = 'advisory' AND objid = "
                + DbInitializer.MigrationLockKey;

            return Convert.ToInt32(comando.ExecuteScalar());
        }
        finally
        {
            if (la_abrimos_nosotros) connection.Close();
        }
    }
}
