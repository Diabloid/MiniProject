using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaGames.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLastPlayedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPlayedAt",
                table: "UserLibraries",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPlayedAt",
                table: "UserLibraries");
        }
    }
}
