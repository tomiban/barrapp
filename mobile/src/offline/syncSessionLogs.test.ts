import type { RegisterSessionLogItemInput, SessionLogSession } from '@/api/sessionLogs';
import type { PendingSessionLog } from './sessionLogOutbox';
import { syncPendingSessionLogs } from './syncSessionLogs';

function request(
  exerciseId: string,
  clientId: string,
  sets: PendingSessionLog['sets'],
): RegisterSessionLogItemInput {
  return {
    session: {
      kind: 'mesocycle',
      date: '2026-10-05',
      mesocycleId: 'meso-1',
      microcycleNumber: 1,
      sessionDay: 2,
    },
    item: {
      exerciseId,
      role: 'strength',
      pattern: 'push',
      prescribedSets: sets.length,
      repsMin: 8,
      repsMax: 12,
      holdSecondsMin: null,
      holdSecondsMax: null,
      note: null,
      sets: sets.map((set) => ({
        setNumber: set.setNumber,
        value: set.value,
        actualRir: set.effort,
        loadKg: null,
      })),
    },
    clientId,
  };
}

/** Registro de ejemplo pendiente de subir (ancla `push_up`, día 2). */
const PENDING_PUSH_UP: PendingSessionLog = {
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
  request: request('push_up', 'client-1', [
    { setNumber: 1, value: 10, effort: null },
    { setNumber: 2, value: 11, effort: null },
  ]),
};

/** Segundo registro pendiente (ancla `pull_up`, mismo día). */
const PENDING_PULL_UP: PendingSessionLog = {
  clientId: 'client-2',
  exerciseId: 'pull_up',
  exerciseName: 'Dominadas',
  metric: 'reps',
  mesocycleId: null,
  sessionDay: 2,
  sets: [{ setNumber: 1, value: 5, effort: null }],
  updatedAt: '2026-10-05T10:01:00.000Z',
  request: request('pull_up', 'client-2', [{ setNumber: 1, value: 5, effort: null }]),
};

/** Confirmación que el servidor devuelve tras un alta correcta. */
const SAVED_LOG: SessionLogSession = {
  id: 'session-1',
  kind: 'mesocycle',
  sessionDate: '2026-10-05',
  mesocycleId: 'meso-1',
  microcycleNumber: 1,
  sessionDay: 2,
  recordedAtUtc: '2026-10-05T18:30:00Z',
  completedAtUtc: null,
  completed: false,
  items: [],
};

/** Outbox de prueba: pendientes configurables; `remove` registra los ids retirados. */
function createOutboxStub(pending: PendingSessionLog[] = []) {
  const removed: string[] = [];
  return {
    removed,
    listPending: jest.fn(async () => [...pending]),
    remove: jest.fn(async (clientId: string) => {
      removed.push(clientId);
    }),
  };
}

function createRegisterStub(failureOn?: string[]) {
  const calls: RegisterSessionLogItemInput[] = [];
  return {
    calls,
    register: jest.fn(async (input: RegisterSessionLogItemInput): Promise<SessionLogSession> => {
      if (failureOn?.includes(input.item.exerciseId)) {
        throw new Error('Network request failed');
      }
      calls.push(input);
      return SAVED_LOG;
    }),
  };
}

describe('syncPendingSessionLogs', () => {
  it('sube cada registro pendiente en orden y lo retira de la cola', async () => {
    const outbox = createOutboxStub([PENDING_PULL_UP, PENDING_PUSH_UP]);
    const { register, calls } = createRegisterStub();

    const result = await syncPendingSessionLogs(outbox, register);

    expect(calls).toEqual([
      {
        session: PENDING_PULL_UP.request?.session,
        item: PENDING_PULL_UP.request?.item,
        clientId: 'client-2',
      },
      {
        ...PENDING_PUSH_UP.request,
      },
    ]);
    expect(outbox.remove).toHaveBeenCalledWith('client-2');
    expect(outbox.remove).toHaveBeenCalledWith('client-1');
    expect(result).toEqual({ synced: 2, remaining: 0 });
  });

  it('sube el esfuerzo (RIR/RPE) real anotado en cada serie', async () => {
    const pendingWithEffort: PendingSessionLog = {
      ...PENDING_PUSH_UP,
      sets: [
        { setNumber: 1, value: 10, effort: 2 },
        { setNumber: 2, value: 11, effort: 2 },
      ],
      request: request('push_up', 'client-1', [
        { setNumber: 1, value: 10, effort: 2 },
        { setNumber: 2, value: 11, effort: 2 },
      ]),
    };
    const outbox = createOutboxStub([pendingWithEffort]);
    const { register, calls } = createRegisterStub();

    const result = await syncPendingSessionLogs(outbox, register);

    expect(calls).toEqual([
      {
        ...pendingWithEffort.request,
      },
    ]);
    expect(outbox.remove).toHaveBeenCalledWith('client-1');
    expect(result).toEqual({ synced: 1, remaining: 0 });
  });

  it('no vuelve a subir lo ya sincronizado en un segundo intento (sin duplicados)', async () => {
    const outbox = createOutboxStub([PENDING_PUSH_UP]);
    const { register } = createRegisterStub();
    outbox.listPending.mockImplementation(async () =>
      outbox.removed.includes(PENDING_PUSH_UP.clientId) ? [] : [PENDING_PUSH_UP],
    );

    await syncPendingSessionLogs(outbox, register);
    await syncPendingSessionLogs(outbox, register);

    expect(register).toHaveBeenCalledTimes(1);
  });

  it('cada alta lleva el clientId idempotente de la outbox (ticket #26)', async () => {
    const outbox = createOutboxStub([PENDING_PUSH_UP]);
    const { register, calls } = createRegisterStub();

    await syncPendingSessionLogs(outbox, register);

    expect(calls).toHaveLength(1);
    expect(calls[0].clientId).toBe('client-1');
    expect(register).toHaveBeenCalledWith(expect.objectContaining({ clientId: 'client-1' }));
  });

  it('se detiene ante el primer fallo y deja lo pendiente para el siguiente intento', async () => {
    const outbox = createOutboxStub([PENDING_PUSH_UP, PENDING_PULL_UP]);
    const { register } = createRegisterStub(['push_up']);

    const result = await syncPendingSessionLogs(outbox, register);

    expect(register).toHaveBeenCalledTimes(1);
    expect(result).toEqual({ synced: 0, remaining: 2 });
    expect(outbox.remove).not.toHaveBeenCalled();
  });

  it('retira lo subido aunque el intento se detenga después por un fallo', async () => {
    const outbox = createOutboxStub([PENDING_PULL_UP, PENDING_PUSH_UP]);
    const { register } = createRegisterStub(['push_up']);

    const result = await syncPendingSessionLogs(outbox, register);

    expect(outbox.remove).toHaveBeenCalledWith('client-2');
    expect(result).toEqual({ synced: 1, remaining: 1 });
  });

  it('no hace nada cuando la cola está vacía', async () => {
    const outbox = createOutboxStub([]);
    const { register } = createRegisterStub();

    const result = await syncPendingSessionLogs(outbox, register);

    expect(register).not.toHaveBeenCalled();
    expect(result).toEqual({ synced: 0, remaining: 0 });
  });
});
