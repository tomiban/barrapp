using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Metrics;

/// <summary>
/// Pide las métricas derivadas del mesociclo activo: la <i>Adherencia</i> y el <i>Volumen semanal</i>,
/// prescrito y ejecutado (spec 0003, § Cimientos de backend y motor).
/// </summary>
/// <param name="MicrocycleNumber">
/// Microciclo del que se quiere el volumen; si no se indica, se usa el semana en curso.
/// </param>
public sealed record GetMetricsQuery(int? MicrocycleNumber = null) : IQuery<MetricsResponse>;
