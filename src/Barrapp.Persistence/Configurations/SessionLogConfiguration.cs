using Barrapp.Domain.Sessions;
using Barrapp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barrapp.Persistence.Configurations;

/// <summary>
/// Mapeo EF Core del registro de sesión (ADR-0014). La cabecera vive en <c>SessionLogs</c> y cada
/// ítem con su foto en <c>SessionLogItems</c>; las series son objetos valor owned del ítem, en
/// <c>SessionLogSets</c>, con la clave (ítem, número de serie) que garantiza una fila por serie.
/// </summary>
/// <remarks>
/// <para>
/// No hay claves foráneas al plan: <c>MesocycleId</c> es un valor (ADR-0012, ADR-0014). La única
/// clave foránea del registro es la del usuario y la de sus ítems.
/// </para>
/// <para>
/// La clave de sesión determinista (tipo, mesociclo, microciclo y día) es única por atleta para las
/// sesiones de mesociclo —el índice filtrado solo las cubre—, de modo que la misma sesión no se
/// registre dos veces por la misma vía. Las sesiones sueltas no la usan: su identidad es la que les
/// da el cliente (#93).
/// </para>
/// <para>
/// Idempotencia de la outbox offline (ADR-0003): un ítem tiene a lo sumo un <c>ClientId</c>, con un
/// índice único filtrado sobre la columna; el índice único (sesión, ejercicio) garantiza un ítem por
/// ejercicio en la sesión, que es la granularidad con la que la app registra.
/// </para>
/// </remarks>
internal sealed class SessionLogConfiguration : IEntityTypeConfiguration<SessionLog>
{
    public void Configure(EntityTypeBuilder<SessionLog> builder)
    {
        builder.ToTable("SessionLogs");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.UserId).IsRequired();
        builder.Property(log => log.Kind).IsRequired();
        builder.Property(log => log.SessionDate).IsRequired();
        builder.Property(log => log.MesocycleId);
        builder.Property(log => log.MicrocycleNumber);
        builder.Property(log => log.SessionDay);
        builder.Property(log => log.RecordedAtUtc).IsRequired();
        builder.Property(log => log.CompletedAtUtc);

        builder.HasIndex(log => log.UserId);

        // La adherencia y el historial leen por fecha (spec 0003, pantallas Inicio e Historial).
        builder.HasIndex(log => new { log.UserId, log.SessionDate });

        // El cierre del mesociclo y el ajuste de máximos leen por mesociclo; no escanean la tabla.
        builder.HasIndex(log => log.MesocycleId);

        // Clave de sesión determinista: una sesión de mesociclo por (atleta, mesociclo, microciclo,
        // día). El filtro deja fuera las sueltas, que no llevan esa clave (#93).
        builder.HasIndex(log => new { log.UserId, log.Kind, log.MesocycleId, log.MicrocycleNumber, log.SessionDay })
            .IsUnique()
            .HasFilter($"\"Kind\" = {(int)SessionLogKind.Mesocycle}");

        // Los ítems son parte del registro: tabla propia, una fila por ejercicio en la sesión.
        builder.HasMany(log => log.Items)
            .WithOne()
            .HasForeignKey(item => item.SessionLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
