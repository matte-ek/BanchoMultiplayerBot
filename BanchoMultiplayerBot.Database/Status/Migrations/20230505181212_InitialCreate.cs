using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoMultiplayerBot.Database.Status.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GamesPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    MessagesSent = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MessagesReceived = table.Column<int>(type: "INTEGER", nullable: false),
                    ApiLookups = table.Column<int>(type: "INTEGER", nullable: false),
                    ApiErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PerformancePointCalculations = table.Column<int>(type: "INTEGER", nullable: false),
                    PerformancePointCalculationErrors = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SuccessfulStatusCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LobbySnapshot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    BotLobbyIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Players = table.Column<int>(type: "INTEGER", nullable: false),
                    GamesPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    HostViolations = table.Column<int>(type: "INTEGER", nullable: false),
                    BotSnapshotId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LobbySnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LobbySnapshot_BotSnapshots_BotSnapshotId",
                        column: x => x.BotSnapshotId,
                        principalTable: "BotSnapshots",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LobbySnapshot_BotSnapshotId",
                table: "LobbySnapshot",
                column: "BotSnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LobbySnapshot");

            migrationBuilder.DropTable(
                name: "StatusSnapshots");

            migrationBuilder.DropTable(
                name: "BotSnapshots");
        }
    }
}
