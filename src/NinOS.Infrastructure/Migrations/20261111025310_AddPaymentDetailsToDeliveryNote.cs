using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentDetailsToDeliveryNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payment_account_text",
                table: "delivery_note",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_header_text",
                table: "delivery_note",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_movil_text",
                table: "delivery_note",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_transfer_text",
                table: "delivery_note",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payment_account_text",
                table: "delivery_note");

            migrationBuilder.DropColumn(
                name: "payment_header_text",
                table: "delivery_note");

            migrationBuilder.DropColumn(
                name: "payment_movil_text",
                table: "delivery_note");

            migrationBuilder.DropColumn(
                name: "payment_transfer_text",
                table: "delivery_note");
        }
    }
}
