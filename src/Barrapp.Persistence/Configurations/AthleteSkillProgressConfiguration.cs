using Barrapp.Domain.SkillProgress;
using Barrapp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barrapp.Persistence.Configurations;

/// <summary>
/// Mapeo EF Core de la etapa actual del atleta por skill: una fila por atleta y skill, con la
/// etapa dentro de la escalera.
/// </summary>
internal sealed class AthleteSkillProgressConfiguration : IEntityTypeConfiguration<AthleteSkillProgress>
{
    public void Configure(EntityTypeBuilder<AthleteSkillProgress> builder)
    {
        builder.ToTable("AthleteSkillProgresses");

        builder.HasKey(progress => progress.Id);

        builder.Property(progress => progress.UserId).IsRequired();
        builder.Property(progress => progress.SkillId).IsRequired();
        builder.Property(progress => progress.StageOrder).IsRequired();

        builder.HasIndex(progress => new { progress.UserId, progress.SkillId }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(progress => progress.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
