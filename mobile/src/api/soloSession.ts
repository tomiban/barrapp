import { apiError, getApiBaseUrl } from '@/api/client';
import type { ExerciseGroupCode } from '@/api/catalog/exercises';

/** Tiempo disponible para la sesión suelta, en minutos. */
export type SoloSessionTime = 15 | 30 | 45 | 60;

/** Energía declarada para la sesión suelta. */
export type SoloSessionEnergy = 'baja' | 'media' | 'alta';

/** Foco de la sesión suelta: patrón, skill o «sorpréndeme». */
export type SoloSessionFocus = 'patron' | 'skill' | 'sorprendeme';

/** Patrón de fuerza general, válido como foco de patrón. */
export type SoloSessionPattern = Extract<'push' | 'pull' | 'leg', ExerciseGroupCode>;

/** Papel de una fila dentro de la sesión suelta. */
export type SoloSessionItemRole = 'skill' | 'strength' | 'core';

/** Fila de la sesión suelta: ejercicio, papel, series y rango. */
export type SoloSessionItem = {
  exerciseId: string;
  exerciseName: string;
  role: SoloSessionItemRole;
  pattern: ExerciseGroupCode | null;
  sets: number;
  repsMin: number | null;
  repsMax: number | null;
  holdSecondsMin: number | null;
  holdSecondsMax: number | null;
  note: string | null;
};

/** Sesión suelta tal y como la sirve el API: parámetros resueltos y filas. */
export type SoloSession = {
  focus: SoloSessionFocus;
  pattern: SoloSessionPattern | null;
  skillId: string | null;
  skillName: string | null;
  timeMinutes: SoloSessionTime;
  energy: SoloSessionEnergy;
  items: SoloSessionItem[];
};

/** Cuerpo del `POST /sessions/suelta`. El patrón solo viaja con el foco de patrón. */
export type GenerateSoloSessionCommand = {
  timeMinutes: SoloSessionTime;
  energy: SoloSessionEnergy;
  focus: SoloSessionFocus;
  pattern?: SoloSessionPattern;
};

/**
 * Genera una sesión suelta (`POST /sessions/suelta`). Requiere conexión: el motor vive en el
 * servidor y compone la sesión a partir del perfil, el objetivo y los parámetros.
 */
export async function generateSoloSession(
  command: GenerateSoloSessionCommand,
  signal?: AbortSignal,
): Promise<SoloSession> {
  const response = await fetch(`${getApiBaseUrl()}/sessions/suelta`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(command),
    signal,
  });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as SoloSession;
}
