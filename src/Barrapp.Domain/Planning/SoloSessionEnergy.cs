namespace Barrapp.Domain.Planning;

/// <summary>
/// Energía que declara el atleta para la sesión suelta. Decide las series de cada trabajo y la
/// reserva: a menos energía, menos series y más repeticiones en reserva (entrenamiento más leve);
/// a más energía, más series y menos reserva.
/// </summary>
public enum SoloSessionEnergy
{
    /// <summary>Poca energía: sesión ligera, RIR alto.</summary>
    Low = 0,

    /// <summary>Energía normal.</summary>
    Medium = 1,

    /// <summary>Mucha energía: sesión intensa, RIR bajo.</summary>
    High = 2,
}
