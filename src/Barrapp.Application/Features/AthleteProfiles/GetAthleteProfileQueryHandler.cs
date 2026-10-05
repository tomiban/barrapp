using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;

namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Lee el perfil por el camino corto: proyecta directo a DTO, sin cargar la entidad.
/// </summary>
internal sealed class GetAthleteProfileQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAthleteProfileQuery, AthleteProfileResponse>
{
    public Task<Result<AthleteProfileResponse>> Handle(
        GetAthleteProfileQuery request,
        CancellationToken cancellationToken)
    {
        var response = dbContext.AthleteProfiles
            .Where(profile => profile.UserId == SingleUser.Id)
            .Select(profile => new AthleteProfileResponse(
                profile.WeightKilograms,
                profile.HeightCentimeters))
            .FirstOrDefault();

        return Task.FromResult(response is null
            ? Result.Failure<AthleteProfileResponse>(DomainErrors.AthleteProfile.NotFound)
            : Result.Success(response));
    }
}
