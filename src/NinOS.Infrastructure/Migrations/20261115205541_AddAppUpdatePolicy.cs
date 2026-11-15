using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUpdatePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_update_policy",
                columns: table => new
                {
                    id_policy = table.Column<int>(type: "integer", nullable: false),
                    minimum_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    recommended_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notice_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notice_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_update_policy", x => x.id_policy);
                    table.CheckConstraint("ck_app_update_policy_single_row", "id_policy = 1");
                });

            migrationBuilder.Sql(
                "INSERT INTO app_update_policy (id_policy, minimum_version, recommended_version, notice_at_utc, notice_message, updated_at_utc) " +
                "VALUES (1, '1.0.0', '1.0.9', NULL, NULL, now()) ON CONFLICT (id_policy) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_update_policy");
        }
    }
}
