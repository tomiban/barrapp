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

describe('ProfileScreen', () => {
  const originalFetch = global.fetch;

  afterEach(() => {
    global.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  it('warns about out-of-range values without calling the API', async () => {
    const fetchMock = jest.fn().mockResolvedValue(jsonResponse(404, {}));
    global.fetch = fetchMock as unknown as typeof fetch;

    await render(<ProfileScreen />);
    await waitFor(() => expect(screen.getByTestId('profile-save')).toBeOnTheScreen());

    await fireEvent.changeText(screen.getByTestId('profile-weight'), '10');
    await fireEvent.changeText(screen.getByTestId('profile-height'), '100');
    await fireEvent.press(screen.getByTestId('profile-save'));

    await waitFor(() =>
      expect(screen.getByText('El peso debe estar entre 30 y 200 kg.')).toBeOnTheScreen(),
    );
    expect(screen.getByText('La altura debe estar entre 120 y 220 cm.')).toBeOnTheScreen();
    // Solo se llamó al GET inicial: la validación cortó antes del PUT.
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('saves values within range and shows the persisted profile with units', async () => {
    let saved: unknown = null;
    const fetchMock = jest.fn((_input: RequestInfo | URL, init?: RequestInit) => {
      if (init?.method === 'PUT') {
        saved = JSON.parse(String(init.body));
        return Promise.resolve(jsonResponse(200, saved));
      }
      return Promise.resolve(saved ? jsonResponse(200, saved) : jsonResponse(404, {}));
    });
    global.fetch = fetchMock as unknown as typeof fetch;

    await render(<ProfileScreen />);
    await waitFor(() => expect(screen.getByTestId('profile-save')).toBeOnTheScreen());

    await fireEvent.changeText(screen.getByTestId('profile-weight'), '30');
    await fireEvent.changeText(screen.getByTestId('profile-height'), '120');
    await fireEvent.press(screen.getByTestId('profile-save'));

    await waitFor(() => expect(screen.getByText('30 kg · 120 cm')).toBeOnTheScreen());
  });

  it('shows the API problem detail when the server rejects the save', async () => {
    const detail = 'El peso debe estar entre 30 y 200 kg.';
    const fetchMock = jest.fn((_input: RequestInfo | URL, init?: RequestInit) => {
      if (init?.method === 'PUT') {
        return Promise.resolve(jsonResponse(400, { detail }));
      }
      return Promise.resolve(jsonResponse(404, {}));
    });
    global.fetch = fetchMock as unknown as typeof fetch;

    await render(<ProfileScreen />);
    await waitFor(() => expect(screen.getByTestId('profile-save')).toBeOnTheScreen());

    await fireEvent.changeText(screen.getByTestId('profile-weight'), '30');
    await fireEvent.changeText(screen.getByTestId('profile-height'), '120');
    await fireEvent.press(screen.getByTestId('profile-save'));

    await waitFor(() => expect(screen.getByTestId('profile-feedback')).toBeOnTheScreen());
    expect(screen.getByText(detail)).toBeOnTheScreen();
  });
});
