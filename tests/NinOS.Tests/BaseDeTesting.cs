using Microsoft.EntityFrameworkCore;
using NinOS.Infrastructure.Data;

namespace NinOS.Tests;

/// <summary>
/// Guardian de las pruebas que tocan la base de datos.
///
/// Regla del CLAUDE.md: una prueba que toca la base DEBE negarse a correr si la conexion no
/// apunta a una base de testing. Sin esto, una variable de entorno mal puesta haria que las
/// pruebas escribieran en PRODUCCION.
/// </summary>
public static class BaseDeTesting
{
    /// <summary>
    /// Funcion pura: decide si una cadena de conexion apunta a una base de pruebas.
    /// Solo acepta nombres de base que contengan "testing" o "test".
    /// </summary>
    public static bool es_base_de_testing(string? connection_string)
    {
        string? nombre = nombre_de_la_base(connection_string);
        if (nombre == null) return false;

        string minusculas = nombre.ToLowerInvariant();
        return minusculas.Contains("testing") || minusculas.Contains("test");
    }

    /// <summary>Saca el valor de Database= de la cadena. Devuelve null si no lo encuentra.</summary>
    public static string? nombre_de_la_base(string? connection_string)
    {
        if (string.IsNullOrWhiteSpace(connection_string)) return null;

        foreach (string parte in connection_string.Split(';'))
        {
            string[] mitades = parte.Split('=', 2);
            if (mitades.Length != 2) continue;

            if (mitades[0].Trim().Equals("Database", StringComparison.OrdinalIgnoreCase))
            {
                string valor = mitades[1].Trim();
                return valor.Length == 0 ? null : valor;
            }
        }

        return null;
    }

    /// <summary>
    /// Devuelve un contexto contra la base de testing, o null si no hay ninguna configurada
    /// (en ese caso la prueba de integracion se salta).
    /// Si hay conexion configurada pero NO es de testing, lanza excepcion: no se corre nada.
    /// </summary>
    public static NinOSDbContext? crear_contexto_o_null()
    {
        if (!DbConnectionFactory.TryGetConnectionString(out string? cadena, out _))
        {
            return null;
        }

        if (!es_base_de_testing(cadena))
        {
            throw new InvalidOperationException(
                "PRUEBAS DETENIDAS: la cadena de conexion configurada apunta a la base '"
                + (nombre_de_la_base(cadena) ?? "desconocida")
                + "', que no parece de testing. Las pruebas nunca corren contra produccion. "
                + "Defina NINOS_DB_CONNECTION apuntando a ninos_db_testing.");
        }

        DbContextOptionsBuilder<NinOSDbContext> opciones = new DbContextOptionsBuilder<NinOSDbContext>();
        opciones.UseNpgsql(cadena);

        return new NinOSDbContext(opciones.Options);
    }
}
