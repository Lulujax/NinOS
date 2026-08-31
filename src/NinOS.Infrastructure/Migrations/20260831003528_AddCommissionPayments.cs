using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commission_payment",
                columns: table => new
                {
                    id_commission_payment = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_commission = table.Column<int>(type: "integer", nullable: false),
                    amount_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    amount_bs = table.Column<decimal>(type: "numeric", nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    payment_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commission_payment", x => x.id_commission_payment);
                    table.ForeignKey(
                        name: "FK_commission_payment_commission_id_commission",
                        column: x => x.id_commission,
                        principalTable: "commission",
                        principalColumn: "id_commission",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commission_payment_id_commission",
                table: "commission_payment",
                column: "id_commission");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commission_payment");
        }
    }
}
