import { apiError, getApiBaseUrl } from '@/api/client';

/** Patrón de movimiento con el que se agrupan los ejercicios del catálogo. */
export type ExerciseGroupCode = 'push' | 'pull' | 'leg' | 'core' | 'cardio';

/** Unidad del ejercicio: repeticiones o segundos mantenidos. */
export type ExerciseMetric = 'reps' | 'seconds';

/** Ejercicio del catálogo tal y como lo sirve el API. */
export type CatalogExercise = {
  id: string;
  name: string;
  metric: ExerciseMetric;
  tracksMaximum: boolean;
  regressionId: string | null;
  skillId: string | null;
};

/** Ejercicios de un patrón, con el patrón en minúsculas. */
export type ExerciseGroup = {
  group: ExerciseGroupCode;
  exercises: CatalogExercise[];
};

/** Catálogo agrupado por patrón; solo incluye los grupos con ejercicios. */
export type ExerciseCatalog = {
  groups: ExerciseGroup[];
};

/** Nombre para la UI de cada patrón. */
export const EXERCISE_GROUP_LABELS: Record<ExerciseGroupCode, string> = {
  push: 'Empuje',
  pull: 'Tirón',
  leg: 'Pierna',
  core: 'Core',
  cardio: 'Cardio',
};

/**
 * Lee el catálogo de ejercicios (`GET /catalog/exercises`). El API ya lo devuelve
 * agrupado y ordenado por patrón; la vista no reordena.
 */
export async function fetchExerciseCatalog(signal?: AbortSignal): Promise<ExerciseCatalog> {
  const response = await fetch(`${getApiBaseUrl()}/catalog/exercises`, { signal });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as ExerciseCatalog;
}
