using Barrapp.Domain.Common;

namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Catálogo en memoria de la base de conocimiento: ejercicios, skills y programas generales.
/// </summary>
/// <remarks>
/// Se construye con <see cref="Create"/>, que valida en conjunto las referencias, la forma de las
/// escaleras y de las rutinas y devuelve un <see cref="Result{TValue}"/> con el catálogo o el
/// error de dominio correspondiente. El cargador de <c>Barrapp.Persistence</c> deserializa los
/// JSON y usa esta puerta como única validación.
/// </remarks>
public sealed class KnowledgeBase : IGenerationCatalog
{
    private KnowledgeBase(
        IReadOnlyList<Exercise> exercises,
        IReadOnlyList<Skill> skills,
        IReadOnlyList<RoutineProgram> programs)
    {
        Exercises = exercises;
        Skills = skills;
        Programs = programs;
    }

    /// <summary>Ejercicios del catálogo.</summary>
    public IReadOnlyList<Exercise> Exercises { get; }

    /// <summary>Skills con su escalera de progresión y sus rutinas de patrón.</summary>
    public IReadOnlyList<Skill> Skills { get; }

    /// <summary>Programas generales de acondicionamiento.</summary>
    public IReadOnlyList<RoutineProgram> Programs { get; }

    /// <summary>
    /// Valida el catálogo completo: ids únicos, referencias resolubles, forma de los ejercicios
    /// (<c>kind</c>/<c>group</c>/<c>skillId</c>), escaleras de 4–6 etapas con <c>order</c>
    /// consecutivo desde 1, criterios positivos, filas de rutina con rango y superseries
    /// consecutivas, y programas con al menos una rutina y bloques de al menos una vuelta.
    /// </summary>
    public static Result<KnowledgeBase> Create(
        IReadOnlyList<Exercise> exercises,
        IReadOnlyList<Skill> skills,
        IReadOnlyList<RoutineProgram> programs)
    {
        if (exercises is null || skills is null || programs is null)
        {
            return Result.Failure<KnowledgeBase>(DomainErrors.Knowledge.MissingData);
        }

        var exerciseById = new Dictionary<string, Exercise>(StringComparer.Ordinal);
        foreach (var exercise in exercises)
        {
            if (string.IsNullOrWhiteSpace(exercise.Id))
            {
                return Fail(DomainErrors.Knowledge.EmptyExerciseId);
            }

            if (string.IsNullOrWhiteSpace(exercise.Name))
            {
                return Fail(DomainErrors.Knowledge.EmptyExerciseName(exercise.Id));
            }

            if (!exerciseById.TryAdd(exercise.Id, exercise))
            {
                return Fail(DomainErrors.Knowledge.DuplicateExerciseId(exercise.Id));
            }
        }

        var skillById = new Dictionary<string, Skill>(StringComparer.Ordinal);
        foreach (var skill in skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Id))
            {
                return Fail(DomainErrors.Knowledge.EmptySkillId);
            }

            if (string.IsNullOrWhiteSpace(skill.Name))
            {
                return Fail(DomainErrors.Knowledge.EmptySkillName(skill.Id));
            }

