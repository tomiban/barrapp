using Barrapp.Domain.Common;
using MediatR;

namespace Barrapp.Application.Abstractions;

/// <summary>
/// Caso de uso de solo lectura. Siempre devuelve un <see cref="Result{TResponse}"/>.
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
