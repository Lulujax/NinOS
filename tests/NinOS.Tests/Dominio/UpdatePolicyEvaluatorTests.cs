using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class UpdatePolicyEvaluatorTests
{
    [Fact]
    public void caso_feliz_version_igual_a_la_recomendada_esta_al_dia()
    {
        update_status estado = UpdatePolicyEvaluator.evaluate("1.0.10", "1.0.0", "1.0.10");

        Assert.Equal(update_status.al_dia, estado);
    }

    [Fact]
    public void caso_feliz_version_menor_que_la_recomendada_pero_no_menor_que_la_minima_es_recomendada()
    {
        update_status estado = UpdatePolicyEvaluator.evaluate("1.0.9", "1.0.0", "1.0.10");

        Assert.Equal(update_status.recomendada, estado);
    }

    [Fact]
    public void caso_feliz_version_menor_que_la_minima_es_obligatoria()
    {
        update_status estado = UpdatePolicyEvaluator.evaluate("1.0.8", "1.0.9", "1.0.10");

        Assert.Equal(update_status.obligatoria, estado);
    }

    [Fact]
    public void caso_limite_igual_a_la_minima_pero_menor_que_la_recomendada_es_recomendada()
    {
        update_status estado = UpdatePolicyEvaluator.evaluate("1.0.9", "1.0.9", "1.0.10");

        Assert.Equal(update_status.recomendada, estado);
    }

    [Fact]
    public void caso_limite_igual_a_la_minima_y_a_la_recomendada_esta_al_dia()
    {
        update_status estado = UpdatePolicyEvaluator.evaluate("1.0.9", "1.0.9", "1.0.9");

        Assert.Equal(update_status.al_dia, estado);
    }

    [Fact]
    public void caso_limite_el_sufijo_de_compilacion_no_cambia_la_comparacion()
    {
        update_status estado = UpdatePolicyEvaluator.evaluate("1.0.10+9f7faa97", "1.0.0", "1.0.10");

        Assert.Equal(update_status.al_dia, estado);
    }

    [Fact]
    public void caso_error_version_actual_vacia_lanza_excepcion()
    {
        Assert.Throws<ArgumentException>(() => UpdatePolicyEvaluator.evaluate("", "1.0.0", "1.0.10"));
    }

    [Fact]
    public void caso_error_version_minima_nula_lanza_excepcion()
    {
        Assert.Throws<ArgumentException>(() => UpdatePolicyEvaluator.evaluate("1.0.10", null, "1.0.10"));
    }

    [Fact]
    public void caso_error_version_con_formato_invalido_lanza_excepcion()
    {
        Assert.Throws<ArgumentException>(() => UpdatePolicyEvaluator.evaluate("version-rara", "1.0.0", "1.0.10"));
    }
}
