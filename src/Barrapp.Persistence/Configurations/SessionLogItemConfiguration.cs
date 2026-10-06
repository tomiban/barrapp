using Barrapp.Domain.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barrapp.Persistence.Configurations;

/// <summary>
/// Mapeo EF Core del ítem registrado de una sesión (ADR-0014): la <b>foto por ítem</b> —ejercicio,
/// nombre, papel, patrón, unidad y objetivo de series/reps/segundos, congelados al registrar— más
/// las series realmente ejecutadas, que son objetos valor owned en <c>SessionLogSets</c>.
/// </summary>
/// <remarks>
/// La foto es la que hace el historial legible aunque la base de conocimiento cambie: aquí no se
/// resuelve nada contra el catálogo, se lee lo que se guardó. Un ítem por ejercicio en la sesión
/// (índice único) y, como mucho, un <c>ClientId</c> por ítem para la idempotencia offline.
/// </remarks>
internal sealed class SessionLogItemConfiguration : IEntityTypeConfiguration<SessionLogItem>
{
    public void Configure(EntityTypeBuilder<SessionLogItem> builder)
    {
        builder.ToTable("SessionLogItems");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Position).IsRequired();
        builder.Property(item => item.ExerciseId).IsRequired();
        builder.Property(item => item.ExerciseName).IsRequired();
        builder.Property(item => item.Role).IsRequired();
        builder.Property(item => item.Pattern);
        builder.Property(item => item.Metric).IsRequired();
        builder.Property(item => item.PrescribedSets).IsRequired();
        builder.Property(item => item.RepsMin);
        builder.Property(item => item.RepsMax);
        builder.Property(item => item.HoldSecondsMin);
        builder.Property(item => item.HoldSecondsMax);
        builder.Property(item => item.Note);
        builder.Property(item => item.ClientId);

        // Un solo ítem por ejercicio en la sesión: la app registra ejercicio a ejercicio.
        builder.HasIndex(item => new { item.SessionLogId, item.ExerciseId }).IsUnique();

        // Idempotencia de la outbox offline: un id de cliente identifica un único ítem.
        builder.HasIndex(item => item.ClientId)
            .IsUnique()
            .HasFilter($"{nameof(SessionLogItem.ClientId)} IS NOT NULL");

        // Las series son parte del ítem: una fila por serie, en orden.
        builder.OwnsMany(item => item.Sets, sets =>
        {
            sets.ToTable("SessionLogSets");
            sets.WithOwner().HasForeignKey("SessionLogItemId");
            sets.HasKey("SessionLogItemId", nameof(SessionLogSet.SetNumber));
            sets.Property(set => set.SetNumber).IsRequired();
            sets.Property(set => set.Value).IsRequired();
            sets.Property(set => set.ActualRir);
            sets.Property(set => set.LoadKg);
        });
    }
}
