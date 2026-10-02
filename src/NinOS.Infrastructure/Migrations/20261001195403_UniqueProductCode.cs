using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UniqueProductCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Esta base ya tiene un indice unico sobre product_code llamado uq_product_code,
            // creado a mano. Crear otro con el nombre de EF seria redundante, asi que solo
            // se agrega si de verdad no existe ninguno.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                          FROM pg_indexes
                         WHERE schemaname = 'public'
                           AND tablename = 'product'
                           AND indexdef ILIKE '%UNIQUE%'
                           AND indexdef ILIKE '%product_code%'
                    ) THEN
                        CREATE UNIQUE INDEX IX_product_product_code ON product (product_code);
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Solo se baja el indice si es el de EF: uq_product_code es anterior a esta
            // migracion y no le corresponde.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_indexes
                         WHERE schemaname = 'public'
                           AND tablename = 'product'
                           AND indexname = 'IX_product_product_code'
                    ) THEN
                        DROP INDEX IX_product_product_code;
                    END IF;
                END $$;");
        }
    }
}
