using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixNoteDetailPromotionAndSellerPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "customer_code_prefix",
                table: "seller",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE \"seller\" SET \"customer_code_prefix\" = '3301' WHERE \"full_name\" = 'Sandra' AND \"customer_code_prefix\" = '';");
            migrationBuilder.Sql("UPDATE \"seller\" SET \"customer_code_prefix\" = '3300' WHERE \"full_name\" = 'Anais' AND \"customer_code_prefix\" = '';");
            migrationBuilder.Sql("UPDATE \"seller\" SET \"customer_code_prefix\" = '3305' WHERE \"full_name\" = 'Alejandra' AND \"customer_code_prefix\" = '';");

            migrationBuilder.AlterColumn<int>(
                name: "id_product",
                table: "note_detail",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "id_promotion",
                table: "note_detail",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_note_detail_id_promotion",
                table: "note_detail",
                column: "id_promotion");

            migrationBuilder.AddForeignKey(
                name: "FK_note_detail_promotion_id_promotion",
                table: "note_detail",
                column: "id_promotion",
                principalTable: "promotion",
                principalColumn: "id_promotion",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_note_detail_promotion_id_promotion",
                table: "note_detail");

            migrationBuilder.DropIndex(
                name: "IX_note_detail_id_promotion",
                table: "note_detail");

            migrationBuilder.DropColumn(
                name: "customer_code_prefix",
                table: "seller");

            migrationBuilder.DropColumn(
                name: "id_promotion",
                table: "note_detail");

            migrationBuilder.AlterColumn<int>(
                name: "id_product",
                table: "note_detail",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
