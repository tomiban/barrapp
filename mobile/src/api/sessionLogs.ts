import { apiError, getApiBaseUrl } from './client';

/** Unidad del valor registrado; la deriva el API del tipo de ejercicio. */
export type SessionLogMetric = 'reps' | 'seconds' | null;

/** Una serie de un registro de sesión: número, valor real y esfuerzo (RIR/RPE) opcional. */
export type SessionLogSet = {
  setNumber: number;
  value: number;
  effort: number | null;
};

/** Registro de sesión tal y como lo sirve el API. */
export type SessionLog = {
  id: string;
  exerciseId: string;
  exerciseName: string;
  metric: SessionLogMetric;
  mesocycleId: string | null;
  sessionDay: number;
  recordedAtUtc: string;
  sets: SessionLogSet[];
};

/** Entrada para registrar un ejercicio de la sesión, serie a serie. */
export type RegisterSessionLogInput = {
  exerciseId: string;
  mesocycleId?: string | null;
  sessionDay: number;
  sets: { setNumber: number; value: number; effort?: number | null }[];
};

/**
 * Registra lo ejecutado, serie a serie, en un ejercicio de una sesión (`POST /session-logs`).
 * Devuelve el registro tal y como quedó guardado (el servidor deriva la unidad del ejercicio).
 */
export async function registerSessionLog(input: RegisterSessionLogInput): Promise<SessionLog> {
  const response = await fetch(`${getApiBaseUrl()}/session-logs`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as SessionLog;
}

/** Lista los registros de sesión guardados (`GET /session-logs`). */
export async function fetchSessionLogs(signal?: AbortSignal): Promise<SessionLog[]> {
  const response = await fetch(`${getApiBaseUrl()}/session-logs`, { signal });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as SessionLog[];
}
