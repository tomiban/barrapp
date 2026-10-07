import { apiError, getApiBaseUrl } from '@/api/client';
import type { TrainingWeekdayCode } from '@/api/athleteProfile';
import type { ExerciseGroupCode } from '@/api/catalog/exercises';
import type { SkillStage } from '@/api/catalog/skills';

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
  note?: string | null;
};

/** Sesión del plan: el día que ocupa y sus filas. */
export type PlanSession = {
  day: number;
  /** Día de la semana en el que se entrena la sesión; `null` en una sesión suelta. */
  weekday: TrainingWeekdayCode | null;
  /** Fecha en la que se entrena la sesión; `null` en una sesión suelta. */
  date: string | null;
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
  mesocycleId?: string | null;
  /** Fecha en la que arranca el mesociclo (su primer día de entrenamiento). */
  startDate: string;
  /** Etapa actual del skill objetivo, con su criterio para avanzar. */
  skillStage: SkillStage;
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

/**
 * Genera el mesociclo y lo deja como el activo del atleta (`POST /plan`). `startDate` es la fecha
 * que el atleta elige para arrancar; sin ella el mesociclo arranca hoy. El mesociclo empieza
 * siempre en el primer día de entrenamiento elegido que cae en o después de esa fecha, de modo que
 * cada sesión cae en su día de la semana.
 */
export async function generatePlan(startDate?: string, signal?: AbortSignal): Promise<Plan> {
  const response = await fetch(`${getApiBaseUrl()}/plan`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ startDate: startDate ?? null }),
    signal,
  });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as Plan;
}

/** Máximo de un ejercicio básico tras el ajuste del cierre. */
export type ClosedMesocycleMaximum = {
  exerciseCode: string;
  repetitions: number;
};

/** Resultado de cerrar el mesociclo activo (`POST /plan/close`). */
export type ClosedMesocycle = {
  mesocycleId: string;
  skillId: string;
  skillName: string;
  trainingDays: number;
  startedAtUtc: string;
  closedAtUtc: string;
  /** Máximos del atleta tras el ajuste; nunca inferiores a los anteriores. */
  maximums: ClosedMesocycleMaximum[];
};

/**
 * Cierra el mesociclo activo (`POST /plan/close`): el servidor ajusta los máximos del atleta con
 * las sesiones registradas del mesociclo, lo publica en el historial y devuelve los máximos ya
 * ajustados. El siguiente `GET /plan` se genera con ellos.
 */
export async function closeMesocycle(signal?: AbortSignal): Promise<ClosedMesocycle> {
  const response = await fetch(`${getApiBaseUrl()}/plan/close`, { method: 'POST', signal });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as ClosedMesocycle;
}
