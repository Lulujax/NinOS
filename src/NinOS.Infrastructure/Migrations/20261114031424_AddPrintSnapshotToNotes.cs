using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintSnapshotToNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "print_snapshot",
                table: "delivery_note",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "print_snapshot_version",
                table: "delivery_note",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "print_snapshot",
                table: "credit_note",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "print_snapshot_version",
                table: "credit_note",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "print_snapshot",
                table: "delivery_note");

            migrationBuilder.DropColumn(
                name: "print_snapshot_version",
                table: "delivery_note");

            migrationBuilder.DropColumn(
                name: "print_snapshot",
                table: "credit_note");

            migrationBuilder.DropColumn(
                name: "print_snapshot_version",
                table: "credit_note");
        }
    }
}
