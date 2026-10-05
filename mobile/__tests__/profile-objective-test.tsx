import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import ProfileScreen from '../src/app/(tabs)/profile';

/** Respuesta mínima con la forma que consume el módulo de API. */
function jsonResponse(status: number, body: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 404 ? 'Not Found' : 'OK',
    json: async () => body,
  } as unknown as Response;
}

const PROFILE = {
  weightKilograms: 78.5,
  heightCentimeters: 181,
  trainingDays: 4,
  maximums: [
    { exerciseCode: 'push_up', repetitions: 10 },
    { exerciseCode: 'pull_up', repetitions: 0 },
    { exerciseCode: 'squat', repetitions: 20 },
  ],
};

describe('ProfileScreen objective', () => {
  const originalFetch = global.fetch;

  afterEach(() => {
    global.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  it('shows the four catalog skills and saves the chosen objective', async () => {
    let saved: unknown = null;
    let objective: { skillId: string } | null = null;
    const fetchMock = jest.fn((input: RequestInfo | URL, init?: RequestInit) => {
      if (String(input).endsWith('/profile/objective')) {
        if (init?.method === 'PUT') {
          saved = JSON.parse(String(init.body));
          objective = saved as { skillId: string };
          return Promise.resolve(jsonResponse(200, objective));
        }
        return Promise.resolve(objective ? jsonResponse(200, objective) : jsonResponse(404, {}));
      }
      return Promise.resolve(jsonResponse(200, PROFILE));
    });
    global.fetch = fetchMock as unknown as typeof fetch;

    await render(<ProfileScreen />);
    await waitFor(() => expect(screen.getByTestId('profile-objective')).toBeOnTheScreen());

    expect(screen.getByText('Pino')).toBeOnTheScreen();
    expect(screen.getByText('Front lever')).toBeOnTheScreen();
    expect(screen.getByText('Planche')).toBeOnTheScreen();
    expect(screen.getByText('Pistol squat')).toBeOnTheScreen();

    await fireEvent.press(screen.getByTestId('profile-objective-planche'));
    await fireEvent.press(screen.getByTestId('profile-objective-save'));

    await waitFor(() => expect(screen.getByText('Objetivo: Planche')).toBeOnTheScreen());
    expect(saved).toEqual({ skillId: 'planche' });
  });

  it('reads and shows the persisted objective', async () => {
    const fetchMock = jest.fn((input: RequestInfo | URL) => {
      if (String(input).endsWith('/profile/objective')) {
        return Promise.resolve(jsonResponse(200, { skillId: 'handstand' }));
      }
      return Promise.resolve(jsonResponse(200, PROFILE));
    });
    global.fetch = fetchMock as unknown as typeof fetch;

    await render(<ProfileScreen />);

    await waitFor(() => expect(screen.getByTestId('profile-objective-handstand')).toBeSelected());
    expect(screen.getByText('Objetivo: Pino')).toBeOnTheScreen();
  });
});
