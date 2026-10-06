using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Genera y devuelve el plan del mesociclo. Falla con <c>Not Found</c> si falta el perfil o el
/// objetivo y con <c>Validation</c> si la frecuencia todavía no tiene reparto.
/// </summary>
public sealed record GetPlanQuery : IQuery<PlanResponse>;
