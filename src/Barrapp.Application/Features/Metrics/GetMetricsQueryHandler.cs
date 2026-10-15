using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Application.Features.Plans;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Sessions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.Metrics;

/// <summary>
/// Calcula la <i>Adherencia</i> y el <i>Volumen semanal</i> del mesociclo activo (spec 0003, §
/// Cimientos de backend y motor). Ambas métricas son derivadas: se computan de los registros y del
/// calendario en cada lectura.
/// </summary>
/// <remarks>
/// La adherencia es «sesiones de mesociclo completadas / sesiones de mesociclo programadas hasta la
/// fecha actual»; las sesiones sueltas no entran ni en el numerador ni en el denominador. El volumen
/// prescrito sale del plan (reutiliza <see cref="GetPlanQuery"/> en lugar de duplicar la lógica del
/// mesociclo activo); el ejecutado, de los registros.
/// </remarks>
internal sealed class GetMetricsQueryHandler(
    ISender sender,
    IApplicationDbContext dbContext,
    TimeProvider timeProvider)
    : IQueryHandler<GetMetricsQuery, MetricsResponse>
{
    public async Task<Result<MetricsResponse>> Handle(
        GetMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var planResult = await sender.Send(new GetPlanQuery(), cancellationToken);
        if (planResult.IsFailure)
        {
            return Result.Failure<MetricsResponse>(planResult.Error);
        }

        var plan = planResult.Value;
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // Denominador: sesiones de mesociclo programadas hasta hoy (el plan solo programa mesociclo).
        var scheduled = plan.Microcycles
            .SelectMany(microcycle => microcycle.Sessions)
            .Count(session => session.Date is { } date && date <= today);

        // Numerador: sesiones de mesociclo completadas. Sin mesociclo persistido no hay ninguna.
        var completed = await dbContext.SessionLogs
            .AsNoTracking()
            .Where(log => log.UserId == SingleUser.Id
                && log.Kind == SessionLogKind.Mesocycle
                && log.MesocycleId == plan.MesocycleId
                && log.IsCompleted)
            .CountAsync(cancellationToken);

        var adherence = scheduled == 0
            ? 0
            : (int)Math.Min(100, Math.Round(100.0 * completed / scheduled));

        var microcycleNumber = request.MicrocycleNumber ?? CurrentMicrocycle(plan, today);
        var prescribed = PrescribedVolume(plan.Microcycles.FirstOrDefault(m => m.Number == microcycleNumber));
        var executed = await ExecutedVolume(dbContext, plan.MesocycleId, microcycleNumber, cancellationToken);

        return new MetricsResponse(
            adherence,
            completed,
            scheduled,
            microcycleNumber,
            prescribed,
            executed);
    }

    /// <summary>
    /// Semana en curso: la primera con alguna sesión programada en o después de hoy; si el mesociclo
    /// ya pasó entero, la última.
    /// </summary>
    private static int CurrentMicrocycle(PlanResponse plan, DateOnly today) => plan.Microcycles
            .Where(microcycle => microcycle.Sessions.Any(session => session.Date is { } date && date >= today))
            .Select(microcycle => microcycle.Number)
            .DefaultIfEmpty(plan.Microcycles.Count)
            .Min();

    /// <summary>Volumen prescrito: series por patrón del microciclo (el bloque de skill y el core no tienen patrón).</summary>
    private static PatternSetsResponse PrescribedVolume(MicrocycleResponse? microcycle)
    {
        if (microcycle is null)
        {
            return new PatternSetsResponse(0, 0, 0);
        }

        var push = 0;
        var pull = 0;
        var leg = 0;
        foreach (var session in microcycle.Sessions)
        {
            foreach (var item in session.Items)
            {
                if (item.Pattern is null)
                {
                    continue;
                }

                var sets = item.Sets;
                if (string.Equals(item.Pattern, "push", StringComparison.Ordinal))
                {
                    push += sets;
                }
                else if (string.Equals(item.Pattern, "pull", StringComparison.Ordinal))
                {
                    pull += sets;
                }
                else if (string.Equals(item.Pattern, "leg", StringComparison.Ordinal))
                {
                    leg += sets;
                }
            }
        }

        return new PatternSetsResponse(push, pull, leg);
    }

    /// <summary>Volumen ejecutado: series registradas por patrón en ese microciclo del mesociclo activo.</summary>
    private static async Task<PatternSetsResponse> ExecutedVolume(
        IApplicationDbContext dbContext,
        Guid? mesocycleId,
        int microcycleNumber,
        CancellationToken cancellationToken)
    {
        if (mesocycleId is null)
        {
            return new PatternSetsResponse(0, 0, 0);
        }

        var items = await dbContext.SessionLogs
            .AsNoTracking()
            .Where(log => log.UserId == SingleUser.Id
                && log.Kind == SessionLogKind.Mesocycle
                && log.MesocycleId == mesocycleId
                && log.MicrocycleNumber == microcycleNumber)
            .SelectMany(log => log.Items)
            .ToListAsync(cancellationToken);

        var push = 0;
        var pull = 0;
        var leg = 0;
        foreach (var item in items)
        {
            if (item.Pattern is not { } pattern)
            {
                continue;
            }

            var sets = item.Sets.Count;
            if (pattern == ExerciseGroup.Push)
            {
                push += sets;
            }
            else if (pattern == ExerciseGroup.Pull)
            {
                pull += sets;
            }
            else if (pattern == ExerciseGroup.Leg)
            {
                leg += sets;
            }
        }

        return new PatternSetsResponse(push, pull, leg);
    }
}
