namespace Barrapp.Domain.Common;

/// <summary>
/// Error de dominio. Es un valor, no una excepción: el flujo esperado se comunica con <see cref="Result"/>.
/// </summary>
/// <param name="Code">Código estable y legible por máquina.</param>
/// <param name="Description">Descripción legible por personas.</param>
/// <param name="Type">Clasificación que usa la capa de presentación para traducir a HTTP.</param>
public sealed record Error(string Code, string Description, ErrorType Type = ErrorType.Failure)
{
    /// <summary>Ausencia de error. Es el estado de un resultado exitoso.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);
}
