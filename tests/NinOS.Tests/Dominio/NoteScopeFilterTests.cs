using System;
using System.Linq;
using NinOS.Domain;
using NinOS.Infrastructure.Services;
using Xunit;

namespace NinOS.Tests.Dominio
{
    public class NoteScopeFilterTests
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

        private static delivery_note nota_con_tipo(int? note_type_id)
        {
            return new delivery_note("000_001", new DateTime(2026, 11, 16), 1, 1, 10m, "Pendiente", 10m)
            {
                note_type_id = note_type_id
            };
        }

        [Fact]
        public void caso_feliz_general_excluye_pro_venta_y_vesupronta()
        {
            var scope = NoteScopeFilter.build_scope(business_unit.General, tipos_de_prueba);

            Assert.Equal(new[] { 3, 4, 5, 6 }, scope.excluded_type_ids);
            Assert.True(NoteScopeFilter.belongs_to_scope(scope, 1));
            Assert.True(NoteScopeFilter.belongs_to_scope(scope, 2));
            Assert.False(NoteScopeFilter.belongs_to_scope(scope, 3));
            Assert.False(NoteScopeFilter.belongs_to_scope(scope, 5));
        }

        [Fact]
        public void caso_feliz_vesupronta_solo_acepta_sus_tipos()
        {
            var scope = NoteScopeFilter.build_scope(business_unit.Vesupronta, tipos_de_prueba);

            Assert.Equal(new[] { 1, 2, 3, 4 }, scope.excluded_type_ids);
            Assert.True(NoteScopeFilter.belongs_to_scope(scope, 5));
            Assert.True(NoteScopeFilter.belongs_to_scope(scope, 6));
            Assert.False(NoteScopeFilter.belongs_to_scope(scope, 1));
            Assert.False(NoteScopeFilter.belongs_to_scope(scope, 3));
        }

        [Fact]
        public void caso_limite_nota_sin_tipo_es_general_pero_no_vesupronta()
        {
            var general = NoteScopeFilter.build_scope(business_unit.General, tipos_de_prueba);
            var vesupronta = NoteScopeFilter.build_scope(business_unit.Vesupronta, tipos_de_prueba);

            Assert.True(NoteScopeFilter.belongs_to_scope(general, null));
            Assert.False(NoteScopeFilter.belongs_to_scope(vesupronta, null));
        }

        [Fact]
        public void caso_limite_lista_de_tipos_vacia_no_excluye_nada_en_general()
        {
            var scope = NoteScopeFilter.build_scope(business_unit.General, Array.Empty<(int, string?)>());

            Assert.Empty(scope.excluded_type_ids);
            Assert.True(NoteScopeFilter.belongs_to_scope(scope, 99));
        }

        [Fact]
        public void caso_limite_codigo_desconocido_cuenta_como_general_y_queda_fuera_de_vesupronta()
        {
            var tipos_con_desconocido = new (int id_note_type, string? code)[] { (7, "XYZ") };

            var general = NoteScopeFilter.build_scope(business_unit.General, tipos_con_desconocido);
            var vesupronta = NoteScopeFilter.build_scope(business_unit.Vesupronta, tipos_con_desconocido);

            Assert.True(NoteScopeFilter.belongs_to_scope(general, 7));
            Assert.False(NoteScopeFilter.belongs_to_scope(vesupronta, 7));
        }

        [Fact]
        public void caso_feliz_expresion_de_ef_coincide_con_la_regla_en_memoria()
        {
            var notas = new[] { nota_con_tipo(1), nota_con_tipo(3), nota_con_tipo(5), nota_con_tipo(null) };
            var general = NoteScopeFilter.build_scope(business_unit.General, tipos_de_prueba);
            var vesupronta = NoteScopeFilter.build_scope(business_unit.Vesupronta, tipos_de_prueba);

            var en_general = notas.Where(NoteScopeFilter.delivery_notes_in_scope(general).Compile()).Select(n => n.note_type_id).ToList();
            var en_vesupronta = notas.Where(NoteScopeFilter.delivery_notes_in_scope(vesupronta).Compile()).Select(n => n.note_type_id).ToList();

            Assert.Equal(new int?[] { 1, null }, en_general);
            Assert.Equal(new int?[] { 5 }, en_vesupronta);
        }

        [Fact]
        public void caso_error_argumentos_nulos_lanzan_excepcion()
        {
            Assert.Throws<ArgumentNullException>(() => NoteScopeFilter.build_scope(business_unit.General, null!));
            Assert.Throws<ArgumentNullException>(() => NoteScopeFilter.belongs_to_scope(null!, 1));
            Assert.Throws<ArgumentNullException>(() => NoteScopeFilter.delivery_notes_in_scope(null!));
        }
    }
}
