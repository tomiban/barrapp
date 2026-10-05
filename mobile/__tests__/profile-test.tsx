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

/** Rellena los tres máximos obligatorios. */
async function fillMaximums(pushUp = '10', pullUp = '0', squat = '20') {
  await fireEvent.changeText(screen.getByTestId('profile-maximum-push_up'), pushUp);
  await fireEvent.changeText(screen.getByTestId('profile-maximum-pull_up'), pullUp);
  await fireEvent.changeText(screen.getByTestId('profile-maximum-squat'), squat);
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

  it('renders one labelled maximum field per basic exercise', async () => {
    global.fetch = jest.fn().mockResolvedValue(jsonResponse(404, {})) as unknown as typeof fetch;

    await render(<ProfileScreen />);
    await waitFor(() => expect(screen.getByTestId('profile-save')).toBeOnTheScreen());

    expect(screen.getByText('Máximos (reps)')).toBeOnTheScreen();
    expect(screen.getByText('Empuje')).toBeOnTheScreen();
    expect(screen.getByText('Tirón')).toBeOnTheScreen();
    expect(screen.getByText('Pierna')).toBeOnTheScreen();
    expect(screen.getByTestId('profile-maximum-push_up')).toBeOnTheScreen();
    expect(screen.getByTestId('profile-maximum-pull_up')).toBeOnTheScreen();
    expect(screen.getByTestId('profile-maximum-squat')).toBeOnTheScreen();
  });

  it('warns about an empty, invalid or negative maximum without calling the API', async () => {
    const fetchMock = jest.fn().mockResolvedValue(jsonResponse(404, {}));
    global.fetch = fetchMock as unknown as typeof fetch;

    await render(<ProfileScreen />);
    await waitFor(() => expect(screen.getByTestId('profile-save')).toBeOnTheScreen());

    await fireEvent.changeText(screen.getByTestId('profile-weight'), '30');
    await fireEvent.changeText(screen.getByTestId('profile-height'), '120');
    await fillMaximums('', 'abc', '-1');
    await fireEvent.press(screen.getByTestId('profile-save'));

    await waitFor(() => expect(screen.getByText('Introduce las repeticiones.')).toBeOnTheScreen());
    expect(screen.getByText('Introduce un número entero de repeticiones.')).toBeOnTheScreen();
    expect(screen.getByText('El máximo no puede ser negativo.')).toBeOnTheScreen();
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
    await fillMaximums();
    await fireEvent.press(screen.getByTestId('profile-save'));

    await waitFor(() =>
      expect(screen.getByText('30 kg · 120 cm · 3 días/semana')).toBeOnTheScreen(),
    );
    expect(screen.getByText('Flexión 10 · Dominada 0 · Sentadilla 20')).toBeOnTheScreen();
  });

  it('saves the selected training days and shows them in the summary', async () => {
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
    await fillMaximums('12', '3', '0');
    await fireEvent.press(screen.getByTestId('profile-training-days-5'));
    await fireEvent.press(screen.getByTestId('profile-save'));

    await waitFor(() =>
      expect(screen.getByText('30 kg · 120 cm · 5 días/semana')).toBeOnTheScreen(),
    );
    expect(screen.getByText('Flexión 12 · Dominada 3 · Sentadilla 0')).toBeOnTheScreen();
    expect(saved).toEqual({
      weightKilograms: 30,
      heightCentimeters: 120,
      trainingDays: 5,
      maximums: [
        { exerciseCode: 'push_up', repetitions: 12 },
        { exerciseCode: 'pull_up', repetitions: 3 },
        { exerciseCode: 'squat', repetitions: 0 },
      ],
    });
  });

  it('shows the API problem detail when the server rejects the save', async () => {
    const detail = 'El máximo no puede ser negativo.';
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
    await fillMaximums();
    await fireEvent.press(screen.getByTestId('profile-save'));

    await waitFor(() => expect(screen.getByTestId('profile-feedback')).toBeOnTheScreen());
    expect(screen.getByText(detail)).toBeOnTheScreen();
  });
});
