using Barrapp.Domain.Common;

namespace Barrapp.Domain.Athlete;

/// <summary>
/// Perfil del atleta: sus datos corporales y de entrenamiento. Hoy guarda <b>peso</b> (kg),
/// <b>altura</b> (cm) y <b>días de entrenamiento</b> por semana (3–5); los máximos por
/// ejercicio llegan en tickets posteriores.
/// </summary>
/// <remarks>
/// El estado solo cambia por sus métodos (<see cref="Create"/>, <see cref="Update"/>), que
/// validan los invariantes. Pertenece a un usuario (<see cref="UserId"/>); en el MVP
/// mono-usuario, sin login, todos los perfiles pertenecen al mismo usuario fijo.
/// </remarks>
public sealed class AthleteProfile
{
    /// <summary>Peso mínimo admitido, en kilogramos.</summary>
    public const double MinWeightKilograms = 30;

    /// <summary>Peso máximo admitido, en kilogramos.</summary>
    public const double MaxWeightKilograms = 200;

    /// <summary>Altura mínima admitida, en centímetros.</summary>
    public const double MinHeightCentimeters = 120;

    /// <summary>Altura máxima admitida, en centímetros.</summary>
    public const double MaxHeightCentimeters = 220;

    /// <summary>Número mínimo de días de entrenamiento por semana.</summary>
    public const int MinTrainingDays = 3;

    /// <summary>Número máximo de días de entrenamiento por semana.</summary>
    public const int MaxTrainingDays = 5;

    private AthleteProfile(
        Guid id,
        Guid userId,
        double weightKilograms,
        double heightCentimeters,
        int trainingDays)
    {
        Id = id;
        UserId = userId;
        WeightKilograms = weightKilograms;
        HeightCentimeters = heightCentimeters;
        TrainingDays = trainingDays;
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

    /// <summary>Días de entrenamiento por semana (3–5).</summary>
    public int TrainingDays { get; private set; }

    /// <summary>
    /// Crea un perfil para <paramref name="userId"/>. Falla si el peso (30–200 kg), la
    /// altura (120–220 cm) o los días de entrenamiento (3–5) están fuera de rango.
    /// </summary>
    public static Result<AthleteProfile> Create(
        Guid userId,
        double weightKilograms,
        double heightCentimeters,
        int trainingDays)
    {
        var validation = ValidateProfile(weightKilograms, heightCentimeters, trainingDays);
        return validation.IsFailure
            ? Result.Failure<AthleteProfile>(validation.Error)
            : new AthleteProfile(
                Guid.NewGuid(),
                userId,
                weightKilograms,
                heightCentimeters,
                trainingDays);
    }

    /// <summary>
    /// Actualiza los datos del perfil. Falla si el peso (30–200 kg), la altura (120–220 cm)
    /// o los días de entrenamiento (3–5) están fuera de rango; en ese caso no se modifica nada.
    /// </summary>
    public Result Update(double weightKilograms, double heightCentimeters, int trainingDays)
    {
        var validation = ValidateProfile(weightKilograms, heightCentimeters, trainingDays);
        if (validation.IsFailure)
        {
            return validation;
        }

        WeightKilograms = weightKilograms;
        HeightCentimeters = heightCentimeters;
        TrainingDays = trainingDays;

        return Result.Success();
    }

    private static Result ValidateProfile(
        double weightKilograms,
        double heightCentimeters,
        int trainingDays)
    {
        if (!IsWithinRange(weightKilograms, MinWeightKilograms, MaxWeightKilograms))
        {
            return Result.Failure(DomainErrors.AthleteProfile.WeightOutOfRange);
        }

        if (!IsWithinRange(heightCentimeters, MinHeightCentimeters, MaxHeightCentimeters))
        {
            return Result.Failure(DomainErrors.AthleteProfile.HeightOutOfRange);
        }

        return IsWithinRange(trainingDays, MinTrainingDays, MaxTrainingDays)
            ? Result.Success()
            : Result.Failure(DomainErrors.AthleteProfile.TrainingDaysOutOfRange);
    }

    private static bool IsWithinRange(double value, double min, double max) =>
        double.IsFinite(value) && value >= min && value <= max;

    private static bool IsWithinRange(int value, int min, int max) => value >= min && value <= max;
}
