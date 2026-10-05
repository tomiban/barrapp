using Barrapp.Domain.Athlete;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barrapp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingDaysToAthleteProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrainingDays",
                table: "AthleteProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: AthleteProfile.MinTrainingDays);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrainingDays",
                table: "AthleteProfiles");
        }
    }
}
