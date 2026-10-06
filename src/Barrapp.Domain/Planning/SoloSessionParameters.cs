using Barrapp.Domain.Knowledge;

namespace Barrapp.Domain.Planning;

/// <summary>
/// Parámetros de la sesión suelta: <see cref="Time"/> disponible, <see cref="Energy"/> declarada y
/// <see cref="Focus"/> elegido. Cuando el foco es <see cref="SoloSessionFocus.Pattern"/>,
/// <see cref="Pattern"/> indica el grupo (empuje, tirón o pierna); en el resto de focos es
/// <c>null</c>.
/// </summary>
public sealed record SoloSessionParameters(
    SoloSessionTime Time,
    SoloSessionEnergy Energy,
    SoloSessionFocus Focus,
    ExerciseGroup? Pattern = null);
