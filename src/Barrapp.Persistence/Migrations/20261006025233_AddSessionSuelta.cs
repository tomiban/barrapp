using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barrapp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionSuelta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionSuelta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TimeMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Energy = table.Column<int>(type: "INTEGER", nullable: false),
                    Focus = table.Column<int>(type: "INTEGER", nullable: false),
                    Pattern = table.Column<int>(type: "INTEGER", nullable: true),
                    SkillId = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionSuelta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionSuelta_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionSueltaItems",
                columns: table => new
                {
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionSueltaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExerciseId = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    Pattern = table.Column<int>(type: "INTEGER", nullable: true),
                    Sets = table.Column<int>(type: "INTEGER", nullable: false),
                    RepsMin = table.Column<int>(type: "INTEGER", nullable: true),
                    RepsMax = table.Column<int>(type: "INTEGER", nullable: true),
                    HoldSecondsMin = table.Column<int>(type: "INTEGER", nullable: true),
                    HoldSecondsMax = table.Column<int>(type: "INTEGER", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionSueltaItems", x => new { x.SessionSueltaId, x.Position });
                    table.ForeignKey(
                        name: "FK_SessionSueltaItems_SessionSuelta_SessionSueltaId",
                        column: x => x.SessionSueltaId,
                        principalTable: "SessionSuelta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionSuelta_UserId",
                table: "SessionSuelta",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionSueltaItems");

            migrationBuilder.DropTable(
                name: "SessionSuelta");
        }
    }
}
