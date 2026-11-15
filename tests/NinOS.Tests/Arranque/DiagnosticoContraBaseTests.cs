using NinOS.Infrastructure.Data;

namespace NinOS.Tests.Arranque;

/// <summary>
/// Prueba de integracion: corre el diagnostico contra la base de testing de verdad.
///
/// Las pruebas de logica pura no detectan una consulta que no se pueda traducir a SQL
/// (es justo el error que ya reventó antes el arranque con `is_pro_venta`). Esta si.
///
/// Si no hay base configurada, la prueba no hace nada. Si la base configurada NO es de
/// testing, BaseDeTesting lanza excepcion y la prueba falla a proposito.
/// </summary>
public class DiagnosticoContraBaseTests
{
    [Fact]
    public void el_diagnostico_corre_contra_la_base_de_testing_sin_modificar_nada()
    {
        using NinOSDbContext? db_context = BaseDeTesting.crear_contexto_o_null();
        if (db_context == null) return;

        db_diagnostic_report reporte = DbDiagnostics.inspect(db_context);

        // Si llegamos aqui, las consultas se tradujeron a SQL y la base respondio.
        Assert.True(reporte.clientes_sin_zona >= 0);
        Assert.True(reporte.clientes_con_codigo_invalido >= 0);
        Assert.True(reporte.notas_pro_venta_sin_relacion >= 0);
        Assert.True(reporte.tipos_de_nota_obsoletos >= 0);
        Assert.True(reporte.pagos_tipo_nota_de_credito >= 0);

        // El diagnostico es de solo lectura: no debe dejar nada pendiente de guardar.
        Assert.False(db_context.ChangeTracker.HasChanges(),
            "El diagnostico no debe modificar nada: dejo cambios pendientes en el contexto.");

        // La lista de avisos se tiene que poder construir sin reventar.
        Assert.NotNull(reporte.advertencias());
    }

    [Fact]
    public void la_base_de_testing_ya_tiene_datos_asi_que_el_sembrador_se_niega()
    {
        using NinOSDbContext? db_context = BaseDeTesting.crear_contexto_o_null();
        if (db_context == null) return;

        // ninos_db_testing tiene notas, clientes y productos reales copiados de produccion.
        Assert.False(DbSeeder.esta_vacia(db_context),
            "La base de testing deberia tener datos. Si esta vacia, revise a que base apunta.");

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => DbSeeder.seed_initial_data(db_context));

        Assert.Contains("vacia", error.Message);
        Assert.False(db_context.ChangeTracker.HasChanges(),
            "Al negarse, el sembrador no debe dejar ningun cambio pendiente.");
    }
}
