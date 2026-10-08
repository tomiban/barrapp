using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Barrapp.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Abre un registro de sesión por su identificador, con sus ítems y su foto (ADR-0014). Devuelve
/// <see cref="DomainErrors.SessionLog.NotFound"/> si no existe.
/// </summary>
internal sealed class GetSessionLogQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetSessionLogQuery, SessionLogResponse>
{
    public async Task<Result<SessionLogResponse>> Handle(
        GetSessionLogQuery request,
        CancellationToken cancellationToken)
    {
        var log = await dbContext.SessionLogs
            .AsNoTracking()
            .Include(candidate => candidate.Items)
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.SessionLogId && candidate.UserId == SingleUser.Id,
                cancellationToken);

        return log is null
            ? Result.Failure<SessionLogResponse>(DomainErrors.SessionLog.NotFound)
            : SessionLogResponses.From(log);
    }
}
