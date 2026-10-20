using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaGames.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPublishedToGames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Products",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Products");
        }
    }
}
