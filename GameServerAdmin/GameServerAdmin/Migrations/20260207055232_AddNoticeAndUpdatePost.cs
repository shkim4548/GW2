using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GameServerAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddNoticeAndUpdatePost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "post",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "post",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_comment_enabled",
                table: "post",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "view_count",
                table: "post",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "notice",
                columns: table => new
                {
                    notice_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    admin_id = table.Column<long>(type: "bigint", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    display_start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    display_end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    is_comment_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notice", x => x.notice_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notice_admin_id",
                table: "notice",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_notice_category",
                table: "notice",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_notice_created_at",
                table: "notice",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_notice_display_period",
                table: "notice",
                columns: new[] { "display_start_at", "display_end_at" });

            migrationBuilder.CreateIndex(
                name: "IX_notice_status",
                table: "notice",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_notice_status_is_pinned",
                table: "notice",
                columns: new[] { "status", "is_pinned" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notice");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "post");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "post");

            migrationBuilder.DropColumn(
                name: "is_comment_enabled",
                table: "post");

            migrationBuilder.DropColumn(
                name: "view_count",
                table: "post");
        }
    }
}
