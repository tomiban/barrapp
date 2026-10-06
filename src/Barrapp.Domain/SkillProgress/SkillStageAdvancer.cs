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
/// Regla de «dos sesiones consecutivas», decidida para #23:
/// <list type="bullet">
/// <item>
/// Una <b>sesión</b> es un día de sesión (<see cref="SessionLog.SessionDay"/>) en el que se
/// registró el ejercicio de la etapa actual (<see cref="SkillStage.ExerciseId"/>). Las sesiones de
/// otros ejercicios no cuentan: en el reparto de 4 días el skill se practica solo en el tren
/// superior, así que «consecutivas» se refiere a las sesiones en las que se practicó la etapa.
/// </item>
/// <item>
/// Permanece <c>log.SessionDay</c> como identidad de sesión: el mesociclo aún no se persiste
/// (ticket #27) y no hay <c>MesocycleId</c> real. Dos registros del mismo ejercicio en el mismo
/// día se funden en una sola sesión, mandando el más reciente (<c>last-write-wins</c>, coherente
/// con la sincronización offline, decisión D9).
/// </item>
/// <item>
/// El tiempo de una sesión es el de su registro más reciente (<see cref="SessionLog.RecordedAtUtc"/>).
/// </item>
/// <item>
/// Una sesión <b>cumple el criterio</b> cuando las primeras <see cref="StageCriterion.Sets"/>
/// series alcanzan el <see cref="StageCriterion.Target"/> (según <see cref="StageCriterion.Metric"/>);
/// si alguna de las series exigidas se queda corta, no cumple.
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
    /// <paramref name="logs"/>. Falla con <see cref="DomainErrors.SkillProgress.UnknownSkill"/> si
    /// el skill es nulo y con <see cref="DomainErrors.SkillProgress.UnknownStage"/> si la etapa no
    /// existe en la escalera. Devolver la etapa de salida (subida o no) es determinista.
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
    /// Las sesiones de práctica del ejercicio de la etapa, de la más antigua a la más reciente. En
    /// un mismo día manda el registro más reciente (last-write-wins, ver remarks).
    /// </summary>
    private static IReadOnlyList<SkillSession> SkillSessions(
        SkillStage stage,
        IReadOnlyList<SessionLog> logs) =>
        logs
            .Where(log => log.ExerciseId == stage.ExerciseId)
            .GroupBy(log => log.SessionDay)
            .Select(group => new SkillSession(
                group.Key,
                group.Max(log => log.RecordedAtUtc),
                group.OrderByDescending(log => log.RecordedAtUtc).First()))
            .OrderBy(session => session.PerformedAtUtc)
            .ThenBy(session => session.SessionDay)
            .ToList();

    /// <summary>
    /// La sesión cumple el criterio cuando registró al menos las series exigidas y las primeras
    /// <see cref="StageCriterion.Sets"/> series alcanzan el objetivo.
    /// </summary>
    private static bool MeetsCriterion(SkillSession session, StageCriterion criterion) =>
        session.Log.Sets.Count >= criterion.Sets
        && session.Log.Sets
            .OrderBy(set => set.SetNumber)
            .Take(criterion.Sets)
            .All(set => set.Value >= criterion.Target);

    private static Result<StageAdvanceEvaluation> NoAdvance(int stageOrder) =>
        Result.Success(new StageAdvanceEvaluation(stageOrder, Advanced: false));

    /// <summary>Una sesión de práctica del ejercicio de la etapa, con su tiempo y su registro.</summary>
    private sealed record SkillSession(int SessionDay, DateTimeOffset PerformedAtUtc, SessionLog Log);
}
