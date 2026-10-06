import { apiError, getApiBaseUrl } from './client';
import type { Plan } from './plan';

/** Resumen de un mesociclo del historial tal y como lo sirve el API (`GET /plan/history`). */
export type MesocycleSummary = {
  id: string;
  skillId: string;
  skillName: string;
  trainingDays: number;
  startedAtUtc: string;
  closedAtUtc: string;
};

/**
 * Lista el historial de mesociclos cerrados, del más reciente al más antiguo
 * (`GET /plan/history`). Sin mesociclos pasados todavía devuelve una lista vacía.
 */
export async function fetchMesocycleHistory(signal?: AbortSignal): Promise<MesocycleSummary[]> {
  const response = await fetch(`${getApiBaseUrl()}/plan/history`, { signal });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as MesocycleSummary[];
}

/**
 * Abre el detalle de un mesociclo del historial (`GET /plan/history/{id}`): el plan tal y como
 * se guardó al generarse, con el mismo contrato que `GET /plan`.
 */
export async function fetchMesocycleDetail(
  mesocycleId: string,
  signal?: AbortSignal,
): Promise<Plan> {
  const response = await fetch(`${getApiBaseUrl()}/plan/history/${mesocycleId}`, { signal });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as Plan;
}
