using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteTermsToDeliveryNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "conditions_text",
                table: "delivery_note",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "credit_days",
                table: "delivery_note",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_conditions_text",
                table: "delivery_note",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "due_date",
                table: "delivery_note",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "conditions_text",
                table: "delivery_note");

            migrationBuilder.DropColumn(
                name: "credit_days",
                table: "delivery_note");

            migrationBuilder.DropColumn(
                name: "discount_conditions_text",
                table: "delivery_note");

            migrationBuilder.DropColumn(
                name: "due_date",
                table: "delivery_note");
        }
    }
}
