using NinOS.Domain;

namespace NinOS.Tests.Dominio;

/// <summary>
/// Regla del codigo de cliente: correlativo global de 5 digitos.
/// La usa el diagnostico del arranque para avisar de codigos fuera de formato, en vez de
/// renumerar a todos los clientes como hacia antes.
/// </summary>
public class CustomerCodeRulesTests
{
    // ---------- Caso feliz ----------

    [Theory]
    [InlineData("00001")]
    [InlineData("00302")]
    [InlineData("99999")]
    public void caso_feliz_codigo_de_cinco_digitos_es_valido(string codigo)
    {
        Assert.True(customer_code_rules.es_valido(codigo), $"'{codigo}' tiene 5 digitos y debia ser valido.");
    }

    [Fact]
    public void caso_feliz_ignora_espacios_alrededor()
    {
        Assert.True(customer_code_rules.es_valido("  00042  "));
    }

    // ---------- Casos limite ----------

    [Theory]
    [InlineData("00000")]
    [InlineData("10000")]
    public void caso_limite_los_extremos_del_rango_son_validos(string codigo)
    {
        Assert.True(customer_code_rules.es_valido(codigo));
    }

    [Theory]
    [InlineData("0001")]
    [InlineData("000001")]
    public void caso_limite_un_digito_de_mas_o_de_menos_es_invalido(string codigo)
    {
        Assert.False(customer_code_rules.es_valido(codigo),
            $"'{codigo}' no tiene exactamente {customer_code_rules.CodeDigits} digitos.");
    }

    // ---------- Casos de error ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void caso_error_vacio_o_nulo_es_invalido(string? codigo)
    {
        Assert.False(customer_code_rules.es_valido(codigo));
    }

    [Theory]
    [InlineData("3200_5")]
    [InlineData("ABCDE")]
    [InlineData("001-A")]
    [InlineData("12 34")]
    [InlineData("1234A")]
    public void caso_error_codigos_del_sistema_anterior_o_con_letras_son_invalidos(string codigo)
    {
        Assert.False(customer_code_rules.es_valido(codigo),
            $"'{codigo}' no es un correlativo de 5 digitos y debia marcarse como invalido.");
    }
}
