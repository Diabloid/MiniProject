using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaGames.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDigitalGameModelForZip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DownloadKey",
                table: "Products",
                newName: "GameZipUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GameZipUrl",
                table: "Products",
                newName: "DownloadKey");
        }
    }
}
