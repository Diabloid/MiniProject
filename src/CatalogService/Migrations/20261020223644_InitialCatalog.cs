using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CatalogService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CoverUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IconUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GenresJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DownloadSize = table.Column<double>(type: "double", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Динамічні екшн-ігри", "Action" },
                    { 2, "Рольові ігри з глибоким сюжетом", "RPG" },
                    { 3, "Тактичні та стратегічні ігри", "Strategy" },
                    { 4, "Ігри від незалежних розробників", "Indie" }
                });

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "CategoryId", "CoverUrl", "CreatedAt", "Description", "Discount", "DownloadSize", "GenresJson", "IconUrl", "IsActive", "ItemType", "Price", "StockQuantity", "Title" },
                values: new object[,]
                {
                    { 1, 2, "/Assets/default_game.png", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Пригодницький рольовий бойовик у відкритому світі темного майбутнього.", 15.00m, 70.5, "[\"RPG\", \"Sci-Fi\", \"Action\"]", "/Assets/default_icon.png", true, "Digital", 1199.00m, null, "Cyberpunk 2077: Phantom Liberty" },
                    { 2, 2, "/Assets/default_game.png", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Сюжетна рольова гра нового покоління у фантастичному відкритому всесвіті.", 50.00m, 50.0, "[\"RPG\", \"Open World\", \"Fantasy\"]", "/Assets/default_icon.png", true, "Digital", 799.00m, null, "The Witcher 3: Wild Hunt - Complete Edition" }
                });

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "CategoryId", "CoverUrl", "CreatedAt", "Description", "DownloadSize", "GenresJson", "IconUrl", "IsActive", "ItemType", "Price", "StockQuantity", "Title" },
                values: new object[,]
                {
                    { 3, 1, "/Assets/default_game.png", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Армії пекла вторглися на Землю. Станьте Катом Року у битві за порятунок людства.", 0.0, "[\"Action\", \"Shooter\", \"First-Person\"]", "/Assets/default_icon.png", true, "Physical", 999.00m, 25, "DOOM Eternal: Deluxe Edition" },
                    { 4, 4, "/Assets/default_game.png", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Досліджуйте розкішне королівство, де панують шовк та пісня.", 8.5, "[\"Indie\", \"Metroidvania\", \"Souls-like\"]", "/Assets/default_icon.png", true, "Digital", 450.00m, null, "Hollow Knight: Silksong" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoryId",
                table: "Items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_IsActive_CategoryId",
                table: "Items",
                columns: new[] { "IsActive", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_Title",
                table: "Items",
                column: "Title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
