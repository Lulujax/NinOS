using System.Reflection;
using NinOS.Infrastructure.Data;

namespace NinOS.Tests.Arranque;

/// <summary>
/// Spec 007: el arranque solo mira y avisa. Estas pruebas cubren el diagnostico (funcion
/// pura, sin base de datos) y dejan constancia de que el codigo que modificaba datos al
/// abrir la app ya no existe.
/// </summary>
public class DiagnosticoDeArranqueTests
{
    private static db_diagnostic_report base_sana() => new db_diagnostic_report
    {
        clientes_sin_zona = 0,
        clientes_con_codigo_invalido = 0,
        notas_pro_venta_sin_relacion = 0,
        tipos_de_nota_obsoletos = 0,
        pagos_tipo_nota_de_credito = 0,
        tipos_de_nota_activos = 6,
        lineas_de_producto_activas = 10,
        base_vacia = false
    };

    // ---------- Caso feliz ----------

    [Fact]
    public void caso_feliz_base_sana_no_genera_ninguna_advertencia()
    {
        db_diagnostic_report reporte = base_sana();

        Assert.Empty(reporte.advertencias());
        Assert.False(reporte.hay_advertencias, "Una base sana no debia tener nada que revisar.");
    }

    // ---------- Casos limite ----------

    [Fact]
    public void caso_limite_base_vacia_avisa_del_comando_de_siembra_y_no_repite_los_demas_avisos()
    {
        db_diagnostic_report reporte = new db_diagnostic_report
        {
            base_vacia = true,
            tipos_de_nota_activos = 0,
            lineas_de_producto_activas = 0
        };

        IReadOnlyList<string> avisos = reporte.advertencias();

        Assert.Single(avisos);
        Assert.Contains("--seed-initial-data", avisos[0]);
    }

    [Fact]
    public void caso_limite_un_solo_cliente_sin_zona_ya_se_avisa()
    {
        db_diagnostic_report reporte = new db_diagnostic_report
        {
            clientes_sin_zona = 1,
            tipos_de_nota_activos = 6,
            lineas_de_producto_activas = 10
        };

        string aviso = Assert.Single(reporte.advertencias());
        Assert.Contains("1 cliente(s) sin zona", aviso);
    }

    [Fact]
    public void caso_limite_sin_tipos_de_nota_ni_lineas_avisa_de_los_dos()
    {
        db_diagnostic_report reporte = new db_diagnostic_report
        {
            base_vacia = false,
            tipos_de_nota_activos = 0,
            lineas_de_producto_activas = 0
        };

        IReadOnlyList<string> avisos = reporte.advertencias();

        Assert.Equal(2, avisos.Count);
        Assert.Contains(avisos, a => a.Contains("tipos de nota"));
        Assert.Contains(avisos, a => a.Contains("lineas de producto"));
    }

    // ---------- Casos de error ----------

    [Fact]
    public void caso_error_pagos_de_nota_de_credito_se_reportan_pero_NO_se_borran()
    {
        // Antes el arranque BORRABA estos pagos en cada apertura. Ahora solo avisa.
        db_diagnostic_report reporte = new db_diagnostic_report
        {
            pagos_tipo_nota_de_credito = 7,
            tipos_de_nota_activos = 6,
            lineas_de_producto_activas = 10
        };

        string aviso = Assert.Single(reporte.advertencias());
        Assert.Contains("7 pago(s) de tipo NOTA DE CREDITO", aviso);
        Assert.Contains("script supervisado", aviso);
    }

    [Fact]
    public void caso_error_varios_problemas_a_la_vez_se_reportan_todos()
    {
        db_diagnostic_report reporte = new db_diagnostic_report
        {
            clientes_sin_zona = 3,
            clientes_con_codigo_invalido = 2,
            notas_pro_venta_sin_relacion = 1,
            tipos_de_nota_obsoletos = 3,
            pagos_tipo_nota_de_credito = 5,
            tipos_de_nota_activos = 6,
            lineas_de_producto_activas = 10
        };

        IReadOnlyList<string> avisos = reporte.advertencias();

        Assert.Equal(5, avisos.Count);
        Assert.True(reporte.hay_advertencias);
    }

    [Fact]
    public void los_tipos_obsoletos_que_se_vigilan_son_los_tres_conocidos()
    {
        Assert.Equal(new[] { "PRO", "CAN", "HLS" }, DbDiagnostics.CodigosDeTipoObsoletos);
    }

    // ---------- Regresion: el arranque ya no modifica datos ----------

    [Theory]
    [InlineData("migrate_legacy_series")]
    [InlineData("has_non_3digit_suffix")]
    [InlineData("migrate_customer_correlatives_to_global5")]
    [InlineData("cleanup_credit_note_payments_and_adjust_totals")]
    [InlineData("initialize_zonas_and_sellers")]
    public void regresion_DbInitializer_ya_no_tiene_los_metodos_que_modificaban_datos(string metodo)
    {
        // Estos corrian en CADA apertura de la app: borraban pagos, renumeraban notas y
        // clientes, y restauraban zonas eliminadas. Si alguno vuelve a DbInitializer,
        // esta prueba falla.
        MethodInfo? encontrado = typeof(DbInitializer).GetMethod(
            metodo,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

        Assert.Null(encontrado);
    }

    [Fact]
    public void regresion_DbInitializer_solo_expone_initialize()
    {
        string[] publicos = typeof(DbInitializer)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();

        Assert.Equal(new[] { "initialize" }, publicos);
    }

    [Fact]
    public void el_sembrador_explica_por_que_se_niega_en_una_base_con_datos()
    {
        Assert.Contains("vacia", DbSeeder.MensajeBaseConDatos);
        Assert.Contains("real", DbSeeder.MensajeBaseConDatos);
    }
}
