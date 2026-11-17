using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class SellerUnitScopeTests
{
    private static seller vendedora_con_zonas(params zona[] zonas)
    {
        var vendedora = new seller("Sandra", "001", "S", 0.10m);
        foreach (var zona in zonas)
            vendedora.seller_zones.Add(new seller_zone { zona = zona, id_zona = zona.id_zona });
        return vendedora;
    }

    private static zona zona_general() => new zona { name = "Gral", code = "G" };

    private static zona zona_vesupronta() => new zona { name = "Ves", code = "V", is_vesupronta = true };

    private static zona zona_pro_venta() => new zona { name = "Mar", code = "M", is_pro_venta = true };

    [Fact]
    public void feliz_vendedora_con_zona_vesupronta_pertenece_a_vesupronta()
    {
        var vendedora = vendedora_con_zonas(zona_vesupronta());

        Assert.True(SellerUnitScope.belongs_to_unit(vendedora, business_unit.Vesupronta));
    }

    [Fact]
    public void limite_vendedora_sin_zonas_no_pertenece_a_vesupronta()
    {
        var vendedora = vendedora_con_zonas();

        Assert.False(SellerUnitScope.belongs_to_unit(vendedora, business_unit.Vesupronta));
    }

    [Fact]
    public void error_vendedora_solo_general_no_pertenece_a_vesupronta()
    {
        var vendedora = vendedora_con_zonas(zona_general());

        Assert.False(SellerUnitScope.belongs_to_unit(vendedora, business_unit.Vesupronta));
    }

    [Fact]
    public void feliz_vendedora_general_pertenece_a_general()
    {
        var vendedora = vendedora_con_zonas(zona_general());

        Assert.True(SellerUnitScope.belongs_to_unit(vendedora, business_unit.General));
    }

    [Fact]
    public void limite_vendedora_con_zona_general_y_vesupronta_sigue_en_general()
    {
        var vendedora = vendedora_con_zonas(zona_general(), zona_vesupronta());

        Assert.True(SellerUnitScope.belongs_to_unit(vendedora, business_unit.General));
        Assert.True(SellerUnitScope.belongs_to_unit(vendedora, business_unit.Vesupronta));
    }

    [Fact]
    public void error_vendedora_solo_vesupronta_no_aparece_en_general()
    {
        var vendedora = vendedora_con_zonas(zona_vesupronta());

        Assert.False(SellerUnitScope.belongs_to_unit(vendedora, business_unit.General));
    }

    [Fact]
    public void error_vendedora_solo_pro_venta_no_aparece_en_general()
    {
        var vendedora = vendedora_con_zonas(zona_pro_venta());

        Assert.False(SellerUnitScope.belongs_to_unit(vendedora, business_unit.General));
    }
}
