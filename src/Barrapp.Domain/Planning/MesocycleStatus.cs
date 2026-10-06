namespace Barrapp.Domain.Planning;

/// <summary>
/// Estado del mesociclo persistido (#27, D7): <see cref="Active"/> mientras es el plan en curso y
/// <see cref="Closed"/> cuando ya forma parte del historial. La transición la expone
/// <see cref="Mesocycle.Close"/>; el ticket #24 la disparará desde la API.
/// </summary>
public enum MesocycleStatus
{
    /// <summary>Mesociclo en curso: a lo sumo uno por atleta.</summary>
    Active = 0,

    /// <summary>Mesociclo cerrado: ya está en el historial de pasados.</summary>
    Closed = 1,
}
