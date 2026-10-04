using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZonaMasterAndCustomerZona : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_zona",
                table: "customer",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "zona",
                columns: table => new
                {
                    id_zona = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zona", x => x.id_zona);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_id_zona",
                table: "customer",
                column: "id_zona");

            migrationBuilder.CreateIndex(
                name: "IX_zona_code",
                table: "zona",
                column: "code",
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_zona_name",
                table: "zona",
                column: "name",
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_customer_zona_id_zona",
                table: "customer",
                column: "id_zona",
                principalTable: "zona",
                principalColumn: "id_zona",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customer_zona_id_zona",
                table: "customer");

            migrationBuilder.DropTable(
                name: "zona");

            migrationBuilder.DropIndex(
                name: "IX_customer_id_zona",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "id_zona",
                table: "customer");
        }
    }
}
