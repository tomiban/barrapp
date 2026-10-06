using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Abstractions;

/// <summary>
/// Puerto de persistencia del registro de sesión. Las escrituras las confirma la unidad de
/// trabajo; las lecturas del listado las proyecta <see cref="IApplicationDbContext"/>.
/// </summary>
public interface ISessionLogRepository
{
    /// <summary>Marca un registro nuevo para que se inserte al guardar la unidad de trabajo.</summary>
    void Add(SessionLog sessionLog);
}
