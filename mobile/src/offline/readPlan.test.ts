import type { Plan } from '@/api/plan';
import { readPlan } from './readPlan';
import type { PlanStore } from './planStore';

/** Plan de ejemplo servido por la red. */
const NETWORK_PLAN: Plan = {
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

/** Plan distinto que ya vive en la caché local. */
const CACHED_PLAN: Plan = {
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

/** Almacén de prueba: por defecto no hay caché y guardar no falla. */
function createStoreStub(overrides: Partial<PlanStore> = {}): PlanStore {
  return {
    savePlan: jest.fn(async () => undefined),
    loadPlan: jest.fn(async () => null),
    ...overrides,
  };
}

function createAbortError(): Error {
  const error = new Error('Aborted');
  error.name = 'AbortError';
  return error;
}

function createNetworkError(message: string): Error {
  return new Error(message);
}

describe('readPlan', () => {
  it('serves the plan from the network and caches it', async () => {
    const store = createStoreStub();

    const result = await readPlan(async () => NETWORK_PLAN, store);

    expect(result).toEqual({ source: 'network', plan: NETWORK_PLAN });
    expect(store.savePlan).toHaveBeenCalledWith(NETWORK_PLAN);
  });

  it('serves the cached plan when the network is unreachable', async () => {
    const store = createStoreStub({ loadPlan: jest.fn(async () => CACHED_PLAN) });

    const result = await readPlan(async () => {
      throw createNetworkError('Network request failed');
    }, store);

    expect(result).toEqual({ source: 'cache', plan: CACHED_PLAN });
  });

  it('reports a failure when offline and nothing is cached', async () => {
    const store = createStoreStub();

    const result = await readPlan(async () => {
      throw createNetworkError('Network request failed');
    }, store);

    expect(result).toEqual({ source: 'failure', message: 'Network request failed' });
  });

  it('serves the network plan even when saving to the cache fails', async () => {
    const store = createStoreStub({
      savePlan: jest.fn(async () => {
        throw createNetworkError('disk full');
      }),
    });

    const result = await readPlan(async () => NETWORK_PLAN, store);

    expect(result).toEqual({ source: 'network', plan: NETWORK_PLAN });
  });

  it('reports the network failure when the cache cannot be read either', async () => {
    const store = createStoreStub({
      loadPlan: jest.fn(async () => {
        throw createNetworkError('db locked');
      }),
    });

    const result = await readPlan(async () => {
      throw createNetworkError('Network request failed');
    }, store);

    expect(result).toEqual({ source: 'failure', message: 'Network request failed' });
  });

  it('propagates an aborted request so the caller can ignore it', async () => {
    const abortError = createAbortError();

    await expect(readPlan(async () => Promise.reject(abortError), createStoreStub())).rejects.toBe(
      abortError,
    );
  });
});
