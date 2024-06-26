using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoMultiplayerBot.Database.Bot.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministratorProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Administrator",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Administrator",
                table: "Users");
        }
    }
}
