import { apiError, getApiBaseUrl } from '@/api/client';
import type { ExerciseGroupCode } from '@/api/catalog/exercises';

/** Papel de una fila dentro de la sesión del plan. */
export type PlanItemRole = 'skill' | 'strength' | 'core';

/** Fila de una sesión del plan: ejercicio, papel, series y rango. */
export type PlanSessionItem = {
  exerciseId: string;
  exerciseName: string;
  role: PlanItemRole;
  pattern: ExerciseGroupCode | null;
  sets: number;
  repsMin: number | null;
  repsMax: number | null;
  holdSecondsMin: number | null;
  holdSecondsMax: number | null;
};

/** Sesión del plan: el día que ocupa y sus filas. */
export type PlanSession = {
  day: number;
  items: PlanSessionItem[];
};

/** Semana del mesociclo con sus sesiones. */
export type PlanMicrocycle = {
  number: number;
  sessions: PlanSession[];
};

/** Plan mensual tal y como lo sirve el API. */
export type Plan = {
  skillId: string;
  trainingDays: number;
  microcycles: PlanMicrocycle[];
};

/**
 * Lee el plan del mesociclo (`GET /plan`). El API lo genera a partir del perfil y el objetivo ya
 * guardados; la app solo lo pinta.
 */
export async function fetchPlan(signal?: AbortSignal): Promise<Plan> {
  const response = await fetch(`${getApiBaseUrl()}/plan`, { signal });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as Plan;
}
