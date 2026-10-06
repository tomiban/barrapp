namespace Barrapp.Domain.Common;

/// <summary>
/// Errores de dominio, agrupados por agregado. Son valores, no excepciones: el flujo
/// esperado se comunica con <see cref="Result"/>.
/// </summary>
public static class DomainErrors
{
    /// <summary>Errores del perfil del atleta.</summary>
    public static class AthleteProfile
    {
        /// <summary>El peso está fuera del rango admitido (30–200 kg).</summary>
        public static readonly Error WeightOutOfRange = Error.Validation(
            "athlete_profile.weight_out_of_range",
            "El peso debe estar entre 30 y 200 kg.");

        /// <summary>La altura está fuera del rango admitido (120–220 cm).</summary>
        public static readonly Error HeightOutOfRange = Error.Validation(
            "athlete_profile.height_out_of_range",
            "La altura debe estar entre 120 y 220 cm.");

        /// <summary>Los días de entrenamiento están fuera del rango admitido (3–5).</summary>
        public static readonly Error TrainingDaysOutOfRange = Error.Validation(
            "athlete_profile.training_days_out_of_range",
            "Los días de entrenamiento deben estar entre 3 y 5.");

        /// <summary>El máximo de un ejercicio no puede ser negativo (0 sí vale).</summary>
        public static readonly Error MaximumMustBeNonNegative = Error.Validation(
            "athlete_profile.maximum_must_be_non_negative",
            "El máximo no puede ser negativo.");

        /// <summary>El código no corresponde a ningún ejercicio básico del catálogo.</summary>
        public static readonly Error UnknownExerciseCode = Error.Validation(
            "athlete_profile.unknown_exercise_code",
            "El ejercicio indicado no es un ejercicio básico.");

        /// <summary>Hay dos máximos para el mismo ejercicio.</summary>
        public static readonly Error DuplicateExerciseMaximum = Error.Validation(
            "athlete_profile.duplicate_exercise_maximum",
            "No puedes repetir el máximo de un mismo ejercicio.");

        /// <summary>Falta el máximo de algún ejercicio básico.</summary>
        public static readonly Error MissingExerciseMaximum = Error.Validation(
            "athlete_profile.missing_exercise_maximum",
            "Debes indicar el máximo de todos los ejercicios básicos.");

        /// <summary>Todavía no hay ningún perfil guardado.</summary>
        public static readonly Error NotFound = Error.NotFound(
            "athlete_profile.not_found",
            "No hay ningún perfil guardado para este atleta.");
    }

    /// <summary>Errores del objetivo del mesociclo.</summary>
    public static class Objective
    {
        /// <summary>El skill indicado no existe en el catálogo.</summary>
        public static readonly Error UnknownSkill = Error.Validation(
            "objective.unknown_skill",
            "El skill indicado no existe en el catálogo.");

        /// <summary>Todavía no hay ningún objetivo guardado.</summary>
        public static readonly Error NotFound = Error.NotFound(
            "objective.not_found",
            "No hay ningún objetivo guardado para este atleta.");
    }

    /// <summary>Errores de la etapa actual del atleta por skill.</summary>
    public static class SkillProgress
    {
        /// <summary>El skill indicado no existe en el catálogo.</summary>
        public static readonly Error UnknownSkill = Error.Validation(
            "skill_progress.unknown_skill",
            "El skill indicado no existe en el catálogo.");

        /// <summary>La etapa indicada no existe en la escalera del skill.</summary>
        public static readonly Error UnknownStage = Error.Validation(
            "skill_progress.unknown_stage",
            "La etapa indicada no existe en la escalera del skill.");
    }

    /// <summary>Errores de la base de conocimiento (catálogo, escaleras y rutinas).</summary>
    public static class Knowledge
    {
        /// <summary>Faltan datos obligatorios.</summary>
        public static readonly Error MissingData = Error.Validation(
            "knowledge.missing_data",
            "Los datos de la base de conocimiento son obligatorios.");

        /// <summary>El catálogo no tiene ningún ejercicio que servir.</summary>
        public static readonly Error EmptyCatalog = Error.NotFound(
            "knowledge.empty_catalog",
            "El catálogo de ejercicios está vacío.");

        /// <summary>Un ejercicio no tiene id.</summary>
        public static readonly Error EmptyExerciseId = Error.Validation(
            "knowledge.empty_exercise_id",
            "Todo ejercicio necesita un id.");

