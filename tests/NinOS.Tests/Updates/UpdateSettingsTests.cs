using NinOS.UI.Updates;

namespace NinOS.Tests.Updates;

public class UpdateSettingsTests
{
    [Fact]
    public void caso_feliz_una_direccion_https_se_acepta_y_termina_en_barra()
    {
        bool valida = UpdateSettings.try_parse_base_url("https://82.39.109.158/updates", out string base_url, out string reason);

        Assert.True(valida, $"Una direccion https valida debia aceptarse: {reason}");
        Assert.Equal("https://82.39.109.158/updates/", base_url);
    }

    [Fact]
    public void caso_limite_una_direccion_que_ya_termina_en_barra_no_se_duplica()
    {
        bool valida = UpdateSettings.try_parse_base_url("https://82.39.109.158/updates/", out string base_url, out _);

        Assert.True(valida);
        Assert.Equal("https://82.39.109.158/updates/", base_url);
    }

    [Fact]
    public void caso_error_http_sin_cifrar_es_rechazado()
    {
        bool valida = UpdateSettings.try_parse_base_url("http://82.39.109.158/updates", out string base_url, out string reason);

        Assert.False(valida, "Una direccion http sin cifrar debia rechazarse.");
        Assert.Equal(string.Empty, base_url);
        Assert.Contains("HTTPS", reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no es una direccion")]
    public void caso_error_vacia_o_invalida_es_rechazada(string? direccion)
    {
        bool valida = UpdateSettings.try_parse_base_url(direccion, out _, out string reason);

        Assert.False(valida, $"La direccion '{direccion}' debia rechazarse.");
        Assert.False(string.IsNullOrWhiteSpace(reason));
    }

    [Fact]
    public void caso_limite_seccion_updates_sin_baseurl_devuelve_nulo()
    {
        string? valor = UpdateSettings.read_base_url_from_json("{ \"Updates\": { } }");

        Assert.Null(valor);
    }

    [Fact]
    public void caso_feliz_lee_baseurl_desde_el_json()
    {
        string? valor = UpdateSettings.read_base_url_from_json("{ \"Updates\": { \"BaseUrl\": \"https://82.39.109.158/updates\" } }");

        Assert.Equal("https://82.39.109.158/updates", valor);
    }

    [Fact]
    public void caso_error_json_invalido_no_revienta()
    {
        string? valor = UpdateSettings.read_base_url_from_json("{ esto no es json");

        Assert.Null(valor);
    }
}
