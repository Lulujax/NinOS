using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerToSalesGoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sales_goal_goal_month_start",
                table: "sales_goal");

            migrationBuilder.AddColumn<int>(
                name: "id_seller",
                table: "sales_goal",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_goal_goal_month_start",
                table: "sales_goal",
                column: "goal_month_start",
                unique: true,
                filter: "\"id_seller\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_sales_goal_goal_month_start_id_seller",
                table: "sales_goal",
                columns: new[] { "goal_month_start", "id_seller" },
                unique: true,
                filter: "\"id_seller\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_sales_goal_id_seller",
                table: "sales_goal",
                column: "id_seller");

            migrationBuilder.AddForeignKey(
                name: "FK_sales_goal_seller_id_seller",
                table: "sales_goal",
                column: "id_seller",
                principalTable: "seller",
                principalColumn: "id_seller",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sales_goal_seller_id_seller",
                table: "sales_goal");

            migrationBuilder.DropIndex(
                name: "IX_sales_goal_goal_month_start",
                table: "sales_goal");

            migrationBuilder.DropIndex(
                name: "IX_sales_goal_goal_month_start_id_seller",
                table: "sales_goal");

            migrationBuilder.DropIndex(
                name: "IX_sales_goal_id_seller",
                table: "sales_goal");

            migrationBuilder.DropColumn(
                name: "id_seller",
                table: "sales_goal");

            migrationBuilder.CreateIndex(
                name: "IX_sales_goal_goal_month_start",
                table: "sales_goal",
                column: "goal_month_start",
                unique: true);
        }
    }
}
