using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Barrapp.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Lista los registros de sesión del atleta con sus ítems y su foto (ADR-0014), de la fecha más
/// reciente a la más antigua: es el Historial (spec 0003, pantalla 6), que agrupa por semana y filtra
/// por tipo de sesión. Sin registros todavía, devuelve una lista vacía.
/// </summary>
/// <remarks>
/// No consulta el catálogo: cada ítem lleva su propia foto (nombre, papel, unidad y objetivo), así
/// que el historial se lee sin regenerar el plan y aunque la base de conocimiento haya cambiado.
/// </remarks>
internal sealed class GetSessionLogsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetSessionLogsQuery, IReadOnlyList<SessionLogResponse>>
{
    public async Task<Result<IReadOnlyList<SessionLogResponse>>> Handle(
        GetSessionLogsQuery request,
        CancellationToken cancellationToken)
    {
        var logs = await dbContext.SessionLogs
            .AsNoTracking()
            .Include(log => log.Items)
            .Where(log => log.UserId == SingleUser.Id)
            .ToListAsync(cancellationToken);

        // SQLite no ordena por DateOnly en SQL; se ordena en memoria (misma convención que el resto
        // de lecturas del repo).
        return logs
            .OrderByDescending(log => log.SessionDate)
            .ThenByDescending(log => log.RecordedAtUtc)
            .Select(SessionLogResponses.From)
            .ToList();
    }
}
