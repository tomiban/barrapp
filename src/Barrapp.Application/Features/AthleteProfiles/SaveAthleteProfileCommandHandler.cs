using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;

namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Cargar el perfil → crearlo o actualizarlo → guardar. Los invariantes viven en
/// <see cref="AthleteProfile"/>.
/// </summary>
internal sealed class SaveAthleteProfileCommandHandler(
    IAthleteProfileRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SaveAthleteProfileCommand, AthleteProfileResponse>
{
    public async Task<Result<AthleteProfileResponse>> Handle(
        SaveAthleteProfileCommand request,
        CancellationToken cancellationToken)
    {
        // Los códigos del cable los valida el pipeline; aquí solo se traducen al día del dominio.
        IReadOnlyList<DayOfWeek>? weekdays = null;
        if (request.TrainingWeekdays is not null)
        {
            weekdays = request.TrainingWeekdays
                .Select(WeekdayCode.ToWeekday)
                .Select(weekday => weekday!.Value)
                .ToList();
        }

        var profile = await repository.GetByUserIdAsync(SingleUser.Id, cancellationToken);

        if (profile is null)
        {
            var creation = AthleteProfile.Create(
                SingleUser.Id,
                request.WeightKilograms,
                request.HeightCentimeters,
                request.ArmSpanCentimeters,
                request.InseamCentimeters,
                request.TrainingDays,
                request.Maximums,
                weekdays);

            if (creation.IsFailure)
            {
                return Result.Failure<AthleteProfileResponse>(creation.Error);
            }

            profile = creation.Value;
            repository.Add(profile);
        }
        else
        {
            var update = profile.Update(
                request.WeightKilograms,
                request.HeightCentimeters,
                request.ArmSpanCentimeters,
                request.InseamCentimeters,
                request.TrainingDays,
                request.Maximums,
                weekdays);
            if (update.IsFailure)
            {
                return Result.Failure<AthleteProfileResponse>(update.Error);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AthleteProfileResponse(
            profile.WeightKilograms,
            profile.HeightCentimeters,
            profile.ArmSpanCentimeters,
            profile.InseamCentimeters,
            profile.TrainingDays,
            MaximumResponses.From(profile.Maximums),
            profile.TrainingDaysInOrder().Select(WeekdayCode.ToCode).ToList());
    }
}
