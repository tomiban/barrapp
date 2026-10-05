using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Devuelve el perfil del atleta. Falla con <c>Not Found</c> si todavía no hay ninguno guardado.
/// </summary>
public sealed record GetAthleteProfileQuery : IQuery<AthleteProfileResponse>;
