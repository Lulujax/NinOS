using System.Reflection;
using NinOS.UI.Common;

namespace NinOS.Tests.Seguridad;

public class AdminSecurityTests
{
    [Fact]
    public void regresion_no_existe_ninguna_clave_por_defecto_en_el_codigo()
    {
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

    [Fact]
    public void caso_feliz_el_minimo_de_la_clave_es_doce_caracteres()
    {
        Assert.Equal(12, AdminSecurity.MinimumPasswordLength);
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
