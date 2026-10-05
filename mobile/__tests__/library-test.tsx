import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import LibraryScreen from '../src/app/(tabs)/biblioteca';

/** Respuesta mínima con la forma que consume el módulo de API. */
function jsonResponse(status: number, body: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 404 ? 'Not Found' : 'OK',
    json: async () => body,
  } as unknown as Response;
}

/** Catálogo mínimo con dos patrones, incluido cardio. */
const CATALOG = {
  groups: [
    {
      group: 'push',
      exercises: [
        {
          id: 'push-up',
          name: 'Flexiones',
          metric: 'reps',
          tracksMaximum: true,
          regressionId: 'incline-push-up',
          skillId: null,
        },
        {
          id: 'dip',
          name: 'Fondos en paralelas',
          metric: 'reps',
          tracksMaximum: true,
          regressionId: 'bench-dip',
          skillId: null,
        },
        {
          id: 'pike-push-up',
          name: 'Flexiones en pica',
          metric: 'reps',
          tracksMaximum: false,
          regressionId: null,
          skillId: 'handstand',
        },
      ],
    },
    {
      group: 'cardio',
      exercises: [
        {
          id: 'burpees',
          name: 'Burpees',
          metric: 'reps',
          tracksMaximum: false,
          regressionId: null,
          skillId: null,
        },
      ],
    },
  ],
};

/** Skills mínimos con su escalera y una rutina de patrón de apoyo. */
const SKILLS = [
  {
    id: 'planche',
    name: 'Planche',
    group: 'push',
    lever: true,
    stages: [
      {
        order: 1,
        name: 'Planche inclinada',
        exerciseId: 'planche-lean',
        criterion: { metric: 'seconds', target: 20, sets: 3 },
        notes: 'Hombros por delante de las manos.',
      },
      {
        order: 2,
        name: 'Planche agrupada',
        exerciseId: 'planche-tuck',
        criterion: { metric: 'seconds', target: 10, sets: 3 },
        notes: 'Rodillas al pecho.',
      },
    ],
    patternRoutines: [
      {
        id: 'r1',
        name: 'Modelo R1',
        intensity: 2,
        equipment: 'Sin equipamiento',
        items: [
          {
            exerciseId: 'planche-tuck',
            sets: 4,
            repsMin: null,
            repsMax: null,
            holdSecondsMin: 5,
            holdSecondsMax: 15,
            restSeconds: 180,
            tempo: null,
            supersetGroup: null,
            notes: null,
          },
          {
            exerciseId: 'pike-push-up',
            sets: 3,
            repsMin: 5,
            repsMax: 8,
            restSeconds: 120,
            tempo: null,
            supersetGroup: null,
            notes: null,
          },
        ],
      },
    ],
  },
  {
    id: 'pistol-squat',
    name: 'Pistol squat',
    group: 'leg',
    lever: false,
    stages: [
      {
        order: 1,
        name: 'Pistol al cajón',
        exerciseId: 'pistol-box',
        criterion: { metric: 'reps', target: 5, sets: 3 },
        notes: 'Bajar hasta un cajón alto.',
      },
    ],
    patternRoutines: [
      {
        id: 'r1',
        name: 'Modelo R1',
        intensity: 2,
        equipment: null,
        items: [
          {
            exerciseId: 'cossack-squat',
            sets: 3,
            repsMin: 6,
            repsMax: 8,
            restSeconds: 90,
            tempo: null,
            supersetGroup: null,
            notes: null,
          },
        ],
      },
    ],
  },
];

/** Programas generales mínimos: un circuito por tiempo y una sesión de fuerza. */
const ROUTINES = [
  {
    id: 'ponte-en-forma',
    name: 'Ponte en forma',
    type: 'circuit',
    description: 'Rutinas por tiempo para acondicionamiento general.',
    routines: [
      {
        id: 'ponte-en-forma-r1',
        name: 'Rutina 1',
        intensity: 2,
        durationMinutes: 14,
        blocks: [
          {
            name: 'SET 1',
            rounds: 3,
            restSeconds: 0,
            notes: 'Sin descanso.',
            items: [
              {
                exerciseId: 'push-up',
                sets: 1,
                repsMin: null,
                repsMax: null,
                holdSecondsMin: 15,
                holdSecondsMax: 15,
                restSeconds: 0,
                tempo: null,
                supersetGroup: null,
                notes: null,
              },
            ],
          },
        ],
      },
    ],
  },
  {
    id: 'home-workout',
    name: 'Home Workout Avanzados',
    type: 'strength',
    description: null,
    routines: [
      {
        id: 'home-workout-d1',
        name: 'Día 1 — Empuje',
        intensity: 3,
        durationMinutes: null,
        blocks: [
          {
            name: 'Empuje',
            rounds: 1,
            restSeconds: 0,
            notes: null,
            items: [
              {
                exerciseId: 'dip',
                sets: 4,
                repsMin: 6,
                repsMax: 10,
                holdSecondsMin: null,
                holdSecondsMax: null,
                restSeconds: 120,
                tempo: null,
                supersetGroup: 1,
                notes: null,
              },
            ],
          },
        ],
      },
    ],
  },
];

