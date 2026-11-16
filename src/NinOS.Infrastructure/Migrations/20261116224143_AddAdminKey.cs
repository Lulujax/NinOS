using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_key",
                columns: table => new
                {
                    id_key = table.Column<int>(type: "integer", nullable: false),
                    password_salt = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    iterations = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_key", x => x.id_key);
                    table.CheckConstraint("ck_admin_key_single_row", "id_key = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_key");
        }
    }
}
