using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "credit_note",
                columns: table => new
                {
                    id_credit_note = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    note_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    creation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_delivery_note = table.Column<int>(type: "integer", nullable: false),
                    id_seller = table.Column<int>(type: "integer", nullable: false),
                    id_customer = table.Column<int>(type: "integer", nullable: false),
                    total_amount_usd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    observations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_note", x => x.id_credit_note);
                    table.ForeignKey(
                        name: "FK_credit_note_customer_id_customer",
                        column: x => x.id_customer,
                        principalTable: "customer",
                        principalColumn: "id_customer",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_note_delivery_note_id_delivery_note",
                        column: x => x.id_delivery_note,
                        principalTable: "delivery_note",
                        principalColumn: "id_delivery_note",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_note_seller_id_seller",
                        column: x => x.id_seller,
                        principalTable: "seller",
                        principalColumn: "id_seller",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credit_note_detail",
                columns: table => new
                {
                    id_credit_note_detail = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_credit_note = table.Column<int>(type: "integer", nullable: false),
                    id_product = table.Column<int>(type: "integer", nullable: true),
                    id_promotion = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price_usd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    subtotal_usd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_note_detail", x => x.id_credit_note_detail);
                    table.ForeignKey(
                        name: "FK_credit_note_detail_credit_note_id_credit_note",
                        column: x => x.id_credit_note,
                        principalTable: "credit_note",
                        principalColumn: "id_credit_note",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_credit_note_detail_product_id_product",
                        column: x => x.id_product,
                        principalTable: "product",
                        principalColumn: "id_product",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_note_detail_promotion_id_promotion",
                        column: x => x.id_promotion,
                        principalTable: "promotion",
                        principalColumn: "id_promotion",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_id_customer",
                table: "credit_note",
                column: "id_customer");

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_id_delivery_note",
                table: "credit_note",
                column: "id_delivery_note");

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_id_seller",
                table: "credit_note",
                column: "id_seller");

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_note_number",
                table: "credit_note",
                column: "note_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_detail_id_credit_note",
                table: "credit_note_detail",
                column: "id_credit_note");

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_detail_id_product",
                table: "credit_note_detail",
                column: "id_product");

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_detail_id_promotion",
                table: "credit_note_detail",
                column: "id_promotion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_note_detail");

            migrationBuilder.DropTable(
                name: "credit_note");
        }
    }
}
