using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barrapp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMesocycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Mesocycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SkillId = table.Column<string>(type: "TEXT", nullable: false),
                    TrainingDays = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Snapshot = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mesocycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mesocycles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mesocycles_UserId",
                table: "Mesocycles",
                column: "UserId",
                unique: true,
                filter: "Status = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mesocycles");
        }
    }
}
