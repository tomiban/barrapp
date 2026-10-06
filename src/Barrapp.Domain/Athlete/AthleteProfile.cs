using Barrapp.Domain.Common;

namespace Barrapp.Domain.Athlete;

/// <summary>
/// Perfil del atleta: sus datos corporales, de entrenamiento y sus máximos por ejercicio
/// básico. Guarda <b>peso</b> (kg), <b>altura</b> (cm), <b>envergadura</b> (cm),
/// <b>entrepierna</b> (cm), <b>días de entrenamiento</b> por semana (3–5) y el
/// <see cref="Maximum"/> de cada ejercicio de <see cref="BasicExercises"/>.
/// </summary>
/// <remarks>
/// El estado solo cambia por sus métodos (<see cref="Create"/>, <see cref="Update"/>), que
/// validan los invariantes y son atómicos: si algo falla, no se modifica nada. Pertenece a un
/// usuario (<see cref="UserId"/>); en el MVP mono-usuario, sin login, todos los perfiles
/// pertenecen al mismo usuario fijo.
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

    /// <summary>Envergadura mínima admitida, en centímetros.</summary>
    public const double MinArmSpanCentimeters = 100;

    /// <summary>Envergadura máxima admitida, en centímetros.</summary>
    public const double MaxArmSpanCentimeters = 250;

    /// <summary>Entrepierna mínima admitida, en centímetros.</summary>
    public const double MinInseamCentimeters = 50;

    /// <summary>Entrepierna máxima admitida, en centímetros.</summary>
    public const double MaxInseamCentimeters = 130;

    /// <summary>Número mínimo de días de entrenamiento por semana.</summary>
    public const int MinTrainingDays = 3;

    /// <summary>Número máximo de días de entrenamiento por semana.</summary>
    public const int MaxTrainingDays = 5;

    private readonly List<Maximum> _maximums = [];

    private AthleteProfile(
        Guid id,
        Guid userId,
        double weightKilograms,
        double heightCentimeters,
        double armSpanCentimeters,
        double inseamCentimeters,
        int trainingDays)
    {
        Id = id;
        UserId = userId;
        WeightKilograms = weightKilograms;
        HeightCentimeters = heightCentimeters;
        ArmSpanCentimeters = armSpanCentimeters;
        InseamCentimeters = inseamCentimeters;
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

    /// <summary>Envergadura (arm span) del atleta en centímetros.</summary>
    public double ArmSpanCentimeters { get; private set; }

    /// <summary>Entrepierna (inseam) del atleta en centímetros.</summary>
    public double InseamCentimeters { get; private set; }

    /// <summary>Días de entrenamiento por semana (3–5).</summary>
    public int TrainingDays { get; private set; }

    /// <summary>Máximo por cada ejercicio básico; uno por ejercicio, siempre completo.</summary>
    public IReadOnlyCollection<Maximum> Maximums => _maximums;

    /// <summary>
    /// Devuelve las repeticiones máximas del atleta para <paramref name="exerciseCode"/>, o
    /// <c>null</c> si ese código no está entre sus máximos. El perfil es el dueño del emparejado
    /// código → repeticiones, de modo que quien lo consulte no lo reconstruya por su cuenta.
    /// </summary>
    public int? MaximumFor(string exerciseCode) =>
        _maximums
            .FirstOrDefault(current => string.Equals(current.ExerciseCode, exerciseCode, StringComparison.Ordinal))
            ?.Repetitions;

    /// <summary>
    /// Crea un perfil para <paramref name="userId"/>. Falla si el peso (30–200 kg), la
    /// altura (120–220 cm), la envergadura (100–250 cm), la entrepierna (50–130 cm) o los
    /// días de entrenamiento (3–5) están fuera de rango, o si los máximos no cubren
    /// exactamente los ejercicios básicos (falta alguno, hay repetidos, un código
    /// desconocido o unas repeticiones negativas).
    /// </summary>
    public static Result<AthleteProfile> Create(
        Guid userId,
        double weightKilograms,
        double heightCentimeters,
        double armSpanCentimeters,
        double inseamCentimeters,
        int trainingDays,
        IReadOnlyCollection<MaximumInput> maximums)
    {
        var validation = ValidateProfile(
            weightKilograms,
            heightCentimeters,
            armSpanCentimeters,
            inseamCentimeters,
            trainingDays);
        if (validation.IsFailure)
        {
            return Result.Failure<AthleteProfile>(validation.Error);
        }

        var maximumValidation = ValidateMaximums(maximums);
        if (maximumValidation.IsFailure)
        {
            return Result.Failure<AthleteProfile>(maximumValidation.Error);
        }

        var profile = new AthleteProfile(
            Guid.NewGuid(),
            userId,
            weightKilograms,
            heightCentimeters,
            armSpanCentimeters,
            inseamCentimeters,
            trainingDays);
        profile._maximums.AddRange(maximumValidation.Value);

        return profile;
    }

    /// <summary>
    /// Actualiza los datos del perfil. Falla si el peso (30–200 kg), la altura (120–220 cm),
    /// la envergadura (100–250 cm), la entrepierna (50–130 cm), los días de entrenamiento
    /// (3–5) o los máximos no son válidos; en ese caso no se modifica nada.
    /// </summary>
    public Result Update(
        double weightKilograms,
        double heightCentimeters,
        double armSpanCentimeters,
        double inseamCentimeters,
        int trainingDays,
        IReadOnlyCollection<MaximumInput> maximums)
    {
        var validation = ValidateProfile(
            weightKilograms,
            heightCentimeters,
            armSpanCentimeters,
            inseamCentimeters,
            trainingDays);
        if (validation.IsFailure)
        {
            return validation;
        }

        var maximumValidation = ValidateMaximums(maximums);
        if (maximumValidation.IsFailure)
        {
            return maximumValidation;
        }

        WeightKilograms = weightKilograms;
        HeightCentimeters = heightCentimeters;
        ArmSpanCentimeters = armSpanCentimeters;
        InseamCentimeters = inseamCentimeters;
        TrainingDays = trainingDays;
        ApplyMaximums(maximumValidation.Value);

        return Result.Success();
    }

    private static Result ValidateProfile(
        double weightKilograms,
        double heightCentimeters,
        double armSpanCentimeters,
        double inseamCentimeters,
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

        if (!IsWithinRange(armSpanCentimeters, MinArmSpanCentimeters, MaxArmSpanCentimeters))
        {
            return Result.Failure(DomainErrors.AthleteProfile.ArmSpanOutOfRange);
        }

        if (!IsWithinRange(inseamCentimeters, MinInseamCentimeters, MaxInseamCentimeters))
        {
            return Result.Failure(DomainErrors.AthleteProfile.InseamOutOfRange);
        }

        return IsWithinRange(trainingDays, MinTrainingDays, MaxTrainingDays)
            ? Result.Success()
            : Result.Failure(DomainErrors.AthleteProfile.TrainingDaysOutOfRange);
    }

    private static Result<List<Maximum>> ValidateMaximums(IReadOnlyCollection<MaximumInput> maximums)
    {
        if (maximums is null || maximums.Count == 0)
        {
            return Result.Failure<List<Maximum>>(DomainErrors.AthleteProfile.MissingExerciseMaximum);
        }

        var built = new List<Maximum>(maximums.Count);
        foreach (var input in maximums)
        {
            var creation = Maximum.Create(input.ExerciseCode, input.Repetitions);
            if (creation.IsFailure)
            {
                return Result.Failure<List<Maximum>>(creation.Error);
            }

            built.Add(creation.Value);
        }

        var codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var maximum in built)
        {
            if (!codes.Add(maximum.ExerciseCode))
            {
                return Result.Failure<List<Maximum>>(
                    DomainErrors.AthleteProfile.DuplicateExerciseMaximum);
            }
        }

        foreach (var exercise in BasicExercises.All)
        {
            if (!codes.Contains(exercise.Code))
            {
                return Result.Failure<List<Maximum>>(DomainErrors.AthleteProfile.MissingExerciseMaximum);
            }
        }

        return Result.Success(built);
    }

    private void ApplyMaximums(IReadOnlyCollection<Maximum> maximums)
    {
        foreach (var maximum in maximums)
        {
            var existing = _maximums.FirstOrDefault(
                current => string.Equals(current.ExerciseCode, maximum.ExerciseCode, StringComparison.Ordinal));

            if (existing is null)
            {
                _maximums.Add(maximum);
            }
            else
            {
                existing.SetRepetitions(maximum.Repetitions);
            }
        }
    }

    private static bool IsWithinRange(double value, double min, double max) =>
        double.IsFinite(value) && value >= min && value <= max;

    private static bool IsWithinRange(int value, int min, int max) => value >= min && value <= max;
}
