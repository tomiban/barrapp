using Barrapp.Domain.Objectives;
using Barrapp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barrapp.Persistence.Configurations;

/// <summary>
/// Mapeo EF Core del objetivo del mesociclo. La tabla guarda una fila por atleta; en el MVP
/// mono-usuario, una sola.
/// </summary>
internal sealed class ObjectiveConfiguration : IEntityTypeConfiguration<Objective>
{
    public void Configure(EntityTypeBuilder<Objective> builder)
    {
        builder.ToTable("Objectives");

        builder.HasKey(objective => objective.Id);

        builder.Property(objective => objective.UserId).IsRequired();
        builder.Property(objective => objective.SkillId).IsRequired();

        builder.HasIndex(objective => objective.UserId).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(objective => objective.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
