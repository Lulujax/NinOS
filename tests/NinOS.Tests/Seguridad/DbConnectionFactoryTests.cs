using System.IO;
using System.Reflection;
using NinOS.Infrastructure.Data;

namespace NinOS.Tests.Seguridad;

/// <summary>
/// Spec 002, tarea 3: la cadena de conexion sale solo de la variable de entorno o del
/// appsettings.json local, y si no hay ninguna la app NO se conecta a produccion.
///
/// Las pruebas de esta clase cambian la variable de entorno y un archivo del directorio de
/// salida, asi que van todas en una sola clase (xUnit corre en serie dentro de una clase).
/// </summary>
public class DbConnectionFactoryTests : IDisposable
{
    private const string CadenaDePrueba = "Host=localhost;Port=5432;Database=prueba;Username=u;Password=p;";

    private readonly string? _variable_original;
    private readonly string _ruta_appsettings;
    private readonly string? _contenido_original;

    public DbConnectionFactoryTests()
    {
        _variable_original = Environment.GetEnvironmentVariable(DbConnectionFactory.EnvironmentVariableName);
        _ruta_appsettings = DbConnectionFactory.SettingsFilePath;
        _contenido_original = File.Exists(_ruta_appsettings) ? File.ReadAllText(_ruta_appsettings) : null;

        Environment.SetEnvironmentVariable(DbConnectionFactory.EnvironmentVariableName, null);
        if (File.Exists(_ruta_appsettings)) File.Delete(_ruta_appsettings);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(DbConnectionFactory.EnvironmentVariableName, _variable_original);

        if (_contenido_original != null) File.WriteAllText(_ruta_appsettings, _contenido_original);
        else if (File.Exists(_ruta_appsettings)) File.Delete(_ruta_appsettings);
    }

    private void escribir_appsettings(string contenido) => File.WriteAllText(_ruta_appsettings, contenido);

    private static string appsettings_con(string clave, string cadena)
        => ("{ 'ConnectionStrings': { '" + clave + "': '" + cadena + "' } }").Replace('\'', '"');

    // ---------- Caso feliz ----------

    [Fact]
    public void caso_feliz_toma_la_cadena_de_la_variable_de_entorno()
    {
        Environment.SetEnvironmentVariable(DbConnectionFactory.EnvironmentVariableName, CadenaDePrueba);

        bool encontrada = DbConnectionFactory.TryGetConnectionString(out string? cadena, out string origen);

        Assert.True(encontrada, "Con la variable de entorno definida debia encontrar la cadena.");
        Assert.Equal(CadenaDePrueba, cadena);
        Assert.Contains(DbConnectionFactory.EnvironmentVariableName, origen);
    }

    [Fact]
    public void caso_feliz_toma_la_cadena_del_appsettings()
    {
        escribir_appsettings(appsettings_con("NinOSDb", CadenaDePrueba));

        bool encontrada = DbConnectionFactory.TryGetConnectionString(out string? cadena, out string origen);

        Assert.True(encontrada, "Con appsettings.json valido debia encontrar la cadena.");
        Assert.Equal(CadenaDePrueba, cadena);
        Assert.Contains(DbConnectionFactory.SettingsFileName, origen);
    }

    [Fact]
    public void acepta_tambien_la_clave_DefaultConnection()
    {
        escribir_appsettings(appsettings_con("DefaultConnection", CadenaDePrueba));

        bool encontrada = DbConnectionFactory.TryGetConnectionString(out string? cadena, out _);

        Assert.True(encontrada, "DefaultConnection debia servir como alternativa a NinOSDb.");
        Assert.Equal(CadenaDePrueba, cadena);
    }

    [Fact]
    public void la_variable_de_entorno_manda_sobre_el_archivo()
    {
        escribir_appsettings(appsettings_con("NinOSDb", "Host=del-archivo;"));
        Environment.SetEnvironmentVariable(DbConnectionFactory.EnvironmentVariableName, "Host=de-la-variable;");

        DbConnectionFactory.TryGetConnectionString(out string? cadena, out _);

        Assert.Equal("Host=de-la-variable;", cadena);
    }

