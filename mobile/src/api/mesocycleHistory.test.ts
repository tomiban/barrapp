import { fetchMesocycleDetail, fetchMesocycleHistory } from './mesocycleHistory';

/**
 * El cliente del historial de mesociclos habla con `GET /plan/history` y
 * `GET /plan/history/{id}`. Se mockea `fetch` en la frontera del sistema; la URL base se fija con
 * `EXPO_PUBLIC_API_URL` para no depender del host del dev server.
 */

const BASE = 'http://api.test';

const plan = {
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
              exerciseName: 'Planche inclinada',
              role: 'skill',
              pattern: null,
              sets: 3,
              repsMin: null,
              repsMax: null,
              holdSecondsMin: 20,
              holdSecondsMax: 20,
            },
          ],
        },
      ],
    },
  ],
};

const summary = {
  id: 'meso-1',
  skillId: 'planche',
  skillName: 'Planche',
  trainingDays: 3,
  startedAtUtc: '2026-10-01T08:00:00Z',
  closedAtUtc: '2026-11-01T08:00:00Z',
};

function jsonResponse(body: unknown, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 200 ? 'OK' : 'Not Found',
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

describe('fetchMesocycleHistory', () => {
  it('hace un GET a /plan/history y devuelve los mesociclos cerrados', async () => {
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse([summary]));

    const history = await fetchMesocycleHistory();

    expect(fetchMock).toHaveBeenCalledWith(
      `${BASE}/plan/history`,
      expect.objectContaining({ signal: undefined }),
    );
    expect(history).toEqual([summary]);
  });

  it('propaga el detalle del Problem Details cuando el API falla', async () => {
    jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(jsonResponse({ detail: 'Error interno del servidor' }, 500));

    await expect(fetchMesocycleHistory()).rejects.toThrow('Error interno del servidor');
  });
});

describe('fetchMesocycleDetail', () => {
  it('hace un GET a /plan/history/{id} y devuelve el plan del mesociclo', async () => {
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(plan));

    const detail = await fetchMesocycleDetail('meso-1');

    expect(fetchMock).toHaveBeenCalledWith(
      `${BASE}/plan/history/meso-1`,
      expect.objectContaining({ signal: undefined }),
    );
    expect(detail).toEqual(plan);
  });

  it('propaga el detalle del Problem Details cuando el mesociclo no existe', async () => {
    jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(
        jsonResponse({ detail: 'No hay ningún mesociclo guardado con ese identificador.' }, 404),
      );

    await expect(fetchMesocycleDetail('meso-desconocido')).rejects.toThrow(
      'No hay ningún mesociclo guardado con ese identificador.',
    );
  });
});
