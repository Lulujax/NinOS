using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSellerCodesTo3Digits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE seller SET seller_code = '001', customer_code_prefix = '001' WHERE id_seller = 1 OR full_name = 'Sandra';
                UPDATE seller SET seller_code = '002', customer_code_prefix = '002' WHERE id_seller = 2 OR full_name = 'Anais';
                UPDATE seller SET seller_code = '003', customer_code_prefix = '003' WHERE id_seller = 3 OR full_name = 'Alejandra';
                UPDATE seller SET seller_code = '004', customer_code_prefix = '004' WHERE id_seller = 4 OR full_name = 'Juan Luis';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE seller SET seller_code = '3200', customer_code_prefix = '3301' WHERE id_seller = 1 OR full_name = 'Sandra';
                UPDATE seller SET seller_code = '3300', customer_code_prefix = '3300' WHERE id_seller = 2 OR full_name = 'Anais';
                UPDATE seller SET seller_code = '3500', customer_code_prefix = '3305' WHERE id_seller = 3 OR full_name = 'Alejandra';
                UPDATE seller SET seller_code = '3400', customer_code_prefix = '3400' WHERE id_seller = 4 OR full_name = 'Juan Luis';
            ");
        }
    }
}

