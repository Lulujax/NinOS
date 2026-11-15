using System.Collections.Generic;
using NinOS.Domain;
using Xunit;

namespace NinOS.Tests.Dominio
{
    public class PromoItemsSnapshotStoreTests
    {
        [Fact]
        public void caso_feliz_lista_hace_round_trip()
        {
            var items = new List<promo_item_snapshot>
            {
                new promo_item_snapshot { id_product = 1, quantity_required = 2 },
                new promo_item_snapshot { id_product = 2, quantity_required = 1 },
                new promo_item_snapshot { id_product = 3, quantity_required = 3 }
            };

            string? json = promo_items_snapshot_store.serialize(items);
            var result = promo_items_snapshot_store.deserialize(json);

            Assert.NotNull(result);
            Assert.Equal(3, result!.Count);
            Assert.Equal(2, result[0].quantity_required);
        }

        [Fact]
        public void caso_limite_lista_vacia_serializa_a_null()
        {
            Assert.Null(promo_items_snapshot_store.serialize(new List<promo_item_snapshot>()));
            Assert.Null(promo_items_snapshot_store.serialize(null));
        }

        [Fact]
        public void caso_error_json_invalido_devuelve_null_sin_lanzar()
        {
            Assert.Null(promo_items_snapshot_store.deserialize("{esto no es json valido"));
            Assert.Null(promo_items_snapshot_store.deserialize(null));
            Assert.Null(promo_items_snapshot_store.deserialize(""));
        }
    }
}
