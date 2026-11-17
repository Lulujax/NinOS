using System;
using NinOS.Domain;
using NinOS.Infrastructure.Services;
using Xunit;

namespace NinOS.Tests.Dominio
{
    public class CreditNoteScopeTests
    {
        private static readonly (int id_note_type, string? code)[] tipos_de_prueba =
        {
            (1, "GEN"),
            (2, "PRM"),
            (3, "MAR"),
            (4, "PVP"),
            (5, "VES"),
            (6, "VESVOL"),
        };

        [Fact]
        public void caso_feliz_nc_de_nota_vesupronta_solo_aparece_en_vesupronta()
        {
            var vesupronta = NoteScopeFilter.build_credit_scope(business_unit.Vesupronta, tipos_de_prueba);
            var general = NoteScopeFilter.build_credit_scope(business_unit.General, tipos_de_prueba);

            Assert.True(NoteScopeFilter.belongs_to_scope_with_fallback(vesupronta, true, 5));
            Assert.True(NoteScopeFilter.belongs_to_scope_with_fallback(vesupronta, true, 6));
            Assert.False(NoteScopeFilter.belongs_to_scope_with_fallback(general, true, 5));
            Assert.False(NoteScopeFilter.belongs_to_scope_with_fallback(general, true, 6));
        }

        [Fact]
        public void caso_feliz_nc_de_nota_general_no_aparece_en_vesupronta()
        {
            var vesupronta = NoteScopeFilter.build_credit_scope(business_unit.Vesupronta, tipos_de_prueba);
            var general = NoteScopeFilter.build_credit_scope(business_unit.General, tipos_de_prueba);

            Assert.True(NoteScopeFilter.belongs_to_scope_with_fallback(general, true, 1));
            Assert.False(NoteScopeFilter.belongs_to_scope_with_fallback(vesupronta, true, 1));
        }

        [Fact]
        public void caso_feliz_general_conserva_las_nc_de_pro_venta_porque_se_emiten_desde_su_pestaña()
        {
            var general = NoteScopeFilter.build_credit_scope(business_unit.General, tipos_de_prueba);

            Assert.True(NoteScopeFilter.belongs_to_scope_with_fallback(general, true, 3));
            Assert.True(NoteScopeFilter.belongs_to_scope_with_fallback(general, true, 4));
        }

        [Fact]
        public void caso_limite_nota_original_sin_tipo_es_general_y_no_vesupronta()
        {
            var general = NoteScopeFilter.build_credit_scope(business_unit.General, tipos_de_prueba);
            var vesupronta = NoteScopeFilter.build_credit_scope(business_unit.Vesupronta, tipos_de_prueba);

            Assert.True(NoteScopeFilter.belongs_to_scope_with_fallback(general, true, null));
            Assert.False(NoteScopeFilter.belongs_to_scope_with_fallback(vesupronta, true, null));
        }

        [Fact]
        public void caso_limite_codigo_desconocido_cuenta_como_general_y_no_vesupronta()
        {
            var tipos_con_desconocido = new (int id_note_type, string? code)[] { (7, "XYZ") };
            var general = NoteScopeFilter.build_credit_scope(business_unit.General, tipos_con_desconocido);
            var vesupronta = NoteScopeFilter.build_credit_scope(business_unit.Vesupronta, tipos_con_desconocido);

            Assert.True(NoteScopeFilter.belongs_to_scope_with_fallback(general, true, 7));
            Assert.False(NoteScopeFilter.belongs_to_scope_with_fallback(vesupronta, true, 7));
        }

        [Fact]
        public void caso_limite_nota_original_no_encontrada_va_a_general_y_no_a_vesupronta()
        {
            var general = NoteScopeFilter.build_credit_scope(business_unit.General, tipos_de_prueba);
            var vesupronta = NoteScopeFilter.build_credit_scope(business_unit.Vesupronta, tipos_de_prueba);

            Assert.True(NoteScopeFilter.belongs_to_scope_with_fallback(general, false, null));
            Assert.False(NoteScopeFilter.belongs_to_scope_with_fallback(vesupronta, false, null));
            Assert.True(NoteScopeFilter.belongs_to_scope_with_fallback(general, false, 5));
            Assert.False(NoteScopeFilter.belongs_to_scope_with_fallback(vesupronta, false, 5));
        }

        [Fact]
        public void caso_error_alcance_nulo_lanza_excepcion()
        {
            Assert.Throws<ArgumentNullException>(() => NoteScopeFilter.belongs_to_scope_with_fallback(null!, true, 1));
        }

        [Fact]
        public void caso_error_lista_de_tipos_nula_lanza_excepcion()
        {
            Assert.Throws<ArgumentNullException>(() => NoteScopeFilter.build_credit_scope(business_unit.General, null!));
        }
    }
}
