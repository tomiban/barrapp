using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barrapp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLogClientIdAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                table: "SessionLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_MesocycleId",
                table: "SessionLogs",
                column: "MesocycleId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_UserId_ClientId",
                table: "SessionLogs",
                columns: new[] { "UserId", "ClientId" },
                unique: true,
                filter: "ClientId IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionLogs_MesocycleId",
                table: "SessionLogs");

            migrationBuilder.DropIndex(
                name: "IX_SessionLogs_UserId_ClientId",
                table: "SessionLogs");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "SessionLogs");
        }
    }
}
