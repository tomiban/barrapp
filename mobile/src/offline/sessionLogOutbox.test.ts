import * as SQLite from 'expo-sqlite';

import type { SessionLog } from '@/api/sessionLogs';
import {
  createSessionLogOutbox,
  mergeSessionLogs,
  openSessionLogOutbox,
  pendingToSessionLog,
} from './sessionLogOutbox';
import type { EnqueueSessionLogInput } from './sessionLogOutbox';

jest.mock('expo-sqlite', () => ({
  openDatabaseAsync: jest.fn(),
}));

const openDatabaseAsyncMock = SQLite.openDatabaseAsync as jest.Mock;

type FakeRow = {
  client_id: string;
  exercise_id: string;
  exercise_name: string;
  metric: string | null;
  mesocycle_id: string | null;
  microcycle_number: number | null;
  session_day: number;
  sets: string;
  updated_at: string;
};

/**
 * Base de datos de prueba en memoria: imita el contrato observable de la tabla `session_log_outbox`
 * (una fila por ejercicio y sesión, la última escritura gana, ordenada por fecha de actualización)
 * sin recurrir a SQLite nativo. Igual que el fake de `planStore.test.ts`.
 */
function createInMemoryOutboxTable() {
  const rows = new Map<string, FakeRow>();
  return {
    execAsync: jest.fn(async (_source: string): Promise<void> => undefined),
    async runAsync(source: string, ...params: unknown[]): Promise<unknown> {
      if (source.includes('INSERT INTO session_log_outbox')) {
        const [
          client_id,
          exercise_id,
          exercise_name,
          metric,
          mesocycle_id,
          microcycle_number,
          session_day,
          sets,
          updated_at,
        ] = params as [
          string,
          string,
          string,
          string | null,
          string | null,
          number | null,
          number,
          string,
          string,
        ];
        rows.set(`${exercise_id}:${mesocycle_id}:${microcycle_number}:${session_day}`, {
          client_id,
          exercise_id,
          exercise_name,
          metric,
          mesocycle_id,
          microcycle_number,
          session_day,
          sets,
          updated_at,
        });
        return { lastInsertRowId: 1, changes: 1 };
      }
      if (source.includes('DELETE FROM session_log_outbox')) {
        const [client_id] = params as [string];
        for (const [key, row] of rows) {
          if (row.client_id === client_id) {
            rows.delete(key);
          }
        }
        return { changes: 1 };
      }
      throw new Error(`runAsync no esperado: ${source}`);
    },
    async getFirstAsync<T>(source: string, ...params: unknown[]): Promise<T | null> {
      if (source.includes('SELECT client_id')) {
        const [exercise_id, mesocycle_id, microcycle_number, session_day] = params as [
          string,
          string | null,
          number | null,
          number,
        ];
        const row = rows.get(`${exercise_id}:${mesocycle_id}:${microcycle_number}:${session_day}`);
        return (row ? { client_id: row.client_id } : null) as T | null;
      }
      throw new Error(`getFirstAsync no esperado: ${source}`);
    },
    async getAllAsync<T>(source: string): Promise<T[]> {
      if (source.includes('PRAGMA table_info')) {
        return [
          'client_id',
          'exercise_id',
          'exercise_name',
          'metric',
          'mesocycle_id',
          'microcycle_number',
          'session_day',
          'sets',
          'updated_at',
        ].map((name) => ({ name })) as T[];
      }
      const sorted = [...rows.values()].sort((a, b) => a.updated_at.localeCompare(b.updated_at));
      return sorted as T[];
    },
  };
}

/** Entrada conocida e independiente del código bajo test (ancla `push_up`, día 2). */
const PUSH_UP_INPUT: EnqueueSessionLogInput = {
  exerciseId: 'push_up',
  exerciseName: 'Flexiones',
  metric: 'reps',
  mesocycleId: null,
  sessionDay: 2,
  sets: [
    { setNumber: 1, value: 10, effort: null },
    { setNumber: 2, value: 11, effort: null },
  ],
};

