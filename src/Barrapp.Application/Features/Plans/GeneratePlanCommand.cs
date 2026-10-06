using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Genera el mesociclo con el motor y lo persiste como el mesociclo activo del atleta (ticket
/// #27, D7): reemplaza cualquier activo anterior y deja <c>GET /plan</c> sirviendo el plan
/// guardado. Devuelve el plan con el mismo contrato que la lectura.
/// </summary>
public sealed record GeneratePlanCommand : ICommand<PlanResponse>;