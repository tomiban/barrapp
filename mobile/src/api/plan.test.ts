import { closeMesocycle, generatePlan } from './plan';

/** El cliente del cierre del mesociclo habla con `POST /plan/close`. Se mockea `fetch` en la
 * frontera del sistema; la URL base se fija con `EXPO_PUBLIC_API_URL`. */

const BASE = 'http://api.test';

const closed = {
  mesocycleId: 'meso-1',
  skillId: 'planche',
  skillName: 'Planche',
  trainingDays: 3,
  startedAtUtc: '2026-10-01T08:00:00Z',
  closedAtUtc: '2026-11-01T08:00:00Z',
  maximums: [
    { exerciseCode: 'pull_up', repetitions: 5 },
    { exerciseCode: 'push_up', repetitions: 14 },
    { exerciseCode: 'squat', repetitions: 20 },
  ],
};

function jsonResponse(body: unknown, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 200 ? 'OK' : 'Conflict',
    json: async () => body,
  } as Response;
}

const originalEnv = process.env.EXPO_PUBLIC_API_URL;

beforeEach(() => {
  process.env.EXPO_PUBLIC_API_URL = BASE;
  jest.restoreAllMocks();
});

afterAll(() => {
  if (originalEnv === undefined) {
    delete process.env.EXPO_PUBLIC_API_URL;
  } else {
    process.env.EXPO_PUBLIC_API_URL = originalEnv;
  }
});

describe('closeMesocycle', () => {
  it('hace un POST a /plan/close y devuelve el cierre con los máximos ajustados', async () => {
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(closed));

    const result = await closeMesocycle();

    expect(fetchMock).toHaveBeenCalledWith(
      `${BASE}/plan/close`,
      expect.objectContaining({ method: 'POST', signal: undefined }),
    );
    expect(result).toEqual(closed);
    expect(result.maximums).toContainEqual({ exerciseCode: 'push_up', repetitions: 14 });
  });

  it('propaga el detalle del Problem Details cuando no hay mesociclo en curso', async () => {
    jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(
        jsonResponse({ detail: 'No hay ningún mesociclo en curso para cerrar.' }, 409),
      );

    await expect(() => closeMesocycle()).rejects.toThrow(
      'No hay ningún mesociclo en curso para cerrar.',
    );
  });

  it('falls back to a generic message when the body has no Problem Details', async () => {
    jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse({}, 500));

    await expect(closeMesocycle()).rejects.toThrow('El API respondió 500');
  });
});

describe('generatePlan (#94)', () => {
  const plan = {
    skillId: 'planche',
    trainingDays: 3,
    startDate: '2026-03-09',
    skillStage: {
      order: 1,
      name: 'Tuck',
      exerciseId: 'planche-tuck',
      criterion: { metric: 'seconds', target: 10, sets: 3 },
      notes: '',
    },
    microcycles: [],
  };

  it('posts the start date the athlete chose and returns the generated plan', async () => {
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(plan));

    const result = await generatePlan('2026-03-03');

    expect(fetchMock).toHaveBeenCalledWith(
      `${BASE}/plan`,
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ startDate: '2026-03-03' }),
      }),
    );
    expect(result).toEqual(plan);
  });

  it('sends no start date when the athlete has not chosen one', async () => {
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(plan));

    await generatePlan();

    expect(fetchMock).toHaveBeenCalledWith(
      `${BASE}/plan`,
      expect.objectContaining({ body: JSON.stringify({ startDate: null }) }),
    );
  });
});
