using System;
using NinOS.Domain;
using Xunit;

namespace NinOS.Tests.Dominio
{
    public class NoteSettlementTests
    {
        [Fact]
        public void caso_feliz_pago_completo_queda_pagada_y_crea_comision()
        {
            var result = NoteSettlement.evaluate(
                adjusted_total_usd: 100m,
                total_paid_usd: 100m,
                existing_commission_is_paid: null,
                eligible_for_commission: true,
                commission_percentage: 0.10m);

            Assert.Equal("Pagada", result.status);
            Assert.Equal(CommissionAction.Create, result.commission_action);
            Assert.Equal(10.00m, result.commission_amount_usd);
        }

        [Fact]
        public void caso_feliz_editar_pago_incompleto_vuelve_pendiente_y_quita_comision()
        {
            var result = NoteSettlement.evaluate(
                adjusted_total_usd: 100m,
                total_paid_usd: 60m,
                existing_commission_is_paid: false,
                eligible_for_commission: true,
                commission_percentage: 0.10m);

            Assert.Equal("Pendiente", result.status);
            Assert.Equal(CommissionAction.Remove, result.commission_action);
            Assert.Null(result.commission_amount_usd);
        }

        [Fact]
        public void caso_limite_devolucion_total_queda_devuelta_y_quita_comision_no_pagada()
        {
            var result = NoteSettlement.evaluate(
                adjusted_total_usd: 0m,
                total_paid_usd: 0m,
                existing_commission_is_paid: false,
                eligible_for_commission: false,
                commission_percentage: 0m,
                is_fully_returned: true);

            Assert.Equal("Devuelta", result.status);
            Assert.Equal(CommissionAction.Remove, result.commission_action);
        }

        [Fact]
        public void caso_limite_devolucion_parcial_saldada_por_pago_crea_comision_sobre_monto_ajustado()
        {
            var result = NoteSettlement.evaluate(
                adjusted_total_usd: 80m,
                total_paid_usd: 80m,
                existing_commission_is_paid: null,
                eligible_for_commission: true,
                commission_percentage: 0.10m);

            Assert.Equal("Pagada", result.status);
            Assert.Equal(CommissionAction.Create, result.commission_action);
            Assert.Equal(8.00m, result.commission_amount_usd);
        }

        [Fact]
        public void caso_limite_comision_pagada_nunca_se_toca_aunque_la_nota_quede_pendiente()
        {
            var result = NoteSettlement.evaluate(
                adjusted_total_usd: 100m,
                total_paid_usd: 60m,
                existing_commission_is_paid: true,
                eligible_for_commission: true,
                commission_percentage: 0.10m);

            Assert.Equal("Pendiente", result.status);
            Assert.Equal(CommissionAction.None, result.commission_action);
        }

        [Fact]
        public void caso_error_monto_negativo_lanza_excepcion()
        {
            Assert.Throws<ArgumentException>(() => NoteSettlement.evaluate(
                adjusted_total_usd: -1m,
                total_paid_usd: 0m,
                existing_commission_is_paid: null,
                eligible_for_commission: false,
                commission_percentage: 0m));
        }

        [Fact]
        public void caso_error_porcentaje_fuera_de_rango_lanza_excepcion()
        {
            Assert.Throws<ArgumentException>(() => NoteSettlement.evaluate(
                adjusted_total_usd: 100m,
                total_paid_usd: 100m,
                existing_commission_is_paid: null,
                eligible_for_commission: true,
                commission_percentage: 1.5m));
        }
    }
}
