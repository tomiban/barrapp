namespace Barrapp.Persistence.Knowledge;

/// <summary>
/// Los JSON de la base de conocimiento no cumplen el esquema: versión no soportada, JSON
/// malformado o datos que no pasan la validación de <c>KnowledgeBase.Create</c>. El arranque
/// falla rápido con este error.
/// </summary>
public sealed class KnowledgeBaseValidationException : InvalidOperationException
{
    /// <summary>Crea la excepción con un mensaje para personas y un código estable.</summary>
    public KnowledgeBaseValidationException(string message, string code)
        : base(message)
    {
        Code = code;
    }

    /// <summary>Código estable y legible por máquina del fallo de validación.</summary>
    public string Code { get; }
}