    // ---------- Casos limite ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void caso_limite_variable_vacia_o_con_espacios_se_ignora(string valor)
    {
        Environment.SetEnvironmentVariable(DbConnectionFactory.EnvironmentVariableName, valor);

        bool encontrada = DbConnectionFactory.TryGetConnectionString(out _, out _);

        Assert.False(encontrada, $"Una variable con el valor '{valor}' no es configuracion valida.");
    }

    [Fact]
    public void caso_limite_la_cadena_se_devuelve_sin_espacios_sobrantes()
    {
        Environment.SetEnvironmentVariable(DbConnectionFactory.EnvironmentVariableName, "   " + CadenaDePrueba + "   ");

        DbConnectionFactory.TryGetConnectionString(out string? cadena, out _);

        Assert.Equal(CadenaDePrueba, cadena);
    }

    [Theory]
    [InlineData("{ }")]
    [InlineData("{ 'ConnectionStrings': { } }")]
    [InlineData("{ 'ConnectionStrings': { 'Otra': 'algo' } }")]
    public void caso_limite_appsettings_sin_la_clave_esperada_no_devuelve_cadena(string contenido)
    {
        escribir_appsettings(contenido.Replace('\'', '"'));

        bool encontrada = DbConnectionFactory.TryGetConnectionString(out _, out _);

        Assert.False(encontrada, $"Este appsettings no tiene conexion y no debia dar una: {contenido}");
    }

    // ---------- Casos de error ----------

    [Fact]
    public void caso_error_sin_ninguna_configuracion_no_devuelve_cadena()
    {
        bool encontrada = DbConnectionFactory.TryGetConnectionString(out string? cadena, out string origen);

        Assert.False(encontrada, "Sin variable y sin archivo no debia haber cadena.");
        Assert.Null(cadena);
        Assert.Equal(string.Empty, origen);
    }

    [Fact]
    public void caso_error_sin_configuracion_lanza_excepcion_que_explica_donde_buscar()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => DbConnectionFactory.GetConnectionString());

        Assert.Contains(DbConnectionFactory.EnvironmentVariableName, error.Message);
        Assert.Contains(DbConnectionFactory.SettingsFileName, error.Message);
    }

    [Fact]
    public void caso_error_appsettings_con_json_invalido_no_revienta()
    {
        escribir_appsettings("{ esto no es json");

        bool encontrada = DbConnectionFactory.TryGetConnectionString(out _, out _);

        Assert.False(encontrada, "Un JSON invalido debia tratarse como 'sin configuracion', no reventar.");
    }

    // ---------- Regresion de seguridad ----------

    [Fact]
    public void regresion_la_clase_no_trae_ninguna_cadena_de_conexion_escrita_en_el_codigo()
    {
        // Antes existia DefaultConnectionString apuntando a la base de PRODUCCION, con la
        // contrasena incluida, y el repositorio es publico. Esta prueba existe para que
        // nadie la vuelva a agregar sin darse cuenta.
        List<string> sospechosos = new List<string>();

        foreach (FieldInfo campo in typeof(DbConnectionFactory).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (campo.GetValue(null) is string valor && parece_cadena_de_conexion(valor))
                sospechosos.Add(campo.Name);
        }

        foreach (PropertyInfo propiedad in typeof(DbConnectionFactory).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (propiedad.GetValue(null) is string valor && parece_cadena_de_conexion(valor))
                sospechosos.Add(propiedad.Name);
        }

        Assert.True(sospechosos.Count == 0,
            "Estos miembros parecen tener una cadena de conexion escrita en el codigo: "
            + string.Join(", ", sospechosos));
    }

    private static bool parece_cadena_de_conexion(string valor)
    {
        return valor.Contains("Password=", StringComparison.OrdinalIgnoreCase)
            || valor.Contains("Username=", StringComparison.OrdinalIgnoreCase)
            || valor.Contains("Host=", StringComparison.OrdinalIgnoreCase);
    }
}
