using Barrapp.Application.Abstractions;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;

namespace Barrapp.Application.Features.Catalog;

/// <summary>
/// Proyecta los programas en memoria de <see cref="IKnowledgeBase"/> al DTO que consume la app:
/// rutinas con sus bloques y filas, y el tipo de programa en minúsculas.
/// </summary>
internal sealed class GetRoutinesQueryHandler(IKnowledgeBase knowledgeBase)
    : IQueryHandler<GetRoutinesQuery, IReadOnlyList<RoutineProgramResponse>>
{
    public Task<Result<IReadOnlyList<RoutineProgramResponse>>> Handle(
        GetRoutinesQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RoutineProgramResponse> programs = knowledgeBase.Programs
            .Select(ToResponse)
            .ToList();

        return Task.FromResult(Result.Success(programs));
    }

    private static RoutineProgramResponse ToResponse(RoutineProgram program) => new(
        program.Id,
        program.Name,
        CatalogMappings.ToCode(program.Type),
        program.Description,
        program.Routines.Select(ToResponse).ToList());

    private static ProgramRoutineResponse ToResponse(RoutineTemplate routine) => new(
        routine.Id,
        routine.Name,
        routine.Intensity,
        routine.DurationMinutes,
        routine.Blocks.Select(ToResponse).ToList());

    private static RoutineBlockResponse ToResponse(RoutineBlock block) => new(
        block.Name,
        block.Rounds,
        block.RestSeconds,
        block.Notes,
        block.Items.Select(CatalogMappings.ToResponse).ToList());
}
