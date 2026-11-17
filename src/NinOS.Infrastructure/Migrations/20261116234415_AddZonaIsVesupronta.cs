using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZonaIsVesupronta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_vesupronta",
                table: "zona",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "ck_zona_una_sola_empresa",
                table: "zona",
                sql: "NOT (is_pro_venta AND is_vesupronta)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_zona_una_sola_empresa",
                table: "zona");

            migrationBuilder.DropColumn(
                name: "is_vesupronta",
                table: "zona");
        }
    }
}
