using Barrapp.Domain.Knowledge;

namespace Barrapp.Domain.Planning;

/// <summary>
/// Plan mensual generado para un objetivo: el mesociclo de cuatro semanas con sus sesiones
/// (ver <c>GLOSSARY.md</c>, término <i>Plan</i>).
/// </summary>
/// <remarks>
/// Es un valor puro y determinista que produce <see cref="PlanGenerator"/>: mismos datos de
/// entrada, mismo plan. No lleva identidad ni usuario porque todavía no se persiste; su forma es
/// el propio contenido generado. Junto al mesociclo guarda la etapa actual del skill objetivo
/// (la misma que practica el bloque de skill), derivada de la escalera y la progresión del atleta.
/// </remarks>
public sealed class Plan
{
    internal Plan(
        string skillId,
        int trainingDays,
        DateOnly startDate,
        SkillStage currentStage,
        IReadOnlyList<Microcycle> microcycles)
    {
        SkillId = skillId;
        TrainingDays = trainingDays;
        StartDate = startDate;
        CurrentStage = currentStage;
        Microcycles = microcycles;
    }

    /// <summary>Slug del skill objetivo del mesociclo.</summary>
    public string SkillId { get; }

    /// <summary>Días de entrenamiento por semana.</summary>
    public int TrainingDays { get; }

    /// <summary>
    /// Fecha en la que arranca el mesociclo: el primer <b>día de entrenamiento</b> en o después de
    /// la fecha elegida por el atleta (#94). Cada sesión cae en el día de la semana que le toca de
    /// <see cref="Session.Weekday"/>.
    /// </summary>
    public DateOnly StartDate { get; }

    /// <summary>Etapa actual del atleta en el skill objetivo, con su criterio de avance.</summary>
    public SkillStage CurrentStage { get; }

    /// <summary>Semanas del mesociclo, en orden.</summary>
    public IReadOnlyList<Microcycle> Microcycles { get; }
}
