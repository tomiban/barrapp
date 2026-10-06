using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.SoloSessions;

/// <summary>
/// Lista las sueltas y resuelve nombres contra el catálogo, igual que los registros de sesión.
/// </summary>
/// <remarks>
/// Excepción pragmática documentada a «las queries proyectan directo a DTO»: los ítems de la suelta
/// son una colección owned del agregado y el nombre de cada ejercicio se resuelve contra el
/// catálogo en memoria, así que se carga el árbol entero y se mapea con <see cref="SueltaResponses"/>,
/// el mismo patrón que ya usan el plan y el historial (ADR-0012 y ADR-0013).
/// </remarks>
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
