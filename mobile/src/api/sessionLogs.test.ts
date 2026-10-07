import {
  deleteSessionLogItem,
  fetchSessionLogs,
  registerSessionLogItem,
  updateSessionLogItem,
} from './sessionLogs';

const BASE = 'http://api.test';

const savedSession = {
  id: 'session-1',
  kind: 'mesocycle',
  sessionDate: '2026-10-05',
  mesocycleId: 'meso-1',
  microcycleNumber: 1,
  sessionDay: 2,
  recordedAtUtc: '2026-10-05T18:30:00Z',
  completedAtUtc: null,
  completed: false,
  items: [
    {
      id: 'item-1',
      sessionLogId: 'session-1',
      position: 1,
      exerciseId: 'push_up',
      exerciseName: 'Flexiones',
      role: 'strength',
      pattern: 'push',
      metric: 'reps',
      objective: { sets: 2, repsMin: 8, repsMax: 12, holdSecondsMin: null, holdSecondsMax: null },
      note: null,
      sets: [
        { setNumber: 1, value: 10, metric: 'reps', actualRir: null, loadKg: null },
        { setNumber: 2, value: 11, metric: 'reps', actualRir: 2, loadKg: null },
      ],
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

describe('registerSessionLogItem', () => {
  it('posts a nested session and item snapshot with actual RIR', async () => {
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(savedSession));
    const input = {
      session: {
        kind: 'mesocycle' as const,
        date: '2026-10-05',
        mesocycleId: 'meso-1',
        microcycleNumber: 1,
        sessionDay: 2,
      },
      item: {
        exerciseId: 'push_up',
        role: 'strength' as const,
        pattern: 'push',
        prescribedSets: 2,
        repsMin: 8,
        repsMax: 12,
        holdSecondsMin: null,
        holdSecondsMax: null,
        note: null,
        sets: [{ setNumber: 1, value: 10, actualRir: 2, loadKg: null }],
      },
      clientId: 'client-1',
    };

    await expect(registerSessionLogItem(input)).resolves.toEqual(savedSession);
    expect(fetchMock).toHaveBeenCalledWith(
      `${BASE}/session-logs`,
      expect.objectContaining({ method: 'POST', body: JSON.stringify(input) }),
    );
  });

  it('propagates the API Problem Details when registration fails', async () => {
    jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(
        jsonResponse({ detail: 'El valor real de una serie no puede ser negativo.' }, 400),
      );

    await expect(registerSessionLogItem({} as never)).rejects.toThrow(
      'El valor real de una serie no puede ser negativo.',
    );
  });
});

describe('fetchSessionLogs', () => {
  it('flattens aggregate sessions into view records while retaining session and item ids', async () => {
    const fetchMock = jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(jsonResponse([savedSession]));

    const logs = await fetchSessionLogs();

    expect(fetchMock).toHaveBeenCalledWith(
      `${BASE}/session-logs`,
      expect.objectContaining({ signal: undefined }),
    );
    expect(logs).toEqual([
      {
        id: 'item-1',
        sessionLogId: 'session-1',
        exerciseId: 'push_up',
        exerciseName: 'Flexiones',
        metric: 'reps',
        mesocycleId: 'meso-1',
        sessionDay: 2,
        microcycleNumber: 1,
        recordedAtUtc: savedSession.recordedAtUtc,
        sets: [
          { setNumber: 1, value: 10, effort: null },
          { setNumber: 2, value: 11, effort: 2 },
        ],
      },
    ]);
  });
});

describe('updateSessionLogItem', () => {
  it('uses the session- and item-scoped route with aggregate set fields', async () => {
    const item = savedSession.items[0];
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(item));

    await expect(
      updateSessionLogItem('session-1', 'item-1', [
        { setNumber: 1, value: 12, actualRir: 1, loadKg: null },
      ]),
    ).resolves.toEqual(item);

    expect(fetchMock).toHaveBeenCalledWith(
      `${BASE}/session-logs/session-1/items/item-1`,
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({ sets: [{ setNumber: 1, value: 12, actualRir: 1, loadKg: null }] }),
      }),
    );
  });
});

describe('deleteSessionLogItem', () => {
  it('uses the session- and item-scoped route', async () => {
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(null, 204));

    await deleteSessionLogItem('session-1', 'item-1');

    expect(fetchMock).toHaveBeenCalledWith(
      `${BASE}/session-logs/session-1/items/item-1`,
      expect.objectContaining({ method: 'DELETE' }),
    );
  });
});
