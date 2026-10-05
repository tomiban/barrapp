using Barrapp.Domain.Common;
using MediatR;

namespace Barrapp.Application.Abstractions;

/// <summary>
/// Manejador de una <see cref="IQuery{TResponse}"/>.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
