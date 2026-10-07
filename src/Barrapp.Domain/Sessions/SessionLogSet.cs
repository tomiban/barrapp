using Barrapp.Domain.Common;

namespace Barrapp.Domain.Sessions;

/// <summary>
/// Una serie de un <see cref="SessionLogItem"/>: el número de serie y el valor real ejecutado. La
/// unidad es la del ítem —repeticiones o segundos mantenidos, derivada del tipo de ejercicio y
/// congelada en la foto— y el <b>RIR real</b> y el <b>lastre</b> son opcionales por serie
/// (spec 0001, US-35; ver <c>GLOSSARY.md</c>).
/// </summary>
public sealed class SessionLogSet
{
    private SessionLogSet(int setNumber, int value, int? actualRir, double? loadKg)
    {
        SetNumber = setNumber;
        Value = value;
        ActualRir = actualRir;
        LoadKg = loadKg;
    }

    // Requerido por EF Core para materializar el objeto valor; nunca se usa desde el dominio.
    private SessionLogSet()
    {
    }

    /// <summary>Número de orden de la serie dentro del ejercicio (desde 1).</summary>
    public int SetNumber { get; private set; }

    /// <summary>
    /// Valor real ejecutado en la serie: repeticiones o segundos según la unidad del ítem. Nunca
    /// negativo; 0 se admite (p. ej. una serie fallida).
    /// </summary>
    public int Value { get; private set; }

    /// <summary>
    /// RIR real de la serie, entre 0 y 10; <c>null</c> si no se anotó. Es informativo: no mueve el
    /// avance de etapa del skill (spec 0001, US-35).
    /// </summary>
    public int? ActualRir { get; private set; }

    /// <summary>
    /// Lastre en kg añadido a la serie; <c>null</c> si se hizo a peso corporal. No altera el
    /// <i>máximo</i>, que se mide a peso corporal (ADR-0009).
    /// </summary>
    public double? LoadKg { get; private set; }

    /// <summary>
    /// Crea la serie. Falla si el número de serie es menor que 1, si el valor es negativo, si el
    /// RIR real escapa del rango 0–10 o si el lastre es negativo.
    /// </summary>
    public static Result<SessionLogSet> Create(int setNumber, int value, int? actualRir, double? loadKg)
    {
        if (setNumber < 1)
        {
            return Result.Failure<SessionLogSet>(DomainErrors.SessionLog.SetNumberOutOfRange);
        }

        if (value < 0)
        {
            return Result.Failure<SessionLogSet>(DomainErrors.SessionLog.ValueMustBeNonNegative);
        }

        if (actualRir is < 0 or > 10)
        {
            return Result.Failure<SessionLogSet>(DomainErrors.SessionLog.ActualRirOutOfRange);
        }

        if (loadKg is < 0)
        {
            return Result.Failure<SessionLogSet>(DomainErrors.SessionLog.LoadMustBeNonNegative);
        }

        return new SessionLogSet(setNumber, value, actualRir, loadKg);
    }
}
