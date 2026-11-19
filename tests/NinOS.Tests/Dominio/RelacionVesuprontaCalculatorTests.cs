using System;
using System.Collections.Generic;
using System.Linq;
using NinOS.Domain;
using Xunit;

namespace NinOS.Tests.Dominio
{
    public class RelacionVesuprontaCalculatorTests
    {
        private static rango_fechas mes(int anio, int mes_numero) =>
            new rango_fechas(new DateTime(anio, mes_numero, 1), new DateTime(anio, mes_numero, DateTime.DaysInMonth(anio, mes_numero)));

        private static rango_fechas anio(int anio_numero) =>
            new rango_fechas(new DateTime(anio_numero, 1, 1), new DateTime(anio_numero, 12, 31));

        private static rango_fechas todos() => rango_fechas.sin_limites();

        private static relacion_vesupronta_fuente nota(
            int id = 1,
            string numero = "000_001",
            DateTime? creacion_utc = null,
            DateTime? despacho = null,
            string estado = "Pendiente",
            decimal facturado = 100m,
            bool descuento = false,
            decimal registrado = 100m,
            decimal abonado = 0m,
            DateTime? ultimo_pago = null)
        {
            return new relacion_vesupronta_fuente(
                id,
                numero,
                "CLIENTE DE PRUEBA",
                creacion_utc ?? new DateTime(2026, 8, 10, 16, 0, 0, DateTimeKind.Utc),
                despacho,
                estado,
                facturado,
                descuento,
                registrado,
                abonado,
                ultimo_pago);
        }

        [Fact]
        public void caso_feliz_nota_de_100_con_abono_de_30_deja_saldo_70_y_comision_10()
        {
            var fuentes = new[] { nota(facturado: 100m, registrado: 100m, abonado: 30m, descuento: true) };

            var reporte = relacion_vesupronta_calculator.construir(fuentes, mes(2026, 8));

            var fila = Assert.Single(reporte.filas);
            Assert.Equal(100m, fila.monto_pagado);
            Assert.Equal(70m, fila.saldo_actualizado);
            Assert.Equal(10m, fila.comision);
            Assert.Null(fila.fecha_pago);
            Assert.Equal("SI", fila.descuento_texto);
            Assert.Equal(100m, reporte.total_ventas);
            Assert.Equal(70m, reporte.total_por_cobrar);
        }

        [Fact]
        public void caso_limite_nota_editada_de_100_a_80_se_lleva_con_80()
        {
            var fuentes = new[] { nota(facturado: 100m, registrado: 80m, abonado: 0m) };

            var reporte = relacion_vesupronta_calculator.construir(fuentes, mes(2026, 8));

            var fila = Assert.Single(reporte.filas);
            Assert.Equal(100m, fila.monto_facturado);
            Assert.Equal(80m, fila.monto_pagado);
            Assert.Equal(80m, fila.saldo_actualizado);
            Assert.Equal(8m, fila.comision);
        }

        [Fact]
        public void caso_limite_nota_pagada_muestra_la_fecha_del_ultimo_pago_y_saldo_cero()
        {
            var ultimo_pago = new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
            var fuentes = new[] { nota(registrado: 100m, abonado: 100m, ultimo_pago: ultimo_pago) };

            var reporte = relacion_vesupronta_calculator.construir(fuentes, mes(2026, 8));

            var fila = Assert.Single(reporte.filas);
            Assert.Equal(ultimo_pago, fila.fecha_pago);
            Assert.Equal(0m, fila.saldo_actualizado);
            Assert.Equal(0m, reporte.total_por_cobrar);
        }

        [Fact]
        public void caso_limite_abono_mayor_que_la_nota_no_deja_saldo_negativo()
        {
            var fuentes = new[] { nota(registrado: 50m, abonado: 60m) };

            var reporte = relacion_vesupronta_calculator.construir(fuentes, mes(2026, 8));

            Assert.Equal(0m, Assert.Single(reporte.filas).saldo_actualizado);
            Assert.Equal(0m, reporte.total_por_cobrar);
        }

        [Fact]
        public void caso_limite_sin_descuento_muestra_NO()
        {
            var fuentes = new[] { nota(descuento: false) };

            var reporte = relacion_vesupronta_calculator.construir(fuentes, mes(2026, 8));

            Assert.Equal("NO", Assert.Single(reporte.filas).descuento_texto);
        }

        [Fact]
        public void caso_limite_comision_redondea_a_dos_decimales()
        {
            var fuentes = new[] { nota(registrado: 33.33m) };

            var reporte = relacion_vesupronta_calculator.construir(fuentes, mes(2026, 8));

            Assert.Equal(3.33m, Assert.Single(reporte.filas).comision);
        }

        [Fact]
        public void caso_limite_la_fecha_de_creacion_se_mide_en_hora_de_venezuela()
        {
            var creada_el_31_a_las_2200_hora_venezuela = new DateTime(2026, 9, 1, 2, 0, 0, DateTimeKind.Utc);
            var fuentes = new[] { nota(creacion_utc: creada_el_31_a_las_2200_hora_venezuela) };

            var agosto = relacion_vesupronta_calculator.construir(fuentes, mes(2026, 8));
            var septiembre = relacion_vesupronta_calculator.construir(fuentes, mes(2026, 9));

            Assert.Single(agosto.filas);
            Assert.Empty(septiembre.filas);
        }

