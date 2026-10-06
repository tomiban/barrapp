namespace Barrapp.Domain.Sessions;

/// <summary>
/// Estado de una <see cref="SessionSuelta"/> en el historial. La suelta nace <see cref="Generated"/>
/// cuando el motor la compone y puede pasar a <see cref="Recorded"/> cuando el atleta la da por
/// hecha; esa transición no alimenta ningún otro flujo.
/// </summary>
public enum SessionSueltaStatus
{
    /// <summary>Generada por el motor y guardada en el historial, todavía sin hacer.</summary>
    Generated = 0,

    /// <summary>Marcada como registrada por el atleta.</summary>
    Recorded = 1,
}
