import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import PlanScreen from '../src/app/(tabs)/plan';

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

const STRENGTH_ITEM = {
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

/** Semana del mesociclo con tres sesiones full-body. */
function microcycle(number: number) {
  const skillName = number === 1 ? 'Planche inclinada' : 'Planche agrupada';

  return {
    number,
    sessions: [1, 2, 3].map((day) => ({
      day,
      items: [skillItem(skillName), STRENGTH_ITEM, CORE_ITEM],
    })),
  };
}

const PLAN = {
  skillId: 'planche',
  trainingDays: 3,
  skillStage: {
    order: 1,
    name: 'Planche inclinada',
    exerciseId: 'planche-lean',
    criterion: { metric: 'seconds', target: 20, sets: 3 },
    notes: 'Inclinación con los hombros por delante de las manos.',
  },
  microcycles: [1, 2, 3, 4].map(microcycle),
};

describe('PlanScreen', () => {
  const originalFetch = global.fetch;

  afterEach(() => {
    global.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  it('renders the sessions of the selected week with their items, and switches week', async () => {
    global.fetch = jest.fn().mockResolvedValue(jsonResponse(200, PLAN)) as unknown as typeof fetch;

    await render(<PlanScreen />);

    await waitFor(() => expect(screen.getByTestId('plan-microcycle-1')).toBeOnTheScreen());

    // La semana 1 trae sus tres sesiones con las filas del día.
    expect(screen.getByTestId('plan-session-1')).toBeOnTheScreen();
    expect(screen.getByTestId('plan-session-2')).toBeOnTheScreen();
    expect(screen.getByTestId('plan-session-3')).toBeOnTheScreen();
    // La etapa actual del skill y su criterio de avance abren el plan (#18).
    expect(screen.getByText('Etapa 1 · Planche inclinada')).toBeOnTheScreen();
    expect(screen.getByText('Supera 20 s × 3 series')).toBeOnTheScreen();
    expect(screen.getByText('Etapa actual')).toBeOnTheScreen();
    // Cada una de las tres sesiones repite el bloque de skill, la fuerza y el core.
    expect(screen.getAllByText('Planche inclinada')).toHaveLength(3);
    expect(screen.getAllByText('Skill · 3 × 20–20 s')).toHaveLength(3);
    expect(screen.getAllByText('Fuerza · Empuje · 3 × 8–12 reps')).toHaveLength(3);
    expect(screen.getAllByText('Cuerpo hueco')).toHaveLength(3);
    expect(screen.getAllByText('Core · 3 × 20–30 s')).toHaveLength(3);

    // La semana 2 todavía no se muestra.
    expect(screen.queryByTestId('plan-microcycle-2')).not.toBeOnTheScreen();

    await fireEvent.press(screen.getByTestId('plan-week-2'));

    await waitFor(() => expect(screen.getByTestId('plan-microcycle-2')).toBeOnTheScreen());
    expect(screen.getAllByText('Planche agrupada').length).toBeGreaterThan(0);
    expect(screen.queryByTestId('plan-microcycle-1')).not.toBeOnTheScreen();
  });

  it('shows the API problem detail and retries on failure', async () => {
    const fetchMock = jest
      .fn()
      .mockResolvedValueOnce(
        jsonResponse(404, { detail: 'No hay ningún perfil guardado para este atleta.' }),
      )
      .mockResolvedValueOnce(jsonResponse(200, PLAN));
    global.fetch = fetchMock as unknown as typeof fetch;

    await render(<PlanScreen />);

    await waitFor(() =>
      expect(screen.getByText('No hay ningún perfil guardado para este atleta.')).toBeOnTheScreen(),
    );

    await fireEvent.press(screen.getByTestId('plan-retry'));

    await waitFor(() => expect(screen.getByTestId('plan-microcycle-1')).toBeOnTheScreen());
  });
});
