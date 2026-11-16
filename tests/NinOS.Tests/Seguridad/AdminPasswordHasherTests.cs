using System;
using NinOS.Domain;

namespace NinOS.Tests.Seguridad;

public class AdminPasswordHasherTests
{
    private const string ClaveValida = "clave-de-prueba-123";
    private static readonly DateTime AhoraUtc = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void caso_feliz_la_clave_creada_coincide_con_la_misma_clave()
    {
        admin_key guardada = AdminPasswordHasher.create(ClaveValida, AhoraUtc);

        Assert.True(AdminPasswordHasher.matches(ClaveValida, guardada));
    }

    [Fact]
    public void caso_feliz_el_hash_no_contiene_la_clave_en_texto_plano()
    {
        admin_key guardada = AdminPasswordHasher.create(ClaveValida, AhoraUtc);

        Assert.DoesNotContain(ClaveValida, guardada.password_hash);
        Assert.DoesNotContain(ClaveValida, guardada.password_salt);
        Assert.Equal(AdminPasswordHasher.Iterations, guardada.iterations);
        Assert.Equal(admin_key.SingleRowId, guardada.id_key);
    }

    [Fact]
    public void caso_feliz_dos_claves_iguales_generan_sales_distintas()
    {
        admin_key primera = AdminPasswordHasher.create(ClaveValida, AhoraUtc);
        admin_key segunda = AdminPasswordHasher.create(ClaveValida, AhoraUtc);

        Assert.NotEqual(primera.password_salt, segunda.password_salt);
        Assert.NotEqual(primera.password_hash, segunda.password_hash);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(6)]
    [InlineData(0)]
    public void caso_limite_clave_mas_corta_que_el_minimo_se_rechaza(int largo)
    {
        string clave = new string('a', largo);

        Assert.Throws<ArgumentException>(() => AdminPasswordHasher.create(clave, AhoraUtc));
        Assert.False(AdminPasswordHasher.is_long_enough(clave));
    }

    [Fact]
    public void caso_limite_clave_del_largo_minimo_exacto_se_acepta()
    {
        string clave = new string('a', AdminPasswordHasher.MinimumLength);

        Assert.True(AdminPasswordHasher.is_long_enough(clave));
        Assert.True(AdminPasswordHasher.matches(clave, AdminPasswordHasher.create(clave, AhoraUtc)));
    }

    [Fact]
    public void caso_error_clave_incorrecta_no_coincide()
    {
        admin_key guardada = AdminPasswordHasher.create(ClaveValida, AhoraUtc);

        Assert.False(AdminPasswordHasher.matches("otra-clave-456-x", guardada));
    }

    [Fact]
    public void caso_error_sin_fila_guardada_no_coincide()
    {
        Assert.False(AdminPasswordHasher.matches(ClaveValida, null));
    }

    [Fact]
    public void caso_error_clave_vacia_no_coincide()
    {
        admin_key guardada = AdminPasswordHasher.create(ClaveValida, AhoraUtc);

        Assert.False(AdminPasswordHasher.matches(string.Empty, guardada));
        Assert.False(AdminPasswordHasher.matches(null, guardada));
    }

    [Fact]
    public void caso_error_fila_con_base64_danado_no_coincide()
    {
        admin_key guardada = AdminPasswordHasher.create(ClaveValida, AhoraUtc);
        guardada.password_hash = "esto no es base64!!";

        Assert.False(AdminPasswordHasher.matches(ClaveValida, guardada));
    }

    [Fact]
    public void caso_error_fila_con_sal_demasiado_corta_no_coincide()
    {
        admin_key guardada = AdminPasswordHasher.create(ClaveValida, AhoraUtc);
        guardada.password_salt = Convert.ToBase64String(new byte[3]);

        Assert.False(AdminPasswordHasher.matches(ClaveValida, guardada));
    }

    [Fact]
    public void caso_error_fila_con_cero_iteraciones_no_coincide()
    {
        admin_key guardada = AdminPasswordHasher.create(ClaveValida, AhoraUtc);
        guardada.iterations = 0;

        Assert.False(AdminPasswordHasher.matches(ClaveValida, guardada));
    }
}
