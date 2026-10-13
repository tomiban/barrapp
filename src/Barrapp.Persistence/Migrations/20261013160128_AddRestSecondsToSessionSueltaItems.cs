using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barrapp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRestSecondsToSessionSueltaItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RestSeconds",
                table: "SessionSueltaItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Backfill (#95): las filas guardadas antes de que el descanso existiera quedan con el
            // default de su papel, los mismos valores que `SessionItemRest.DefaultFor`. Los enteros
            // son los ordinales de `SessionItemRole` (Skill = 0, Strength = 1, Core = 2); toda fila
            // existente vale 0 justo después de añadir la columna, porque ninguna la traía.
            migrationBuilder.Sql("""
                UPDATE SessionSueltaItems
                SET RestSeconds = CASE Role
                    WHEN 0 THEN 120
                    WHEN 1 THEN 90
                    WHEN 2 THEN 60
                    ELSE RestSeconds
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RestSeconds",
                table: "SessionSueltaItems");
        }
    }
}
