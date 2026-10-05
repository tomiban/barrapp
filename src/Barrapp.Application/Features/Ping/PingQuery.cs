using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Ping;

/// <summary>
/// Comprueba que el API y su pipeline de MediatR están vivos. Es el contrato del esqueleto caminante.
/// </summary>
public sealed record PingQuery : IQuery<PingResponse>;
