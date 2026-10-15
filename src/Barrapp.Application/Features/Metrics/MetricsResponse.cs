namespace Barrapp.Application.Features.Metrics;

/// <summary>
/// Series de un microciclo agrupadas por patrón de fuerza (empuje/tirón/pierna). Es la unidad del
/// <i>Volumen semanal</i> (spec 0003, § Cimientos de backend y motor): se compara el volumen
/// <b>prescrito</b> por el motor con el <b>ejecutado</b> por el atleta.
/// </summary>
/// <param name="Push">Series de empuje del microciclo.</param>
/// <param name="Pull">Series de tirón del microciclo.</param>
/// <param name="Leg">Series de pierna del microciclo.</param>
public sealed record PatternSetsResponse(int Push, int Pull, int Leg);

/// <summary>
/// Métricas derivadas del mesociclo activo (spec 0003, § Cimientos de backend y motor). Se calculan
/// de los registros y del calendario; nunca se guardan como columnas mutables.
/// </summary>
/// <param name="AdherencePercent">
/// Adherencia en porcentaje: sesiones de mesociclo completadas entre las programadas hasta la fecha
/// actual. <c>0</c> cuando todavía no hay nada programado.
/// </param>
/// <param name="AdherenceCompleted">Sesiones de mesociclo marcadas como completadas.</param>
/// <param name="AdherenceScheduled">Sesiones de mesociclo programadas hasta la fecha actual.</param>
/// <param name="MicrocycleNumber">Microciclo al que pertenecen los volúmenes (el elegido o el en curso).</param>
/// <param name="Prescribed">Volumen prescrito por el motor para ese microciclo.</param>
/// <param name="Executed">Volumen ejecutado por el atleta en ese microciclo, según los registros.</param>
public sealed record MetricsResponse(
    int AdherencePercent,
    int AdherenceCompleted,
    int AdherenceScheduled,
    int MicrocycleNumber,
    PatternSetsResponse Prescribed,
    PatternSetsResponse Executed);
