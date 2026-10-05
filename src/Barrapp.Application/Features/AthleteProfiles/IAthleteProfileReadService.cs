namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Puerto de lectura del perfil: la query proyecta directamente a DTO sin pasar por el dominio.
/// Lo implementa Persistence, que es quien conoce EF Core; Application permanece sin referenciar
/// el motor de persistencia (ver <c>docs/architecture.md</c>).
/// </summary>
public interface IAthleteProfileReadService
{
    /// <summary>Devuelve el perfil del atleta o <c>null</c> si todavía no existe.</summary>
    Task<AthleteProfileResponse?> GetAsync(Guid userId, CancellationToken cancellationToken);
}
