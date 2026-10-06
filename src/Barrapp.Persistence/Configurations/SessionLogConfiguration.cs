using Barrapp.Domain.Sessions;
using Barrapp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barrapp.Persistence.Configurations;

/// <summary>
/// Mapeo EF Core del registro de sesión. Cada fila es un ejercicio registrado en una sesión; las
/// series viven en su propia tabla como objetos valor owned, con la clave compuesta
/// (registro, número de serie) que garantiza un valor por serie y su orden.
/// </summary>
internal sealed class SessionLogConfiguration : IEntityTypeConfiguration<SessionLog>
{
    public void Configure(EntityTypeBuilder<SessionLog> builder)
    {
        builder.ToTable("SessionLogs");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.UserId).IsRequired();
        builder.Property(log => log.ExerciseId).IsRequired();
        builder.Property(log => log.MesocycleId);
        builder.Property(log => log.SessionDay).IsRequired();
        builder.Property(log => log.RecordedAtUtc).IsRequired();

        builder.HasIndex(log => log.UserId);

        // Las series son parte del registro: tabla propia, una fila por serie. La clave
        // compuesta (registro, número) garantiza un valor por serie y su orden.
        builder.OwnsMany(log => log.Sets, sets =>
        {
            sets.ToTable("SessionLogSets");
            sets.WithOwner().HasForeignKey("SessionLogId");
            sets.HasKey("SessionLogId", nameof(SessionLogSet.SetNumber));
            sets.Property(set => set.SetNumber).IsRequired();
            sets.Property(set => set.Value).IsRequired();
            sets.Property(set => set.Effort);
        });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
