using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_price_list",
                table: "note_detail",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "price_list",
                columns: table => new
                {
                    id_price_list = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_list", x => x.id_price_list);
                });

            migrationBuilder.CreateTable(
                name: "product_price",
                columns: table => new
                {
                    id_product_price = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_product = table.Column<int>(type: "integer", nullable: false),
                    id_price_list = table.Column<int>(type: "integer", nullable: false),
                    unit_price_usd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_price", x => x.id_product_price);
                    table.ForeignKey(
                        name: "FK_product_price_price_list_id_price_list",
                        column: x => x.id_price_list,
                        principalTable: "price_list",
                        principalColumn: "id_price_list",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_price_product_id_product",
                        column: x => x.id_product,
                        principalTable: "product",
                        principalColumn: "id_product",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_note_detail_id_price_list",
                table: "note_detail",
                column: "id_price_list");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_name",
                table: "price_list",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_price_id_price_list",
                table: "product_price",
                column: "id_price_list");

            migrationBuilder.CreateIndex(
                name: "IX_product_price_id_product_id_price_list",
                table: "product_price",
                columns: new[] { "id_product", "id_price_list" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_note_detail_price_list_id_price_list",
                table: "note_detail",
                column: "id_price_list",
                principalTable: "price_list",
                principalColumn: "id_price_list",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_note_detail_price_list_id_price_list",
                table: "note_detail");

            migrationBuilder.DropTable(
                name: "product_price");

            migrationBuilder.DropTable(
                name: "price_list");

            migrationBuilder.DropIndex(
                name: "IX_note_detail_id_price_list",
                table: "note_detail");

            migrationBuilder.DropColumn(
                name: "id_price_list",
                table: "note_detail");
        }
    }
}
