namespace Barrapp.Domain.Planning;

/// <summary>
/// Foco de la sesión suelta: lo que el atleta quiere trabajar hoy. Un <i>patrón</i> centra la
/// sesión en la fuerza general de ese grupo; el <i>skill</i> la centra en el objetivo (su escalera
/// y sus rutinas de patrón); <i>sorpréndeme</i> deja que el motor elija de forma determinista.
/// </summary>
public enum SoloSessionFocus
{
    /// <summary>Centrar la sesión en un patrón de fuerza general (empuje, tirón o pierna).</summary>
    Pattern = 0,

    /// <summary>Centrar la sesión en el skill objetivo del atleta.</summary>
    Skill = 1,

    /// <summary>Que el motor elija por el atleta, con una variación determinista de las entradas.</summary>
    Surprise = 2,
}
