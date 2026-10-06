using Barrapp.Domain.Knowledge;

namespace Barrapp.Domain.Planning;

/// <summary>
/// Fila de una sesión: un ejercicio con su papel, su patrón (solo en fuerza), sus series y su
/// prescripción —un rango de repeticiones o de segundos mantenidos— (ver <c>GLOSSARY.md</c>,
/// términos <i>Sesión</i> y <i>Patrón</i>).
/// </summary>
/// <remarks>
/// La construye el <see cref="PlanGenerator"/>; es inmutable. <see cref="Role"/> decide qué
/// rangos llevan valor: <see cref="SessionItemRole.Strength"/> usa reps y
/// <see cref="SessionItemRole.Skill"/>/<see cref="SessionItemRole.Core"/> pueden usar segundos
/// (los skills de repeticiones, reps).
/// </remarks>
public sealed class SessionItem
{
    internal SessionItem(
        string exerciseId,
        SessionItemRole role,
        ExerciseGroup? pattern,
        int sets,
        int? repsMin,
        int? repsMax,
        int? holdSecondsMin,
        int? holdSecondsMax,
        string? note = null)
    {
        ExerciseId = exerciseId;
        Role = role;
        Pattern = pattern;
        Sets = sets;
        RepsMin = repsMin;
        RepsMax = repsMax;
        HoldSecondsMin = holdSecondsMin;
        HoldSecondsMax = holdSecondsMax;
        Note = note;
    }

    /// <summary>Slug del ejercicio en el catálogo.</summary>
    public string ExerciseId { get; }

    /// <summary>Papel del ejercicio dentro de la sesión.</summary>
    public SessionItemRole Role { get; }

    /// <summary>Patrón de fuerza; <c>null</c> en el bloque de skill y en el core.</summary>
    public ExerciseGroup? Pattern { get; }

    /// <summary>Número de series.</summary>
    public int Sets { get; }

    /// <summary>Repeticiones mínimas del rango; <c>null</c> cuando el ejercicio se mide en segundos.</summary>
    public int? RepsMin { get; }

    /// <summary>Repeticiones máximas del rango; <c>null</c> cuando el ejercicio se mide en segundos.</summary>
    public int? RepsMax { get; }

    /// <summary>Segundos mantenidos mínimos del rango; <c>null</c> cuando se mide en repeticiones.</summary>
    public int? HoldSecondsMin { get; }

    /// <summary>Segundos mantenidos máximos del rango; <c>null</c> cuando se mide en repeticiones.</summary>
    public int? HoldSecondsMax { get; }

    /// <summary>
    /// Nota para la UI sobre la fila; <c>null</c> cuando no hay nada que explicar. Hoy solo la lleva
    /// el bloque de skill de un skill apalancado, con el ritmo de progreso esperado por la palanca
    /// (ver <see cref="Barrapp.Domain.Athlete.AthleteLever"/>).
    /// </summary>
    public string? Note { get; }
}
