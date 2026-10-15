import { EXERCISE_GROUP_LABELS } from '@/api/catalog/exercises';
import type { PlanItemRole, PlanSessionItem } from '@/api/plan';
import type { SemanticRole } from '@/design-system/semantic';

/**
 * Derivaciones de la «Sesión en curso» (spec 0003 §4, ticket #85): todo lo que la vista necesita
 * saber de una fila del plan —la unidad, el modo de ejecución, la estructura, el objetivo y el
 * descanso— sin tocar React. El motor ya fija la unidad y el rango; aquí solo se traducen a los
 * textos y valores de la pantalla.
 */

/** Métrica de la fila: repeticiones o segundos de sostén. La fija el motor, no el cliente. */
export type SessionMetric = 'reps' | 'seconds';

/** Estado de una serie dentro del ejercicio en curso. */
export type SetState = 'passed' | 'current' | 'pending';

/** Etiqueta textual de cada estado; el estado nunca se comunica solo por color (spec 0002). */
export const SET_STATE_LABEL: Record<SetState, string> = {
  passed: 'Superado',
  current: 'En curso',
  pending: 'Pendiente',
};

/**
 * Rol semántico de cada estado. Superado es confirmado (cobalto), en curso es activo (amarillo) y
 * pendiente es inactivo; el motivo lo distingue la etiqueta textual.
 */
export const SET_STATE_ROLE: Record<SetState, SemanticRole> = {
  passed: 'confirmed',
  current: 'active',
  pending: 'inactive',
};

/** Nombre de la fase de cada papel de la sesión. */
const ROLE_LABELS: Record<PlanItemRole, string> = {
  skill: 'SKILL',
  strength: 'FUERZA',
  core: 'CORE',
};

/** Valores de lastre que ofrece el selector rápido, en kilos (0 = peso corporal). */
export const LASTRE_OPTIONS: readonly number[] = [0, 2.5, 5, 10];

/** Unidades del RIR real que ofrece el selector (#85); el RIR es informativo. */
export const RIR_OPTIONS: readonly number[] = [0, 1, 2, 3];

/** Índice u orden a dos dígitos, p. ej. `02`. */
export function twoDigits(value: number): string {
  return String(Math.max(Math.trunc(value), 0)).padStart(2, '0');
}

/** Métrica de una fila: holds en segundos, lo demás en repeticiones. */
export function metricOf(item: PlanSessionItem): SessionMetric {
  return item.holdSecondsMin !== null || item.holdSecondsMax !== null ? 'seconds' : 'reps';
}

/** Unidad del readout del cronómetro, p. ej. `SEG`. */
export function unitLabelOf(item: PlanSessionItem): string {
  return metricOf(item) === 'seconds' ? 'SEG' : 'REPS';
}

/** Modo de ejecución según la métrica: `ISOMÉTRICO` (holds) o `DINÁMICO` (reps). */
export function modeOf(item: PlanSessionItem): string {
  return metricOf(item) === 'seconds' ? 'ISOMÉTRICO' : 'DINÁMICO';
}

/** Fase del bloque en curso, p. ej. `FASE SKILL · ISOMETRÍA PURA`. */
export function phaseOf(item: PlanSessionItem): string {
  const detail =
    metricOf(item) === 'seconds'
      ? 'ISOMETRÍA PURA'
      : item.pattern
        ? EXERCISE_GROUP_LABELS[item.pattern].toUpperCase()
        : 'DINÁMICO';

  return `FASE ${ROLE_LABELS[item.role]} · ${detail}`;
}

/** Rango prescrito de una serie: mínimo y máximo, o un único valor, p. ej. `4 × 10s`. */
function rangeBounds(item: PlanSessionItem): { min: number; max: number } {
  if (metricOf(item) === 'seconds') {
    const min = item.holdSecondsMin ?? item.holdSecondsMax ?? 0;
    const max = item.holdSecondsMax ?? item.holdSecondsMin ?? 0;
    return { min, max };
  }

  const min = item.repsMin ?? item.repsMax ?? 0;
  const max = item.repsMax ?? item.repsMin ?? 0;
  return { min, max };
}

/** Estructura del ejercicio, p. ej. `4 × 10s` o `3 × 8–12 reps`. */
export function rangeOf(item: PlanSessionItem): string {
  const { min, max } = rangeBounds(item);
  const span = min === max ? `${min}` : `${min}–${max}`;
  const unit = metricOf(item) === 'seconds' ? 's' : ' reps';
  return `${item.sets} × ${span}${unit}`;
}

/** Objetivo de la serie en curso, p. ej. `OBJETIVO: 10s SOSTÉN` o `OBJETIVO: 8–12 REPS`. */
export function objectiveOf(item: PlanSessionItem): string {
  const { min, max } = rangeBounds(item);
  const span = min === max ? `${min}` : `${min}–${max}`;
  return metricOf(item) === 'seconds' ? `OBJETIVO: ${span}s SOSTÉN` : `OBJETIVO: ${span} REPS`;
}

/** Valor con el que arranca el stepper: el techo del rango prescrito. */
export function targetValueOf(item: PlanSessionItem): number {
  return rangeBounds(item).max;
}

/** Duración del cronómetro del hold, en segundos (0 en un ejercicio de reps). */
export function holdSecondsOf(item: PlanSessionItem): number {
  return metricOf(item) === 'seconds' ? rangeBounds(item).max : 0;
}

/**
 * Valores rápidos para el stepper, alrededor del rango prescrito; sirven de atajo con las manos
 * sudadas. Sin duplicados y nunca negativos.
 */
export function presetsOf(item: PlanSessionItem): number[] {
  const { min, max } = rangeBounds(item);
  const candidates = [min - 2, min, max, max + 2].filter((value) => value >= 0);
  return [...new Set(candidates)].sort((a, b) => a - b);
}

/** Descanso entre series en segundos (lo expone el `SessionItem`, #95). */
export function restSecondsOf(item: PlanSessionItem): number {
  return Math.max(Math.trunc(item.restSeconds), 0);
}

/** Estado de una serie a partir de si está registrada y de si es la serie en curso. */
export function setStateOf(registered: boolean, current: boolean): SetState {
  if (registered) {
    return 'passed';
  }
  return current ? 'current' : 'pending';
}

/** Resumen de una serie registrada, p. ej. `12s SOSTÉN · RIR 1 · +2.5kg`. */
export function setSummaryOf(
  set: { value: number; effort: number | null; loadKg: number | null },
  item: PlanSessionItem,
): string {
  const parts = [
    metricOf(item) === 'seconds' ? `${set.value}s SOSTÉN` : `${set.value} reps`,
    set.effort === null ? null : `RIR ${set.effort}`,
    lastreRowLabel(set.loadKg ?? 0),
  ].filter((part): part is string => part !== null);
  return parts.join(' · ');
}

/** Lastre de la fila de serie, p. ej. `+2.5kg`; corporal (`+0kg`) cuando no hay sobrecarga. */
export function lastreRowLabel(kg: number): string {
  return kg > 0 ? `+${kg}kg` : '+0kg';
}

/** Lastre del selector, p. ej. `+2.5 KG`; `+0.0 KG (CORPORAL)` sin sobrecarga. */
export function lastreButtonLabel(kg: number): string {
  return kg > 0 ? `+${kg.toFixed(1)} KG` : '+0.0 KG (CORPORAL)';
}
