namespace Barrapp.Application.Abstractions;

/// <summary>
/// Unidad de trabajo: confirma en una sola transacción todos los cambios pendientes de los
/// repositorios. Es el único camino por el que un <i>command</i> persiste.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Guarda los cambios pendientes y devuelve cuántos se escribieron.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
