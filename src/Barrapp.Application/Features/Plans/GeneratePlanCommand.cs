using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Genera el mesociclo con el motor y lo persiste como el mesociclo activo del atleta (ticket
/// #27, D7): reemplaza cualquier activo anterior y deja <c>GET /plan</c> sirviendo el plan
/// guardado. Devuelve el plan con el mismo contrato que la lectura.
/// </summary>
/// <param name="StartDate">
/// Fecha elegida por el atleta para arrancar el mesociclo (#94). Si no se indica, el mesociclo
/// arranca hoy; en cualquier caso arranca en el primer día de entrenamiento en o después de ella.
/// </param>
public sealed record GeneratePlanCommand(DateOnly? StartDate = null) : ICommand<PlanResponse>;
