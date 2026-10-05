import { apiError, getApiBaseUrl } from '@/api/client';
import type { ExerciseGroupCode, ExerciseMetric } from '@/api/catalog/exercises';

/** Criterio para superar una etapa de la escalera. */
export type StageCriterion = {
  metric: ExerciseMetric;
  target: number;
  sets: number;
};

/** Etapa de la escalera de progresión de un skill. */
export type SkillStage = {
  order: number;
  name: string;
  exerciseId: string;
  criterion: StageCriterion;
  notes: string;
};

/** Fila de una rutina de patrón: un ejercicio con sus series, rango y descanso. */
export type RoutineItem = {
  exerciseId: string;
  sets: number;
  repsMin: number | null;
  repsMax: number | null;
  holdSecondsMin: number | null;
  holdSecondsMax: number | null;
  restSeconds: number;
  tempo: string | null;
  supersetGroup: number | null;
  notes: string | null;
};

/** Rutina de patrón de un skill: un modelo por intensidad y material. */
export type PatternRoutine = {
  id: string;
  name: string;
  intensity: number;
  equipment: string | null;
  items: RoutineItem[];
};

/** Skill con su patrón, su flag de palanca, su escalera y sus rutinas de patrón. */
export type Skill = {
  id: string;
  name: string;
  group: ExerciseGroupCode;
  lever: boolean;
  stages: SkillStage[];
  patternRoutines: PatternRoutine[];
};

/**
 * Lee los skills (`GET /catalog/skills`) con su escalera y sus rutinas de patrón.
 */
export async function fetchSkillCatalog(signal?: AbortSignal): Promise<Skill[]> {
  const response = await fetch(`${getApiBaseUrl()}/catalog/skills`, { signal });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as Skill[];
}