        /// <summary>Un skill no tiene id.</summary>
        public static readonly Error EmptySkillId = Error.Validation(
            "knowledge.empty_skill_id",
            "Todo skill necesita un id.");

        /// <summary>Un programa no tiene id.</summary>
        public static readonly Error EmptyProgramId = Error.Validation(
            "knowledge.empty_program_id",
            "Todo programa necesita un id.");

        /// <summary>Hay dos ejercicios con el mismo id.</summary>
        public static Error DuplicateExerciseId(string id) => Error.Validation(
            "knowledge.duplicate_exercise_id",
            $"Hay más de un ejercicio con el id '{id}'.");

        /// <summary>Hay dos skills con el mismo id.</summary>
        public static Error DuplicateSkillId(string id) => Error.Validation(
            "knowledge.duplicate_skill_id",
            $"Hay más de un skill con el id '{id}'.");

        /// <summary>Hay dos programas con el mismo id.</summary>
        public static Error DuplicateProgramId(string id) => Error.Validation(
            "knowledge.duplicate_program_id",
            $"Hay más de un programa con el id '{id}'.");

        /// <summary>Hay dos rutinas con el mismo id dentro del mismo padre.</summary>
        public static Error DuplicateRoutineId(string ownerId, string routineId) => Error.Validation(
            "knowledge.duplicate_routine_id",
            $"Hay más de una rutina con el id '{routineId}' en '{ownerId}'.");

        /// <summary>Un ejercicio no tiene nombre.</summary>
        public static Error EmptyExerciseName(string id) => Error.Validation(
            "knowledge.empty_exercise_name",
            $"El ejercicio '{id}' necesita un nombre.");

        /// <summary>Un skill no tiene nombre.</summary>
        public static Error EmptySkillName(string id) => Error.Validation(
            "knowledge.empty_skill_name",
            $"El skill '{id}' necesita un nombre.");

        /// <summary>Un programa no tiene nombre.</summary>
        public static Error EmptyProgramName(string id) => Error.Validation(
            "knowledge.empty_program_name",
            $"El programa '{id}' necesita un nombre.");

        /// <summary>Una etapa no tiene nombre.</summary>
        public static Error EmptyStageName(string skillId) => Error.Validation(
            "knowledge.empty_stage_name",
            $"Todas las etapas del skill '{skillId}' necesitan un nombre.");

        /// <summary>Una rutina o bloque no tiene nombre.</summary>
        public static Error EmptyRoutineName(string ownerId) => Error.Validation(
            "knowledge.empty_routine_name",
            $"Todas las rutinas de '{ownerId}' necesitan id y nombre.");

        /// <summary>Un bloque no tiene nombre.</summary>
        public static Error EmptyBlockName(string routineId) => Error.Validation(
            "knowledge.empty_block_name",
            $"Todos los bloques de la rutina '{routineId}' necesitan un nombre.");

        /// <summary>Una referencia apunta a un ejercicio inexistente.</summary>
        public static Error UnknownExerciseReference(string exerciseId) => Error.Validation(
            "knowledge.unknown_exercise_reference",
            $"El ejercicio referenciado '{exerciseId}' no existe en el catálogo.");

        /// <summary>Una referencia apunta a un skill inexistente.</summary>
        public static Error UnknownSkillReference(string skillId) => Error.Validation(
            "knowledge.unknown_skill_reference",
            $"El skill referenciado '{skillId}' no existe en el catálogo.");

        /// <summary>Un ejercicio de acondicionamiento no declara grupo.</summary>
        public static Error ConditioningRequiresGroup(string id) => Error.Validation(
            "knowledge.conditioning_requires_group",
            $"El ejercicio de acondicionamiento '{id}' necesita un grupo.");

        /// <summary>Un ejercicio que registra su máximo no declara regresión.</summary>
        public static Error BasicExerciseRequiresRegression(string id) => Error.Validation(
            "knowledge.basic_exercise_requires_regression",
            $"El ejercicio básico '{id}' registra su máximo y necesita declarar una regresión.");

        /// <summary>Un movimiento de skill no declara su skill.</summary>
        public static Error SkillExerciseRequiresSkillId(string id) => Error.Validation(
            "knowledge.skill_exercise_requires_skill_id",
            $"El movimiento de skill '{id}' necesita un skillId.");

        /// <summary>Un movimiento de skill declara grupo.</summary>
        public static Error SkillExerciseHasGroup(string id) => Error.Validation(
            "knowledge.skill_exercise_has_group",
            $"El movimiento de skill '{id}' no puede declarar grupo.");

