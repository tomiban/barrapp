namespace Barrapp.Domain.Planning;

/// <summary>
/// Papel de un ejercicio dentro de una sesión: bloque de <i>skill</i>, trabajo de fuerza por
/// patrón o trabajo de core (ver <c>GLOSSARY.md</c>, términos <i>Sesión</i> y <i>Patrón</i>).
/// </summary>
public enum SessionItemRole
{
    /// <summary>Bloque de skill del objetivo, al inicio de la sesión.</summary>
    Skill = 0,

    /// <summary>Trabajo de fuerza general de un patrón (empuje, tirón o pierna).</summary>
    Strength = 1,

    /// <summary>Trabajo de core.</summary>
    Core = 2,
}
