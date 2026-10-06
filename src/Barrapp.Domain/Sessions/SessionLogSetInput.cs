namespace Barrapp.Domain.Sessions;

/// <summary>
/// Entrada de una serie para crear un <see cref="SessionLog"/>: número de serie, valor real
/// ejecutado y esfuerzo (RIR/RPE) opcional.
/// </summary>
/// <param name="SetNumber">Número de orden de la serie, desde 1.</param>
/// <param name="Value">Valor real ejecutado: reps o segundos según el ejercicio.</param>
/// <param name="Effort">Esfuerzo real (RIR/RPE), entre 0 y 10; <c>null</c> si no se anota.</param>
public sealed record SessionLogSetInput(int SetNumber, int Value, int? Effort = null);
