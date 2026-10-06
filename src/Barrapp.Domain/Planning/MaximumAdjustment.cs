using Barrapp.Domain.Athlete;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Sessions;

namespace Barrapp.Domain.Planning;

/// <summary>
/// Regla pura del ajuste de máximos al cerrar el mesociclo (spec 0001, US-24; ticket #24, D7):
/// dadas las sesiones registradas del mesociclo, para cada ejercicio básico el nuevo máximo es
/// <c>max(máximo actual, mejor marca real lograda)</c> y nunca baja.
/// </summary>
/// <remarks>
/// <para>
/// La <b>mejor marca</b> de un básico es la repetición más alta registrada en series de ese mismo
/// ejercicio básico (los tres básicos —<c>push_up</c>, <c>pull_up</c>, <c>squat</c>— se miden en
/// repeticiones en el catálogo). Solo cuentan los registros del básico en sí: las variantes (p. ej.
/// <c>dip</c>, <c>australian-row</c>) y las regresiones (p. ej. <c>incline-push-up</c>) quedan
/// fuera, porque su dificultad no es comparable con la del básico y traducir su marca al ancla
/// sobreestimaría o perjudicaría la prescripción («nunca al fallo»). El catálogo participa solo
/// para reconocer los básicos que arrastran máximo (<see cref="Exercise.TracksMaximum"/>) y
/// descartar marcas en segundos.
/// </para>
/// <para>
/// El cálculo es puro y determinista: no muta el perfil; devuelve los valores nuevos de los tres
/// básicos en el orden canónico (<see cref="BasicExercises.All"/>), que el caso de uso aplica al
/// perfil. Sin registros (o sin marcas que superen lo actual) los máximos quedan igual.
/// </para>
/// </remarks>
public static class MaximumAdjustment
{
    /// <summary>
    /// Calcula los máximos nuevos de los tres ejercicios básicos al cerrar el mesociclo:
    /// <c>max(máximo actual, mejor repetición registrada)</c>. Nunca devuelve un valor menor que
    /// el actual; un básico sin registros conserva su máximo.
    /// </summary>
    /// <param name="profile">El perfil con los máximos actuales; no se modifica.</param>
    /// <param name="logs">
    /// Los registros de sesión del mesociclo que se cierra; los de ejercicios que no son un básico
    /// (o que no arrastran máximo, o medidos en segundos) se ignoran.
    /// </param>
    /// <param name="catalog">El catálogo, para resolver la métrica y el arrastre de máximo.</param>
    public static IReadOnlyList<MaximumInput> Compute(
        AthleteProfile profile,
        IReadOnlyCollection<SessionLog> logs,
        IGenerationCatalog catalog)
    {
        var bestMarkByCode = BestMarkByCode(logs, catalog);

        return BasicExercises.All
            .Select(basic =>
            {
                var current = profile.MaximumFor(basic.Code) ?? 0;

                return new MaximumInput(
                    basic.Code,
                    bestMarkByCode.TryGetValue(basic.Code, out var bestMark)
                        ? Math.Max(current, bestMark)
                        : current);
            })
            .ToList();
    }

    /// <summary>La repetición más alta registrada por cada ejercicio básico.</summary>
    private static Dictionary<string, int> BestMarkByCode(
        IReadOnlyCollection<SessionLog> logs,
        IGenerationCatalog catalog)
    {
        var best = new Dictionary<string, int>(StringComparer.Ordinal);
        if (logs is null)
        {
            return best;
        }

        foreach (var log in logs)
        {
            // Solo las series de un básico que arrastra máximo y se mide en repeticiones; las
            // variantes, regresiones y los holds en segundos no aportan marca.
            if (log is null
                || BasicExercises.FindByCode(log.ExerciseId) is null
                || catalog.FindExercise(log.ExerciseId) is not { Metric: Metric.Reps, TracksMaximum: true })
            {
                continue;
            }

            var bestMark = log.Sets
                .Where(set => set is not null)
                .Select(set => set.Value)
                .DefaultIfEmpty(0)
                .Max();

            var previous = best.TryGetValue(log.ExerciseId, out var known) ? known : 0;
            if (bestMark > previous)
            {
                best[log.ExerciseId] = bestMark;
            }
        }

        return best;
    }
}