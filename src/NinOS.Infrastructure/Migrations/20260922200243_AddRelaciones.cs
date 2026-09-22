using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRelaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_relacion",
                table: "delivery_note",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "relacion",
                columns: table => new
                {
                    id_relacion = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    relation_number = table.Column<int>(type: "integer", nullable: false),
                    week_start = table.Column<DateTime>(type: "date", nullable: false),
                    week_end = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_relacion", x => x.id_relacion);
                });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_id_relacion",
                table: "delivery_note",
                column: "id_relacion");

            migrationBuilder.CreateIndex(
                name: "IX_relacion_relation_number",
                table: "relacion",
                column: "relation_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_relacion_week_start",
                table: "relacion",
                column: "week_start",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_delivery_note_relacion_id_relacion",
                table: "delivery_note",
                column: "id_relacion",
                principalTable: "relacion",
                principalColumn: "id_relacion",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_delivery_note_relacion_id_relacion",
                table: "delivery_note");

            migrationBuilder.DropTable(
                name: "relacion");

            migrationBuilder.DropIndex(
                name: "IX_delivery_note_id_relacion",
                table: "delivery_note");

            migrationBuilder.DropColumn(
                name: "id_relacion",
                table: "delivery_note");
        }
    }
}
