namespace Barrapp.Domain.Sessions;

/// <summary>
/// Entrada de una serie para registrar dentro de un <see cref="SessionLogItem"/>: número de serie,
/// valor real ejecutado y, opcionalmente, el <b>RIR real</b> (informativo: no mueve el avance de
/// etapa, spec 0001, US-35) y el <b>lastre</b> añadido a la serie (ver <c>GLOSSARY.md</c>).
/// </summary>
/// <param name="SetNumber">Número de orden de la serie, desde 1.</param>
/// <param name="Value">
/// Valor real ejecutado: repeticiones o segundos según la unidad que deriva el tipo de ejercicio
/// (<see cref="SessionLogItem.Metric"/>), nunca el cliente.
/// </param>
/// <param name="ActualRir">RIR real de la serie, entre 0 y 10; <c>null</c> si no se anota.</param>
/// <param name="LoadKg">Lastre en kg añadido a la serie; <c>null</c> si se hizo a peso corporal.</param>
public sealed record SessionLogSetInput(int SetNumber, int Value, int? ActualRir = null, double? LoadKg = null);
