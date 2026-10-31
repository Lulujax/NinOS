using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToPriceList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "price_list",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "price_list");
        }
    }
}
