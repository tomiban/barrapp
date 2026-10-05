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

  it('switches to the Skills and Rutinas placeholders', async () => {
    global.fetch = jest
      .fn()
      .mockResolvedValue(jsonResponse(200, CATALOG)) as unknown as typeof fetch;

    await render(<LibraryScreen />);
    await waitFor(() => expect(screen.getByText('Flexiones')).toBeOnTheScreen());

    await fireEvent.press(screen.getByTestId('library-section-skills'));
    expect(screen.getByTestId('library-skills-empty')).toBeOnTheScreen();
    expect(screen.getByText('Próximamente')).toBeOnTheScreen();

    await fireEvent.press(screen.getByTestId('library-section-routines'));
    expect(screen.getByTestId('library-routines-empty')).toBeOnTheScreen();
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
