using Barrapp.Domain.Sessions;
using Barrapp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barrapp.Persistence.Configurations;

/// <summary>
/// Mapeo EF Core de la sesión suelta del historial. Cada fila es una suelta con los parámetros con
/// los que se pidió, el foco ya resuelto y su estado; el snapshot de la sesión vive en su propia
/// tabla como objetos valor owned, con la clave compuesta (suelta, posición) que garantiza una
/// fila por puesto y su orden. Es una tabla hermanada de <c>SessionLogs</c>: la suelta nunca se
/// registra como sesión del mesociclo (D8).
/// </summary>
internal sealed class SessionSueltaConfiguration : IEntityTypeConfiguration<SessionSuelta>
{
    public void Configure(EntityTypeBuilder<SessionSuelta> builder)
    {
        builder.ToTable("SessionSuelta");

        builder.HasKey(suelta => suelta.Id);

        builder.Property(suelta => suelta.UserId).IsRequired();
        builder.Property(suelta => suelta.TimeMinutes).IsRequired();
        builder.Property(suelta => suelta.Energy).IsRequired();
        builder.Property(suelta => suelta.Focus).IsRequired();
        builder.Property(suelta => suelta.Pattern);
        builder.Property(suelta => suelta.SkillId);
        builder.Property(suelta => suelta.Status).IsRequired();
        builder.Property(suelta => suelta.CreatedAtUtc).IsRequired();
        builder.Property(suelta => suelta.RecordedAtUtc);

        builder.HasIndex(suelta => suelta.UserId);

        // El snapshot es parte de la suelta: tabla propia, una fila por ítem. La clave compuesta
        // (suelta, posición) garantiza una fila por puesto y su orden de ejecución.
        builder.OwnsMany(suelta => suelta.Items, items =>
        {
            items.ToTable("SessionSueltaItems");
            items.WithOwner().HasForeignKey("SessionSueltaId");
            items.HasKey("SessionSueltaId", nameof(SessionSueltaItem.Position));
            items.Property(item => item.Position).IsRequired();
            items.Property(item => item.ExerciseId).IsRequired();
            items.Property(item => item.Role).IsRequired();
            items.Property(item => item.Pattern);
            items.Property(item => item.Sets).IsRequired();
            items.Property(item => item.RepsMin);
            items.Property(item => item.RepsMax);
            items.Property(item => item.HoldSecondsMin);
            items.Property(item => item.HoldSecondsMax);
            items.Property(item => item.Note);
        });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(suelta => suelta.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}