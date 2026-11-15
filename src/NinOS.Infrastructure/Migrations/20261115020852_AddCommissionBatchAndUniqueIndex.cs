using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionBatchAndUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_commission_id_delivery_note",
                table: "commission");

            migrationBuilder.AddColumn<Guid>(
                name: "id_batch",
                table: "commission_payment",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_commission_id_delivery_note",
                table: "commission",
                column: "id_delivery_note",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_commission_id_delivery_note",
                table: "commission");

            migrationBuilder.DropColumn(
                name: "id_batch",
                table: "commission_payment");

            migrationBuilder.CreateIndex(
                name: "IX_commission_id_delivery_note",
                table: "commission",
                column: "id_delivery_note");
        }
    }
}
