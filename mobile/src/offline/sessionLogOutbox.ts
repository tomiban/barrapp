import * as SQLite from 'expo-sqlite';

import type { SessionLog, SessionLogMetric, SessionLogSet } from '@/api/sessionLogs';

/** Superficie de la tabla `session_log_outbox` que usa la cola; estructural para permitir un fake en tests. */
export interface SessionLogOutboxTable {
  runAsync(source: string, ...params: unknown[]): Promise<unknown>;
  getFirstAsync<T>(source: string, ...params: unknown[]): Promise<T | null>;
  getAllAsync<T>(source: string, ...params: unknown[]): Promise<T[]>;
}

/** Nombre de la base de datos local de la app en el dispositivo. */
const DATABASE_NAME = 'barrapp.db';

const OUTBOX_SCHEMA_SQL = `
  PRAGMA journal_mode = WAL;
  CREATE TABLE IF NOT EXISTS session_log_outbox (
    client_id TEXT PRIMARY KEY NOT NULL,
    exercise_id TEXT NOT NULL,
    exercise_name TEXT NOT NULL,
    metric TEXT,
    mesocycle_id TEXT,
    session_day INTEGER NOT NULL,
    sets TEXT NOT NULL,
    updated_at TEXT NOT NULL
  );
  CREATE UNIQUE INDEX IF NOT EXISTS session_log_outbox_business_key
    ON session_log_outbox (exercise_id, session_day);
`;

/** Entrada de la cola de sincronización: un registro de sesión hecho sin conexión, pendiente de subir. */
export type PendingSessionLog = {
  /** Id idempotente generado en el cliente; se conserva al re-guardar el mismo ejercicio y día. */
  clientId: string;
  exerciseId: string;
  exerciseName: string;
  metric: SessionLogMetric;
  mesocycleId: string | null;
  sessionDay: number;
  sets: SessionLogSet[];
  /** Fecha de la última escritura (ISO 8601): con ella gana la última escritura (LWW). */
  updatedAt: string;
};

/** Datos para encolar un registro: lo que el API necesita más lo que la vista consume al mostrarlo. */
export type EnqueueSessionLogInput = {
  exerciseId: string;
  exerciseName: string;
  metric: SessionLogMetric;
  mesocycleId?: string | null;
  sessionDay: number;
  sets: SessionLogSet[];
};

/** Cola de sincronización local: encola registros sin conexión y los retira al subirlos. */
export type SessionLogOutbox = {
  enqueue(input: EnqueueSessionLogInput): Promise<PendingSessionLog>;
  listPending(): Promise<PendingSessionLog[]>;
  remove(clientId: string): Promise<void>;
};

/** Inyecciones para hacer la cola determinista en tests (reloj e ids). */
export type SessionLogOutboxOptions = {
  generateId?: () => string;
  now?: () => string;
};

/** Fila tal y como vive en la tabla. */
type OutboxRow = {
  client_id: string;
  exercise_id: string;
  exercise_name: string;
  metric: string | null;
  mesocycle_id: string | null;
  session_day: number;
  sets: string;
  updated_at: string;
};

const ENQUEUE_SQL = `
  INSERT INTO session_log_outbox
    (client_id, exercise_id, exercise_name, metric, mesocycle_id, session_day, sets, updated_at)
  VALUES (?, ?, ?, ?, ?, ?, ?, ?)
  ON CONFLICT(exercise_id, session_day) DO UPDATE SET
    client_id = excluded.client_id,
    exercise_name = excluded.exercise_name,
    metric = excluded.metric,
    mesocycle_id = excluded.mesocycle_id,
    sets = excluded.sets,
    updated_at = excluded.updated_at;
`;

const LIST_PENDING_SQL = `
  SELECT client_id, exercise_id, exercise_name, metric, mesocycle_id, session_day, sets, updated_at
  FROM session_log_outbox
  ORDER BY updated_at ASC;
`;

const FIND_BY_BUSINESS_KEY_SQL = `
  SELECT client_id FROM session_log_outbox WHERE exercise_id = ? AND session_day = ?;
`;

