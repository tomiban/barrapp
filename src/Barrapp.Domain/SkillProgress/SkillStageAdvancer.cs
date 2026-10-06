using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Sessions;

namespace Barrapp.Domain.SkillProgress;

/// <summary>
/// Motor determinista del avance de etapa del skill (spec 0001, US-19; decisión D4): evalúa si el
/// atleta cumplió el criterio de su etapa actual en dos sesiones consecutivas y, si es así,
/// devuelve la próxima etapa de la escalera. Es puro: mismas entradas, misma evaluación.
/// </summary>
/// <remarks>
/// Regla de «dos sesiones consecutivas», decidida para #23 y afinada por ADR-0014 (el registro es
/// la <b>sesión</b>):
/// <list type="bullet">
/// <item>
/// Una <b>sesión</b> es una fila del registro —la sesión del mesociclo o la suelta— en la que se
/// registró el ejercicio de la etapa actual (<see cref="SkillStage.ExerciseId"/>), buscado entre
/// los ítems que guardan la foto de lo prescrito. Las sesiones de otros ejercicios no cuentan: en
/// el reparto de 4 días el skill se practica solo en el tren superior, así que «consecutivas» se
/// refiere a las sesiones en las que se practicó la etapa.
/// </item>
/// <item>
/// La identidad de sesión es la clave de sesión determinista —mesociclo, microciclo y día
/// (ADR-0014)—: dos semanas distintas del mismo día no se funden. El motor no conoce la clave; el
/// caso de uso le pasa únicamente los registros del <b>mesociclo en curso</b> (#27, FIX-3), así que
/// las sesiones de un mesociclo cerrado ya no llegan aquí.
/// </item>
/// <item>
/// El tiempo de una sesión es el de su registro (<see cref="SessionLog.RecordedAtUtc"/>, que fija el
/// servidor); los ítems más recientes mandan sobre los anteriores (last-write-wins, coherente con la
/// sincronización offline, decisión D9).
/// </item>
/// <item>
/// Una sesión <b>cumple el criterio</b> cuando el ítem del ejercicio de la etapa trae al menos las
/// series exigidas y las primeras <see cref="StageCriterion.Sets"/> alcanzan el
/// <see cref="StageCriterion.Target"/> (según <see cref="StageCriterion.Metric"/>); si alguna de las
/// series exigidas se queda corta, no cumple.
/// </item>
/// <item>
/// Se avanza cuando las <b>dos sesiones de práctica más recientes</b> cumplen el criterio: por
/// construcción son consecutivas (nada entre ellas) y son las únicas que pueden justificar subir
/// hoy de etapa. Si ya se está en la última etapa de la escalera, no se avanza.
/// </item>
/// </list>
/// </remarks>
public static class SkillStageAdvancer
{
    /// <summary>
    /// Evalúa si <paramref name="currentStageOrder"/> debe avanzar con los registros
    /// <paramref name="logs"/>. El llamador debe pasar solo los registros del mesociclo en curso
    /// (FIX-3): el motor es puro y no conoce mesociclos. Falla con
    /// <see cref="DomainErrors.SkillProgress.UnknownSkill"/> si el skill es nulo y con
    /// <see cref="DomainErrors.SkillProgress.UnknownStage"/> si la etapa no existe en la escalera.
    /// Devolver la etapa de salida (subida o no) es determinista.
    /// </summary>
    public static Result<StageAdvanceEvaluation> Evaluate(
        Skill? skill,
        int currentStageOrder,
        IReadOnlyList<SessionLog>? logs)
    {
        if (skill is null || string.IsNullOrWhiteSpace(skill.Id))
        {
            return Result.Failure<StageAdvanceEvaluation>(DomainErrors.SkillProgress.UnknownSkill);
        }

        var currentStage = skill.Stages.FirstOrDefault(stage => stage.Order == currentStageOrder);
        if (currentStage is null)
        {
            return Result.Failure<StageAdvanceEvaluation>(DomainErrors.SkillProgress.UnknownStage);
        }

        var sessions = SkillSessions(currentStage, logs ?? []);
        if (sessions.Count < 2)
        {
            return NoAdvance(currentStageOrder);
        }

        var previous = sessions[^2];
        var last = sessions[^1];
        if (!MeetsCriterion(previous, currentStage.Criterion) || !MeetsCriterion(last, currentStage.Criterion))
        {
            return NoAdvance(currentStageOrder);
        }

        var nextStage = skill.Stages.FirstOrDefault(stage => stage.Order == currentStageOrder + 1);
        return nextStage is null
            ? NoAdvance(currentStageOrder)
            : Result.Success(new StageAdvanceEvaluation(nextStage.Order, Advanced: true));
    }

    /// <summary>
    /// Las sesiones de práctica del ejercicio de la etapa, de la más antigua a la más reciente,
    /// deduplicadas por su clave de sesión (ADR-0014): si la misma sesión se registró más de una
    /// vez, manda la última escritura.
    /// </summary>
    private static IReadOnlyList<SkillSession> SkillSessions(
        SkillStage stage,
        IReadOnlyList<SessionLog> logs) =>
        logs
            .Where(log => log is not null)
            .Select(log => new SkillSession(log, log.Items.FirstOrDefault(item => item is not null
                && string.Equals(item.ExerciseId, stage.ExerciseId, StringComparison.Ordinal))))
            .Where(session => session.Item is not null)
            .GroupBy(session => session.Key)
            .Select(group => group.OrderByDescending(session => session.Log.RecordedAtUtc).First())
            .OrderBy(session => session.Log.RecordedAtUtc)
            .ThenBy(session => session.Key)
            .ToList();

    /// <summary>
    /// La sesión cumple el criterio cuando su ítem registró al menos las series exigidas y las
    /// primeras <see cref="StageCriterion.Sets"/> series alcanzan el objetivo.
    /// </summary>
    private static bool MeetsCriterion(SkillSession session, StageCriterion criterion) =>
        session.Item!.Sets.Count >= criterion.Sets
        && session.Item.Sets
            .OrderBy(set => set.SetNumber)
            .Take(criterion.Sets)
            .All(set => set.Value >= criterion.Target);

    private static Result<StageAdvanceEvaluation> NoAdvance(int stageOrder) =>
        Result.Success(new StageAdvanceEvaluation(stageOrder, Advanced: false));

    /// <summary>
    /// Una sesión de práctica del ejercicio de la etapa: el registro, su clave de sesión
    /// (ADR-0014) y el ítem del ejercicio que se practicó.
    /// </summary>
    private sealed record SkillSession(SessionLog Log, SessionLogItem? Item)
    {
        /// <summary>
        /// Clave de sesión determinista: tipo, mesociclo, microciclo, día y fecha. Dos semanas
        /// distintas del mismo día de sesión no chocan.
        /// </summary>
        public string Key => string.Join(
            ':',
            Log.Kind,
            Log.MesocycleId?.ToString() ?? "-",
            Log.MicrocycleNumber?.ToString() ?? "-",
            Log.SessionDay?.ToString() ?? "-",
            Log.SessionDate.ToString("yyyy-MM-dd"));
    }
}
