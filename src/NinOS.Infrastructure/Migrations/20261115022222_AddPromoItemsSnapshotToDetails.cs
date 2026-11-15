using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromoItemsSnapshotToDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "promo_items_snapshot",
                table: "note_detail",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "promo_items_snapshot",
                table: "credit_note_detail",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "promo_items_snapshot",
                table: "note_detail");

            migrationBuilder.DropColumn(
                name: "promo_items_snapshot",
                table: "credit_note_detail");
        }
    }
}
