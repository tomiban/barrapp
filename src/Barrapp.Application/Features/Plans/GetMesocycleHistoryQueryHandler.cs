using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Barrapp.Domain.Planning;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Proyecta el historial de mesociclos cerrados (los pasados) directo a DTO (lado de lectura),
/// más reciente primero, resolviendo el nombre del skill contra el catálogo para la fila de
/// resumen de la app.
/// </summary>
internal sealed class GetMesocycleHistoryQueryHandler(
    IApplicationDbContext dbContext,
    IKnowledgeBase catalog)
    : IQueryHandler<GetMesocycleHistoryQuery, IReadOnlyList<MesocycleSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<MesocycleSummaryResponse>>> Handle(
        GetMesocycleHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var mesocycles = await dbContext.Mesocycles
            .AsNoTracking()
            .Where(mesocycle => mesocycle.UserId == SingleUser.Id
                && mesocycle.Status == MesocycleStatus.Closed)
            .ToListAsync(cancellationToken);

        // SQLite no ordena por DateTimeOffset en SQL; se ordena en memoria.
        return mesocycles
            .OrderByDescending(mesocycle => mesocycle.ClosedAtUtc)
            .ThenByDescending(mesocycle => mesocycle.Id)
            .Select(mesocycle => new MesocycleSummaryResponse(
                mesocycle.Id,
                mesocycle.SkillId,
                catalog.FindSkill(mesocycle.SkillId)?.Name ?? mesocycle.SkillId,
                mesocycle.TrainingDays,
                mesocycle.StartedAtUtc,
                mesocycle.ClosedAtUtc!.Value))
            .ToList();
    }
}
