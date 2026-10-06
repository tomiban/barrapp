using Barrapp.Domain.Planning;

namespace Barrapp.Application.Abstractions;

/// <summary>
/// Puerto de persistencia del mesociclo (#27, D7). Las escrituras las confirma la unidad de
/// trabajo; el listado del historial lo proyecta <see cref="IApplicationDbContext"/>. El
/// mesociclo <see cref="MesocycleStatus.Active"/> se reemplaza al regenerar el plan, así que el
/// caso de uso lo carga y lo elimina; la invariante «a lo sumo un activo por atleta» se refuerza
/// en la base (índice filtrado de la configuración EF).
/// </summary>
public interface IMesocycleRepository
{
    /// <summary>Devuelve el mesociclo del atleta por identificador; <c>null</c> si no existe.</summary>
    Task<Mesocycle?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>Devuelve el mesociclo en curso del atleta; <c>null</c> si no hay ninguno activo.</summary>
    Task<Mesocycle?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Marca un mesociclo nuevo para que se inserte al guardar la unidad de trabajo.</summary>
    void Add(Mesocycle mesocycle);

    /// <summary>Marca un mesociclo para que se elimine al guardar la unidad de trabajo.</summary>
    void Remove(Mesocycle mesocycle);
}