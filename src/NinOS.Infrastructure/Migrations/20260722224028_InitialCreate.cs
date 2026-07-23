using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer",
                columns: table => new
                {
                    id_customer = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer", x => x.id_customer);
                });

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    id_product = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unit_price_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    stock_quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product", x => x.id_product);
                });

            migrationBuilder.CreateTable(
                name: "seller",
                columns: table => new
                {
                    id_seller = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    seller_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seller", x => x.id_seller);
                });

            migrationBuilder.CreateTable(
                name: "delivery_note",
                columns: table => new
                {
                    id_delivery_note = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    note_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    creation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_seller = table.Column<int>(type: "integer", nullable: false),
                    id_customer = table.Column<int>(type: "integer", nullable: false),
                    total_amount_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delivery_note", x => x.id_delivery_note);
                    table.ForeignKey(
                        name: "FK_delivery_note_customer_id_customer",
                        column: x => x.id_customer,
                        principalTable: "customer",
                        principalColumn: "id_customer",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_note_seller_id_seller",
                        column: x => x.id_seller,
                        principalTable: "seller",
                        principalColumn: "id_seller",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commission",
                columns: table => new
                {
                    id_commission = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_seller = table.Column<int>(type: "integer", nullable: false),
                    id_delivery_note = table.Column<int>(type: "integer", nullable: false),
                    commission_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    amount_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    payout_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commission", x => x.id_commission);
                    table.ForeignKey(
                        name: "FK_commission_delivery_note_id_delivery_note",
                        column: x => x.id_delivery_note,
                        principalTable: "delivery_note",
                        principalColumn: "id_delivery_note",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_commission_seller_id_seller",
                        column: x => x.id_seller,
                        principalTable: "seller",
                        principalColumn: "id_seller",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "note_detail",
                columns: table => new
                {
                    id_note_detail = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_delivery_note = table.Column<int>(type: "integer", nullable: false),
                    id_product = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    subtotal_usd = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_detail", x => x.id_note_detail);
                    table.ForeignKey(
                        name: "FK_note_detail_delivery_note_id_delivery_note",
                        column: x => x.id_delivery_note,
                        principalTable: "delivery_note",
                        principalColumn: "id_delivery_note",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_note_detail_product_id_product",
                        column: x => x.id_product,
                        principalTable: "product",
                        principalColumn: "id_product",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment",
                columns: table => new
                {
                    id_payment = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_delivery_note = table.Column<int>(type: "integer", nullable: false),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    amount_bs = table.Column<decimal>(type: "numeric", nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment", x => x.id_payment);
                    table.ForeignKey(
                        name: "FK_payment_delivery_note_id_delivery_note",
                        column: x => x.id_delivery_note,
                        principalTable: "delivery_note",
                        principalColumn: "id_delivery_note",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commission_id_delivery_note",
                table: "commission",
                column: "id_delivery_note");

            migrationBuilder.CreateIndex(
                name: "IX_commission_id_seller",
                table: "commission",
                column: "id_seller");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_id_customer",
                table: "delivery_note",
                column: "id_customer");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_id_seller",
                table: "delivery_note",
                column: "id_seller");

            migrationBuilder.CreateIndex(
                name: "IX_note_detail_id_delivery_note",
                table: "note_detail",
                column: "id_delivery_note");

            migrationBuilder.CreateIndex(
                name: "IX_note_detail_id_product",
                table: "note_detail",
                column: "id_product");

            migrationBuilder.CreateIndex(
                name: "IX_payment_id_delivery_note",
                table: "payment",
                column: "id_delivery_note");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commission");

            migrationBuilder.DropTable(
                name: "note_detail");

            migrationBuilder.DropTable(
                name: "payment");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "delivery_note");

            migrationBuilder.DropTable(
                name: "customer");

            migrationBuilder.DropTable(
                name: "seller");
        }
    }
}
