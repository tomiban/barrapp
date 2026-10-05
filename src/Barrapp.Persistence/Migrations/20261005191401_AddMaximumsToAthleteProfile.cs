using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barrapp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaximumsToAthleteProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Maximums",
                columns: table => new
                {
                    ExerciseCode = table.Column<string>(type: "TEXT", nullable: false),
                    AthleteProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Repetitions = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maximums", x => new { x.AthleteProfileId, x.ExerciseCode });
                    table.ForeignKey(
                        name: "FK_Maximums_AthleteProfiles_AthleteProfileId",
                        column: x => x.AthleteProfileId,
                        principalTable: "AthleteProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Maximums");
        }
    }
}
