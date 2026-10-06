using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Abre el detalle de un mesociclo del historial: reconstruye el <see cref="Barrapp.Domain.Planning.Plan"/>
/// desde el snapshot guardado y lo proyecta con el mismo contrato que <c>GET /plan</c>, así la app
/// pinta un mesociclo pasado exactamente igual que el actual (ticket #27).
/// </summary>
internal sealed class GetMesocycleDetailQueryHandler(
    IApplicationDbContext dbContext,
    IKnowledgeBase catalog)
    : IQueryHandler<GetMesocycleDetailQuery, PlanResponse>
{
    public async Task<Result<PlanResponse>> Handle(
        GetMesocycleDetailQuery request,
        CancellationToken cancellationToken)
    {
        var mesocycle = await dbContext.Mesocycles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.MesocycleId
                    && candidate.UserId == SingleUser.Id,
                cancellationToken);

        if (mesocycle is null)
        {
            return Result.Failure<PlanResponse>(DomainErrors.Mesocycle.NotFound);
        }

        return PlanMappings.ToResponse(mesocycle.Snapshot.ToPlan(), catalog);
    }
}
