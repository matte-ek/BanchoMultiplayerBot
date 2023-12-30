using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoMultiplayerBot.Database.Bot.Migrations
{
    /// <inheritdoc />
    public partial class AddMapTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BeatmapId = table.Column<long>(type: "INTEGER", nullable: false),
                    BeatmapSetId = table.Column<long>(type: "INTEGER", nullable: false),
                    BeatmapName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    BeatmapArtist = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DifficultyName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    StarRating = table.Column<float>(type: "REAL", nullable: true),
                    TimesPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    AveragePassPercentage = table.Column<float>(type: "REAL", nullable: true),
                    AverageLeavePercentage = table.Column<float>(type: "REAL", nullable: true),
                    LastPlayed = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Maps");
        }
    }
}
