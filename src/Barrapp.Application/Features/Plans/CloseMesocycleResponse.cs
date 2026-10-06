using Barrapp.Application.Features.AthleteProfiles;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Resultado del cierre del mesociclo (ticket #24, D7): el identificador y los datos del
/// mesociclo cerrado —que ya forma parte del historial— y los <b>máximos del perfil tras el
/// ajuste</b>, para que la app los pueda reflejar sin volver a leer el perfil.
/// </summary>
/// <param name="MesocycleId">Identificador del mesociclo cerrado, para abrirlo en el historial.</param>
/// <param name="SkillId">Slug del skill objetivo del mesociclo.</param>
/// <param name="SkillName">Nombre del skill para la UI, en español.</param>
/// <param name="TrainingDays">Días de entrenamiento por semana del mesociclo.</param>
/// <param name="StartedAtUtc">Momento en el que se generó el mesociclo (UTC).</param>
/// <param name="ClosedAtUtc">Momento en el que se cerró (UTC).</param>
/// <param name="Maximums">Máximos del atleta tras el ajuste; nunca inferiores a los anteriores.</param>
public sealed record CloseMesocycleResponse(
    Guid MesocycleId,
    string SkillId,
    string SkillName,
    int TrainingDays,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset ClosedAtUtc,
    IReadOnlyList<MaximumResponse> Maximums);
