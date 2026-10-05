using Barrapp.Domain.Athlete;
using Barrapp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barrapp.Persistence.Configurations;

/// <summary>
/// Mapeo EF Core del perfil del atleta. La tabla guarda una fila por atleta; en el MVP
/// mono-usuario, una sola.
/// </summary>
internal sealed class AthleteProfileConfiguration : IEntityTypeConfiguration<AthleteProfile>
{
    public void Configure(EntityTypeBuilder<AthleteProfile> builder)
    {
        builder.ToTable("AthleteProfiles");

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.UserId).IsRequired();
        builder.Property(profile => profile.WeightKilograms).IsRequired();
        builder.Property(profile => profile.HeightCentimeters).IsRequired();
        builder.Property(profile => profile.TrainingDays).IsRequired();

        builder.HasIndex(profile => profile.UserId).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