            if (!skillById.TryAdd(skill.Id, skill))
            {
                return Fail(DomainErrors.Knowledge.DuplicateSkillId(skill.Id));
            }
        }

        var programById = new Dictionary<string, RoutineProgram>(StringComparer.Ordinal);
        foreach (var program in programs)
        {
            if (string.IsNullOrWhiteSpace(program.Id))
            {
                return Fail(DomainErrors.Knowledge.EmptyProgramId);
            }

            if (string.IsNullOrWhiteSpace(program.Name))
            {
                return Fail(DomainErrors.Knowledge.EmptyProgramName(program.Id));
            }

            if (!programById.TryAdd(program.Id, program))
            {
                return Fail(DomainErrors.Knowledge.DuplicateProgramId(program.Id));
            }
        }

        var exercisesValidation = ValidateExercises(exercises, exerciseById, skillById);
        if (exercisesValidation.IsFailure)
        {
            return Fail(exercisesValidation.Error);
        }

        var skillsValidation = ValidateSkills(skills, exerciseById);
        if (skillsValidation.IsFailure)
        {
            return Fail(skillsValidation.Error);
        }

        var programsValidation = ValidatePrograms(programs, exerciseById);
        if (programsValidation.IsFailure)
        {
            return Fail(programsValidation.Error);
        }

        return Result.Success(new KnowledgeBase(exercises, skills, programs));
    }

    /// <summary>Ejercicios del grupo indicado.</summary>
    public IReadOnlyList<Exercise> ExercisesByGroup(ExerciseGroup group) =>
        Exercises.Where(exercise => exercise.Group == group).ToList();

    /// <summary>Busca un ejercicio por su id; <c>null</c> si no existe.</summary>
    public Exercise? FindExercise(string exerciseId) =>
        Exercises.FirstOrDefault(exercise => string.Equals(exercise.Id, exerciseId, StringComparison.Ordinal));

    /// <summary>Busca un skill por su id; <c>null</c> si no existe.</summary>
    public Skill? FindSkill(string skillId) =>
        Skills.FirstOrDefault(skill => string.Equals(skill.Id, skillId, StringComparison.Ordinal));

    private static Result ValidateExercises(
        IReadOnlyList<Exercise> exercises,
        IReadOnlyDictionary<string, Exercise> exerciseById,
        IReadOnlyDictionary<string, Skill> skillById)
    {
        foreach (var exercise in exercises)
        {
            if (exercise.Kind == ExerciseKind.Conditioning)
            {
                if (exercise.Group is null)
                {
                    return Result.Failure(DomainErrors.Knowledge.ConditioningRequiresGroup(exercise.Id));
                }
            }
            else
            {
                if (exercise.Group is not null)
                {
                    return Result.Failure(DomainErrors.Knowledge.SkillExerciseHasGroup(exercise.Id));
                }

                if (string.IsNullOrWhiteSpace(exercise.SkillId))
                {
                    return Result.Failure(DomainErrors.Knowledge.SkillExerciseRequiresSkillId(exercise.Id));
                }
            }

            if (exercise.TracksMaximum && exercise.RegressionId is null)
            {
                return Result.Failure(DomainErrors.Knowledge.BasicExerciseRequiresRegression(exercise.Id));
            }

            if (exercise.RegressionId is not null && !exerciseById.ContainsKey(exercise.RegressionId))
            {
                return Result.Failure(DomainErrors.Knowledge.UnknownExerciseReference(exercise.RegressionId));
            }

            if (exercise.SkillId is not null && !skillById.ContainsKey(exercise.SkillId))
            {
                return Result.Failure(DomainErrors.Knowledge.UnknownSkillReference(exercise.SkillId));
            }
        }

        return Result.Success();
    }

    private static Result ValidateSkills(
        IReadOnlyList<Skill> skills,
        IReadOnlyDictionary<string, Exercise> exerciseById)
    {
        foreach (var skill in skills)
        {
            if (skill.Group == ExerciseGroup.Cardio)
            {
                return Result.Failure(DomainErrors.Knowledge.SkillGroupOutOfRange(skill.Id));
            }

            if (skill.Stages.Count is < 4 or > 6)
            {
                return Result.Failure(DomainErrors.Knowledge.SkillStagesOutOfRange(skill.Id));
            }

            var orders = new HashSet<int>();
            foreach (var stage in skill.Stages)
            {
                if (string.IsNullOrWhiteSpace(stage.Name))
                {
                    return Result.Failure(DomainErrors.Knowledge.EmptyStageName(skill.Id));
                }

                if (!orders.Add(stage.Order) || stage.Order < 1 || stage.Order > skill.Stages.Count)
                {
                    return Result.Failure(DomainErrors.Knowledge.StageOrderNotConsecutive(skill.Id));
                }

                if (!exerciseById.ContainsKey(stage.ExerciseId))
                {
                    return Result.Failure(DomainErrors.Knowledge.UnknownExerciseReference(stage.ExerciseId));
                }

                if (stage.Criterion.Target <= 0)
                {
                    return Result.Failure(
                        DomainErrors.Knowledge.CriterionTargetMustBePositive(skill.Id, stage.Order));
                }

                if (stage.Criterion.Sets <= 0)
                {
                    return Result.Failure(
                        DomainErrors.Knowledge.CriterionSetsMustBePositive(skill.Id, stage.Order));
                }
            }

            if (skill.PatternRoutines.Count == 0)
            {
                return Result.Failure(DomainErrors.Knowledge.EmptyPatternRoutines(skill.Id));
            }

            var routineIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var routine in skill.PatternRoutines)
            {
                if (string.IsNullOrWhiteSpace(routine.Id) || string.IsNullOrWhiteSpace(routine.Name))
                {
                    return Result.Failure(DomainErrors.Knowledge.EmptyRoutineName(skill.Id));
                }

                if (!routineIds.Add(routine.Id))
                {
                    return Result.Failure(DomainErrors.Knowledge.DuplicateRoutineId(skill.Id, routine.Id));
                }

                var itemsValidation = ValidateRoutineItems(
                    routine.Items,
                    $"la rutina de patrón '{skill.Id}/{routine.Id}'",
                    exerciseById);
                if (itemsValidation.IsFailure)
                {
                    return itemsValidation;
                }
            }
        }

        return Result.Success();
    }

    private static Result ValidatePrograms(
        IReadOnlyList<RoutineProgram> programs,
        IReadOnlyDictionary<string, Exercise> exerciseById)
    {
        foreach (var program in programs)
        {
            if (program.Routines.Count == 0)
            {
                return Result.Failure(DomainErrors.Knowledge.ProgramRequiresRoutines(program.Id));
            }

            var routineIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var routine in program.Routines)
            {
                if (string.IsNullOrWhiteSpace(routine.Id) || string.IsNullOrWhiteSpace(routine.Name))
                {
                    return Result.Failure(DomainErrors.Knowledge.EmptyRoutineName(program.Id));
                }

                if (!routineIds.Add(routine.Id))
                {
                    return Result.Failure(DomainErrors.Knowledge.DuplicateRoutineId(program.Id, routine.Id));
                }

                if (routine.Blocks.Count == 0)
                {
                    return Result.Failure(DomainErrors.Knowledge.RoutineRequiresBlocks(routine.Id));
                }

                foreach (var block in routine.Blocks)
                {
                    if (string.IsNullOrWhiteSpace(block.Name))
                    {
                        return Result.Failure(DomainErrors.Knowledge.EmptyBlockName(routine.Id));
                    }

                    if (block.Rounds < 1)
                    {
                        return Result.Failure(
                            DomainErrors.Knowledge.BlockRoundsMustBePositive(routine.Id, block.Name));
                    }

                    var context = $"el bloque '{block.Name}' de la rutina '{routine.Id}'";
                    if (block.RestSeconds < 0)
                    {
                        return Result.Failure(DomainErrors.Knowledge.RestSecondsMustBeNonNegative(context));
                    }

                    if (block.Items.Count == 0)
                    {
                        return Result.Failure(
                            DomainErrors.Knowledge.BlockItemsRequired(routine.Id, block.Name));
                    }

                    var itemsValidation = ValidateRoutineItems(block.Items, context, exerciseById);
                    if (itemsValidation.IsFailure)
                    {
                        return itemsValidation;
                    }
                }
            }
        }

        return Result.Success();
    }

    private static Result ValidateRoutineItems(
        IReadOnlyList<RoutineItem> items,
        string context,
        IReadOnlyDictionary<string, Exercise> exerciseById)
    {
        if (items.Count == 0)
        {
            return Result.Failure(DomainErrors.Knowledge.RoutineItemsRequired(context));
        }

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.ExerciseId) || !exerciseById.ContainsKey(item.ExerciseId))
            {
                return Result.Failure(DomainErrors.Knowledge.UnknownExerciseReference(item.ExerciseId));
            }

            if (item.Sets <= 0)
            {
                return Result.Failure(DomainErrors.Knowledge.RoutineItemSetsMustBePositive(context));
            }

            if (item.RestSeconds < 0)
            {
                return Result.Failure(DomainErrors.Knowledge.RestSecondsMustBeNonNegative(context));
            }

            var rangeValidation = ValidateRange(item, context);
            if (rangeValidation.IsFailure)
            {
                return rangeValidation;
            }
        }

        return ValidateSupersetGroups(items, context);
    }

    private static Result ValidateRange(RoutineItem item, string context)
    {
        var hasRepsRange = item.RepsMin.HasValue || item.RepsMax.HasValue;
        var hasHoldRange = item.HoldSecondsMin.HasValue || item.HoldSecondsMax.HasValue;

        if (!hasRepsRange && !hasHoldRange)
        {
            return Result.Failure(DomainErrors.Knowledge.RoutineItemRequiresRange(context));
        }

        if (hasRepsRange)
        {
            if (item.RepsMin is null || item.RepsMax is null)
            {
                return Result.Failure(DomainErrors.Knowledge.RoutineItemRequiresRange(context));
            }

            if (item.RepsMin > item.RepsMax)
            {
                return Result.Failure(DomainErrors.Knowledge.RoutineRangeMinGreaterThanMax(context));
            }
        }

        if (hasHoldRange)
        {
            if (item.HoldSecondsMin is null || item.HoldSecondsMax is null)
            {
                return Result.Failure(DomainErrors.Knowledge.RoutineItemRequiresRange(context));
            }

            if (item.HoldSecondsMin > item.HoldSecondsMax)
            {
                return Result.Failure(DomainErrors.Knowledge.RoutineRangeMinGreaterThanMax(context));
            }
        }

        return Result.Success();
    }

    private static Result ValidateSupersetGroups(IReadOnlyList<RoutineItem> items, string context)
    {
        var closedGroups = new HashSet<int>();
        int? currentGroup = null;

        foreach (var item in items)
        {
            if (item.SupersetGroup is null)
            {
                if (currentGroup is not null)
                {
                    closedGroups.Add(currentGroup.Value);
                    currentGroup = null;
                }

                continue;
            }

            var group = item.SupersetGroup.Value;
            if (currentGroup == group)
            {
                continue;
            }

            if (closedGroups.Contains(group))
            {
                return Result.Failure(DomainErrors.Knowledge.SupersetGroupNotConsecutive(context));
            }

            if (currentGroup is not null)
            {
                closedGroups.Add(currentGroup.Value);
            }

            currentGroup = group;
        }

        return Result.Success();
    }

    private static Result<KnowledgeBase> Fail(Error error) => Result.Failure<KnowledgeBase>(error);
}
