using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoMultiplayerBot.Database.Bot.Migrations
{
    /// <inheritdoc />
    public partial class ChangePlayerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
             *  Create a new Users table and copy all the data
             */
            
            migrationBuilder.RenameTable(
                name: "Users",
                newName: "UsersGuid");
            
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Playtime = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchesPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberOneResults = table.Column<int>(type: "INTEGER", nullable: false),
                    Administrator = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    AutoSkipEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.Sql("INSERT INTO Users (UserId, Name, Playtime, MatchesPlayed, NumberOneResults, Administrator, AutoSkipEnabled) SELECT UserId, Name, Playtime, MatchesPlayed, NumberOneResults, Administrator, AutoSkipEnabled FROM UsersGuid");
            
            /*
             *  Use the new Users table with PlayerBans
             */
            
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerBans_Users_UserId",
                table: "PlayerBans");
            
            migrationBuilder.DropIndex(
                "IX_PlayerBans_UserId",
                "PlayerBans");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "PlayerBans",
                newName: "PlayerIdGuid");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "PlayerBans",
                type: "INTEGER",
                defaultValue: 0,
                nullable: false);

            // Set the new id by matching the old guid
            migrationBuilder.Sql(
                "UPDATE PlayerBans SET UserId=Users.Id FROM Users WHERE Name = (SELECT Name FROM UsersGuid WHERE Id = PlayerBans.PlayerIdGuid)");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerBans",
                table: "PlayerBans");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerBans",
                table: "PlayerBans",
                column: "UserId");
            
            migrationBuilder.AddForeignKey(
                name: "FK_PlayerBans_Users_UserId",
                table: "PlayerBans",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
            
            migrationBuilder.CreateIndex(
                name: "IX_PlayerBans_UserId",
                table: "PlayerBans",
                column: "UserId");

            migrationBuilder.DropColumn(
                name: "PlayerIdGuid",
                table: "PlayerBans");
            
            // Dropping this here will erase everything in PlayerBans for whatever reason,
            // even though I can just drop it manually afterwards without any problems.
            
            //migrationBuilder.DropTable(
            //    name: "UsersGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
