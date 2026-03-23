using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GameServerAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddGachaSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gacha_history",
                columns: table => new
                {
                    gacha_history_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    gacha_pool_id = table.Column<long>(type: "bigint", nullable: false),
                    gacha_item_id = table.Column<long>(type: "bigint", nullable: false),
                    pull_type = table.Column<int>(type: "integer", nullable: false),
                    is_duplicate = table.Column<bool>(type: "boolean", nullable: false),
                    compensation_amount = table.Column<int>(type: "integer", nullable: false),
                    pulled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gacha_history", x => x.gacha_history_id);
                });

            migrationBuilder.CreateTable(
                name: "gacha_item",
                columns: table => new
                {
                    gacha_item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    grade = table.Column<int>(type: "integer", nullable: false),
                    duplicate_compensation = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gacha_item", x => x.gacha_item_id);
                });

            migrationBuilder.CreateTable(
                name: "gacha_pool",
                columns: table => new
                {
                    gacha_pool_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    single_pull_cost = table.Column<int>(type: "integer", nullable: false),
                    ten_pull_cost = table.Column<int>(type: "integer", nullable: false),
                    pity_threshold = table.Column<int>(type: "integer", nullable: false),
                    pity_grade = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gacha_pool", x => x.gacha_pool_id);
                });

            migrationBuilder.CreateTable(
                name: "player_gacha_currency",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_gacha_currency", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "player_gacha_pity",
                columns: table => new
                {
                    player_gacha_pity_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    gacha_pool_id = table.Column<long>(type: "bigint", nullable: false),
                    pull_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_gacha_pity", x => x.player_gacha_pity_id);
                });

            migrationBuilder.CreateTable(
                name: "player_gacha_inventory",
                columns: table => new
                {
                    player_gacha_inventory_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    gacha_item_id = table.Column<long>(type: "bigint", nullable: false),
                    acquired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_gacha_inventory", x => x.player_gacha_inventory_id);
                    table.ForeignKey(
                        name: "FK_player_gacha_inventory_gacha_item_gacha_item_id",
                        column: x => x.gacha_item_id,
                        principalTable: "gacha_item",
                        principalColumn: "gacha_item_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gacha_pool_item",
                columns: table => new
                {
                    gacha_pool_item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gacha_pool_id = table.Column<long>(type: "bigint", nullable: false),
                    gacha_item_id = table.Column<long>(type: "bigint", nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gacha_pool_item", x => x.gacha_pool_item_id);
                    table.ForeignKey(
                        name: "FK_gacha_pool_item_gacha_item_gacha_item_id",
                        column: x => x.gacha_item_id,
                        principalTable: "gacha_item",
                        principalColumn: "gacha_item_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gacha_pool_item_gacha_pool_gacha_pool_id",
                        column: x => x.gacha_pool_id,
                        principalTable: "gacha_pool",
                        principalColumn: "gacha_pool_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gacha_history_pulled_at",
                table: "gacha_history",
                column: "pulled_at");

            migrationBuilder.CreateIndex(
                name: "IX_gacha_history_user_id",
                table: "gacha_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gacha_pool_item_gacha_item_id",
                table: "gacha_pool_item",
                column: "gacha_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_gacha_pool_item_pool_id",
                table: "gacha_pool_item",
                column: "gacha_pool_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_gacha_inventory_gacha_item_id",
                table: "player_gacha_inventory",
                column: "gacha_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_gacha_inventory_user_id",
                table: "player_gacha_inventory",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_gacha_inventory_user_item",
                table: "player_gacha_inventory",
                columns: new[] { "user_id", "gacha_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_player_gacha_pity_user_pool",
                table: "player_gacha_pity",
                columns: new[] { "user_id", "gacha_pool_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gacha_history");

            migrationBuilder.DropTable(
                name: "gacha_pool_item");

            migrationBuilder.DropTable(
                name: "player_gacha_currency");

            migrationBuilder.DropTable(
                name: "player_gacha_inventory");

            migrationBuilder.DropTable(
                name: "player_gacha_pity");

            migrationBuilder.DropTable(
                name: "gacha_pool");

            migrationBuilder.DropTable(
                name: "gacha_item");
        }
    }
}
