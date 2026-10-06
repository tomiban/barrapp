using Barrapp.Domain.Knowledge;

namespace Barrapp.Domain.Planning;

/// <summary>
/// Tipo de composición que el motor resuelve para una sesión suelta: centrada en el <i>skill</i>
/// objetivo (su escalera y sus rutinas de patrón) o centrada en un <i>patrón</i> de fuerza general.
/// </summary>
public enum SoloSessionCompositionKind
{
    /// <summary>La sesión abre con la etapa actual del skill objetivo.</summary>
    Skill = 0,

    /// <summary>La sesión trabaja la fuerza general de un patrón.</summary>
    Pattern = 1,
}

/// <summary>
/// Resolución determinista del foco de la sesión suelta: qué va a componer el motor. Para el foco
/// «sorpréndeme» materializa la elección (skill o patrón), de modo que la respuesta del API puede
/// decirle al atleta qué le ha tocado.
/// </summary>
public sealed record SoloSessionComposition(
    SoloSessionCompositionKind Kind,
    ExerciseGroup? Pattern,
    string? SkillId);
