using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrincipalPriceList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_principal",
                table: "price_list",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_principal",
                table: "price_list");
        }
    }
}
