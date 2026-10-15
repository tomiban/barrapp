import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';
import { router } from 'expo-router';

import { todayStartDate } from '../src/api/athleteProfile';
import PlanScreen from '../src/app/(tabs)/plan';

jest.mock('expo-router', () => ({ router: { navigate: jest.fn(), push: jest.fn() } }));

/** Almacén local falseado: la pantalla guarda/lee la caché sin tocar expo-sqlite. */
jest.mock('../src/offline/planStore', () => {
  const store = {
    async savePlan(): Promise<void> {},
    async loadPlan(): Promise<null> {
      return null;
    },
  };
  return { openPlanStore: jest.fn(async () => store) };
});

/** Respuesta mínima con la forma que consume el módulo de API. */
function jsonResponse(status: number, body: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 404 ? 'Not Found' : 'OK',
    json: async () => body,
  } as unknown as Response;
}

/** Enruta la respuesta del API por endpoint: el plan, los registros y el historial. */
function routeFetch({
  plan,
  logs = [],
  history = [],
}: {
  plan: unknown;
  logs?: unknown[];
  history?: unknown[];
}): typeof fetch {
  return jest.fn(async (input: unknown) => {
    const url = String(input);
    if (url.includes('/session-logs')) {
      return jsonResponse(200, logs);
    }
    if (url.includes('/plan/history')) {
      return jsonResponse(200, history);
    }
    return jsonResponse(200, plan);
  }) as unknown as typeof fetch;
}

/** Fila de skill del plan; el nombre cambia por semana para poder distinguirlas. */
function skillItem(name: string) {
  return {
    exerciseId: 'planche-lean',
    exerciseName: name,
    role: 'skill',
    pattern: null,
    sets: 3,
    repsMin: null,
    repsMax: null,
    holdSecondsMin: 20,
    holdSecondsMax: 20,
  };
}

const PUSH_ITEM = {
  exerciseId: 'push_up',
  exerciseName: 'Flexiones',
  role: 'strength',
  pattern: 'push',
  sets: 3,
  repsMin: 8,
  repsMax: 12,
  holdSecondsMin: null,
  holdSecondsMax: null,
};

const PULL_ITEM = {
  exerciseId: 'pull_up',
  exerciseName: 'Dominadas',
  role: 'strength',
  pattern: 'pull',
  sets: 3,
  repsMin: 6,
  repsMax: 8,
  holdSecondsMin: null,
  holdSecondsMax: null,
};

const CORE_ITEM = {
  exerciseId: 'hollow-body-hold',
  exerciseName: 'Cuerpo hueco',
  role: 'core',
  pattern: null,
  sets: 3,
  repsMin: null,
  repsMax: null,
  holdSecondsMin: 20,
  holdSecondsMax: 30,
};

/** Semana del mesociclo con tres sesiones full-body (skill + empuje + tirón + core). */
function microcycle(number: number) {
  const skillName = number === 1 ? 'Planche inclinada' : 'Planche agrupada';

  return {
    number,
    sessions: [1, 2, 3].map((day) => ({
      day,
      weekday: null,
      date: null,
      items: [skillItem(skillName), PUSH_ITEM, PULL_ITEM, CORE_ITEM],
    })),
  };
}

const PLAN = {
  skillId: 'planche',
  trainingDays: 3,
  mesocycleId: 'meso-1',
  startDate: '2026-03-02',
  skillStage: {
    order: 1,
    name: 'Planche inclinada',
    exerciseId: 'planche-lean',
    criterion: { metric: 'seconds', target: 20, sets: 3 },
    notes: '',
  },
  microcycles: [1, 2, 3, 4].map(microcycle),
};

/**
 * PlanScreen (#84): la hoja *Plan · Periodización 4S* del rediseño. Cabecera con la semana y el
 * reparto, selector de semanas S1–S4, volumen semanal prescrito, sesiones de la semana (la de hoy
 * con **Comenzar sesión**) y el control de ajuste.
 */
