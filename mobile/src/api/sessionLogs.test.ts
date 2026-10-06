import {
  deleteSessionLog,
  fetchSessionLogs,
  registerSessionLog,
  updateSessionLog,
} from './sessionLogs';

/**
 * El cliente de registro habla con `POST /session-logs` y `GET /session-logs`. Se mockea
 * `fetch` en la frontera del sistema; la URL base se fija con `EXPO_PUBLIC_API_URL` para no
 * depender del host del dev server.
 */

const BASE = 'http://api.test';

const savedLog = {
  id: 'log-1',
  exerciseId: 'push_up',
  exerciseName: 'Flexiones',
  metric: 'reps',
  mesocycleId: null,
  sessionDay: 2,
  recordedAtUtc: '2026-10-05T18:30:00Z',
  sets: [
    { setNumber: 1, value: 10, effort: null },
    { setNumber: 2, value: 11, effort: null },
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

describe('registerSessionLog', () => {
  it('envía un POST a /session-logs con las series y devuelve el registro guardado', async () => {
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(savedLog));

    const result = await registerSessionLog({
      exerciseId: 'push_up',
      mesocycleId: null,
      sessionDay: 2,
      sets: [
        { setNumber: 1, value: 10 },
        { setNumber: 2, value: 11 },
      ],
    });

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0];
    expect(String(url)).toBe(`${BASE}/session-logs`);
    expect(init?.method).toBe('POST');
    expect(init?.headers).toMatchObject({ 'Content-Type': 'application/json' });
    expect(JSON.parse(init?.body as string)).toEqual({
      exerciseId: 'push_up',
      mesocycleId: null,
      sessionDay: 2,
      sets: [
        { setNumber: 1, value: 10 },
        { setNumber: 2, value: 11 },
      ],
    });
    expect(result).toEqual(savedLog);
  });

  it('envía el esfuerzo (RIR/RPE) real de cada serie en el POST', async () => {
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(savedLog));

    await registerSessionLog({
      exerciseId: 'push_up',
      mesocycleId: null,
      sessionDay: 2,
      sets: [
        { setNumber: 1, value: 10, effort: 2 },
        { setNumber: 2, value: 11 },
      ],
    });

    expect(JSON.parse(fetchMock.mock.calls[0][1]?.body as string)).toEqual({
      exerciseId: 'push_up',
      mesocycleId: null,
      sessionDay: 2,
      sets: [
        { setNumber: 1, value: 10, effort: 2 },
        { setNumber: 2, value: 11 },
      ],
    });
  });

  it('propaga el detalle del Problem Details cuando el API rechaza el registro', async () => {
    jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(
        jsonResponse({ detail: 'El valor real de una serie no puede ser negativo.' }, 400),
      );

    await expect(
      registerSessionLog({
        exerciseId: 'push_up',
        sessionDay: 1,
        sets: [{ setNumber: 1, value: -1 }],
      }),
    ).rejects.toThrow('El valor real de una serie no puede ser negativo.');
  });
});

describe('fetchSessionLogs', () => {
  it('hace un GET a /session-logs y devuelve la lista de registros', async () => {
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse([savedLog]));

    const logs = await fetchSessionLogs();

    expect(fetchMock).toHaveBeenCalledWith(
      `${BASE}/session-logs`,
      expect.objectContaining({ signal: undefined }),
    );
    expect(logs).toEqual([savedLog]);
  });
});

describe('updateSessionLog', () => {
  it('envía un PUT a /session-logs/{id} con las nuevas series y devuelve el registro actualizado', async () => {
    const updated = { ...savedLog, sets: [{ setNumber: 1, value: 12 }] };
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(updated));

    const result = await updateSessionLog('log-1', {
      sets: [{ setNumber: 1, value: 12 }],
    });

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0];
    expect(String(url)).toBe(`${BASE}/session-logs/log-1`);
    expect(init?.method).toBe('PUT');
    expect(JSON.parse(init?.body as string)).toEqual({
      sets: [{ setNumber: 1, value: 12 }],
    });
    expect(result).toEqual(updated);
  });

  it('propaga el detalle del Problem Details cuando el API rechaza la edición', async () => {
    jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(
        jsonResponse({ detail: 'El registro de sesión indicado no existe.' }, 404),
      );

    await expect(
      updateSessionLog('ghost', { sets: [{ setNumber: 1, value: 12 }] }),
    ).rejects.toThrow('El registro de sesión indicado no existe.');
  });
});

describe('deleteSessionLog', () => {
  it('envía un DELETE a /session-logs/{id} sin cuerpo', async () => {
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(null, 204));

    await deleteSessionLog('log-1');

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0];
    expect(String(url)).toBe(`${BASE}/session-logs/log-1`);
    expect(init?.method).toBe('DELETE');
    expect(init?.body).toBeUndefined();
  });

  it('propaga el detalle del Problem Details cuando el API no encuentra el registro', async () => {
    jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(
        jsonResponse({ detail: 'El registro de sesión indicado no existe.' }, 404),
      );

    await expect(deleteSessionLog('ghost')).rejects.toThrow(
      'El registro de sesión indicado no existe.',
    );
  });
});
