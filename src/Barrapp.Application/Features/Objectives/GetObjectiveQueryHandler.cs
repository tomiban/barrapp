using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.Objectives;

/// <summary>
/// Lee el objetivo por el camino corto: proyecta directo a DTO sobre
/// <see cref="IApplicationDbContext"/>, sin cargar la entidad de dominio.
/// </summary>
internal sealed class GetObjectiveQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetObjectiveQuery, ObjectiveResponse>
{
    public async Task<Result<ObjectiveResponse>> Handle(
        GetObjectiveQuery request,
        CancellationToken cancellationToken)
    {
        var response = await dbContext.Objectives
            .Where(objective => objective.UserId == SingleUser.Id)
            .Select(objective => new ObjectiveResponse(objective.SkillId))
            .FirstOrDefaultAsync(cancellationToken);

        return response is null
            ? Result.Failure<ObjectiveResponse>(DomainErrors.Objective.NotFound)
            : Result.Success(response);
    }
}
