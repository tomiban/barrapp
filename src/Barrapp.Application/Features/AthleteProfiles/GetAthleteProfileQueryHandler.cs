using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;

namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Lee el perfil por el camino corto: delega en el puerto de lectura, que proyecta directo a DTO
/// sin cargar la entidad de dominio.
/// </summary>
internal sealed class GetAthleteProfileQueryHandler(IAthleteProfileReadService readService)
    : IQueryHandler<GetAthleteProfileQuery, AthleteProfileResponse>
{
    public async Task<Result<AthleteProfileResponse>> Handle(
        GetAthleteProfileQuery request,
        CancellationToken cancellationToken)
    {
        var response = await readService.GetAsync(SingleUser.Id, cancellationToken);

        return response is null
            ? Result.Failure<AthleteProfileResponse>(DomainErrors.AthleteProfile.NotFound)
            : Result.Success(response);
    }
}
