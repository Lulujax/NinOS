using System.Linq;

namespace NinOS.Domain
{
    public static class SellerUnitScope
    {
        public static bool belongs_to_unit(seller s, business_unit unit)
        {
            bool has_vesupronta_zone = s.seller_zones.Any(sz => sz.zona != null && sz.zona.is_vesupronta);
            if (unit == business_unit.Vesupronta) return has_vesupronta_zone;

            bool only_vesupronta_zones = has_vesupronta_zone
                && s.seller_zones.All(sz => sz.zona != null && sz.zona.is_vesupronta);
            return !s.is_pro_venta_only && !only_vesupronta_zones;
        }
    }
}
