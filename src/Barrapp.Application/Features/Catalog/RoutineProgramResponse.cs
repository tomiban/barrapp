namespace Barrapp.Application.Features.Catalog;

/// <summary>Bloque de una rutina de programa: un circuito con sus vueltas y sus filas.</summary>
/// <param name="Name">Nombre del bloque para la UI, en español.</param>
/// <param name="Rounds">Vueltas del circuito.</param>
/// <param name="RestSeconds">Descanso entre vueltas, en segundos.</param>
/// <param name="Notes">Notas para la UI, en español; opcional.</param>
/// <param name="Items">Filas del bloque.</param>
public sealed record RoutineBlockResponse(
    string Name,
    int Rounds,
    int RestSeconds,
    string? Notes,
    IReadOnlyList<RoutineItemResponse> Items);

/// <summary>Rutina de un programa general: un modelo con intensidad, duración y bloques.</summary>
/// <param name="Id">Identificador único dentro del programa.</param>
/// <param name="Name">Nombre de la rutina para la UI, en español.</param>
/// <param name="Intensity">Intensidad del modelo (2 o 3).</param>
/// <param name="DurationMinutes">Duración estimada en minutos; opcional.</param>
/// <param name="Blocks">Bloques de la rutina.</param>
public sealed record ProgramRoutineResponse(
    string Id,
    string Name,
    int Intensity,
    int? DurationMinutes,
    IReadOnlyList<RoutineBlockResponse> Blocks);

/// <summary>
/// Programa general de acondicionamiento con sus rutinas, tal y como lo consume la app. El tipo va
/// en minúsculas (<c>circuit</c> para circuitos por tiempo, <c>strength</c> para fuerza por reps).
/// </summary>
/// <param name="Id">Slug estable del programa.</param>
/// <param name="Name">Nombre para la UI, en español.</param>
/// <param name="Type">Tipo de programa en minúsculas.</param>
/// <param name="Description">Descripción para la UI, en español; opcional.</param>
/// <param name="Routines">Rutinas del programa.</param>
public sealed record RoutineProgramResponse(
    string Id,
    string Name,
    string Type,
    string? Description,
    IReadOnlyList<ProgramRoutineResponse> Routines);
