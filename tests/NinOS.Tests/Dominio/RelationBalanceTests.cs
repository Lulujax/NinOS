using System;
using NinOS.Domain;
using Xunit;

namespace NinOS.Tests.Dominio
{
    public class RelationBalanceTests
    {
        [Fact]
        public void caso_feliz_saldo_parcial()
        {
            Assert.Equal(50m, RelationBalance.balance_due(100m, 50m));
            Assert.Equal(50m, RelationBalance.raw_balance(100m, 50m));
        }

        [Fact]
        public void caso_limite_pago_excede_el_monto_balance_due_queda_en_cero()
        {
            Assert.Equal(0m, RelationBalance.balance_due(100m, 103m));
            Assert.Equal(-3m, RelationBalance.raw_balance(100m, 103m));
        }

        [Fact]
        public void caso_limite_sin_abonos_balance_es_el_monto_completo()
        {
            Assert.Equal(100m, RelationBalance.balance_due(100m, 0m));
        }

        [Fact]
        public void caso_error_monto_negativo_lanza_excepcion()
        {
            Assert.Throws<ArgumentException>(() => RelationBalance.raw_balance(-1m, 0m));
        }

        [Fact]
        public void caso_error_pagado_negativo_lanza_excepcion()
        {
            Assert.Throws<ArgumentException>(() => RelationBalance.raw_balance(100m, -1m));
        }
    }
}
