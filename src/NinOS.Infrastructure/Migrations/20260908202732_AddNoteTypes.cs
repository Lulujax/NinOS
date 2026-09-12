using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "note_type_id",
                table: "delivery_note",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "promo_discount_percentage",
                table: "delivery_note",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "note_type",
                columns: table => new
                {
                    id_note_type = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    id_seller = table.Column<int>(type: "integer", nullable: true),
                    header_title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    calculation_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    discount_configurable = table.Column<bool>(type: "boolean", nullable: false),
                    default_discount_percentage = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    promo_discount_percentage = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    conditions_template = table.Column<string>(type: "text", nullable: false),
                    discount_conditions_template = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_type", x => x.id_note_type);
                    table.ForeignKey(
                        name: "FK_note_type_seller_id_seller",
                        column: x => x.id_seller,
                        principalTable: "seller",
                        principalColumn: "id_seller",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_note_type_id",
                table: "delivery_note",
                column: "note_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_note_type_id_seller",
                table: "note_type",
                column: "id_seller");

            migrationBuilder.AddForeignKey(
                name: "FK_delivery_note_note_type_note_type_id",
                table: "delivery_note",
                column: "note_type_id",
                principalTable: "note_type",
                principalColumn: "id_note_type",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_delivery_note_note_type_note_type_id",
                table: "delivery_note");

            migrationBuilder.DropTable(
                name: "note_type");

            migrationBuilder.DropIndex(
                name: "IX_delivery_note_note_type_id",
                table: "delivery_note");

            migrationBuilder.DropColumn(
                name: "note_type_id",
                table: "delivery_note");

            migrationBuilder.DropColumn(
                name: "promo_discount_percentage",
                table: "delivery_note");
        }
    }
}
