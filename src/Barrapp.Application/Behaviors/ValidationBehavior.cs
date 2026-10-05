using System.Reflection;
using Barrapp.Domain.Common;
using FluentValidation;
using MediatR;

namespace Barrapp.Application.Behaviors;

/// <summary>
/// Ejecuta los validadores de FluentValidation del request y corta el pipeline si falla,
/// devolviendo un <see cref="Result{TValue}"/> fallido.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validatorsList = validators.ToList();
        if (validatorsList.Count == 0)
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            validatorsList.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        var error = Error.Validation(
            "validation.failed",
            string.Join(" ", failures.Select(failure => failure.ErrorMessage)));

        return CreateFailure(error);
    }

    private static TResponse CreateFailure(Error error)
    {
        var failureMethod = typeof(TResponse).GetMethod(
            nameof(Result.Failure),
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            binder: null,
            types: [typeof(Error)],
            modifiers: null);

        return (TResponse)failureMethod!.Invoke(null, [error])!;
    }
}
