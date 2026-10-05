using Barrapp.Domain.Athlete;

namespace Barrapp.Application.Abstractions;

/// <summary>
/// Puerto de persistencia del perfil del atleta. En el MVP mono-usuario hay un único
/// perfil por atleta.
/// </summary>
public interface IAthleteProfileRepository
{
    /// <summary>Devuelve el perfil del atleta o <c>null</c> si todavía no existe.</summary>
    Task<AthleteProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Marca un perfil nuevo para que se inserte al guardar la unidad de trabajo.</summary>
    void Add(AthleteProfile profile);
}
