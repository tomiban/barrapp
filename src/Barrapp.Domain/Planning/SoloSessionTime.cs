namespace Barrapp.Domain.Planning;

/// <summary>
/// Tiempo disponible para la sesión suelta. El valor es el número de minutos que el atleta
/// declara tener; el motor lo traduce en cuánto trabajo entra en la sesión.
/// </summary>
public enum SoloSessionTime
{
    /// <summary>15 minutos: una sesión mínima, casi solo el foco.</summary>
    Minutes15 = 15,

    /// <summary>30 minutos.</summary>
    Minutes30 = 30,

    /// <summary>45 minutos.</summary>
    Minutes45 = 45,

    /// <summary>60 minutos: la sesión suelta más larga.</summary>
    Minutes60 = 60,
}
