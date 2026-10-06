namespace Barrapp.Application.Features.Plans;

/// <summary>Resumen de un mesociclo del historial tal y como lo consume la app.</summary>
/// <param name="Id">Identificador del mesociclo para abrir su detalle.</param>
/// <param name="SkillId">Slug del skill objetivo.</param>
/// <param name="SkillName">Nombre del skill para la UI, en español.</param>
/// <param name="TrainingDays">Días de entrenamiento por semana.</param>
/// <param name="StartedAtUtc">Momento en el que se generó el mesociclo (UTC).</param>
/// <param name="ClosedAtUtc">Momento en el que se cerró (UTC).</param>
public sealed record MesocycleSummaryResponse(
    Guid Id,
    string SkillId,
    string SkillName,
    int TrainingDays,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset ClosedAtUtc);
