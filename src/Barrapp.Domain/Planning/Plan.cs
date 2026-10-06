namespace Barrapp.Domain.Planning;

/// <summary>
/// Plan mensual generado para un objetivo: el mesociclo de cuatro semanas con sus sesiones
/// (ver <c>GLOSSARY.md</c>, término <i>Plan</i>).
/// </summary>
/// <remarks>
/// Es un valor puro y determinista que produce <see cref="PlanGenerator"/>: mismos datos de
/// entrada, mismo plan. No lleva identidad ni usuario porque todavía no se persiste; su forma es
/// el propio contenido generado.
/// </remarks>
public sealed class Plan
{
    internal Plan(string skillId, int trainingDays, IReadOnlyList<Microcycle> microcycles)
    {
        SkillId = skillId;
        TrainingDays = trainingDays;
        Microcycles = microcycles;
    }

    /// <summary>Slug del skill objetivo del mesociclo.</summary>
    public string SkillId { get; }

    /// <summary>Días de entrenamiento por semana.</summary>
    public int TrainingDays { get; }

    /// <summary>Semanas del mesociclo, en orden.</summary>
    public IReadOnlyList<Microcycle> Microcycles { get; }
}
