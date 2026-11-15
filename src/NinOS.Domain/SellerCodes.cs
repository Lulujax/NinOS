using System;

namespace NinOS.Domain
{
    public static class SellerCodes
    {
        public const string Sandra = "001";
        public const string Anais = "002";
        public const string Alejandra = "003";
        public const string JuanLuis = "004";

        public const string ObsequioAdminSellerCode = Sandra;

        public static bool is_obsequio_admin_seller(string? seller_code, string? full_name)
        {
            return seller_code == ObsequioAdminSellerCode
                || (full_name?.Contains("Sandra", StringComparison.OrdinalIgnoreCase) ?? false);
        }
    }
}
