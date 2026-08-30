using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdjustedTotalToDeliveryNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "exchange_rate",
                table: "payment",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                table: "payment",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "payment",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "observations",
                table: "payment",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "payment_type",
                table: "payment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "reference_number",
                table: "payment",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "payment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "adjusted_total_usd",
                table: "delivery_note",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE \"delivery_note\" SET \"adjusted_total_usd\" = \"total_amount_usd\";");

            migrationBuilder.AddColumn<decimal>(
                name: "amount_bs",
                table: "commission",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "exchange_rate",
                table: "commission",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "reference_number",
                table: "commission",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bank_name",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "observations",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "payment_type",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "reference_number",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "adjusted_total_usd",
                table: "delivery_note");

            migrationBuilder.DropColumn(
                name: "amount_bs",
                table: "commission");

            migrationBuilder.DropColumn(
                name: "exchange_rate",
                table: "commission");

            migrationBuilder.DropColumn(
                name: "reference_number",
                table: "commission");

            migrationBuilder.AlterColumn<decimal>(
                name: "exchange_rate",
                table: "payment",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);
        }
    }
}
