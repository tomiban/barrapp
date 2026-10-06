namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Grupo al que pertenece un ejercicio. Los tres primeros (<see cref="Push"/>, <see cref="Pull"/>,
/// <see cref="Leg"/>) son los <i>patrones</i> de fuerza general; <see cref="Core"/> y
/// <see cref="Cardio"/> completan el catálogo (ver <c>GLOSSARY.md</c>, término <i>Patrón</i>).
/// </summary>
public enum ExerciseGroup
{
    /// <summary>Empuje.</summary>
    Push = 0,

    /// <summary>Tirón.</summary>
    Pull = 1,

    /// <summary>Pierna.</summary>
    Leg = 2,

    /// <summary>Core.</summary>
    Core = 3,

    /// <summary>Cardio / metabólico.</summary>
    Cardio = 4,
}
