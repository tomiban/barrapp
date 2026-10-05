using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barrapp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AthleteProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeightKilograms = table.Column<double>(type: "REAL", nullable: false),
                    HeightCentimeters = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AthleteProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AthleteProfiles_UserId",
                table: "AthleteProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AthleteProfiles");
        }
    }
}
