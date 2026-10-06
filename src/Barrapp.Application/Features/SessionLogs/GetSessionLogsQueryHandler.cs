using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Lista los registros de sesión del atleta proyectando directo a DTO (lado de lectura) y
/// resolviendo el nombre y la unidad del ejercicio contra el catálogo, igual que el plan
/// (ADR-0012). Las series viajan con el agregado, así que no hace falta un include.
/// </summary>
internal sealed class GetSessionLogsQueryHandler(IApplicationDbContext dbContext, IKnowledgeBase knowledgeBase)
    : IQueryHandler<GetSessionLogsQuery, IReadOnlyList<SessionLogResponse>>
{
    public async Task<Result<IReadOnlyList<SessionLogResponse>>> Handle(
        GetSessionLogsQuery request,
        CancellationToken cancellationToken)
    {
        var logs = await dbContext.SessionLogs
            .AsNoTracking()
            .Where(log => log.UserId == SingleUser.Id)
            .OrderBy(log => log.SessionDay)
            .ThenBy(log => log.ExerciseId)
            .ToListAsync(cancellationToken);

        return logs
            .Select(log => SessionLogResponses.From(log, knowledgeBase))
            .ToList();
    }
}
