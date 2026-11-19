using System;
using NinOS.Domain;
using Xunit;

namespace NinOS.Tests.Dominio
{
    public class RangoFechasTests
    {
        [Fact]
        public void mes_actual_va_del_primer_al_ultimo_dia_del_mes()
        {
            var rango = rango_fechas.mes_actual(new DateTime(2026, 11, 19));

            Assert.Equal(new DateTime(2026, 11, 1), rango.desde);
            Assert.Equal(new DateTime(2026, 11, 30), rango.hasta);
        }

        [Fact]
        public void mes_actual_de_febrero_bisiesto_termina_el_29()
        {
            var rango = rango_fechas.mes_actual(new DateTime(2028, 2, 10));

            Assert.Equal(new DateTime(2028, 2, 29), rango.hasta);
        }

        [Fact]
        public void contiene_incluye_los_extremos_completos()
        {
            var rango = new rango_fechas(new DateTime(2026, 1, 15), new DateTime(2026, 12, 3));

            Assert.True(rango.contiene(AppTimeZone.to_utc(new DateTime(2026, 1, 15, 0, 0, 0))));
            Assert.True(rango.contiene(AppTimeZone.to_utc(new DateTime(2026, 12, 3, 23, 59, 0))));
            Assert.False(rango.contiene(AppTimeZone.to_utc(new DateTime(2026, 1, 14, 23, 59, 0))));
            Assert.False(rango.contiene(AppTimeZone.to_utc(new DateTime(2026, 12, 4, 0, 0, 0))));
        }

        [Fact]
        public void contiene_usa_la_hora_de_venezuela_y_no_utc()
        {
            var rango = new rango_fechas(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

            Assert.True(rango.contiene(new DateTime(2026, 4, 1, 2, 0, 0, DateTimeKind.Utc)));
            Assert.False(rango.contiene(new DateTime(2026, 4, 1, 5, 0, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public void sin_limites_contiene_cualquier_fecha()
        {
            var rango = rango_fechas.sin_limites();

            Assert.True(rango.contiene(new DateTime(2001, 5, 5, 12, 0, 0, DateTimeKind.Utc)));
            Assert.True(rango.contiene(new DateTime(2099, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public void desde_vacio_no_pone_inicio_y_hasta_vacio_no_pone_fin()
        {
            var solo_hasta = new rango_fechas(null, new DateTime(2026, 6, 30));
            var solo_desde = new rango_fechas(new DateTime(2026, 6, 1), null);

            Assert.True(solo_hasta.contiene(new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
            Assert.False(solo_hasta.contiene(new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc)));
            Assert.True(solo_desde.contiene(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
            Assert.False(solo_desde.contiene(new DateTime(2026, 5, 31, 12, 0, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public void rango_que_cruza_el_anio_incluye_ambos_anios()
        {
            var rango = new rango_fechas(new DateTime(2025, 11, 15), new DateTime(2026, 2, 20));

            Assert.True(rango.contiene(new DateTime(2025, 12, 31, 12, 0, 0, DateTimeKind.Utc)));
            Assert.True(rango.contiene(new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc)));
            Assert.False(rango.contiene(new DateTime(2026, 2, 21, 12, 0, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public void desde_posterior_a_hasta_lanza_excepcion_con_mensaje_claro()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new rango_fechas(new DateTime(2026, 5, 10), new DateTime(2026, 5, 9)));

            Assert.Contains("Desde no puede ser posterior", ex.Message);
        }

        [Fact]
        public void anio_invalido_lanza_excepcion()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new rango_fechas(new DateTime(1800, 1, 1), null));
        }

        [Fact]
        public void se_cruza_cuando_la_semana_toca_un_solo_dia_del_rango()
        {
            var rango = new rango_fechas(new DateTime(2026, 4, 1), new DateTime(2026, 4, 30));

            Assert.True(rango.se_cruza_con(new DateTime(2026, 3, 30), new DateTime(2026, 4, 5)));
            Assert.False(rango.se_cruza_con(new DateTime(2026, 3, 23), new DateTime(2026, 3, 29)));
            Assert.False(rango.se_cruza_con(new DateTime(2026, 5, 1), new DateTime(2026, 5, 7)));
        }

        [Fact]
        public void se_cruza_con_fin_anterior_al_inicio_lanza_excepcion()
        {
            var rango = rango_fechas.sin_limites();

            Assert.Throws<ArgumentException>(() =>
                rango.se_cruza_con(new DateTime(2026, 4, 5), new DateTime(2026, 3, 30)));
        }

        [Fact]
        public void mes_unico_solo_existe_cuando_desde_y_hasta_son_del_mismo_mes()
        {
            var mismo_mes = new rango_fechas(new DateTime(2026, 11, 1), new DateTime(2026, 11, 30));
            var varios_meses = new rango_fechas(new DateTime(2026, 11, 1), new DateTime(2026, 12, 1));
            var sin_limite = new rango_fechas(new DateTime(2026, 11, 1), null);

            Assert.Equal((2026, 11), mismo_mes.mes_unico());
            Assert.Null(varios_meses.mes_unico());
            Assert.Null(sin_limite.mes_unico());
        }

        [Fact]
        public void limites_utc_convierten_dias_venezolanos_y_hasta_es_exclusivo()
        {
            var rango = new rango_fechas(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

            var (desde_utc, hasta_excl) = rango.a_limites_utc();

            Assert.Equal(new DateTime(2026, 3, 1, 4, 0, 0, DateTimeKind.Unspecified), desde_utc);
            Assert.Equal(new DateTime(2026, 4, 1, 4, 0, 0, DateTimeKind.Unspecified), hasta_excl);
        }

        [Fact]
        public void limites_utc_sin_limites_son_nulos()
        {
            var (desde_utc, hasta_excl) = rango_fechas.sin_limites().a_limites_utc();

            Assert.Null(desde_utc);
            Assert.Null(hasta_excl);
        }

        [Fact]
        public void titulo_cambia_segun_los_campos_vacios()
        {
            Assert.Equal("Del 15/01/2026 al 03/12/2026",
                new rango_fechas(new DateTime(2026, 1, 15), new DateTime(2026, 12, 3)).texto_titulo());
            Assert.Equal("Desde el inicio hasta el 30/11/2026",
                new rango_fechas(null, new DateTime(2026, 11, 30)).texto_titulo());
            Assert.Equal("Desde el 01/03/2026",
                new rango_fechas(new DateTime(2026, 3, 1), null).texto_titulo());
            Assert.Equal("TODOS LOS PERIODOS", rango_fechas.sin_limites().texto_titulo());
        }

        [Fact]
        public void texto_en_mayusculas_se_usa_en_los_titulos_de_reporte()
        {
            var rango = new rango_fechas(new DateTime(2025, 8, 1), new DateTime(2025, 8, 31));

            Assert.Equal("DEL 01/08/2025 AL 31/08/2025", rango.texto_titulo_mayusculas());
        }
    }
}
