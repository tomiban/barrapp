using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Lista los registros de sesión del atleta y resuelve el nombre y la unidad del ejercicio contra
/// el catálogo, igual que el plan (ADR-0012).
/// </summary>
/// <remarks>
/// Excepción pragmática documentada a «las queries proyectan directo a DTO»: las series son una
/// colección owned del agregado y SQLite no permite proyectarlas a un DTO plano sin partir la
/// consulta, así que se lee el árbol entero (registro + series) y se mapea en memoria con
/// <see cref="SessionLogResponses"/>, el mismo patrón que ya usan el plan y el historial
/// (ADR-0012 y ADR-0013).
/// </remarks>
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
