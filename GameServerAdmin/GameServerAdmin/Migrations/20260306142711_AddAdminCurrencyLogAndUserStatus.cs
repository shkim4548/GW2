using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GameServerAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminCurrencyLogAndUserStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_currency_log",
                columns: table => new
                {
                    admin_currency_log_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admin_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    currency_type = table.Column<int>(type: "integer", nullable: false),
                    change_amount = table.Column<long>(type: "bigint", nullable: false),
                    before_amount = table.Column<long>(type: "bigint", nullable: false),
                    after_amount = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_currency_log", x => x.admin_currency_log_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_currency_log_admin_id",
                table: "admin_currency_log",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_currency_log_user_id",
                table: "admin_currency_log",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_currency_log");
        }
    }
}
