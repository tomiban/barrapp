import { messageOf } from '@/api/messageOf';
import type { Plan } from '@/api/plan';
import type { PlanStore } from './planStore';

/** Resultado de leer el plan dando preferencia a la red y cayendo a la caché local. */
export type PlanReadResult =
  | { source: 'network'; plan: Plan }
  | { source: 'cache'; plan: Plan }
  | { source: 'failure'; message: string };

/** Error lanzado al abortar un `fetch` (desmontaje de pantalla, por ejemplo). */
function isAbortError(error: unknown): boolean {
  return error instanceof Error && error.name === 'AbortError';
}

/**
 * Lee el plan: primero de la red y, si la red falla, de la caché local (modo sin conexión).
 *
 * Cuando la red responde, guarda el plan fresco en la caché para la próxima lectura; un fallo de
 * escritura nunca rompe la lectura online. Un abort de la petición se propaga tal cual para que el
 * llamador (la pantalla) lo ignore.
 */
export async function readPlan(
  fromNetwork: (signal?: AbortSignal) => Promise<Plan>,
  store: PlanStore,
  signal?: AbortSignal,
): Promise<PlanReadResult> {
  try {
    const plan = await fromNetwork(signal);
    await store.savePlan(plan).catch(() => undefined);
    return { source: 'network', plan };
  } catch (error) {
    if (isAbortError(error)) {
      throw error;
    }
    const cached = await store.loadPlan().catch(() => null);
    if (cached) {
      return { source: 'cache', plan: cached };
    }
    return { source: 'failure', message: messageOf(error) };
  }
}