describe('PlanScreen', () => {
  const originalFetch = global.fetch;
  const navigate = router.navigate as jest.Mock;

  beforeEach(() => {
    navigate.mockReset();
  });

  afterEach(() => {
    global.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  it('renders the week sheet with the volume and the sessions, and switches week', async () => {
    global.fetch = routeFetch({ plan: PLAN });

    await render(<PlanScreen />);

    await waitFor(() => expect(screen.getByTestId('plan-microcycle-1')).toBeOnTheScreen());

    // Cabecera con la semana en curso y el reparto de patrones.
    expect(screen.getByText('Semana 1 de 4')).toBeOnTheScreen();
    expect(screen.getByTestId('plan-week-1')).toHaveTextContent(/En curso/);
    expect(screen.getByTestId('plan-week-2')).toHaveTextContent(/Pendiente/);
    expect(screen.getByTestId('plan-week-3')).toHaveTextContent(/RIR 1/);
    expect(screen.getByTestId('plan-week-4')).toHaveTextContent(/Descarga/);

    // Volumen prescrito por patrón: tres sesiones × 3 series de empuje y otras tres de tirón.
    expect(screen.getByTestId('plan-volume-push')).toHaveTextContent(/9 Empuje/);
    expect(screen.getByTestId('plan-volume-pull')).toHaveTextContent(/9 Tirón/);

    // Las tres sesiones de la semana, todavía pendientes.
    expect(screen.getByTestId('plan-session-1-1')).toBeOnTheScreen();
    expect(screen.getByTestId('plan-session-1-3')).toBeOnTheScreen();
    expect(screen.getByText('0 / 3 completadas')).toBeOnTheScreen();

    // Cambiar de semana cambia las sesiones.
    await fireEvent.press(screen.getByTestId('plan-week-2'));

    await waitFor(() => expect(screen.getByTestId('plan-microcycle-2')).toBeOnTheScreen());
    expect(screen.queryByTestId('plan-microcycle-1')).not.toBeOnTheScreen();
    expect(screen.getAllByText(/Planche agrupada/).length).toBeGreaterThan(0);
  });

  it('marks the session of today with its start button and opens Entreno', async () => {
    const today = todayStartDate();
    const dated = {
      ...PLAN,
      microcycles: [
        {
          number: 1,
          sessions: [
            {
              day: 1,
              weekday: null,
              date: today,
              items: [skillItem('Planche inclinada'), PUSH_ITEM],
            },
            {
              day: 2,
              weekday: null,
              date: null,
              items: [skillItem('Planche inclinada'), PULL_ITEM],
            },
          ],
        },
        ...PLAN.microcycles.slice(1),
      ],
    };
    global.fetch = routeFetch({ plan: dated });

    await render(<PlanScreen />);

    await waitFor(() => expect(screen.getByTestId('plan-session-1-1-start')).toBeOnTheScreen());
    expect(screen.getByTestId('plan-session-1-1')).toHaveTextContent(/Hoy · Día 1 · S01/);

    await fireEvent.press(screen.getByTestId('plan-session-1-1-start'));
    expect(navigate).toHaveBeenCalledWith('/entreno');
  });

  it('marks a session as done from its record and counts it', async () => {
    global.fetch = routeFetch({
      plan: PLAN,
      logs: [
        {
          id: 'session-1',
          kind: 'mesocycle',
          sessionDate: '2026-03-02',
          mesocycleId: 'meso-1',
          microcycleNumber: 1,
          sessionDay: 1,
          recordedAtUtc: '2026-03-02T18:00:00Z',
          completedAtUtc: '2026-03-02T18:40:00Z',
          completed: true,
          items: [
            {
              id: 'item-1',
              sessionLogId: 'session-1',
              position: 0,
              exerciseId: 'push_up',
              exerciseName: 'Flexiones',
              role: 'strength',
              pattern: 'push',
              metric: 'reps',
              objective: {
                sets: 3,
                repsMin: 8,
                repsMax: 12,
                holdSecondsMin: null,
                holdSecondsMax: null,
              },
              note: null,
              sets: [{ setNumber: 1, value: 10, metric: 'reps', actualRir: 2, loadKg: null }],
            },
          ],
        },
      ],
    });

    await render(<PlanScreen />);

    await waitFor(() => expect(screen.getByTestId('plan-session-1-1')).toHaveTextContent(/Hecho/));
    expect(screen.getByTestId('plan-week-1')).toHaveTextContent(/En curso/);
    expect(screen.getByText('1 / 3 completadas')).toBeOnTheScreen();
  });

  it('shows the API problem detail and retries on failure', async () => {
    global.fetch = jest
      .fn()
      .mockResolvedValueOnce(
        jsonResponse(404, { detail: 'No hay ningún perfil guardado para este atleta.' }),
      )
      .mockResolvedValue(jsonResponse(200, PLAN)) as unknown as typeof fetch;

    await render(<PlanScreen />);

    await waitFor(() =>
      expect(screen.getByText('No hay ningún perfil guardado para este atleta.')).toBeOnTheScreen(),
    );

    await fireEvent.press(screen.getByTestId('plan-retry'));

    await waitFor(() => expect(screen.getByTestId('plan-microcycle-1')).toBeOnTheScreen());
  });
});
