import type { RegisterSessionLogInput, SessionLog } from '@/api/sessionLogs';
import type { PendingSessionLog } from './sessionLogOutbox';

/** Resultado de un intento de sincronización de la cola. */
export type SessionLogSyncResult = {
  /** Entradas subidas y retiradas de la cola. */
  synced: number;
  /** Entradas que siguen pendientes (por un fallo que detuvo el intento). */
  remaining: number;
};

/** Lo que la sincronización necesita de la outbox; estructural para permitir un stub en tests. */
export type SessionLogSyncOutbox = {
  listPending(): Promise<PendingSessionLog[]>;
  remove(clientId: string): Promise<void>;
};

/**
 * Sube los registros de sesión hechos sin conexión (`spec 0001`, US-32/33): ordena la cola de la
 * más antigua a la más reciente, hace un `POST /session-logs` por entrada y la retira solo cuando
 * el servidor confirma. Cada alta lleva el `clientId` idempotente de la entrada (ticket #26): si el
 * servidor ya tenía la fila de un intento anterior cuya respuesta se perdió, actualiza en lugar de
 * duplicar. Al primer fallo se detiene y deja el resto encolado para el siguiente intento. Así una
 * misma entrada nunca se sube dos veces dentro de un intento, y al recuperar la conexión la cola
 * queda vacía sin duplicar nada.
 */
export async function syncPendingSessionLogs(
  outbox: SessionLogSyncOutbox,
  register: (input: RegisterSessionLogInput) => Promise<SessionLog>,
): Promise<SessionLogSyncResult> {
  const pending = await outbox.listPending();
  let synced = 0;
  for (const log of pending) {
    try {
      await register({
        clientId: log.clientId,
        exerciseId: log.exerciseId,
        mesocycleId: log.mesocycleId,
        sessionDay: log.sessionDay,
        sets: log.sets,
      });
      await outbox.remove(log.clientId);
      synced += 1;
    } catch {
      break;
    }
  }
  return { synced, remaining: pending.length - synced };
}
