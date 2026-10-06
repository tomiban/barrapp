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
  /**
   * Costura para distinguir en la vista un registro que todavía está en la cola local (#26) de uno
   * confirmado por el servidor. Solo el cliente la marca (en `pendingToSessionLog`); los registros
   * del API llegan sin ella, así que `pending` es falsy para los confirmados. La edición/borrado
   * (#22) se limita a los confirmados hasta que la cola soporte esas operaciones.
   */
  pending?: boolean;
};

/** Entrada para registrar un ejercicio de la sesión, serie a serie. */
export type RegisterSessionLogInput = {
  exerciseId: string;
  mesocycleId?: string | null;
  sessionDay: number;
  sets: { setNumber: number; value: number; effort?: number | null }[];
  /**
   * Id idempotente de la outbox offline (#26): si un envío pierde la respuesta y se reintenta con
   * el mismo id, el servidor actualiza el registro original en lugar de duplicar la fila.
   */
  clientId?: string;
};

/**
 * Registra lo ejecutado, serie a serie, en un ejercicio de una sesión (`POST /session-logs`).
 * Devuelve el registro tal y como quedó guardado (el servidor deriva la unidad del ejercicio).
 * Con `clientId` el alta es idempotente: un reintento con el mismo id actualiza, nunca duplica.
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

/**
 * Edita un registro ya guardado (`PUT /session-logs/{id}`): sustituye los valores de sus series.
 * La identidad de la sesión no cambia; el servidor vuelve a derivar la unidad del ejercicio.
 */
export async function updateSessionLog(
  id: string,
  input: { sets: { setNumber: number; value: number; effort?: number | null }[] },
): Promise<SessionLog> {
  const response = await fetch(`${getApiBaseUrl()}/session-logs/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as SessionLog;
}

/** Elimina un registro ya guardado (`DELETE /session-logs/{id}`). */
export async function deleteSessionLog(id: string): Promise<void> {
  const response = await fetch(`${getApiBaseUrl()}/session-logs/${id}`, {
    method: 'DELETE',
  });

  if (!response.ok) {
    throw await apiError(response);
  }
}
