using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_movement",
                columns: table => new
                {
                    id_stock_movement = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    movement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_product = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    document_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    document_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    id_delivery_note = table.Column<int>(type: "integer", nullable: true),
                    id_credit_note = table.Column<int>(type: "integer", nullable: true),
                    id_seller = table.Column<int>(type: "integer", nullable: true),
                    id_customer = table.Column<int>(type: "integer", nullable: true),
                    id_promotion = table.Column<int>(type: "integer", nullable: true),
                    promotion_units = table.Column<int>(type: "integer", nullable: true),
                    sold_as = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    line_description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    unit_price_usd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movement", x => x.id_stock_movement);
                    table.ForeignKey(
                        name: "FK_stock_movement_credit_note_id_credit_note",
                        column: x => x.id_credit_note,
                        principalTable: "credit_note",
                        principalColumn: "id_credit_note",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movement_customer_id_customer",
                        column: x => x.id_customer,
                        principalTable: "customer",
                        principalColumn: "id_customer",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movement_delivery_note_id_delivery_note",
                        column: x => x.id_delivery_note,
                        principalTable: "delivery_note",
                        principalColumn: "id_delivery_note",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movement_product_id_product",
                        column: x => x.id_product,
                        principalTable: "product",
                        principalColumn: "id_product",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movement_promotion_id_promotion",
                        column: x => x.id_promotion,
                        principalTable: "promotion",
                        principalColumn: "id_promotion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movement_seller_id_seller",
                        column: x => x.id_seller,
                        principalTable: "seller",
                        principalColumn: "id_seller",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_id_credit_note",
                table: "stock_movement",
                column: "id_credit_note");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_id_customer",
                table: "stock_movement",
                column: "id_customer");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_id_delivery_note",
                table: "stock_movement",
                column: "id_delivery_note");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_id_product",
                table: "stock_movement",
                column: "id_product");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_id_promotion",
                table: "stock_movement",
                column: "id_promotion");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_id_seller",
                table: "stock_movement",
                column: "id_seller");

            // Backfill: reconstruye el kardex con lo que ya existe, para que el historial
            // de cada producto siga mostrando las mismas ventas de siempre y ademas
            // aparezcan las notas de credito (obsequio = SALIDA, devolucion = ENTRADA).
            // Las notas de entrega anuladas generan ENTRADA, igual que antes se deducía
            // del estado de la nota.
            migrationBuilder.Sql(@"
INSERT INTO stock_movement
    (movement_date, id_product, quantity, movement_type, reason, document_type, document_number,
     document_status, id_delivery_note, id_credit_note, id_seller, id_customer,
     id_promotion, promotion_units, sold_as, line_description, unit_price_usd)
SELECT n.creation_date,
       d.id_product,
       d.quantity,
       CASE WHEN n.status = 'Anulada' THEN 'ENTRADA' ELSE 'SALIDA' END,
       CASE WHEN n.status = 'Anulada' THEN 'ANULACION' ELSE 'VENTA' END,
       'NOTA DE ENTREGA', n.note_number, n.status, n.id_delivery_note, NULL,
       n.id_seller, n.id_customer, NULL, NULL, 'Producto', NULL, d.unit_price_usd
FROM note_detail d
JOIN delivery_note n ON n.id_delivery_note = d.id_delivery_note
WHERE d.id_product IS NOT NULL;
");
            migrationBuilder.Sql(@"
INSERT INTO stock_movement
    (movement_date, id_product, quantity, movement_type, reason, document_type, document_number,
     document_status, id_delivery_note, id_credit_note, id_seller, id_customer,
     id_promotion, promotion_units, sold_as, line_description, unit_price_usd)
SELECT n.creation_date,
       pi.id_product,
       d.quantity * pi.quantity_required,
       CASE WHEN n.status = 'Anulada' THEN 'ENTRADA' ELSE 'SALIDA' END,
       CASE WHEN n.status = 'Anulada' THEN 'ANULACION' ELSE 'VENTA' END,
       'NOTA DE ENTREGA', n.note_number, n.status, n.id_delivery_note, NULL,
       n.id_seller, n.id_customer, d.id_promotion, d.quantity, 'Promocion', p.name,
       CASE WHEN d.quantity > 0 THEN d.unit_price_usd / d.quantity ELSE 0 END
FROM note_detail d
JOIN delivery_note n ON n.id_delivery_note = d.id_delivery_note
JOIN promotion_item pi ON pi.id_promotion = d.id_promotion
LEFT JOIN promotion p ON p.id_promotion = d.id_promotion
WHERE d.id_promotion IS NOT NULL;
");
            migrationBuilder.Sql(@"
INSERT INTO stock_movement
    (movement_date, id_product, quantity, movement_type, reason, document_type, document_number,
     document_status, id_delivery_note, id_credit_note, id_seller, id_customer,
     id_promotion, promotion_units, sold_as, line_description, unit_price_usd)
SELECT c.creation_date,
       d.id_product,
       d.quantity,
       CASE WHEN c.category = 'Obsequio' THEN 'SALIDA' ELSE 'ENTRADA' END,
       CASE WHEN c.category = 'Obsequio' THEN 'OBSEQUIO' ELSE 'DEVOLUCION' END,
       'NOTA DE CREDITO', c.note_number, c.status, c.id_delivery_note, c.id_credit_note,
       c.id_seller, c.id_customer, NULL, NULL, 'Producto', NULL, d.unit_price_usd
FROM credit_note_detail d
JOIN credit_note c ON c.id_credit_note = d.id_credit_note
WHERE d.id_product IS NOT NULL;
");
            migrationBuilder.Sql(@"
INSERT INTO stock_movement
    (movement_date, id_product, quantity, movement_type, reason, document_type, document_number,
     document_status, id_delivery_note, id_credit_note, id_seller, id_customer,
     id_promotion, promotion_units, sold_as, line_description, unit_price_usd)
SELECT c.creation_date,
       pi.id_product,
       d.quantity * pi.quantity_required,
       CASE WHEN c.category = 'Obsequio' THEN 'SALIDA' ELSE 'ENTRADA' END,
       CASE WHEN c.category = 'Obsequio' THEN 'OBSEQUIO' ELSE 'DEVOLUCION' END,
       'NOTA DE CREDITO', c.note_number, c.status, c.id_delivery_note, c.id_credit_note,
       c.id_seller, c.id_customer, d.id_promotion, d.quantity, 'Promocion', p.name,
       CASE WHEN d.quantity > 0 THEN d.unit_price_usd / d.quantity ELSE 0 END
FROM credit_note_detail d
JOIN credit_note c ON c.id_credit_note = d.id_credit_note
JOIN promotion_item pi ON pi.id_promotion = d.id_promotion
LEFT JOIN promotion p ON p.id_promotion = d.id_promotion
WHERE d.id_promotion IS NOT NULL;
");
            // Carga inicial: el stock que cada producto ya tenia antes de que existiera el kardex.
            // Se dated un segundo antes del primer movimiento, para que el historial quede completo
            // y la suma de entradas menos salidas sea igual al stock actual del producto.
            migrationBuilder.Sql(@"
INSERT INTO stock_movement
    (movement_date, id_product, quantity, movement_type, reason, document_type, document_number,
     document_status, id_delivery_note, id_credit_note, id_seller, id_customer,
     id_promotion, promotion_units, sold_as, line_description, unit_price_usd)
SELECT COALESCE(min(m.movement_date) - interval '1 second', now()),
       p.id_product,
       p.stock_quantity - COALESCE(sum(CASE WHEN m.movement_type = 'ENTRADA' THEN m.quantity ELSE -m.quantity END), 0),
       'ENTRADA', 'CARGA INICIAL', 'INVENTARIO', 'INICIAL-' || p.product_code,
       NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'Producto', NULL, p.unit_price_usd
FROM product p
LEFT JOIN stock_movement m ON m.id_product = p.id_product
GROUP BY p.id_product, p.product_code, p.stock_quantity, p.unit_price_usd
HAVING p.stock_quantity - COALESCE(sum(CASE WHEN m.movement_type = 'ENTRADA' THEN m.quantity ELSE -m.quantity END), 0) > 0;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_movement");
        }
    }
}
