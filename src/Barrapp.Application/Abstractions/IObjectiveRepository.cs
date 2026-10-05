using Barrapp.Domain.Objectives;

namespace Barrapp.Application.Abstractions;

/// <summary>
/// Puerto de persistencia del objetivo del mesociclo. En el MVP mono-usuario hay un único
/// objetivo por atleta.
/// </summary>
public interface IObjectiveRepository
{
    /// <summary>Devuelve el objetivo del atleta o <c>null</c> si todavía no existe.</summary>
    Task<Objective?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Marca un objetivo nuevo para que se inserte al guardar la unidad de trabajo.</summary>
    void Add(Objective objective);
}
