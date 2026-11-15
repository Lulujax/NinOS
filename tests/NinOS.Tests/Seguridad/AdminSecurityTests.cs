using System.IO;
using System.Reflection;
using NinOS.UI.Common;

namespace NinOS.Tests.Seguridad;

/// <summary>
/// Spec 002, tarea 7: el Panel Admin no tiene clave por defecto. Si el archivo con la clave
/// falta o esta danado, el panel queda cerrado en vez de regenerarse con una clave escrita
/// en el codigo del repositorio publico.
///
/// AdminSecurity guarda el intento fallido en memoria y bloquea tras varios, asi que cada
/// prueba empieza llamando a change_password, que reinicia esos contadores.
/// </summary>
public class AdminSecurityTests : IDisposable
{
    private const string ClaveValida = "clave-de-prueba-123";

    private readonly string _ruta_config;
    private readonly string? _contenido_original;

    public AdminSecurityTests()
    {
        _ruta_config = AdminSecurity.ConfigPath;
        _contenido_original = File.Exists(_ruta_config) ? File.ReadAllText(_ruta_config) : null;
    }

    public void Dispose()
    {
        if (_contenido_original != null) File.WriteAllText(_ruta_config, _contenido_original);
        else if (File.Exists(_ruta_config)) File.Delete(_ruta_config);
    }

    /// <summary>Deja una clave conocida guardada y los contadores de intentos en cero.</summary>
    private void partir_de_clave_conocida()
    {
        Assert.True(AdminSecurity.change_password(ClaveValida),
            "No se pudo preparar la prueba: change_password fallo con una clave valida.");
    }

    // ---------- Caso feliz ----------

    [Fact]
    public void caso_feliz_guarda_la_clave_y_la_acepta()
    {
        partir_de_clave_conocida();

        Assert.True(AdminSecurity.verify(ClaveValida), "La clave recien guardada debia ser aceptada.");
    }

    [Fact]
    public void caso_feliz_la_clave_se_guarda_como_hash_y_no_en_texto_plano()
    {
        partir_de_clave_conocida();

        string guardado = File.ReadAllText(_ruta_config);

        Assert.DoesNotContain(ClaveValida, guardado);
        Assert.Contains("password_hash", guardado);
        Assert.Contains("password_salt", guardado);
    }

    [Fact]
    public void caso_feliz_cambiar_la_clave_invalida_la_anterior()
    {
        partir_de_clave_conocida();
        Assert.True(AdminSecurity.change_password("otra-clave-456"));

        Assert.False(AdminSecurity.verify(ClaveValida), "La clave vieja no debia seguir sirviendo.");
        Assert.True(AdminSecurity.verify("otra-clave-456"), "La clave nueva debia servir.");
    }

    [Fact]
    public void is_configured_responde_segun_exista_el_archivo()
    {
        partir_de_clave_conocida();
        Assert.True(AdminSecurity.is_configured(), "Con el archivo guardado debia estar configurada.");

        File.Delete(_ruta_config);
        Assert.False(AdminSecurity.is_configured(), "Sin el archivo no debia estar configurada.");
    }

    // ---------- Casos limite ----------

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    public void caso_limite_clave_vacia_o_demasiado_corta_se_rechaza(string clave)
    {
        partir_de_clave_conocida();

        bool guardada = AdminSecurity.change_password(clave);

        Assert.False(guardada, $"La clave '{clave}' es mas corta que el minimo y no debia guardarse.");
        Assert.True(AdminSecurity.verify(ClaveValida), "Al rechazarse la nueva, la anterior debia seguir valiendo.");
    }

    [Fact]
    public void caso_limite_clave_del_largo_minimo_exacto_se_acepta()
    {
        string clave = new string('a', AdminSecurity.MinimumPasswordLength);

        Assert.True(AdminSecurity.change_password(clave),
            $"Una clave de exactamente {AdminSecurity.MinimumPasswordLength} caracteres debia aceptarse.");
        Assert.True(AdminSecurity.verify(clave));
    }

    // ---------- Casos de error ----------

    [Fact]
    public void caso_error_clave_incorrecta_no_entra()
    {
        partir_de_clave_conocida();

        Assert.False(AdminSecurity.verify("la-que-no-es"), "Una clave incorrecta no debia entrar.");
    }

    [Fact]
    public void caso_error_sin_archivo_de_clave_el_panel_queda_cerrado()
    {
        partir_de_clave_conocida();
        File.Delete(_ruta_config);

        Assert.False(AdminSecurity.verify(ClaveValida),
            "Sin archivo de clave, el panel debia quedar cerrado y NO regenerar una clave por defecto.");
        Assert.False(File.Exists(_ruta_config),
            "Verificar sin archivo no debia crear uno nuevo con una clave por defecto.");
    }

    [Fact]
    public void caso_error_archivo_danado_deja_el_panel_cerrado()
    {
        partir_de_clave_conocida();
        File.WriteAllText(_ruta_config, "{ esto no es json valido");

        Assert.False(AdminSecurity.verify(ClaveValida), "Con el archivo danado el panel debia quedar cerrado.");
        Assert.False(AdminSecurity.is_configured(), "Un archivo danado no cuenta como configurado.");
    }

    [Fact]
    public void caso_error_archivo_sin_los_campos_esperados_deja_el_panel_cerrado()
    {
        partir_de_clave_conocida();
        File.WriteAllText(_ruta_config, "{ \"otra_cosa\": 1 }");

        Assert.False(AdminSecurity.verify(ClaveValida));
        Assert.False(AdminSecurity.is_configured());
    }

    // ---------- Regresion de seguridad ----------

    [Fact]
    public void regresion_no_existe_ninguna_clave_por_defecto_en_el_codigo()
    {
        // Antes habia un DefaultPassword con la clave real escrita en el codigo, y el
        // repositorio es publico: cualquiera podia leerla y entrar al Panel Admin.
        Type tipo = typeof(AdminSecurity);

        Assert.Null(tipo.GetField("DefaultPassword", BindingFlags.Public | BindingFlags.Static));
        Assert.Null(tipo.GetProperty("DefaultPassword", BindingFlags.Public | BindingFlags.Static));

        List<string> sospechosos = new List<string>();
        foreach (FieldInfo campo in tipo.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (campo.GetValue(null) is string valor && parece_clave(valor))
                sospechosos.Add($"{campo.Name} = '{valor}'");
        }

        Assert.True(sospechosos.Count == 0,
            "Estos miembros publicos parecen contener una clave escrita en el codigo: "
            + string.Join(", ", sospechosos));
    }

    private static bool parece_clave(string valor)
    {
        if (valor.Length < 4 || valor.Length > 40) return false;
        if (valor.Contains(' ')) return false;

        foreach (char caracter in valor)
        {
            if (!char.IsLetterOrDigit(caracter)) return false;
        }

        return true;
    }
}