        /// <summary>Un skill entrena cardio, que no es un patrón.</summary>
        public static Error SkillGroupOutOfRange(string id) => Error.Validation(
            "knowledge.skill_group_out_of_range",
            $"El skill '{id}' debe entrenar un patrón (push, pull, leg o core), no cardio.");

        /// <summary>Un skill no tiene entre 4 y 6 etapas.</summary>
        public static Error SkillStagesOutOfRange(string id) => Error.Validation(
            "knowledge.skill_stages_out_of_range",
            $"La escalera del skill '{id}' debe tener entre 4 y 6 etapas.");

        /// <summary>Los órdenes de las etapas no son consecutivos desde 1.</summary>
        public static Error StageOrderNotConsecutive(string id) => Error.Validation(
            "knowledge.stage_order_not_consecutive",
            $"El orden de las etapas del skill '{id}' debe ser único y consecutivo desde 1.");

        /// <summary>Un skill no tiene rutinas de patrón.</summary>
        public static Error EmptyPatternRoutines(string id) => Error.Validation(
            "knowledge.empty_pattern_routines",
            $"El skill '{id}' necesita al menos una rutina de patrón.");

        /// <summary>El objetivo del criterio de una etapa no es positivo.</summary>
        public static Error CriterionTargetMustBePositive(string skillId, int order) => Error.Validation(
            "knowledge.criterion_target_must_be_positive",
            $"El objetivo de la etapa {order} del skill '{skillId}' debe ser mayor que 0.");

        /// <summary>Las series del criterio de una etapa no son positivas.</summary>
        public static Error CriterionSetsMustBePositive(string skillId, int order) => Error.Validation(
            "knowledge.criterion_sets_must_be_positive",
            $"Las series de la etapa {order} del skill '{skillId}' deben ser mayores que 0.");

        /// <summary>Una rutina no tiene filas.</summary>
        public static Error RoutineItemsRequired(string context) => Error.Validation(
            "knowledge.routine_items_required",
            $"La rutina {context} necesita al menos una fila.");

        /// <summary>Las series de una fila no son positivas.</summary>
        public static Error RoutineItemSetsMustBePositive(string context) => Error.Validation(
            "knowledge.routine_item_sets_must_be_positive",
            $"Las series de una fila de {context} deben ser mayores que 0.");

        /// <summary>El descanso de una fila es negativo.</summary>
        public static Error RestSecondsMustBeNonNegative(string context) => Error.Validation(
            "knowledge.rest_seconds_must_be_non_negative",
            $"El descanso de {context} no puede ser negativo.");

        /// <summary>Una fila no declara ningún rango.</summary>
        public static Error RoutineItemRequiresRange(string context) => Error.Validation(
            "knowledge.routine_item_requires_range",
            $"Cada fila de {context} debe declarar un rango de repeticiones o de segundos (mínimo y máximo).");

        /// <summary>El mínimo de un rango supera a su máximo.</summary>
        public static Error RoutineRangeMinGreaterThanMax(string context) => Error.Validation(
            "knowledge.routine_range_min_greater_than_max",
            $"El mínimo de un rango de {context} no puede superar al máximo.");

        /// <summary>Un grupo de superserie no es consecutivo.</summary>
        public static Error SupersetGroupNotConsecutive(string context) => Error.Validation(
            "knowledge.superset_group_not_consecutive",
            $"Los grupos de superserie de {context} deben agrupar filas consecutivas.");

        /// <summary>Un programa no tiene rutinas.</summary>
        public static Error ProgramRequiresRoutines(string id) => Error.Validation(
            "knowledge.program_requires_routines",
            $"El programa '{id}' necesita al menos una rutina.");

        /// <summary>Una rutina de programa no tiene bloques.</summary>
        public static Error RoutineRequiresBlocks(string routineId) => Error.Validation(
            "knowledge.routine_requires_blocks",
            $"La rutina '{routineId}' necesita al menos un bloque.");

        /// <summary>Un bloque no tiene filas.</summary>
        public static Error BlockItemsRequired(string routineId, string blockName) => Error.Validation(
            "knowledge.block_items_required",
            $"El bloque '{blockName}' de la rutina '{routineId}' necesita al menos una fila.");

        /// <summary>Un bloque no da al menos una vuelta.</summary>
        public static Error BlockRoundsMustBePositive(string routineId, string blockName) => Error.Validation(
            "knowledge.block_rounds_must_be_positive",
            $"El bloque '{blockName}' de la rutina '{routineId}' debe dar al menos una vuelta.");
    }
}
