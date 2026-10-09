using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_relacion_week_start",
                table: "relacion");

            migrationBuilder.AddColumn<bool>(
                name: "es_hueca",
                table: "relacion",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "observaciones",
                table: "relacion",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "saldo",
                table: "relacion",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_relacion_week_start",
                table: "relacion",
                column: "week_start");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_relacion_week_start",
                table: "relacion");

            migrationBuilder.DropColumn(
                name: "es_hueca",
                table: "relacion");

            migrationBuilder.DropColumn(
                name: "observaciones",
                table: "relacion");

            migrationBuilder.DropColumn(
                name: "saldo",
                table: "relacion");

            migrationBuilder.CreateIndex(
                name: "IX_relacion_week_start",
                table: "relacion",
                column: "week_start",
                unique: true);
        }
    }
}
