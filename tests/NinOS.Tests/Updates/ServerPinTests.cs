using System.Collections.Generic;
using NinOS.Domain.Updates;

namespace NinOS.Tests.Updates;

public class ServerPinTests
{
    private const string HUELLA_ACTUAL = "CaNxkh8HXtRJlcTxYcBEgRJVNkru0ZNZHZz9NikSz1c=";
    private const string HUELLA_RESPALDO = "qKpO03hTCONttn2uu9wYrVOieVa1Q54acDbsFLMHOo4=";
    private const string HUELLA_INVENTADA = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    [Fact]
    public void caso_feliz_la_huella_actual_es_aceptada()
    {
        bool aceptada = ServerPin.is_allowed(HUELLA_ACTUAL, ServerPin.default_pins);

        Assert.True(aceptada, "La huella actual del servidor debia ser aceptada.");
    }

    [Fact]
    public void caso_limite_la_huella_de_respaldo_tambien_es_aceptada()
    {
        bool aceptada = ServerPin.is_allowed(HUELLA_RESPALDO, ServerPin.default_pins);

        Assert.True(aceptada, "La huella de respaldo debia ser aceptada.");
    }

    [Fact]
    public void caso_limite_espacios_y_prefijo_sha256_no_cambian_el_resultado()
    {
        string con_adornos = "  sha256//" + HUELLA_ACTUAL + " ";

        bool aceptada = ServerPin.is_allowed(con_adornos, ServerPin.default_pins);

        Assert.True(aceptada, $"La huella con prefijo y espacios debia normalizarse y aceptarse: '{con_adornos}'.");
    }

    [Fact]
    public void caso_limite_la_forma_hexadecimal_de_la_misma_huella_es_aceptada()
    {
        string hex = System.Convert.ToHexString(System.Convert.FromBase64String(HUELLA_ACTUAL));

        string? normalizada = ServerPin.normalize(hex);

        Assert.Equal(HUELLA_ACTUAL, normalizada);
    }

    [Fact]
    public void caso_limite_la_comparacion_distingue_mayusculas_en_base64()
    {
        string cambiada = HUELLA_ACTUAL.ToUpperInvariant();

        bool aceptada = ServerPin.is_allowed(cambiada, ServerPin.default_pins);

        Assert.False(aceptada, "Cambiar mayusculas de un base64 da otra huella: debia rechazarse.");
    }

    [Fact]
    public void caso_error_una_huella_distinta_es_rechazada()
    {
        bool aceptada = ServerPin.is_allowed(HUELLA_INVENTADA, ServerPin.default_pins);

        Assert.False(aceptada, "Una huella que no esta en la lista debia rechazarse.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-es-una-huella")]
    public void caso_error_huella_vacia_o_invalida_es_rechazada(string? huella)
    {
        bool aceptada = ServerPin.is_allowed(huella, ServerPin.default_pins);

        Assert.False(aceptada, $"La huella invalida '{huella}' debia rechazarse.");
    }

    [Fact]
    public void caso_error_sin_huellas_permitidas_lanza_excepcion()
    {
        Assert.Throws<ArgumentException>(() => ServerPin.is_allowed(HUELLA_ACTUAL, new List<string>()));
    }

    [Fact]
    public void caso_error_huella_permitida_mal_formada_lanza_excepcion()
    {
        Assert.Throws<ArgumentException>(() => ServerPin.is_allowed(HUELLA_ACTUAL, new List<string> { "basura" }));
    }

    [Fact]
    public void caso_error_certificado_nulo_no_se_acepta()
    {
        bool aceptada = ServerPin.matches_default(null);

        Assert.False(aceptada, "Sin certificado no debia aceptarse el servidor.");
    }
}