        [Fact]
        public void caso_limite_nota_anulada_no_entra_en_la_relacion()
        {
            var fuentes = new[]
            {
                nota(id: 1, numero: "000_001", estado: "Anulada"),
                nota(id: 2, numero: "000_002", estado: "Pendiente")
            };

            var reporte = relacion_vesupronta_calculator.construir(fuentes, mes(2026, 8));

            Assert.Equal("000_002", Assert.Single(reporte.filas).nro);
        }

        [Fact]
        public void caso_limite_periodo_anio_incluye_todos_los_meses()
        {
            var fuentes = new[]
            {
                nota(id: 1, numero: "000_001", creacion_utc: new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)),
                nota(id: 2, numero: "000_002", creacion_utc: new DateTime(2026, 12, 15, 12, 0, 0, DateTimeKind.Utc)),
                nota(id: 3, numero: "000_003", creacion_utc: new DateTime(2025, 12, 15, 12, 0, 0, DateTimeKind.Utc))
            };

            var reporte = relacion_vesupronta_calculator.construir(fuentes, anio(2026));

            Assert.Equal(2, reporte.filas.Count);
        }

        [Fact]
        public void caso_limite_periodo_todos_incluye_todos_los_anios()
        {
            var fuentes = new[]
            {
                nota(id: 1, numero: "000_001", creacion_utc: new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc)),
                nota(id: 2, numero: "000_002", creacion_utc: new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc))
            };

            var reporte = relacion_vesupronta_calculator.construir(fuentes, todos());

            Assert.Equal(2, reporte.filas.Count);
            Assert.Equal("TOTAL VENTAS TODOS LOS PERIODOS", reporte.etiqueta_total_ventas);
            Assert.Equal("RELACION POR COBRAR/VENTAS TODOS LOS PERIODOS VESU PRONTA", reporte.titulo);
        }

        [Fact]
        public void caso_limite_sin_notas_devuelve_totales_en_cero()
        {
            var reporte = relacion_vesupronta_calculator.construir(Array.Empty<relacion_vesupronta_fuente>(), mes(2026, 8));

            Assert.Empty(reporte.filas);
            Assert.Equal(0m, reporte.total_ventas);
            Assert.Equal(0m, reporte.total_por_cobrar);
        }

        [Fact]
        public void caso_feliz_titulo_muestra_el_rango_de_fechas_en_mayusculas()
        {
            var reporte = relacion_vesupronta_calculator.construir(Array.Empty<relacion_vesupronta_fuente>(), mes(2025, 8));

            Assert.Equal("RELACION POR COBRAR/VENTAS DEL 01/08/2025 AL 31/08/2025 VESU PRONTA", reporte.titulo);
            Assert.Equal("TOTAL VENTAS DEL 01/08/2025 AL 31/08/2025", reporte.etiqueta_total_ventas);
        }

        [Fact]
        public void caso_feliz_titulo_de_anio_completo_muestra_las_dos_fechas()
        {
            var reporte = relacion_vesupronta_calculator.construir(Array.Empty<relacion_vesupronta_fuente>(), anio(2026));

            Assert.Equal("RELACION POR COBRAR/VENTAS DEL 01/01/2026 AL 31/12/2026 VESU PRONTA", reporte.titulo);
        }

        [Fact]
        public void error_mes_fuera_de_rango_lanza_excepcion()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                relacion_vesupronta_calculator.construir(Array.Empty<relacion_vesupronta_fuente>(), mes(2026, 13)));
        }

        [Fact]
        public void error_anio_invalido_lanza_excepcion()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                relacion_vesupronta_calculator.construir(Array.Empty<relacion_vesupronta_fuente>(), anio(1999)));
        }

        [Fact]
        public void error_lista_de_notas_nula_lanza_excepcion()
        {
            Assert.Throws<ArgumentNullException>(() =>
                relacion_vesupronta_calculator.construir(null!, todos()));
        }

        [Fact]
        public void error_monto_negativo_no_se_puede_crear_la_fuente()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => nota(facturado: -1m));
        }

        [Fact]
        public void error_nota_sin_numero_no_se_puede_crear_la_fuente()
        {
            Assert.Throws<ArgumentException>(() => nota(numero: " "));
        }

        [Fact]
        public void caso_feliz_totales_suman_todas_las_filas_del_periodo()
        {
            var fuentes = new List<relacion_vesupronta_fuente>
            {
                nota(id: 1, numero: "000_001", registrado: 100m, abonado: 30m),
                nota(id: 2, numero: "000_002", registrado: 80m, abonado: 0m),
                nota(id: 3, numero: "000_003", registrado: 50m, abonado: 50m)
            };

            var reporte = relacion_vesupronta_calculator.construir(fuentes, mes(2026, 8));

            Assert.Equal(3, reporte.filas.Count);
            Assert.Equal(230m, reporte.total_ventas);
            Assert.Equal(150m, reporte.total_por_cobrar);
            Assert.True(reporte.filas.All(f => f.fecha_comision_cancelada == null),
                "La fecha de comision cancelada no debe llenarse mientras no existan comisiones de Vesupronta.");
        }
    }
}
