using Barrapp.Domain.Common;
using MediatR;

namespace Barrapp.Application.Abstractions;

/// <summary>
/// Caso de uso que muta el estado. Siempre devuelve un <see cref="Result{TResponse}"/>.
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
