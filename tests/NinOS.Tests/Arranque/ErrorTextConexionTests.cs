using System.IO;
using System.Net.Sockets;
using NinOS.UI.Common;

namespace NinOS.Tests.Arranque;

public class ErrorTextConexionTests
{
    [Fact]
    public void Sin_internet_dice_que_no_hay_conexion_a_internet()
    {
        string texto = ErrorText.Get(new SocketException(11001, "No such host is known"));

        Assert.Contains("No hay conexión a internet", texto);
    }

    [Fact]
    public void Tiempo_de_espera_agotado_dice_conexion_lenta()
    {
        string texto = ErrorText.Get(new TimeoutException("Timeout during reading attempt"));

        Assert.Contains("lenta", texto);
    }

    [Fact]
    public void Conexion_cortada_dice_inestable_o_se_corto()
    {
        string texto = ErrorText.Get(new InvalidOperationException("connection reset by peer"));

        Assert.Contains("inestable o se cortó", texto);
    }

    [Theory]
    [InlineData("No such host is known")]
    [InlineData("connection refused")]
    [InlineData("Timeout during reading attempt")]
    [InlineData("password authentication failed")]
    [InlineData("database \"x\" does not exist")]
    [InlineData("certificate verify failed")]
    [InlineData("server closed the connection unexpectedly")]
    [InlineData("npgsql fallo desconocido")]
    public void Ningun_mensaje_de_conexion_menciona_base_de_datos(string error_tecnico)
    {
        string texto = ErrorText.Get(new InvalidOperationException(error_tecnico));

        Assert.DoesNotContain("base de datos", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Conexion_cortada_por_el_servidor_en_espanol_no_manda_al_administrador()
    {
        Exception error = new InvalidOperationException(
            "An exception has been raised that is likely due to a transient failure.",
            new IOException("Unable to read data from the transport connection: Se ha forzado la interrupción de una conexión existente por el host remoto."));

        string texto = ErrorText.Get(error);

        Assert.Contains("inestable o se cortó", texto);
        Assert.DoesNotContain("administrador", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fallo_transitorio_de_lectura_no_manda_al_administrador()
    {
        string texto = ErrorText.Get(new InvalidOperationException("Exception while reading from stream"));

        Assert.DoesNotContain("administrador", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Excepcion_nula_devuelve_mensaje_generico()
    {
        string texto = ErrorText.Get(null);

        Assert.Contains("error inesperado", texto);
    }
}
