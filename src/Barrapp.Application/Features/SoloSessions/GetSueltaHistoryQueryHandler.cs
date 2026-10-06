using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.SoloSessions;

/// <summary>
/// Lista las sueltas proyectando directo a DTO (lado de lectura) y resolviendo nombres contra el
/// catálogo, igual que los registros de sesión. El snapshot viaja con el agregado, así que no hace
/// falta un include.
/// </summary>
internal sealed class GetSueltaHistoryQueryHandler(IApplicationDbContext dbContext, IKnowledgeBase catalog)
    : IQueryHandler<GetSueltaHistoryQuery, IReadOnlyList<SessionSueltaResponse>>
{
    public async Task<Result<IReadOnlyList<SessionSueltaResponse>>> Handle(
        GetSueltaHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var sueltas = await dbContext.SessionSuelta
            .AsNoTracking()
            .Where(suelta => suelta.UserId == SingleUser.Id)
            .ToListAsync(cancellationToken);

        // SQLite no ordena por DateTimeOffset en SQL; se ordena en memoria.
        return sueltas
            .OrderByDescending(suelta => suelta.CreatedAtUtc)
            .ThenByDescending(suelta => suelta.Id)
            .Select(suelta => SueltaResponses.From(suelta, catalog))
            .ToList();
    }
}