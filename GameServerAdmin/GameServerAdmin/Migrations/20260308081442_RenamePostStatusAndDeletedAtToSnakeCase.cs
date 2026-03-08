using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameServerAdmin.Migrations
{
    /// <inheritdoc />
    public partial class RenamePostStatusAndDeletedAtToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "post",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "post",
                newName: "deleted_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "status",
                table: "post",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "post",
                newName: "DeletedAt");
        }
    }
}
