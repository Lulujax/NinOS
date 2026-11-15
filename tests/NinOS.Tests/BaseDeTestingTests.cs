namespace NinOS.Tests;

/// <summary>
/// Pruebas del guardian que impide que una prueba toque la base de produccion.
/// Esta es la red de seguridad de todas las demas pruebas con base de datos.
/// </summary>
public class BaseDeTestingTests
{
    // ---------- Caso feliz ----------

    [Theory]
    [InlineData("Host=h;Database=ninos_db_testing;Username=u;Password=p;")]
    [InlineData("Host=h;Database=NINOS_DB_TESTING;Username=u;")]
    [InlineData("Database=mi_test;")]
    public void caso_feliz_reconoce_una_base_de_testing(string cadena)
    {
        Assert.True(BaseDeTesting.es_base_de_testing(cadena), $"Debia aceptarse: {cadena}");
    }

    [Fact]
    public void caso_feliz_saca_el_nombre_de_la_base()
    {
        Assert.Equal("ninos_db_testing",
            BaseDeTesting.nombre_de_la_base("Host=h;Port=5432;Database=ninos_db_testing;Username=u;"));
    }

    // ---------- Casos limite ----------

    [Fact]
    public void caso_limite_tolera_espacios_y_mayusculas_en_la_clave()
    {
        Assert.Equal("ninos_db_testing",
            BaseDeTesting.nombre_de_la_base("Host=h;  database = ninos_db_testing ;Username=u;"));
    }

    [Fact]
    public void caso_limite_una_cadena_sin_Database_no_es_de_testing()
    {
        Assert.Null(BaseDeTesting.nombre_de_la_base("Host=h;Username=u;Password=p;"));
        Assert.False(BaseDeTesting.es_base_de_testing("Host=h;Username=u;Password=p;"));
    }

    [Fact]
    public void caso_limite_Database_vacio_no_es_de_testing()
    {
        Assert.Null(BaseDeTesting.nombre_de_la_base("Host=h;Database=;Username=u;"));
        Assert.False(BaseDeTesting.es_base_de_testing("Host=h;Database=;Username=u;"));
    }

    // ---------- Casos de error: lo que NUNCA debe pasar ----------

    [Theory]
    [InlineData("Host=h;Database=ninos_db;Username=ninos_admin;Password=x;")]
    [InlineData("Host=h;Database=produccion;")]
    [InlineData("Host=h;Database=ninos_produccion;")]
    public void caso_error_rechaza_la_base_de_produccion(string cadena)
    {
        Assert.False(BaseDeTesting.es_base_de_testing(cadena),
            $"PELIGRO: esta cadena no es de testing y debia rechazarse: {cadena}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("basura sin formato")]
    public void caso_error_cadena_vacia_o_invalida_no_es_de_testing(string? cadena)
    {
        Assert.False(BaseDeTesting.es_base_de_testing(cadena));
    }
}
