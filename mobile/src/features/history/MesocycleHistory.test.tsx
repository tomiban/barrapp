import { render, screen, userEvent } from '@testing-library/react-native';
import { router } from 'expo-router';

import type { MesocycleSummary } from '@/api/mesocycleHistory';
import { MesocycleHistory } from './MesocycleHistory';

jest.mock('expo-router', () => ({ router: { push: jest.fn() } }));

/** Entrada de ejemplo del historial: un mes de planche de 3 días, ya cerrado. */
const ENTRY: MesocycleSummary = {
  id: 'meso-1',
  skillId: 'planche',
  skillName: 'Planche',
  trainingDays: 3,
  startedAtUtc: '2026-10-01T08:00:00Z',
  closedAtUtc: '2026-11-01T08:00:00Z',
};

/**
 * MesocycleHistory (#27): sección del historial de mesociclos pasados. Lista los mesociclos
 * cerrados vía `GET /plan/history` y cada fila abre su detalle en su propia pantalla.
 */
describe('MesocycleHistory', () => {
  const fetchMock = jest.fn();

  beforeEach(() => {
    fetchMock.mockReset();
    global.fetch = fetchMock as unknown as typeof fetch;
  });

  it('listas los mesociclos pasados y abre su detalle al pulsarlos', async () => {
    fetchMock.mockResolvedValue({ ok: true, json: async () => [ENTRY] });

    await render(<MesocycleHistory />);

    expect(await screen.findByTestId('mesocycle-history-item-meso-1')).toBeTruthy();
    expect(screen.getByText(/Planche · 3 días/)).toBeTruthy();
    expect(screen.getByText(/Cerrado el 1 nov/)).toBeTruthy();
    expect(screen.getByText(/mesociclos anteriores/i)).toBeTruthy();

    await userEvent.setup().press(screen.getByTestId('mesocycle-history-item-meso-1'));

    expect(router.push).toHaveBeenCalledWith('/plan-historial/meso-1');

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/plan/history'),
      expect.objectContaining({ signal: expect.anything() }),
    );
  });

  it('muestra un estado vacío cuando todavía no hay mesociclos pasados', async () => {
    fetchMock.mockResolvedValue({ ok: true, json: async () => [] });

    await render(<MesocycleHistory />);

    expect(await screen.findByTestId('mesocycle-history-empty')).toBeTruthy();
    expect(screen.getByText(/todavía no hay mesociclos pasados/i)).toBeTruthy();
  });

  it('muestra un aviso cuando el API no responde y permite reintentar', async () => {
    fetchMock.mockRejectedValueOnce(new Error('Sin conexión'));
    fetchMock.mockResolvedValue({ ok: true, json: async () => [ENTRY] });

    await render(<MesocycleHistory />);

    expect(await screen.findByTestId('mesocycle-history-error')).toBeTruthy();
    expect(screen.getByText('Sin conexión')).toBeTruthy();

    await userEvent.setup().press(screen.getByTestId('mesocycle-history-retry'));

    expect(await screen.findByTestId('mesocycle-history-item-meso-1')).toBeTruthy();
    expect(screen.queryByTestId('mesocycle-history-error')).toBeNull();
  });
});
