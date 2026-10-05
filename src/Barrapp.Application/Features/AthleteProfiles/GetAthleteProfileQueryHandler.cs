using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Lee el perfil por el camino corto: proyecta directo a DTO sobre <see cref="IApplicationDbContext"/>,
/// sin cargar la entidad de dominio.
/// </summary>
internal sealed class GetAthleteProfileQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAthleteProfileQuery, AthleteProfileResponse>
{
    public async Task<Result<AthleteProfileResponse>> Handle(
        GetAthleteProfileQuery request,
        CancellationToken cancellationToken)
    {
        var response = await dbContext.AthleteProfiles
            .Where(profile => profile.UserId == SingleUser.Id)
            .Select(profile => new AthleteProfileResponse(
                profile.WeightKilograms,
                profile.HeightCentimeters,
                profile.TrainingDays,
                profile.Maximums
                    .OrderBy(maximum => maximum.ExerciseCode)
                    .Select(maximum => new MaximumResponse(maximum.ExerciseCode, maximum.Repetitions))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return response is null
            ? Result.Failure<AthleteProfileResponse>(DomainErrors.AthleteProfile.NotFound)
            : Result.Success(response);
    }
}
