import { render, screen, waitFor } from '@testing-library/react-native';

import HistorialScreen from '../src/app/(tabs)/historial';

/**
 * Pantalla Historial (#82, base del #87): con el shell de navegación la ruta
 * `/historial` muestra lo que ya existe —mesociclos pasados y sesiones
 * sueltas— bajo la cabecera `BARRAS / HISTORIAL`.
 */
describe('HistorialScreen', () => {
  const originalFetch = global.fetch;

  afterEach(() => {
    global.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  function jsonResponse(status: number, body: unknown): Response {
    return {
      ok: status >= 200 && status < 300,
      status,
      statusText: status === 404 ? 'Not Found' : 'OK',
      json: async () => body,
    } as unknown as Response;
  }

  /** Mesociclo cerrado mínimo, con la forma de `GET /plan/history`. */
  const MESOCYCLE = {
    id: 'meso-1',
    skillId: 'planche',
    skillName: 'Planche',
    trainingDays: 3,
    startedAtUtc: '2026-10-01T08:00:00Z',
    closedAtUtc: '2026-11-01T08:00:00Z',
  };

  it('lists mesociclos and solo sessions under the Historial header', async () => {
    const fetchMock = jest.fn((input: unknown) => {
      const url = String(input);
      if (url.includes('/plan/history')) {
        return Promise.resolve(jsonResponse(200, [MESOCYCLE]));
      }
      if (url.includes('/sessions/suelta')) {
        return Promise.resolve(jsonResponse(200, []));
      }
      return Promise.resolve(jsonResponse(404, {}));
    });
    global.fetch = fetchMock as unknown as typeof fetch;

    await render(<HistorialScreen />);

    expect(screen.getByRole('header', { name: 'BARRAS / Historial' })).toBeOnTheScreen();
    await waitFor(() => expect(screen.getByText(/Planche · 3 días/)).toBeOnTheScreen());

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/plan/history'),
      expect.anything(),
    );
    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/sessions/suelta'),
      expect.anything(),
    );
  });
});
