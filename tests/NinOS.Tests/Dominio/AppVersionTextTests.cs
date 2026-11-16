using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class AppVersionTextTests
{
    [Fact]
    public void caso_feliz_version_1_0_11_se_muestra_como_NinOS_v1_0_11()
    {
        string texto = AppVersionText.format(new Version(1, 0, 11, 0));

        Assert.Equal("NinOS v1.0.11", texto);
    }

    [Fact]
    public void caso_limite_el_revision_no_aparece_en_el_texto()
    {
        string texto = AppVersionText.format(new Version(1, 0, 11, 7));

        Assert.Equal("NinOS v1.0.11", texto);
    }

    [Fact]
    public void caso_error_version_nula_lanza_excepcion()
    {
        Assert.Throws<ArgumentNullException>(() => AppVersionText.format(null));
    }

    [Fact]
    public void caso_error_version_sin_compilacion_lanza_excepcion()
    {
        Assert.Throws<ArgumentException>(() => AppVersionText.format(new Version(1, 0)));
    }
}
