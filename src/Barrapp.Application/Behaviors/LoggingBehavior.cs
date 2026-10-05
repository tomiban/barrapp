using System.Diagnostics;
using Barrapp.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Barrapp.Application.Behaviors;

/// <summary>
/// Behavior más externo del pipeline: registra entrada, salida, duración y resultado fallido
/// de cada command/query. No captura excepciones (suben al exception handler global).
/// </summary>
internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Procesando {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        stopwatch.Stop();

        if (response is Result { IsFailure: true } result)
        {
            logger.LogWarning(
                "{RequestName} falló en {ElapsedMilliseconds} ms con {ErrorCode}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                result.Error.Code);
        }
        else
        {
            logger.LogInformation(
                "{RequestName} completado en {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}
