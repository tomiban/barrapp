using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Abstractions;

/// <summary>
/// Puerto de persistencia de la sesión suelta del historial. Las escrituras las confirma la unidad
/// de trabajo; el listado lo proyecta <see cref="IApplicationDbContext"/>. Guardar una suelta es
/// independiente de <see cref="ISessionLogRepository"/>: el aislamiento entre historial de sueltas
/// y registros de sesión es estructural (D8).
/// </summary>
public interface ISessionSueltaRepository
{
    /// <summary>Devuelve la suelta del atleta por identificador; <c>null</c> si no existe.</summary>
    Task<SessionSuelta?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>Marca una suelta nueva para que se inserte al guardar la unidad de trabajo.</summary>
    void Add(SessionSuelta sessionSuelta);
}
