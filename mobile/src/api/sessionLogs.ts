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
  /** Item ID used by item-scoped update and delete routes. */
  id: string;
  sessionLogId?: string;
  exerciseId: string;
  exerciseName: string;
  metric: SessionLogMetric;
  mesocycleId: string | null;
  sessionDay: number;
  microcycleNumber?: number | null;
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

/** Session identity and plan snapshot required by the aggregate workout API. */
export type RegisterSessionLogItemInput = {
  session: {
    kind: 'mesocycle';
    date: string;
    mesocycleId: string;
    microcycleNumber: number;
    sessionDay: number;
  };
  item: {
    exerciseId: string;
    role: 'skill' | 'strength' | 'core';
    pattern: string | null;
    prescribedSets: number;
    repsMin: number | null;
    repsMax: number | null;
    holdSecondsMin: number | null;
    holdSecondsMax: number | null;
    note: string | null;
    sets: { setNumber: number; value: number; actualRir?: number | null; loadKg?: number | null }[];
  };
  clientId?: string;
};

/** A logged item in a session aggregate response. */
export type SessionLogItem = {
  id: string;
  sessionLogId: string;
  position: number;
  exerciseId: string;
  exerciseName: string;
  role: 'skill' | 'strength' | 'core';
  pattern: string | null;
  metric: 'reps' | 'seconds';
  objective: {
    sets: number;
    repsMin: number | null;
    repsMax: number | null;
    holdSecondsMin: number | null;
    holdSecondsMax: number | null;
  };
  note: string | null;
  sets: {
    setNumber: number;
    value: number;
    metric: 'reps' | 'seconds';
    actualRir: number | null;
    loadKg: number | null;
  }[];
};

/** Aggregate response returned by the session-log API. */
export type SessionLogSession = {
  id: string;
  kind: 'mesocycle' | 'suelta';
  sessionDate: string;
  mesocycleId: string | null;
  microcycleNumber: number | null;
  sessionDay: number | null;
  recordedAtUtc: string;
  completedAtUtc: string | null;
  completed: boolean;
  items: SessionLogItem[];
};

/** Registers an exercise item in a session aggregate (`POST /session-logs`). */
export async function registerSessionLogItem(
  input: RegisterSessionLogItemInput,
): Promise<SessionLogSession> {
  const response = await fetch(`${getApiBaseUrl()}/session-logs`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as SessionLogSession;
}

/** Lista los registros de sesión guardados (`GET /session-logs`). */
export async function fetchSessionLogs(signal?: AbortSignal): Promise<SessionLog[]> {
  const response = await fetch(`${getApiBaseUrl()}/session-logs`, { signal });

  if (!response.ok) {
    throw await apiError(response);
  }

  const sessions = (await response.json()) as SessionLogSession[];
  return sessions.flatMap((session) =>
    session.items.map((item) => ({
      id: item.id,
      sessionLogId: session.id,
      exerciseId: item.exerciseId,
      exerciseName: item.exerciseName,
      metric: item.metric,
      mesocycleId: session.mesocycleId,
      sessionDay: session.sessionDay ?? 0,
      microcycleNumber: session.microcycleNumber,
      recordedAtUtc: session.recordedAtUtc,
      sets: item.sets.map((set) => ({
        setNumber: set.setNumber,
        value: set.value,
        effort: set.actualRir,
      })),
    })),
  );
}

/** Replaces an item's sets (`PUT /session-logs/{id}/items/{itemId}`). */
export async function updateSessionLogItem(
  sessionId: string,
  itemId: string,
  sets: RegisterSessionLogItemInput['item']['sets'],
): Promise<SessionLogItem> {
  const response = await fetch(`${getApiBaseUrl()}/session-logs/${sessionId}/items/${itemId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ sets }),
  });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as SessionLogItem;
}

/** Deletes an item (`DELETE /session-logs/{id}/items/{itemId}`). */
export async function deleteSessionLogItem(sessionId: string, itemId: string): Promise<void> {
  const response = await fetch(`${getApiBaseUrl()}/session-logs/${sessionId}/items/${itemId}`, {
    method: 'DELETE',
  });

  if (!response.ok) {
    throw await apiError(response);
  }
}
