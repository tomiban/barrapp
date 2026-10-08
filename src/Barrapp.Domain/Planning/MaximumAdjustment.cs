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
/// repeticiones en el catálogo). El registro es la <b>sesión</b> y cada ítem guarda la foto del
/// ejercicio prescrito (ADR-0014), así que la marca se lee de los ítems del básico en sí: las
/// variantes (p. ej. <c>dip</c>, <c>australian-row</c>) y las regresiones (p. ej.
/// <c>incline-push-up</c>) quedan fuera, porque su dificultad no es comparable con la del básico y
/// traducir su marca al ancla sobreestimaría o perjudicaría la prescripción («nunca al fallo»).
/// El catálogo participa solo para reconocer los básicos que arrastran máximo
/// (<see cref="Exercise.TracksMaximum"/) y descartar marcas en segundos.
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
    /// Los registros de sesión del mesociclo que se cierra; los ítems de ejercicios que no son
    /// un básico (o que no arrastran máximo, o medidos en segundos) se ignoran.
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

        // El registro es la sesión; la marca se busca en sus ítems (foto por ítem, ADR-0014).
        foreach (var item in logs
            .Where(log => log is not null)
            .SelectMany(log => log.Items)
            .Where(item => item is not null))
        {
            // Solo las series de un básico que arrastra máximo y se mide en repeticiones; las
            // variantes, regresiones y los holds en segundos no aportan marca.
            if (BasicExercises.FindByCode(item.ExerciseId) is null
                || catalog.FindExercise(item.ExerciseId) is not { Metric: Metric.Reps, TracksMaximum: true })
            {
                continue;
            }

            var bestMark = item.Sets
                .Where(set => set is not null)
                .Select(set => set.Value)
                .DefaultIfEmpty(0)
                .Max();

            var previous = best.TryGetValue(item.ExerciseId, out var known) ? known : 0;
            if (bestMark > previous)
            {
                best[item.ExerciseId] = bestMark;
            }
        }

        return best;
    }
}
