namespace Barrapp.Domain.Planning;

/// <summary>
/// Descanso por defecto de una <see cref="SessionItem"/>, en segundos, para cuando la fila no viene
/// de una rutina de la base de conocimiento (#95): el plan compone sus filas con los criterios de
/// la etapa y la carga derivada del máximo, sin pasar por rutina, y la app necesita igualmente un
/// descanso real que cronometrar.
/// </summary>
/// <remarks>
/// Cuando la fila sí sale de una rutina, el descanso de esa rutina manda: este default no lo
/// sobrescribe. Son valores por papel —skill 120 s, fuerza 90 s, core 60 s— y no por ejercicio.
/// </remarks>
public static class SessionItemRest
{
    /// <summary>Descanso del bloque de skill: práctica fresca, calidad antes que densidad.</summary>
    private const int SkillSeconds = 120;

    /// <summary>Descanso del trabajo de fuerza general, nunca al fallo (RIR).</summary>
    private const int StrengthSeconds = 90;

    /// <summary>Descanso del core de cierre, isométrico y de bajo gasto.</summary>
    private const int CoreSeconds = 60;

    /// <summary>Descanso por defecto, en segundos, para el papel <paramref name="role"/>.</summary>
    public static int DefaultFor(SessionItemRole role) => role switch
    {
        SessionItemRole.Skill => SkillSeconds,
        SessionItemRole.Strength => StrengthSeconds,
        SessionItemRole.Core => CoreSeconds,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };
}
