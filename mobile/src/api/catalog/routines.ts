import { apiError, getApiBaseUrl } from '@/api/client';
import type { RoutineItem } from '@/api/catalog/skills';

/** Naturaleza de un programa general: circuito por tiempo o fuerza por repeticiones. */
export type RoutineProgramType = 'circuit' | 'strength';

/** Bloque de una rutina de programa: un circuito con sus vueltas y sus filas. */
export type RoutineBlock = {
  name: string;
  rounds: number;
  restSeconds: number;
  notes: string | null;
  items: RoutineItem[];
};

/** Rutina de un programa general: un modelo con intensidad, duración y bloques. */
export type ProgramRoutine = {
  id: string;
  name: string;
  intensity: number;
  durationMinutes: number | null;
  blocks: RoutineBlock[];
};

/** Programa general de acondicionamiento con sus rutinas. */
export type RoutineProgram = {
  id: string;
  name: string;
  type: RoutineProgramType;
  description: string | null;
  routines: ProgramRoutine[];
};

/** Nombre para la UI de cada tipo de programa. */
export const ROUTINE_PROGRAM_TYPE_LABELS: Record<RoutineProgramType, string> = {
  circuit: 'Circuito por tiempo',
  strength: 'Fuerza por repeticiones',
};

/**
 * Lee los programas generales de acondicionamiento (`GET /catalog/routines`). El API ya los
 * devuelve con sus rutinas y bloques; la vista no reordena.
 */
export async function fetchRoutineCatalog(signal?: AbortSignal): Promise<RoutineProgram[]> {
  const response = await fetch(`${getApiBaseUrl()}/catalog/routines`, { signal });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as RoutineProgram[];
}
