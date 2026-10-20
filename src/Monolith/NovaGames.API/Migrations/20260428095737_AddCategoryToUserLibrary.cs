using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaGames.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToUserLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "UserLibraries",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "UserLibraries");
        }
    }
}
