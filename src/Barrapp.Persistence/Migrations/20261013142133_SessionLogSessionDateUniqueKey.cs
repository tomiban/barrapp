using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barrapp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SessionLogSessionDateUniqueKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionLogs_UserId_Kind_MesocycleId_MicrocycleNumber_SessionDay",
                table: "SessionLogs");

            // Antes de esta migración dos sesiones sueltas del mismo día cabían en la tabla; la
            // clave de ADR-0014 las trata como la misma sesión, así que se queda la más reciente
            // (última escritura gana) antes de imponer la unicidad.
            migrationBuilder.Sql(
                "DELETE FROM SessionLogs " +
                "WHERE Kind = 1 AND RowId NOT IN " +
                "(SELECT MAX(RowId) FROM SessionLogs WHERE Kind = 1 GROUP BY UserId, SessionDate);");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_UserId_Kind_SessionDate",
                table: "SessionLogs",
                columns: new[] { "UserId", "Kind", "SessionDate" },
                unique: true,
                filter: "\"Kind\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_UserId_Kind_SessionDate_MesocycleId_MicrocycleNumber_SessionDay",
                table: "SessionLogs",
                columns: new[] { "UserId", "Kind", "SessionDate", "MesocycleId", "MicrocycleNumber", "SessionDay" },
                unique: true,
                filter: "\"Kind\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionLogs_UserId_Kind_SessionDate",
                table: "SessionLogs");

            migrationBuilder.DropIndex(
                name: "IX_SessionLogs_UserId_Kind_SessionDate_MesocycleId_MicrocycleNumber_SessionDay",
                table: "SessionLogs");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_UserId_Kind_MesocycleId_MicrocycleNumber_SessionDay",
                table: "SessionLogs",
                columns: new[] { "UserId", "Kind", "MesocycleId", "MicrocycleNumber", "SessionDay" },
                unique: true,
                filter: "\"Kind\" = 0");
        }
    }
}
