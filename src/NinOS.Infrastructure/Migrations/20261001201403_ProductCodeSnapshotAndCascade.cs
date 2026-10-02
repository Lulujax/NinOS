using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductCodeSnapshotAndCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_product_deleted_cascade",
                table: "promotion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "product_code_snapshot",
                table: "note_detail",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "product_code_snapshot",
                table: "credit_note_detail",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_promotion_id_product_deleted_cascade",
                table: "promotion",
                column: "id_product_deleted_cascade");

            migrationBuilder.AddForeignKey(
                name: "FK_promotion_product_id_product_deleted_cascade",
                table: "promotion",
                column: "id_product_deleted_cascade",
                principalTable: "product",
                principalColumn: "id_product",
                onDelete: ReferentialAction.SetNull);

            // --- Backfills ---
            //
            // Las notas ya emitidas nunca guardaron el codigo del producto, asi que la unica
            // aproximacion posible es el codigo que tiene hoy. A partir de esta migracion si
            // queda congelado. Los renglones de promocion se dejan en null a proposito.
            migrationBuilder.Sql(@"
                UPDATE note_detail d
                   SET product_code_snapshot = p.product_code
                  FROM product p
                 WHERE d.id_product IS NOT NULL
                   AND d.product_code_snapshot IS NULL
                   AND p.id_product = d.id_product;");

            migrationBuilder.Sql(@"
                UPDATE credit_note_detail d
                   SET product_code_snapshot = p.product_code
                  FROM product p
                 WHERE d.id_product IS NOT NULL
                   AND d.product_code_snapshot IS NULL
                   AND p.id_product = d.id_product;");

            // Las promociones que se fueron en cascada con un producto se identificaban
            // comparando el texto del motivo contra el codigo. Se rellena la clave foranea
            // con ese mismo texto para que restores las sigan encontrar. Sin este paso,
            // esas promociones se perderian al cambiar el producto de marca.
            migrationBuilder.Sql(@"
                UPDATE promotion pr
                   SET id_product_deleted_cascade = p.id_product
                  FROM product p
                 WHERE NOT pr.is_active
                   AND pr.id_product_deleted_cascade IS NULL
                   AND pr.deleted_reason IS NOT NULL
                   AND pr.deleted_reason = 'Eliminado junto con el producto ' || p.product_code;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_promotion_product_id_product_deleted_cascade",
                table: "promotion");

            migrationBuilder.DropIndex(
                name: "IX_promotion_id_product_deleted_cascade",
                table: "promotion");

            migrationBuilder.DropColumn(
                name: "id_product_deleted_cascade",
                table: "promotion");

            migrationBuilder.DropColumn(
                name: "product_code_snapshot",
                table: "note_detail");

            migrationBuilder.DropColumn(
                name: "product_code_snapshot",
                table: "credit_note_detail");

            // Los backfills son de una sola via: deshacer las columnas tambien borra
            // el dato que rellenaron.
        }
    }
}