/**
 * Mock de `fetch` que reparte por ruta: skills devuelve la escalera, routines los programas
 * generales, progress la etapa actual por skill y el resto, el catálogo de ejercicios.
 */
function routeFetch(): jest.Mock {
  return jest.fn((input: unknown) => {
    const url = String(input);
    if (url.includes('/catalog/progress')) {
      return Promise.resolve(jsonResponse(200, [{ skillId: 'planche', stageOrder: 2 }]));
    }
    if (url.includes('/catalog/skills')) {
      return Promise.resolve(jsonResponse(200, SKILLS));
    }
    if (url.includes('/catalog/routines')) {
      return Promise.resolve(jsonResponse(200, ROUTINES));
    }
    return Promise.resolve(jsonResponse(200, CATALOG));
  });
}

describe('LibraryScreen · Ejercicios', () => {
  const originalFetch = global.fetch;

  afterEach(() => {
    global.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  it('renders the catalog grouped by pattern, including cardio', async () => {
    const fetchMock = jest.fn().mockResolvedValue(jsonResponse(200, CATALOG));
    global.fetch = fetchMock as unknown as typeof fetch;

    await render(<LibraryScreen />);

    await waitFor(() => expect(screen.getByText('Empuje')).toBeOnTheScreen());
    expect(screen.getByText('Cardio')).toBeOnTheScreen();
    expect(screen.getByText('Flexiones')).toBeOnTheScreen();
    expect(screen.getByText('Fondos en paralelas')).toBeOnTheScreen();
    expect(screen.getByText('Burpees')).toBeOnTheScreen();

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/catalog/exercises'),
      expect.anything(),
    );
  });

  it('switches to the Skills and Rutinas views', async () => {
    global.fetch = routeFetch() as unknown as typeof fetch;

    await render(<LibraryScreen />);
    await waitFor(() => expect(screen.getByText('Flexiones')).toBeOnTheScreen());

    await fireEvent.press(screen.getByTestId('library-section-skills'));
    await waitFor(() => expect(screen.getByTestId('library-skill-planche')).toBeOnTheScreen());

    await fireEvent.press(screen.getByTestId('library-section-routines'));
    await waitFor(() =>
      expect(screen.getByTestId('library-program-ponte-en-forma')).toBeOnTheScreen(),
    );
  });

  it('shows the API problem detail and retries on failure', async () => {
    const fetchMock = jest
      .fn()
      .mockResolvedValueOnce(jsonResponse(500, { detail: 'El catálogo falló.' }))
      .mockResolvedValueOnce(jsonResponse(200, CATALOG));
    global.fetch = fetchMock as unknown as typeof fetch;

    await render(<LibraryScreen />);

    await waitFor(() => expect(screen.getByText('El catálogo falló.')).toBeOnTheScreen());

    await fireEvent.press(screen.getByTestId('library-exercises-retry'));
    await waitFor(() => expect(screen.getByText('Flexiones')).toBeOnTheScreen());
  });
});

describe('LibraryScreen · Skills', () => {
  const originalFetch = global.fetch;

  afterEach(() => {
    global.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  it('renders a ladder with its criterion and a supporting pattern routine', async () => {
    const fetchMock = routeFetch();
    global.fetch = fetchMock as unknown as typeof fetch;

    await render(<LibraryScreen />);
    await waitFor(() => expect(screen.getByText('Flexiones')).toBeOnTheScreen());

    await fireEvent.press(screen.getByTestId('library-section-skills'));

    await waitFor(() => expect(screen.getByText('Planche')).toBeOnTheScreen());

    // Escalera: movimiento y criterio por etapa.
    expect(screen.getByText('Empuje · Apalancado')).toBeOnTheScreen();
    expect(screen.getByText('Planche inclinada')).toBeOnTheScreen();
    expect(screen.getByText('Etapa 1 · 3 × 20 s')).toBeOnTheScreen();
    // El movimiento agrupado aparece como etapa y, resuelto, como ejercicio de la rutina.
    expect(screen.getAllByText('Planche agrupada').length).toBeGreaterThan(0);

    // Rutina de patrón: ejercicio, series, segundos y descanso. El movimiento de skill
    // se nombra por la etapa; el acondicionamiento, por el catálogo de ejercicios.
    expect(screen.getAllByText('Modelo R1').length).toBeGreaterThan(0);
    expect(screen.getByText('4 × 5–15 s · descanso 180 s')).toBeOnTheScreen();
    expect(screen.getByText('3 × 5–8 reps · descanso 120 s')).toBeOnTheScreen();
    expect(screen.getByText('Flexiones en pica')).toBeOnTheScreen();

    // El pistol squat se sirve con su criterio en repeticiones.
    expect(screen.getByText('Pistol squat')).toBeOnTheScreen();
    expect(screen.getByText('Etapa 1 · 3 × 5 reps')).toBeOnTheScreen();

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/catalog/skills'),
      expect.anything(),
    );
  });
});

describe('LibraryScreen · Progreso de skill', () => {
  const originalFetch = global.fetch;

  afterEach(() => {
    global.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  it('highlights the current stage with its criterion and defaults the rest to stage one', async () => {
    const fetchMock = routeFetch();
    global.fetch = fetchMock as unknown as typeof fetch;

    await render(<LibraryScreen />);
    await waitFor(() => expect(screen.getByText('Flexiones')).toBeOnTheScreen());

    await fireEvent.press(screen.getByTestId('library-section-skills'));
    await waitFor(() => expect(screen.getByTestId('library-skill-planche')).toBeOnTheScreen());

    // La etapa actual (planche → 2) queda resaltada con su criterio.
    expect(
      screen.getByTestId('library-skill-planche-stage-2-state', { includeHiddenElements: true }),
    ).toBeOnTheScreen();
    expect(screen.getByTestId('library-skill-planche-stage-2').props.accessibilityLabel).toContain(
      'Etapa actual',
    );
    expect(screen.getByText('Etapa 2 · 3 × 10 s')).toBeOnTheScreen();

    // La etapa no actual no se resalta.
    expect(
      screen.queryByTestId('library-skill-planche-stage-1-state', { includeHiddenElements: true }),
    ).not.toBeOnTheScreen();

    // Un skill sin fila de progreso parte de la etapa 1 y también se resalta.
    expect(
      screen.getByTestId('library-skill-pistol-squat-stage-1-state', {
        includeHiddenElements: true,
      }),
    ).toBeOnTheScreen();

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/catalog/progress'),
      expect.anything(),
    );
  });
});

describe('LibraryScreen · Rutinas', () => {
  const originalFetch = global.fetch;

  afterEach(() => {
    global.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  it('renders the programs with their routines, intensity, duration and blocks', async () => {
    const fetchMock = routeFetch();
    global.fetch = fetchMock as unknown as typeof fetch;

    await render(<LibraryScreen />);
    await waitFor(() => expect(screen.getByText('Flexiones')).toBeOnTheScreen());

    await fireEvent.press(screen.getByTestId('library-section-routines'));

    await waitFor(() => expect(screen.getByText('Ponte en forma')).toBeOnTheScreen());

    // Tipo de programa y descripción.
    expect(screen.getByText('Circuito por tiempo')).toBeOnTheScreen();
    expect(
      screen.getByText('Rutinas por tiempo para acondicionamiento general.'),
    ).toBeOnTheScreen();

    // Rutina con su intensidad, duración y bloques; las filas resuelven el nombre del catálogo.
    expect(screen.getByText('Rutina 1')).toBeOnTheScreen();
    expect(screen.getByText('Intensidad 2 · 14 min')).toBeOnTheScreen();
    expect(screen.getByText('SET 1 · 3 vueltas')).toBeOnTheScreen();
    expect(screen.getByText('1 × 15–15 s · descanso 0 s')).toBeOnTheScreen();

    // Un programa de fuerza: reps, sin duración y con el nombre del bloque.
    expect(screen.getByText('Home Workout Avanzados')).toBeOnTheScreen();
    expect(screen.getByText('Fuerza por repeticiones')).toBeOnTheScreen();
    expect(screen.getByText('Día 1 — Empuje')).toBeOnTheScreen();
    expect(screen.getByText('Intensidad 3')).toBeOnTheScreen();
    expect(screen.getByText('Empuje')).toBeOnTheScreen();
    expect(screen.getByText('4 × 6–10 reps · descanso 120 s')).toBeOnTheScreen();

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/catalog/routines'),
      expect.anything(),
    );
  });
});
