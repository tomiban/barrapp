import * as SQLite from 'expo-sqlite';

import type { Plan } from '@/api/plan';
import { createPlanStore, openPlanStore } from './planStore';

jest.mock('expo-sqlite', () => ({
  openDatabaseAsync: jest.fn(),
}));

const openDatabaseAsyncMock = SQLite.openDatabaseAsync as jest.Mock;

/** Plan de ejemplo (ancla de `planche`), literal conocido e independiente del código bajo test. */
const PLANCHE_PLAN: Plan = {
  skillId: 'planche',
  trainingDays: 3,
  skillStage: {
    order: 1,
    name: 'Planche inclinada',
    exerciseId: 'planche-lean',
    criterion: { metric: 'seconds', target: 20, sets: 3 },
    notes: 'Inclinación con los hombros por delante de las manos.',
  },
  microcycles: [
    {
      number: 1,
      sessions: [
        {
          day: 1,
          items: [
            {
              exerciseId: 'planche-lean',
              exerciseName: 'Lean de planche',
              role: 'skill',
              pattern: null,
              sets: 3,
              repsMin: null,
              repsMax: null,
              holdSecondsMin: 20,
              holdSecondsMax: 30,
            },
          ],
        },
      ],
    },
  ],
};

/** Segundo plan conocido, distinto del primero para probar la sobrescritura. */
const HANDSTAND_PLAN: Plan = {
  skillId: 'handstand',
  trainingDays: 3,
  skillStage: {
    order: 1,
    name: 'Pino apoyado a la pared',
    exerciseId: 'handstand-wall-support',
    criterion: { metric: 'seconds', target: 30, sets: 3 },
    notes: 'Pies apoyados en la pared, cuerpo alineado y hombros activos.',
  },
  microcycles: [
    {
      number: 1,
      sessions: [
        {
          day: 1,
          items: [
            {
              exerciseId: 'handstand-wall-support',
              exerciseName: 'Pino apoyado a la pared',
              role: 'skill',
              pattern: null,
              sets: 3,
              repsMin: null,
              repsMax: null,
              holdSecondsMin: 30,
              holdSecondsMax: 30,
            },
          ],
        },
      ],
    },
  ],
};

/**
 * Base de datos de prueba en memoria: imita el comportamiento observable de la tabla
 * `plan_cache` (una sola fila) sin recurrir a SQLite nativo.
 */
function createInMemoryDatabase() {
  let row: { plan: string } | null = null;
  return {
    execAsync: jest.fn(async (_source: string): Promise<void> => undefined),
    async runAsync(
      _source: string,
      _id: unknown,
      plan: unknown,
      _savedAt: unknown,
    ): Promise<unknown> {
      row = { plan: String(plan) };
      return { lastInsertRowId: 1, changes: 1 };
    },
    async getFirstAsync<T>(_source: string): Promise<T | null> {
      return (row ? { plan: row.plan } : null) as T | null;
    },
  };
}

describe('planStore', () => {
  describe('createPlanStore', () => {
    it('loads null before any plan is saved', async () => {
      const store = createPlanStore(createInMemoryDatabase());

      await expect(store.loadPlan()).resolves.toBeNull();
    });

    it('returns the plan that was saved', async () => {
      const store = createPlanStore(createInMemoryDatabase());

      await store.savePlan(PLANCHE_PLAN);

      await expect(store.loadPlan()).resolves.toEqual(PLANCHE_PLAN);
    });

    it('keeps the most recent plan when saved twice', async () => {
      const store = createPlanStore(createInMemoryDatabase());

      await store.savePlan(PLANCHE_PLAN);
      await store.savePlan(HANDSTAND_PLAN);

      await expect(store.loadPlan()).resolves.toEqual(HANDSTAND_PLAN);
    });
  });

  describe('openPlanStore', () => {
    it('opens the device database, guarantees the cache table and round-trips a plan', async () => {
      const db = createInMemoryDatabase();
      openDatabaseAsyncMock.mockResolvedValue(db);

      const store = await openPlanStore();

      expect(openDatabaseAsyncMock).toHaveBeenCalledWith('barrapp.db');
      expect(db.execAsync.mock.calls[0][0]).toContain('CREATE TABLE IF NOT EXISTS plan_cache');

      await store.savePlan(PLANCHE_PLAN);
      await expect(store.loadPlan()).resolves.toEqual(PLANCHE_PLAN);
    });
  });
});
