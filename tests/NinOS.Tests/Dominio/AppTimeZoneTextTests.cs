using System;
using NinOS.Domain;
using Xunit;

namespace NinOS.Tests.Dominio
{
    public class AppTimeZoneTextTests
    {
        [Fact]
        public void caso_feliz_nueve_de_la_noche_del_dia_12_sale_12()
        {
            DateTime utc = new DateTime(2026, 11, 13, 1, 0, 0, DateTimeKind.Utc);

            string texto = AppTimeZone.to_local_text(utc, "dd/MM/yyyy");

            Assert.True(texto == "12/11/2026", $"21:00 hora de Venezuela del dia 12 debia salir 12/11/2026 y salio '{texto}'.");
        }

        [Fact]
        public void caso_limite_ocho_de_la_noche_exacto_sigue_siendo_el_mismo_dia()
        {
            DateTime utc = new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);

            string texto = AppTimeZone.to_local_text(utc, "dd/MM/yyyy");

            Assert.True(texto == "12/11/2026", $"20:00 exacto hora de Venezuela debia salir 12/11/2026 y salio '{texto}'.");
        }

        [Fact]
        public void caso_limite_medianoche_local_sale_el_dia_nuevo()
        {
            DateTime utc = new DateTime(2026, 11, 13, 4, 0, 0, DateTimeKind.Utc);

            string texto = AppTimeZone.to_local_text(utc, "dd/MM/yyyy");

            Assert.True(texto == "13/11/2026", $"La medianoche local del 13 debia salir 13/11/2026 y salio '{texto}'.");
        }

        [Fact]
        public void caso_limite_formato_dia_mes_corto()
        {
            DateTime utc = new DateTime(2026, 11, 13, 1, 0, 0, DateTimeKind.Utc);

            string texto = AppTimeZone.to_local_text(utc, "dd-MMM").Replace(".", "").ToLowerInvariant();

            Assert.True(texto == "12-nov", $"El formato corto debia ser 12-nov y salio '{texto}'.");
        }

        [Fact]
        public void caso_error_fecha_nula_devuelve_vacio()
        {
            Assert.True(AppTimeZone.to_local_text(null, "dd/MM/yyyy") == string.Empty, "Una fecha nula debia devolver texto vacio.");
        }

        [Fact]
        public void caso_error_fecha_por_defecto_devuelve_vacio()
        {
            Assert.True(AppTimeZone.to_local_text(default(DateTime), "dd/MM/yyyy") == string.Empty, "Una fecha sin valor (DateTime.MinValue) debia devolver texto vacio.");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void caso_error_formato_vacio_lanza_excepcion(string? formato)
        {
            DateTime utc = new DateTime(2026, 11, 13, 1, 0, 0, DateTimeKind.Utc);

            Assert.Throws<ArgumentException>(() => AppTimeZone.to_local_text(utc, formato!));
        }
    }
}
