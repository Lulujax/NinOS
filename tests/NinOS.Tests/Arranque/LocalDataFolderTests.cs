using System;
using System.IO;
using NinOS.Infrastructure.Data;
using Xunit;

namespace NinOS.Tests.Arranque;

public class LocalDataFolderTests : IDisposable
{
    private readonly string _raiz_temporal;

    public LocalDataFolderTests()
    {
        _raiz_temporal = Path.Combine(Path.GetTempPath(), "ninos-pruebas-datos", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_raiz_temporal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_raiz_temporal))
        {
            Directory.Delete(_raiz_temporal, recursive: true);
        }
    }

    [Fact]
    public void En_instalacion_de_velopack_usa_la_carpeta_de_arriba_de_current()
    {
        string raiz = @"C:\Users\prueba\AppData\Local\NinOS";
        string programa = Path.Combine(raiz, "current") + Path.DirectorySeparatorChar;

        string resultado = LocalDataFolder.resolve_data_directory(programa, ruta => ruta == Path.Combine(raiz, "Update.exe"));

        Assert.True(resultado == raiz, $"Se esperaba {raiz} y salio {resultado}");
    }

    [Fact]
    public void Carpeta_current_sin_Update_exe_no_se_considera_velopack()
    {
        string programa = @"C:\proyectos\current";

        string resultado = LocalDataFolder.resolve_data_directory(programa, _ => false);

        Assert.True(resultado == programa, $"Sin Update.exe debe quedarse en {programa} y salio {resultado}");
    }

    [Fact]
    public void Fuera_de_velopack_usa_la_carpeta_del_programa()
    {
        string programa = @"C:\Program Files\NinOS\";

        string resultado = LocalDataFolder.resolve_data_directory(programa, _ => true);

        Assert.True(resultado == @"C:\Program Files\NinOS", $"Instalacion normal: salio {resultado}");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Carpeta_del_programa_vacia_lanza_error(string programa)
    {
        Assert.Throws<ArgumentException>(() => LocalDataFolder.resolve_data_directory(programa, _ => false));
    }

    [Fact]
    public void Sin_funcion_de_existencia_lanza_error()
    {
        Assert.Throws<ArgumentNullException>(() => LocalDataFolder.resolve_data_directory(@"C:\x", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(@"carpeta\appsettings.json")]
    [InlineData(@"C:\appsettings.json")]
    public void Nombre_de_archivo_invalido_lanza_error(string nombre)
    {
        Assert.Throws<ArgumentException>(() => LocalDataFolder.file_path(nombre));
    }

    [Fact]
    public void Copia_el_archivo_viejo_si_no_existe_en_la_carpeta_de_datos()
    {
        string programa = crear_carpeta("current");
        File.WriteAllText(Path.Combine(programa, "appsettings.json"), "{\"viejo\":true}");

        LegacyCopyResult resultado = LocalDataFolder.copy_legacy_file(programa, _raiz_temporal, "appsettings.json");

        Assert.True(resultado == LegacyCopyResult.Copied, $"Se esperaba Copied y salio {resultado}");
        Assert.Equal("{\"viejo\":true}", File.ReadAllText(Path.Combine(_raiz_temporal, "appsettings.json")));
    }

    [Fact]
    public void No_pisa_el_archivo_que_ya_esta_en_la_carpeta_de_datos()
    {
        string programa = crear_carpeta("current");
        File.WriteAllText(Path.Combine(programa, "appsettings.json"), "viejo");
        File.WriteAllText(Path.Combine(_raiz_temporal, "appsettings.json"), "nuevo");

        LegacyCopyResult resultado = LocalDataFolder.copy_legacy_file(programa, _raiz_temporal, "appsettings.json");

        Assert.True(resultado == LegacyCopyResult.AlreadyInDataFolder, $"Se esperaba AlreadyInDataFolder y salio {resultado}");
        Assert.Equal("nuevo", File.ReadAllText(Path.Combine(_raiz_temporal, "appsettings.json")));
    }

    [Fact]
    public void Sin_archivo_viejo_no_copia_nada()
    {
        string programa = crear_carpeta("current");

        LegacyCopyResult resultado = LocalDataFolder.copy_legacy_file(programa, _raiz_temporal, "appsettings.json");

        Assert.True(resultado == LegacyCopyResult.NothingToCopy, $"Se esperaba NothingToCopy y salio {resultado}");
        Assert.False(File.Exists(Path.Combine(_raiz_temporal, "appsettings.json")));
    }

    [Fact]
    public void Misma_carpeta_no_intenta_copiar()
    {
        File.WriteAllText(Path.Combine(_raiz_temporal, "appsettings.json"), "x");

        LegacyCopyResult resultado = LocalDataFolder.copy_legacy_file(_raiz_temporal, _raiz_temporal, "appsettings.json");

        Assert.True(resultado == LegacyCopyResult.SameFolder, $"Se esperaba SameFolder y salio {resultado}");
    }

    [Fact]
    public void Destino_bloqueado_devuelve_fallo_sin_lanzar()
    {
        string programa = crear_carpeta("current");
        File.WriteAllText(Path.Combine(programa, "appsettings.json"), "x");
        string destino_que_es_archivo = Path.Combine(_raiz_temporal, "no-es-carpeta");
        File.WriteAllText(destino_que_es_archivo, "soy un archivo");

        LegacyCopyResult resultado = LocalDataFolder.copy_legacy_file(programa, destino_que_es_archivo, "appsettings.json");

        Assert.True(resultado == LegacyCopyResult.Failed, $"Se esperaba Failed y salio {resultado}");
    }

    [Theory]
    [InlineData("", "destino", "a.json")]
    [InlineData("origen", "", "a.json")]
    [InlineData("origen", "destino", "")]
    public void Copia_con_datos_vacios_lanza_error(string origen, string destino, string nombre)
    {
        Assert.Throws<ArgumentException>(() => LocalDataFolder.copy_legacy_file(origen, destino, nombre));
    }

    [Fact]
    public void Copia_los_respaldos_que_faltan_y_respeta_los_que_ya_estan()
    {
        string respaldos_viejos = crear_carpeta(Path.Combine("current", "Backups"));
        string respaldos_nuevos = crear_carpeta("Backups");
        File.WriteAllText(Path.Combine(respaldos_viejos, "r1.dump"), "uno");
        File.WriteAllText(Path.Combine(respaldos_viejos, "r2.dump"), "viejo");
        File.WriteAllText(Path.Combine(respaldos_nuevos, "r2.dump"), "nuevo");

        int copiados = LocalDataFolder.copy_legacy_backups(respaldos_viejos, respaldos_nuevos);

        Assert.True(copiados == 1, $"Se esperaba 1 respaldo copiado y salieron {copiados}");
        Assert.Equal("nuevo", File.ReadAllText(Path.Combine(respaldos_nuevos, "r2.dump")));
        Assert.True(File.Exists(Path.Combine(respaldos_nuevos, "r1.dump")));
    }

    [Fact]
    public void Sin_carpeta_de_respaldos_vieja_no_copia_nada()
    {
        int copiados = LocalDataFolder.copy_legacy_backups(Path.Combine(_raiz_temporal, "no-existe"), Path.Combine(_raiz_temporal, "Backups"));

        Assert.Equal(0, copiados);
    }

    [Fact]
    public void Respaldos_con_rutas_vacias_lanza_error()
    {
        Assert.Throws<ArgumentException>(() => LocalDataFolder.copy_legacy_backups("", "destino"));
    }

    [Fact]
    public void En_pruebas_la_conexion_se_busca_en_la_carpeta_de_datos()
    {
        Assert.StartsWith(LocalDataFolder.DataDirectory, DbConnectionFactory.SettingsFilePath);
    }

    private string crear_carpeta(string relativa)
    {
        string ruta = Path.Combine(_raiz_temporal, relativa);
        Directory.CreateDirectory(ruta);
        return ruta;
    }
}