/** Identificador idempotente generado en el cliente (uuid v4), con respaldo para entornos sin `crypto.randomUUID`. */
function newClientId(): string {
  const globalCrypto = globalThis.crypto as Crypto | undefined;
  if (globalCrypto?.randomUUID) {
    return globalCrypto.randomUUID();
  }
  const randomHex = () => Math.floor(Math.random() * 16).toString(16);
  const hex = (count: number) => Array.from({ length: count }, randomHex).join('');
  return `${hex(8)}-${hex(4)}-4${hex(3)}-8${hex(3)}-${hex(12)}`;
}

function rowToPending(row: OutboxRow): PendingSessionLog {
  return {
    clientId: row.client_id,
    exerciseId: row.exercise_id,
    exerciseName: row.exercise_name,
    metric: row.metric as SessionLogMetric,
    mesocycleId: row.mesocycle_id,
    sessionDay: row.session_day,
    sets: JSON.parse(row.sets) as SessionLogSet[],
    updatedAt: row.updated_at,
  };
}

/**
 * Crea la cola de sincronización sobre una base ya abierta (y con el esquema garantizado).
 *
 * Sin duplicados y last-write-wins: una entrada única por ejercicio y día de sesión (índice único
 * sobre la clave de negocio); re-guardar el mismo ejercicio y día reemplaza el payload y conserva
 * el id idempotente, y `updated_at` marca la última escritura.
 */
export function createSessionLogOutbox(
  db: SessionLogOutboxTable,
  options: SessionLogOutboxOptions = {},
): SessionLogOutbox {
  const generateId = options.generateId ?? newClientId;
  const now = options.now ?? (() => new Date().toISOString());

  return {
    async enqueue(input) {
      const existing = await db.getFirstAsync<{ client_id: string }>(
        FIND_BY_BUSINESS_KEY_SQL,
        input.exerciseId,
        input.sessionDay,
      );
      const clientId = existing?.client_id ?? generateId();
      const updatedAt = now();
      await db.runAsync(
        ENQUEUE_SQL,
        clientId,
        input.exerciseId,
        input.exerciseName,
        input.metric,
        input.mesocycleId ?? null,
        input.sessionDay,
        JSON.stringify(input.sets),
        updatedAt,
      );
      return {
        clientId,
        exerciseId: input.exerciseId,
        exerciseName: input.exerciseName,
        metric: input.metric,
        mesocycleId: input.mesocycleId ?? null,
        sessionDay: input.sessionDay,
        sets: input.sets,
        updatedAt,
      };
    },

    async listPending() {
      const rows = await db.getAllAsync<OutboxRow>(LIST_PENDING_SQL);
      return rows.map(rowToPending);
    },

    async remove(clientId) {
      await db.runAsync('DELETE FROM session_log_outbox WHERE client_id = ?;', clientId);
    },
  };
}

/** Abre (y crea si hace falta) la cola de sincronización en el dispositivo. */
export async function openSessionLogOutbox(): Promise<SessionLogOutbox> {
  const db = await SQLite.openDatabaseAsync(DATABASE_NAME);
  await db.execAsync(OUTBOX_SCHEMA_SQL);
  return createSessionLogOutbox(db);
}

/** Convierte una entrada pendiente en el `SessionLog` que consume la vista de registro de sesión. */
export function pendingToSessionLog(pending: PendingSessionLog): SessionLog {
  return {
    id: pending.clientId,
    exerciseId: pending.exerciseId,
    exerciseName: pending.exerciseName,
    metric: pending.metric,
    mesocycleId: pending.mesocycleId,
    sessionDay: pending.sessionDay,
    recordedAtUtc: pending.updatedAt,
    sets: pending.sets,
  };
}

/**
 * Combina los registros confirmados del servidor con los que siguen pendientes en la cola local:
 * un ejercicio y día pendiente de sincronizar gana a su copia del servidor (last-write-wins, es la
 * escritura más reciente) y solo aparece una vez.
 */
export function mergeSessionLogs(serverLogs: SessionLog[], pending: PendingSessionLog[]): SessionLog[] {
  const pendingKeys = new Set(pending.map((entry) => `${entry.exerciseId}:${entry.sessionDay}`));
  const fromServer = serverLogs.filter(
    (log) => !pendingKeys.has(`${log.exerciseId}:${log.sessionDay}`),
  );
  return fromServer.concat(pending.map(pendingToSessionLog));
}