import type { ExerciseMetric } from '@/api/catalog/exercises';
import type { Skill, StageCriterion } from '@/api/catalog/skills';
import type { SemanticRole } from '@/design-system/semantic';

/**
 * Estado de una etapa dentro de la escalera de un skill (spec 0003, ticket #86): las etapas por
 * debajo de la actual están **superadas**, la actual es la **actual**, las que quedan por encima
 * (salvo la última) están **bloqueadas** y la última es la **meta final**.
 */
export type StageState = 'passed' | 'current' | 'locked' | 'goal';

/** Etiqueta textual de cada estado; el estado nunca se comunica solo por color (spec 0002). */
export const STAGE_STATE_LABEL: Record<StageState, string> = {
  passed: 'Superado',
  current: 'Actual',
  locked: 'Bloqueado',
  goal: 'Meta final',
};

/**
 * Rol semántico de cada estado. Superado es confirmado (cobalto); actual es activo (amarillo);
 * bloqueado y meta final son inactivos (los distingue la etiqueta textual).
 */
export const STAGE_STATE_ROLE: Record<StageState, SemanticRole> = {
  passed: 'confirmed',
  current: 'active',
  locked: 'inactive',
  goal: 'inactive',
};

/**
 * Resuelve el estado de una etapa a partir de la etapa actual (1-based) y del total de etapas de
 * la escalera. La etapa actual gana sobre la meta final: alcanzar la última etapa la muestra como
 * **actual**, no como meta pendiente.
 */
export function stageState(order: number, currentStage: number, totalStages: number): StageState {
  if (order === currentStage) {
    return 'current';
  }

  if (order === totalStages) {
    return 'goal';
  }

  if (order < currentStage) {
    return 'passed';
  }

  return 'locked';
}

/** Texto del criterio de una etapa, p. ej. `3 × 20 s` o `3 × 5 reps`. */
export function formatStageCriterion(criterion: StageCriterion): string {
  const unit = criterion.metric === 'seconds' ? 's' : 'reps';
  return `${criterion.sets} × ${criterion.target} ${unit}`;
}

/**
 * Progreso global del skill como porcentaje (0–100): la etapa actual sobre el total de etapas.
 * Un atleta en la etapa 2 de 5 va al 40 %.
 */
export function globalProgressPercent(currentStage: number, totalStages: number): number {
  if (!Number.isFinite(totalStages) || totalStages <= 0) {
    return 0;
  }

  const stage = Math.min(Math.max(currentStage, 0), totalStages);
  return (stage / totalStages) * 100;
}

/** Porcentaje del progreso global formateado con un decimal, p. ej. `40.0%`. */
export function formatGlobalProgress(currentStage: number, totalStages: number): string {
  return `${globalProgressPercent(currentStage, totalStages).toFixed(1)}%`;
}

/**
 * Código técnico de la hoja del skill, p. ej. `SKL.01 - ISOMÉTRICO`: el índice del skill en el
 * selector (1-based, a dos dígitos) y su modo según la métrica del criterio (segundos mantenidos
 * o repeticiones).
 */
export function skillCode(index: number, metric: ExerciseMetric): string {
  const digits = String(Math.max(Math.trunc(index), 1)).padStart(2, '0');
  const mode = metric === 'seconds' ? 'ISOMÉTRICO' : 'DINÁMICO';
  return `SKL.${digits} - ${mode}`;
}

/**
 * Material del skill: el equipamiento distinto de sus rutinas de patrón, en orden de aparición y
 * sin repetir (p. ej. `Barra alta y baja / Solo barra baja`). Cadena vacía si no declara material.
 */
export function skillMaterial(skill: Skill): string {
  const seen = new Set<string>();
  const equipment: string[] = [];

  for (const routine of skill.patternRoutines) {
    const value = routine.equipment?.trim();
    if (value && !seen.has(value)) {
      seen.add(value);
      equipment.push(value);
    }
  }

  return equipment.join(' / ');
}
