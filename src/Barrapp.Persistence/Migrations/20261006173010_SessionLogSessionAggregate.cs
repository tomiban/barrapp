using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barrapp.Persistence.Migrations
{
    /// <summary>
    /// El registro de sesión pasa a ser la sesión (ADR-0014): la cabecera <c>SessionLogs</c> lleva
    /// su clave de sesión determinista (tipo, fecha, mesociclo, microciclo y día) y su marca de
    /// completada; cada ítem vive en <c>SessionLogItems</c> con su foto por ítem (nombre, papel,
    /// patrón, unidad y objetivo) y sus series en <c>SessionLogSets</c>, con el RIR real —antes
    /// <c>Effort</c>— y el lastre. Sin claves foráneas al plan.
    /// </summary>
    /// <remarks>
    /// Es un <b>rework</b> del registro, no un añadido, y el MVP no tiene datos que migrar: las
    /// tablas anteriores se recrean en lugar de renombrar columnas que ya no significan lo mismo.
    /// </remarks>
    public partial class SessionLogSessionAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionLogSets");

            migrationBuilder.DropTable(
                name: "SessionLogs");

            migrationBuilder.CreateTable(
                name: "SessionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    MesocycleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MicrocycleNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    SessionDay = table.Column<int>(type: "INTEGER", nullable: true),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionLogId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<string>(type: "TEXT", nullable: false),
                    ExerciseName = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    Pattern = table.Column<int>(type: "INTEGER", nullable: true),
                    Metric = table.Column<int>(type: "INTEGER", nullable: false),
                    PrescribedSets = table.Column<int>(type: "INTEGER", nullable: false),
                    RepsMin = table.Column<int>(type: "INTEGER", nullable: true),
                    RepsMax = table.Column<int>(type: "INTEGER", nullable: true),
                    HoldSecondsMin = table.Column<int>(type: "INTEGER", nullable: true),
                    HoldSecondsMax = table.Column<int>(type: "INTEGER", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    ClientId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogItems_SessionLogs_SessionLogId",
                        column: x => x.SessionLogId,
                        principalTable: "SessionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogSets",
                columns: table => new
                {
                    SessionLogItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SetNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualRir = table.Column<int>(type: "INTEGER", nullable: true),
                    LoadKg = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogSets", x => new { x.SessionLogItemId, x.SetNumber });
                    table.ForeignKey(
                        name: "FK_SessionLogSets_SessionLogItems_SessionLogItemId",
                        column: x => x.SessionLogItemId,
                        principalTable: "SessionLogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_MesocycleId",
                table: "SessionLogs",
                column: "MesocycleId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_UserId",
                table: "SessionLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_UserId_SessionDate",
                table: "SessionLogs",
                columns: new[] { "UserId", "SessionDate" });

            // Clave de sesión determinista: una sesión de mesociclo por (atleta, mesociclo,
            // microciclo y día). El filtro deja fuera las sueltas, que no llevan esa clave (#93).
            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_UserId_Kind_MesocycleId_MicrocycleNumber_SessionDay",
                table: "SessionLogs",
                columns: new[] { "UserId", "Kind", "MesocycleId", "MicrocycleNumber", "SessionDay" },
                unique: true,
                filter: "\"Kind\" = 0");

            // Idempotencia de la outbox offline: un id de cliente identifica un único ítem.
            migrationBuilder.CreateIndex(
                name: "IX_SessionLogItems_ClientId",
                table: "SessionLogItems",
                column: "ClientId",
                unique: true,
                filter: "ClientId IS NOT NULL");

            // Un solo ítem por ejercicio en la sesión: es la granularidad con la que se registra.
            migrationBuilder.CreateIndex(
                name: "IX_SessionLogItems_SessionLogId_ExerciseId",
                table: "SessionLogItems",
                columns: new[] { "SessionLogId", "ExerciseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionLogSets");

            migrationBuilder.DropTable(
                name: "SessionLogItems");

            migrationBuilder.DropTable(
                name: "SessionLogs");
        }
    }
}
