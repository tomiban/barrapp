using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barrapp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingWeekdaysToAthleteProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingWeekdays",
                columns: table => new
                {
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    AthleteProfileId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingWeekdays", x => new { x.AthleteProfileId, x.Day });
                    table.ForeignKey(
                        name: "FK_TrainingWeekdays_AthleteProfiles_AthleteProfileId",
                        column: x => x.AthleteProfileId,
                        principalTable: "AthleteProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Los perfiles que ya existían solo guardaban cuántos días entrenaban, no cuáles. Se
            // rellenan con los días por defecto de su frecuencia (TrainingWeekday.DefaultFor): 3 →
            // lunes/miércoles/viernes, 4 → lunes/martes/jueves/viernes y 5 de lunes a viernes, con
            // DayOfWeek numérico (lunes = 1).
            migrationBuilder.Sql(
                """
                INSERT INTO TrainingWeekdays (Day, AthleteProfileId)
                SELECT 1, Id FROM AthleteProfiles WHERE TrainingDays = 3
                UNION ALL SELECT 3, Id FROM AthleteProfiles WHERE TrainingDays = 3
                UNION ALL SELECT 5, Id FROM AthleteProfiles WHERE TrainingDays = 3
                UNION ALL SELECT 1, Id FROM AthleteProfiles WHERE TrainingDays = 4
                UNION ALL SELECT 2, Id FROM AthleteProfiles WHERE TrainingDays = 4
                UNION ALL SELECT 4, Id FROM AthleteProfiles WHERE TrainingDays = 4
                UNION ALL SELECT 5, Id FROM AthleteProfiles WHERE TrainingDays = 4
                UNION ALL SELECT 1, Id FROM AthleteProfiles WHERE TrainingDays = 5
                UNION ALL SELECT 2, Id FROM AthleteProfiles WHERE TrainingDays = 5
                UNION ALL SELECT 3, Id FROM AthleteProfiles WHERE TrainingDays = 5
                UNION ALL SELECT 4, Id FROM AthleteProfiles WHERE TrainingDays = 5
                UNION ALL SELECT 5, Id FROM AthleteProfiles WHERE TrainingDays = 5;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingWeekdays");
        }
    }
}
