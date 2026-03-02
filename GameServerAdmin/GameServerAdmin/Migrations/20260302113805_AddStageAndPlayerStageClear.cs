using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GameServerAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddStageAndPlayerStageClear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_stage_clear",
                columns: table => new
                {
                    player_stage_clear_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    stage_id = table.Column<int>(type: "integer", nullable: false),
                    cleared_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_stage_clear", x => x.player_stage_clear_id);
                });

            migrationBuilder.CreateTable(
                name: "stage",
                columns: table => new
                {
                    stage_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    required_stamina = table.Column<int>(type: "integer", nullable: false),
                    reward_gold = table.Column<long>(type: "bigint", nullable: false),
                    reward_gem = table.Column<long>(type: "bigint", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stage", x => x.stage_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_stage_clear_user_stage",
                table: "player_stage_clear",
                columns: new[] { "user_id", "stage_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_stage_clear");

            migrationBuilder.DropTable(
                name: "stage");
        }
    }
}
