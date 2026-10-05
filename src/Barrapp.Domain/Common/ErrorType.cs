namespace Barrapp.Domain.Common;

/// <summary>
/// Clasifica un <see cref="Error"/> para que el adaptador HTTP elija el código de estado adecuado.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
}
