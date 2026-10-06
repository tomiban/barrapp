import { fetchSueltaHistory } from './soloSessionHistory';

/**
 * El cliente del historial de sueltas habla con `GET /sessions/suelta`. Se mockea `fetch` en la
 * frontera del sistema; la URL base se fija con `EXPO_PUBLIC_API_URL` para no depender del host
 * del dev server.
 */

const BASE = 'http://api.test';

const entry = {
  id: 'suelta-1',
  timeMinutes: 30,
  energy: 'media',
  focus: 'patron',
  pattern: 'push',
  skillId: null,
  skillName: null,
  status: 'generada',
  createdAtUtc: '2026-10-05T18:30:00Z',
  recordedAtUtc: null,
  items: [
    {
      exerciseId: 'push_up',
      exerciseName: 'Flexión',
      role: 'strength',
      pattern: 'push',
      sets: 3,
      repsMin: 5,
      repsMax: 8,
      holdSecondsMin: null,
      holdSecondsMax: null,
      note: null,
    },
  ],
};

function jsonResponse(body: unknown, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 200 ? 'OK' : 'Bad Request',
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

describe('fetchSueltaHistory', () => {
  it('hace un GET a /sessions/suelta y devuelve la lista de sueltas', async () => {
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse([entry]));

    const history = await fetchSueltaHistory();

    expect(fetchMock).toHaveBeenCalledWith(
      `${BASE}/sessions/suelta`,
      expect.objectContaining({ signal: undefined }),
    );
    expect(history).toEqual([entry]);
  });

  it('propaga el detalle del Problem Details cuando el API rechaza el historial', async () => {
    jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(jsonResponse({ detail: 'Error interno del servidor' }, 500));

    await expect(fetchSueltaHistory()).rejects.toThrow('Error interno del servidor');
  });
});
