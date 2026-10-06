import { apiError, getApiBaseUrl } from './client';
import type { SoloSession } from './soloSession';

/** Estado de una sesión suelta en el historial: generada o registrada. */
export type SueltaStatus = 'generada' | 'registrada';

/**
 * Entrada del historial de sesiones sueltas tal y como la sirve el API (`GET /sessions/suelta`):
 * los parámetros con los que se pidió, el foco ya resuelto y el estado. Queda etiquetada como
 * suelta y no participa en el plan ni en los máximos.
 */
export type SueltaHistoryEntry = {
  id: string;
  timeMinutes: SoloSession['timeMinutes'];
  energy: SoloSession['energy'];
  focus: SoloSession['focus'];
  pattern: SoloSession['pattern'];
  skillId: string | null;
  skillName: string | null;
  status: SueltaStatus;
  createdAtUtc: string;
  recordedAtUtc: string | null;
  items: SoloSession['items'];
};

/**
 * Lista el historial de sesiones sueltas, de la más reciente a la más antigua (`GET
 * /sessions/suelta`). Sin sueltas todavía devuelve una lista vacía.
 */
export async function fetchSueltaHistory(signal?: AbortSignal): Promise<SueltaHistoryEntry[]> {
  const response = await fetch(`${getApiBaseUrl()}/sessions/suelta`, { signal });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as SueltaHistoryEntry[];
}
