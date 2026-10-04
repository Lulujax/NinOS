using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerSoftDeleteAndSellerZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "seller",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_reason",
                table: "seller",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "seller",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "seller_zone",
                columns: table => new
                {
                    id_seller_zone = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_seller = table.Column<int>(type: "integer", nullable: false),
                    id_zona = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seller_zone", x => x.id_seller_zone);
                    table.ForeignKey(
                        name: "FK_seller_zone_seller_id_seller",
                        column: x => x.id_seller,
                        principalTable: "seller",
                        principalColumn: "id_seller",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_seller_zone_zona_id_zona",
                        column: x => x.id_zona,
                        principalTable: "zona",
                        principalColumn: "id_zona",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_seller_seller_code",
                table: "seller",
                column: "seller_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seller_zone_id_seller_id_zona",
                table: "seller_zone",
                columns: new[] { "id_seller", "id_zona" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seller_zone_id_zona",
                table: "seller_zone",
                column: "id_zona");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seller_zone");

            migrationBuilder.DropIndex(
                name: "IX_seller_seller_code",
                table: "seller");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "seller");

            migrationBuilder.DropColumn(
                name: "deleted_reason",
                table: "seller");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "seller");
        }
    }
}
