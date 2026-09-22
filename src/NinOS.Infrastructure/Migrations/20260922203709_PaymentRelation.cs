using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaymentRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "id_delivery_note",
                table: "payment",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "id_relacion",
                table: "payment",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_id_relacion",
                table: "payment",
                column: "id_relacion");

            migrationBuilder.AddForeignKey(
                name: "FK_payment_relacion_id_relacion",
                table: "payment",
                column: "id_relacion",
                principalTable: "relacion",
                principalColumn: "id_relacion",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payment_relacion_id_relacion",
                table: "payment");

            migrationBuilder.DropIndex(
                name: "IX_payment_id_relacion",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "id_relacion",
                table: "payment");

            migrationBuilder.AlterColumn<int>(
                name: "id_delivery_note",
                table: "payment",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
