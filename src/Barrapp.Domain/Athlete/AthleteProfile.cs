using Barrapp.Domain.Common;

namespace Barrapp.Domain.Athlete;

/// <summary>
/// Perfil del atleta: sus datos corporales. Hoy guarda <b>peso</b> (kg) y <b>altura</b> (cm);
/// los máximos por ejercicio y los días de entrenamiento llegan en tickets posteriores.
/// </summary>
/// <remarks>
/// El estado solo cambia por sus métodos (<see cref="Create"/>, <see cref="Update"/>), que
/// validan los invariantes. Pertenece a un usuario (<see cref="UserId"/>); en el MVP
/// mono-usuario, sin login, todos los perfiles pertenecen al mismo usuario fijo.
/// </remarks>
public sealed class AthleteProfile
{
    private AthleteProfile(Guid id, Guid userId, double weightKilograms, double heightCentimeters)
    {
        Id = id;
        UserId = userId;
        WeightKilograms = weightKilograms;
        HeightCentimeters = heightCentimeters;
    }

    // Requerido por EF Core para materializar la entidad; nunca se usa desde el dominio.
    private AthleteProfile()
    {
    }

    /// <summary>Identificador del perfil.</summary>
    public Guid Id { get; private set; }

    /// <summary>Identificador del atleta al que pertenece el perfil.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Peso del atleta en kilogramos.</summary>
    public double WeightKilograms { get; private set; }

    /// <summary>Altura del atleta en centímetros.</summary>
    public double HeightCentimeters { get; private set; }

    /// <summary>
    /// Crea un perfil para <paramref name="userId"/>. Falla si el peso o la altura no son
    /// números positivos.
    /// </summary>
    public static Result<AthleteProfile> Create(
        Guid userId,
        double weightKilograms,
        double heightCentimeters)
    {
        var validation = ValidateMeasurements(weightKilograms, heightCentimeters);
        return validation.IsFailure
            ? Result.Failure<AthleteProfile>(validation.Error)
            : new AthleteProfile(Guid.NewGuid(), userId, weightKilograms, heightCentimeters);
    }

    /// <summary>
    /// Actualiza las medidas del perfil. Falla si el peso o la altura no son números
    /// positivos; en ese caso no se modifica nada.
    /// </summary>
    public Result Update(double weightKilograms, double heightCentimeters)
    {
        var validation = ValidateMeasurements(weightKilograms, heightCentimeters);
        if (validation.IsFailure)
        {
            return validation;
        }

        WeightKilograms = weightKilograms;
        HeightCentimeters = heightCentimeters;

        return Result.Success();
    }

    private static Result ValidateMeasurements(double weightKilograms, double heightCentimeters)
    {
        if (!IsPositive(weightKilograms))
        {
            return Result.Failure(DomainErrors.AthleteProfile.WeightMustBePositive);
        }

        return !IsPositive(heightCentimeters)
            ? Result.Failure(DomainErrors.AthleteProfile.HeightMustBePositive)
            : Result.Success();
    }

    private static bool IsPositive(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
}
