using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Barrapp.Domain.Planning;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Proyecta el historial de mesociclos cerrados (los pasados), más reciente primero, resolviendo el
/// nombre del skill contra el catálogo para la fila de resumen de la app.
/// </summary>
/// <remarks>
/// Excepción pragmática documentada a «las queries proyectan directo a DTO»: el plan del mesociclo
/// vive como snapshot JSON en una única columna y el nombre del skill se resuelve contra el
/// catálogo en memoria, así que se leen los mesociclos enteros y se proyecta con el mismo patrón
/// que el historial de sueltas y el plan (ADR-0012 y ADR-0013).
/// </remarks>
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
