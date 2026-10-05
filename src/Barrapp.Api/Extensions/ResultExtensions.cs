using Barrapp.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace Barrapp.Api.Extensions;

internal static class ResultExtensions
{
    /// <summary>
    /// Traduce un <see cref="Error"/> de dominio a un Problem Details con el código de estado adecuado.
    /// </summary>
    public static IResult ToProblemDetails(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Description);
    }
}
