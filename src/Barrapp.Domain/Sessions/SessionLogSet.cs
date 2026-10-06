using Barrapp.Domain.Common;

namespace Barrapp.Domain.Sessions;

/// <summary>
/// Una serie de un <see cref="SessionLog"/>: el número de serie y el valor real ejecutado —reps
/// en fuerza o segundos en holds/skill—. La unidad se deriva del tipo de ejercicio, no se guarda
/// por serie. El esfuerzo real (RIR/RPE) es opcional (tickets #20/#21).
/// </summary>
public sealed class SessionLogSet
{
    private SessionLogSet(int setNumber, int value, int? effort)
    {
        SetNumber = setNumber;
        Value = value;
        Effort = effort;
    }

    // Requerido por EF Core para materializar el objeto valor; nunca se usa desde el dominio.
    private SessionLogSet()
    {
    }

    /// <summary>Número de orden de la serie dentro del ejercicio (desde 1).</summary>
    public int SetNumber { get; private set; }

    /// <summary>
    /// Valor real ejecutado en la serie: repeticiones o segundos según el ejercicio. Nunca
    /// negativo; 0 se admite (p. ej. una serie fallida).
    /// </summary>
    public int Value { get; private set; }

    /// <summary>
    /// Esfuerzo real (RIR/RPE) de la serie, entre 0 y 10; <c>null</c> si no se anotó.
    /// </summary>
    public int? Effort { get; private set; }

    /// <summary>
    /// Crea la serie. Falla si el número de serie es menor que 1, el valor es negativo o el
    /// esfuerzo, cuando viene, escapa del rango 0–10.
    /// </summary>
    public static Result<SessionLogSet> Create(int setNumber, int value, int? effort)
    {
        if (setNumber < 1)
        {
            return Result.Failure<SessionLogSet>(DomainErrors.SessionLog.SetNumberOutOfRange);
        }

        if (value < 0)
        {
            return Result.Failure<SessionLogSet>(DomainErrors.SessionLog.ValueMustBeNonNegative);
        }

        if (effort is < 0 or > 10)
        {
            return Result.Failure<SessionLogSet>(DomainErrors.SessionLog.EffortOutOfRange);
        }

        return new SessionLogSet(setNumber, value, effort);
    }
}
