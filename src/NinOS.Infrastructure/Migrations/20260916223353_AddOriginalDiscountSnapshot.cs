using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOriginalDiscountSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "original_discount_percentage",
                table: "delivery_note",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "original_volume_discount_percentage",
                table: "delivery_note",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            // Congela los descuentos actuales como snapshot original para el PDF.
            migrationBuilder.Sql(
                "UPDATE \"delivery_note\" SET \"original_discount_percentage\" = \"discount_percentage\", " +
                "\"original_volume_discount_percentage\" = \"volume_discount_percentage\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "original_discount_percentage",
                table: "delivery_note");

            migrationBuilder.DropColumn(
                name: "original_volume_discount_percentage",
                table: "delivery_note");
        }
    }
}
