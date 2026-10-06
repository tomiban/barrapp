using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Genera y devuelve el plan del mesociclo. Falla con <c>Not Found</c> si falta el perfil o el
/// objetivo, con <c>Validation</c> si el skill no tiene la etapa actual y con un error de catálogo
/// si falta un ejercicio o un máximo obligatorios.
/// </summary>
public sealed record GetPlanQuery : IQuery<PlanResponse>;
