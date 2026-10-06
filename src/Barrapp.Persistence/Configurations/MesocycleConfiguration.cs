using System.Text.Json;
using System.Text.Json.Serialization;
using Barrapp.Domain.Planning;
using Barrapp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barrapp.Persistence.Configurations;

/// <summary>
/// Mapeo EF Core del mesociclo persistido (#27, D7). Cada fila es un mesociclo con sus datos de
/// cabecera —usuario, skill, frecuencia, estado y fechas— y el <b>snapshot del plan completo</b>
/// en una única columna JSON (<see cref="MesocycleSnapshot"/>): microciclos, sesiones e ítems
/// viajan serializados con System.Text.Json. Es la elección que documenta D7 para la tabla
/// «Mesocycles + columnas del snapshot serializadas»: un solo campo, sin tablas owned anidadas
/// para un árbol de tres niveles que nunca se consulta por partes.
/// </summary>
/// <remarks>
/// Un atleta tiene a lo sumo un mesociclo <see cref="MesocycleStatus.Active"/>: el índice único
/// filtrado sobre <c>UserId</c> (solo filas activas) refuerza la invariante en la base de datos.
/// </remarks>
internal sealed class MesocycleConfiguration : IEntityTypeConfiguration<Mesocycle>
{
    /// <summary>Opciones de serialización del snapshot: enums como texto legible en la columna.</summary>
    private static readonly JsonSerializerOptions SnapshotOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public void Configure(EntityTypeBuilder<Mesocycle> builder)
    {
        builder.ToTable("Mesocycles");

        builder.HasKey(mesocycle => mesocycle.Id);

        builder.Property(mesocycle => mesocycle.UserId).IsRequired();
        builder.Property(mesocycle => mesocycle.SkillId).IsRequired();
        builder.Property(mesocycle => mesocycle.TrainingDays).IsRequired();
        builder.Property(mesocycle => mesocycle.Status).IsRequired();
        builder.Property(mesocycle => mesocycle.StartedAtUtc).IsRequired();
        builder.Property(mesocycle => mesocycle.ClosedAtUtc);

        // El plan generado se guarda como snapshot en una única columna JSON (decisión de D7).
        builder.Property(mesocycle => mesocycle.Snapshot)
            .IsRequired()
            .HasConversion(
                snapshot => JsonSerializer.Serialize(snapshot, SnapshotOptions),
                json => JsonSerializer.Deserialize<MesocycleSnapshot>(json, SnapshotOptions)!);

        // A lo sumo un mesociclo activo por atleta.
        builder.HasIndex(mesocycle => mesocycle.UserId)
            .HasFilter($"{nameof(Mesocycle.Status)} = {(int)MesocycleStatus.Active}")
            .IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(mesocycle => mesocycle.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}