/** Segunda entrada conocida: otra dominada, misma sesión, para probar LWW sin duplicados. */
const PULL_UP_INPUT: EnqueueSessionLogInput = {
  exerciseId: 'pull_up',
  exerciseName: 'Dominadas',
  metric: 'reps',
  mesocycleId: null,
  sessionDay: 2,
  sets: [{ setNumber: 1, value: 5, effort: null }],
};

/** Reloj e ids deterministas para que los tests no dependan de `Date` ni de aleatoriedad. */
const OPTIONS = {
  generateId: () => 'client-1',
  now: () => '2026-10-05T10:00:00.000Z',
};

describe('sessionLogOutbox', () => {
  describe('createSessionLogOutbox', () => {
    it('lists nothing before any registration is queued', async () => {
      const outbox = createSessionLogOutbox(createInMemoryOutboxTable(), OPTIONS);

      await expect(outbox.listPending()).resolves.toEqual([]);
    });

    it('queues a pending registration that listPending returns with all its fields', async () => {
      const outbox = createSessionLogOutbox(createInMemoryOutboxTable(), OPTIONS);

      await outbox.enqueue(PUSH_UP_INPUT);

      const pending = await outbox.listPending();
      expect(pending).toEqual([
        {
          clientId: 'client-1',
          exerciseId: 'push_up',
          exerciseName: 'Flexiones',
          metric: 'reps',
          mesocycleId: null,
          microcycleNumber: null,
          sessionDay: 2,
          sets: [
            { setNumber: 1, value: 10, effort: null },
            { setNumber: 2, value: 11, effort: null },
          ],
          updatedAt: '2026-10-05T10:00:00.000Z',
        },
      ]);
    });

    it('keeps a single entry per exercise and day: re-saving replaces the payload but keeps the client id (LWW, sin duplicados)', async () => {
      const table = createInMemoryOutboxTable();
      const outbox = createSessionLogOutbox(table, OPTIONS);

      await outbox.enqueue(PUSH_UP_INPUT);
      await outbox.enqueue({
        ...PUSH_UP_INPUT,
        sets: [
          { setNumber: 1, value: 10, effort: null },
          { setNumber: 2, value: 12, effort: null },
          { setNumber: 3, value: 12, effort: null },
        ],
      });

      const pending = await outbox.listPending();
      expect(pending).toHaveLength(1);
      expect(pending[0].clientId).toBe('client-1');
      expect(pending[0].updatedAt).toBe('2026-10-05T10:00:00.000Z');
      expect(pending[0].sets).toEqual([
        { setNumber: 1, value: 10, effort: null },
        { setNumber: 2, value: 12, effort: null },
        { setNumber: 3, value: 12, effort: null },
      ]);
    });

    it('propagates an optional effort (RIR/RPE) per set through the outbox', async () => {
      const outbox = createSessionLogOutbox(createInMemoryOutboxTable(), OPTIONS);

      await outbox.enqueue({
        ...PUSH_UP_INPUT,
        sets: [
          { setNumber: 1, value: 10, effort: 2 },
          { setNumber: 2, value: 11, effort: null },
        ],
      });

      const pending = await outbox.listPending();
      expect(pending).toEqual([
        {
          clientId: 'client-1',
          exerciseId: 'push_up',
          exerciseName: 'Flexiones',
          metric: 'reps',
          mesocycleId: null,
          microcycleNumber: null,
          sessionDay: 2,
          sets: [
            { setNumber: 1, value: 10, effort: 2 },
            { setNumber: 2, value: 11, effort: null },
          ],
          updatedAt: '2026-10-05T10:00:00.000Z',
        },
      ]);
    });

    it('queues different exercises separately and lists them oldest first', async () => {
      const clock = { current: Date.parse('2026-10-05T10:00:00.000Z') };
      const outbox = createSessionLogOutbox(createInMemoryOutboxTable(), {
        generateId: (() => {
          let next = 0;
          return () => `client-${(next += 1)}`;
        })(),
        now: () => new Date((clock.current += 1000)).toISOString(),
      });

      await outbox.enqueue(PULL_UP_INPUT);
      await outbox.enqueue(PUSH_UP_INPUT);

      const pending = await outbox.listPending();
      expect(pending.map((entry) => entry.exerciseId)).toEqual(['pull_up', 'push_up']);
    });

    it('removes a pending registration once it has been uploaded', async () => {
      const outbox = createSessionLogOutbox(createInMemoryOutboxTable(), {
        generateId: (() => {
          let next = 0;
          return () => `client-${(next += 1)}`;
        })(),
        now: OPTIONS.now,
      });
      await outbox.enqueue(PUSH_UP_INPUT);
      await outbox.enqueue(PULL_UP_INPUT);

      await outbox.remove('client-1');

      const pending = await outbox.listPending();
      expect(pending).toHaveLength(1);
      expect(pending[0].exerciseId).toBe('pull_up');
    });

    it('maps a pending entry to the SessionLog shape the view consumes', () => {
      expect(
        pendingToSessionLog({
          clientId: 'client-1',
          exerciseId: 'push_up',
          exerciseName: 'Flexiones',
          metric: 'reps',
          mesocycleId: null,
          sessionDay: 2,
          sets: PUSH_UP_INPUT.sets,
          updatedAt: '2026-10-05T10:00:00.000Z',
        }),
      ).toEqual({
        id: 'client-1',
        exerciseId: 'push_up',
        exerciseName: 'Flexiones',
        metric: 'reps',
        mesocycleId: null,
        sessionDay: 2,
        recordedAtUtc: '2026-10-05T10:00:00.000Z',
        sets: PUSH_UP_INPUT.sets,
        // La marca `pending` es la costura que la vista usa para no ofrecer editar/borrar (#22).
        pending: true,
      });
    });
  });

  describe('openSessionLogOutbox', () => {
    it('opens the device database, guarantees the outbox table with its business-key index and round-trips', async () => {
      const db = createInMemoryOutboxTable();
      openDatabaseAsyncMock.mockResolvedValue(db);

      const outbox = await openSessionLogOutbox();

      expect(openDatabaseAsyncMock).toHaveBeenCalledWith('barrapp.db');
      const schema = db.execAsync.mock.calls[0][0] as string;
      expect(schema).toContain('CREATE TABLE IF NOT EXISTS session_log_outbox');
      const indexes = db.execAsync.mock.calls[1][0] as string;
      expect(indexes).toContain('session_log_outbox_business_key');
      expect(indexes).toContain('(exercise_id, mesocycle_id, microcycle_number, session_day)');

      await outbox.enqueue(PUSH_UP_INPUT);
      await expect(outbox.listPending()).resolves.toHaveLength(1);
    });
  });

  describe('mergeSessionLogs', () => {
    it('prefers the pending local entry over the server copy of the same exercise and day (LWW)', () => {
      const serverLog: SessionLog = {
        id: 'log-server',
        exerciseId: 'push_up',
        exerciseName: 'Flexiones',
        metric: 'reps',
        mesocycleId: null,
        sessionDay: 2,
        recordedAtUtc: '2026-10-05T08:00:00.000Z',
        sets: [{ setNumber: 1, value: 8, effort: null }],
      };

      const merged = mergeSessionLogs(
        [serverLog],
        [
          {
            clientId: 'client-1',
            exerciseId: 'push_up',
            exerciseName: 'Flexiones',
            metric: 'reps',
            mesocycleId: null,
            sessionDay: 2,
            sets: [
              { setNumber: 1, value: 10, effort: null },
              { setNumber: 2, value: 11, effort: null },
            ],
            updatedAt: '2026-10-05T10:00:00.000Z',
          },
        ],
      );

      expect(merged).toHaveLength(1);
      expect(merged[0]).toMatchObject({ id: 'client-1', sets: PUSH_UP_INPUT.sets });
    });

    it('keeps the server logs that have no pending counterpart', () => {
      const serverLog: SessionLog = {
        id: 'log-pull',
        exerciseId: 'pull_up',
        exerciseName: 'Dominadas',
        metric: 'reps',
        mesocycleId: null,
        sessionDay: 2,
        recordedAtUtc: '2026-10-05T08:00:00.000Z',
        sets: [{ setNumber: 1, value: 5, effort: null }],
      };

      const merged = mergeSessionLogs([serverLog], []);

      expect(merged).toEqual([serverLog]);
    });
  });
});
