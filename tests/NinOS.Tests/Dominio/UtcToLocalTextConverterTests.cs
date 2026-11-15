using System.Globalization;
using NinOS.UI.Converters;

namespace NinOS.Tests.Dominio;

public class UtcToLocalTextConverterTests
{
    private static readonly UtcToLocalTextConverter Converter = new UtcToLocalTextConverter();

    private static object convertir(object valor, string? formato = null)
        => Converter.Convert(valor, typeof(string), formato!, CultureInfo.InvariantCulture);

    [Fact]
    public void caso_feliz_pago_a_las_9_pm_de_venezuela_sale_con_su_dia()
    {
        DateTime utc = new DateTime(2026, 11, 13, 1, 0, 0, DateTimeKind.Utc);

        object texto = convertir(utc);

        Assert.True("12/11/2026".Equals(texto), $"Las 9 pm del 12 en Venezuela (01:00 UTC del 13) debia mostrar 12/11/2026 y mostro '{texto}'.");
    }

    [Fact]
    public void caso_feliz_respeta_el_formato_con_hora()
    {
        DateTime utc = new DateTime(2026, 11, 13, 1, 30, 0, DateTimeKind.Utc);

        object texto = convertir(utc, "dd/MM/yyyy HH:mm");

        Assert.True("12/11/2026 21:30".Equals(texto), $"Con hora debia mostrar 12/11/2026 21:30 y mostro '{texto}'.");
    }

    [Fact]
    public void caso_limite_fecha_sin_valor_sale_vacia()
    {
        object texto = convertir(default(DateTime));

        Assert.True(string.Empty.Equals(texto), $"Una fecha sin valor debia salir vacia y salio '{texto}'.");
    }

    [Fact]
    public void caso_limite_justo_a_medianoche_utc_es_el_dia_anterior_en_venezuela()
    {
        DateTime utc = new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);

        object texto = convertir(utc);

        Assert.True("12/11/2026".Equals(texto), $"Medianoche UTC del 13 son las 8 pm del 12 en Venezuela; salio '{texto}'.");
    }

    [Fact]
    public void caso_error_un_valor_que_no_es_fecha_sale_vacio()
    {
        object texto = convertir("no soy una fecha");

        Assert.True(string.Empty.Equals(texto), $"Un valor que no es fecha debia salir vacio y salio '{texto}'.");
    }

    [Fact]
    public void caso_error_convertir_de_vuelta_no_esta_permitido()
    {
        Assert.Throws<NotSupportedException>(
            () => Converter.ConvertBack("12/11/2026", typeof(DateTime), null!, CultureInfo.InvariantCulture));
    }
}
