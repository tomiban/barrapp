namespace Barrapp.Domain.Athlete;

/// <summary>
/// Patrón de movimiento de un ejercicio básico: empuje, tirón o pierna
/// (ver <c>GLOSSARY.md</c>, término <i>Patrón</i>).
/// </summary>
public enum ExercisePattern
{
    /// <summary>Empuje (p. ej. flexiones).</summary>
    Push = 0,

    /// <summary>Tirón (p. ej. dominadas).</summary>
    Pull = 1,

    /// <summary>Pierna (p. ej. sentadillas).</summary>
    Legs = 2,
}
