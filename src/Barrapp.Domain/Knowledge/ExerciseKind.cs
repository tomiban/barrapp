namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Naturaleza de un ejercicio del catálogo: acondicionamiento general o movimiento
/// específico de un skill (ver <c>GLOSSARY.md</c>, términos <i>Acondicionamiento general</i> y
/// <i>Skill</i>).
/// </summary>
public enum ExerciseKind
{
    /// <summary>Acondicionamiento general (empuje, tirón, pierna, core, cardio).</summary>
    Conditioning = 0,

    /// <summary>Movimiento que forma parte de la escalera de un skill.</summary>
    Skill = 1,
}
