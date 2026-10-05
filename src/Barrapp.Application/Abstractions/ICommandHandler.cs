using Barrapp.Domain.Common;
using MediatR;

namespace Barrapp.Application.Abstractions;

/// <summary>
/// Manejador de un <see cref="ICommand{TResponse}"/>.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
