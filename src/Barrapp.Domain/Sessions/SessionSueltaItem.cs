using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;

namespace Barrapp.Domain.Sessions;

/// <summary>
/// Una fila del snapshot de una <see cref="SessionSuelta"/>: el mismo ejercicio, papel, patrón y
/// prescripción —series y rango de repeticiones o de segundos— que el motor compuso al generar.
/// Objeto valor sin lógica propia: toda escritura pasa por <see cref="Create"/>.
/// </summary>
/// <remarks>
/// <see cref="Position"/> preserva el orden de ejecución dentro del snapshot (es la clave junto al
/// identificador de la suelta, igual que el número de serie de <see cref="SessionLogSet"/>).
/// </remarks>
public sealed class SessionSueltaItem
{
    private SessionSueltaItem(
        int position,
        string exerciseId,
        SessionItemRole role,
        ExerciseGroup? pattern,
        int sets,
        int? repsMin,
        int? repsMax,
        int? holdSecondsMin,
        int? holdSecondsMax,
        string? note)
    {
        Position = position;
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

    // Requerido por EF Core para materializar el objeto valor; nunca se usa desde el dominio.
    private SessionSueltaItem()
    {
    }

    /// <summary>Orden de la fila dentro del snapshot, desde 1.</summary>
    public int Position { get; private set; }

    /// <summary>Slug del ejercicio en el catálogo.</summary>
    public string ExerciseId { get; private set; } = string.Empty;

    /// <summary>Papel del ejercicio dentro de la sesión.</summary>
    public SessionItemRole Role { get; private set; }

    /// <summary>Patrón de fuerza; <c>null</c> en el bloque de skill y en el core.</summary>
    public ExerciseGroup? Pattern { get; private set; }

    /// <summary>Número de series.</summary>
    public int Sets { get; private set; }

    /// <summary>Repeticiones mínimas del rango; <c>null</c> cuando el ejercicio se mide en segundos.</summary>
    public int? RepsMin { get; private set; }

    /// <summary>Repeticiones máximas del rango; <c>null</c> cuando el ejercicio se mide en segundos.</summary>
    public int? RepsMax { get; private set; }

    /// <summary>Segundos mantenidos mínimos del rango; <c>null</c> cuando se mide en repeticiones.</summary>
    public int? HoldSecondsMin { get; private set; }

    /// <summary>Segundos mantenidos máximos del rango; <c>null</c> cuando se mide en repeticiones.</summary>
    public int? HoldSecondsMax { get; private set; }

    /// <summary>Nota de la fila para la UI; <c>null</c> cuando no hay nada que explicar.</summary>
    public string? Note { get; private set; }

    /// <summary>
    /// Crea la fila del snapshot. Falla si la fila no trae ejercicio o serie, o si no declara
    /// ningún rango completo (repeticiones o segundos con mínimo y máximo).
    /// </summary>
    public static Result<SessionSueltaItem> Create(int position, SessionSueltaItemInput input)
    {
        if (string.IsNullOrWhiteSpace(input.ExerciseId))
        {
            return Result.Failure<SessionSueltaItem>(DomainErrors.SessionSuelta.ItemExerciseRequired);
        }

        if (input.Sets < 1)
        {
            return Result.Failure<SessionSueltaItem>(DomainErrors.SessionSuelta.ItemSetsMustBePositive);
        }

        if (input.RepsMin is null
            && input.RepsMax is null
            && input.HoldSecondsMin is null
            && input.HoldSecondsMax is null)
        {
            return Result.Failure<SessionSueltaItem>(DomainErrors.SessionSuelta.ItemRequiresRange);
        }

        if (input.RepsMin is not null && input.RepsMax is not null && input.RepsMin > input.RepsMax)
        {
            return Result.Failure<SessionSueltaItem>(DomainErrors.SessionSuelta.ItemRangeMinGreaterThanMax);
        }

        if (input.HoldSecondsMin is not null
            && input.HoldSecondsMax is not null
            && input.HoldSecondsMin > input.HoldSecondsMax)
        {
            return Result.Failure<SessionSueltaItem>(DomainErrors.SessionSuelta.ItemRangeMinGreaterThanMax);
        }

        return new SessionSueltaItem(
            position,
            input.ExerciseId,
            input.Role,
            input.Pattern,
            input.Sets,
            input.RepsMin,
            input.RepsMax,
            input.HoldSecondsMin,
            input.HoldSecondsMax,
            input.Note);
    }
}