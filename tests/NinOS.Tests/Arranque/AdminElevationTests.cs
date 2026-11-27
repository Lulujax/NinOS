using System;
using System.Diagnostics;
using NinOS.UI.Common;
using Xunit;

namespace NinOS.Tests.Arranque;

public class AdminElevationTests
{
    [Fact]
    public void App_instalada_sin_permisos_se_reabre()
    {
        Assert.True(AdminElevation.must_relaunch_elevated(is_admin: false, is_installed_app: true));
    }

    [Fact]
    public void App_instalada_con_permisos_no_se_reabre()
    {
        Assert.False(AdminElevation.must_relaunch_elevated(is_admin: true, is_installed_app: true));
    }

    [Fact]
    public void En_desarrollo_nunca_pide_permisos()
    {
        Assert.False(AdminElevation.must_relaunch_elevated(is_admin: false, is_installed_app: false));
    }

    [Theory]
    [InlineData("--set-admin-password", "--set-admin-password")]
    [InlineData("con espacio", "\"con espacio\"")]
    [InlineData("", "\"\"")]
    [InlineData("di \"hola\"", "\"di \\\"hola\\\"\"")]
    [InlineData(@"C:\carpeta con espacio\", "\"C:\\carpeta con espacio\\\\\"")]
    public void Cita_los_argumentos_para_que_lleguen_iguales(string argumento, string esperado)
    {
        string citado = AdminElevation.quote_argument(argumento);

        Assert.True(citado == esperado, $"Argumento [{argumento}]: se esperaba [{esperado}] y salio [{citado}]");
    }

    [Fact]
    public void Argumento_nulo_lanza_error()
    {
        Assert.Throws<ArgumentNullException>(() => AdminElevation.quote_argument(null!));
    }

    [Fact]
    public void Arranque_elevado_usa_runas_y_pasa_los_argumentos()
    {
        ProcessStartInfo inicio = AdminElevation.build_elevated_start(@"C:\NinOS\current\NinOS.UI.exe", new[] { "--a", "b c" });

        Assert.True(inicio.UseShellExecute);
        Assert.Equal("runas", inicio.Verb);
        Assert.Equal("--a \"b c\"", inicio.Arguments);
        Assert.Equal(@"C:\NinOS\current", inicio.WorkingDirectory);
    }

    [Fact]
    public void Arranque_elevado_sin_argumentos_queda_vacio()
    {
        ProcessStartInfo inicio = AdminElevation.build_elevated_start(@"C:\NinOS\NinOS.UI.exe", Array.Empty<string>());

        Assert.Equal(string.Empty, inicio.Arguments);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Arranque_elevado_sin_ejecutable_lanza_error(string ruta)
    {
        Assert.Throws<ArgumentException>(() => AdminElevation.build_elevated_start(ruta, Array.Empty<string>()));
    }

    [Fact]
    public void Arranque_elevado_sin_lista_de_argumentos_lanza_error()
    {
        Assert.Throws<ArgumentNullException>(() => AdminElevation.build_elevated_start(@"C:\x.exe", null!));
    }
}
