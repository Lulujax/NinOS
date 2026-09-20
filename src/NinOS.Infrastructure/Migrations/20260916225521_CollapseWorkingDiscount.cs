using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CollapseWorkingDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El sistema (CxC) trabaja un único DCTO: se compacta condición + volumen en discount_percentage.
            // El detalle separado original queda congelado en original_discount_percentage / original_volume_discount_percentage.
            migrationBuilder.Sql(
                @"UPDATE delivery_note
                  SET discount_percentage = COALESCE(discount_percentage, 0) + COALESCE(volume_discount_percentage, 0),
                      volume_discount_percentage = NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Migración de datos de un solo sentido: no se revierte.
        }
    }
}
