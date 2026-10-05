using Barrapp.Domain.Common;

namespace Barrapp.Domain.Athlete;

/// <summary>
/// Máximo del atleta en un ejercicio básico: repeticiones estrictas sin lastre
/// (ver <c>GLOSSARY.md</c>, término <i>Máximo</i>). 0 es válido e indica regresión.
/// </summary>
/// <remarks>
/// Pertenece al agregado <see cref="AthleteProfile"/>; se construye con <see cref="Create"/>,
/// que valida el código de ejercicio y que las repeticiones no sean negativas.
/// </remarks>
public sealed class Maximum
{
    private Maximum(string exerciseCode, int repetitions)
    {
        ExerciseCode = exerciseCode;
        Repetitions = repetitions;
    }

    // Requerido por EF Core para materializar la entidad propiedad; nunca se usa desde el dominio.
    private Maximum()
    {
    }

    /// <summary>Código estable del ejercicio básico.</summary>
    public string ExerciseCode { get; private set; } = string.Empty;

    /// <summary>Repeticiones máximas estrictas; 0 indica regresión.</summary>
    public int Repetitions { get; private set; }

    /// <summary>
    /// Crea el máximo de un ejercicio básico. Falla si el código no pertenece al catálogo o
    /// si las repeticiones son negativas; 0 se acepta.
    /// </summary>
    public static Result<Maximum> Create(string exerciseCode, int repetitions)
    {
        if (!BasicExercises.IsKnownCode(exerciseCode))
        {
            return Result.Failure<Maximum>(DomainErrors.AthleteProfile.UnknownExerciseCode);
        }

        if (repetitions < 0)
        {
            return Result.Failure<Maximum>(DomainErrors.AthleteProfile.MaximumMustBeNonNegative);
        }

        return new Maximum(exerciseCode, repetitions);
    }

    /// <summary>Cambia las repeticiones; el agregado ya validó que no son negativas.</summary>
    internal void SetRepetitions(int repetitions) => Repetitions = repetitions;
}
