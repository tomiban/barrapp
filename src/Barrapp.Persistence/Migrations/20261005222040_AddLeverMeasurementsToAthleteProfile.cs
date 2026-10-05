using Barrapp.Domain.Athlete;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barrapp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeverMeasurementsToAthleteProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ArmSpanCentimeters",
                table: "AthleteProfiles",
                type: "REAL",
                nullable: false,
                defaultValue: AthleteProfile.MinArmSpanCentimeters);

            migrationBuilder.AddColumn<double>(
                name: "InseamCentimeters",
                table: "AthleteProfiles",
                type: "REAL",
                nullable: false,
                defaultValue: AthleteProfile.MinInseamCentimeters);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArmSpanCentimeters",
                table: "AthleteProfiles");

            migrationBuilder.DropColumn(
                name: "InseamCentimeters",
                table: "AthleteProfiles");
        }
    }
}
