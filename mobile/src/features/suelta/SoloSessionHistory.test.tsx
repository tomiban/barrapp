import { render, screen, userEvent } from '@testing-library/react-native';

import type { SueltaHistoryEntry } from '@/api/soloSessionHistory';
import { SoloSessionHistory } from './SoloSessionHistory';

/** Entrada de ejemplo del historial: empuje de 30 min, energía media, aún generada. */
const ENTRY: SueltaHistoryEntry = {
  id: 'suelta-1',
  timeMinutes: 30,
  energy: 'media',
  focus: 'patron',
  pattern: 'push',
  skillId: null,
  skillName: null,
  status: 'generada',
  createdAtUtc: '2026-10-05T18:30:00Z',
  recordedAtUtc: null,
  items: [
    {
      exerciseId: 'push_up',
      exerciseName: 'Flexión',
      role: 'strength',
      pattern: 'push',
      sets: 3,
      repsMin: 5,
      repsMax: 8,
      holdSecondsMin: null,
      holdSecondsMax: null,
      note: null,
    },
  ],
};

/**
 * SoloSessionHistory: sección del historial de sesiones sueltas. Las sueltas quedan etiquetadas
 * como tales y no afectan al plan ni a los máximos; la sección solo las muestra, vía `GET
 * /sessions/suelta`.
 */
describe('SoloSessionHistory', () => {
  const fetchMock = jest.fn();

  beforeEach(() => {
    fetchMock.mockReset();
    global.fetch = fetchMock as unknown as typeof fetch;
  });

  it('muestra las sesiones sueltas del historial etiquetadas como tal', async () => {
    fetchMock.mockResolvedValue({ ok: true, json: async () => [ENTRY] });

    await render(<SoloSessionHistory />);

    expect(await screen.findByTestId('suelta-history-item-suelta-1')).toBeTruthy();
    expect(screen.getByText(/Empuje · 30 min · energía Media/)).toBeTruthy();
    expect(screen.getByText(/· suelta/)).toBeTruthy();
    expect(screen.getByText(/sesiones sueltas/i)).toBeTruthy();
    expect(screen.getByTestId('suelta-history-status-suelta-1')).toHaveTextContent('Generada');

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/sessions/suelta'),
      expect.objectContaining({ signal: expect.anything() }),
    );
  });

  it('muestra un estado vacío cuando todavía no hay sesiones sueltas', async () => {
    fetchMock.mockResolvedValue({ ok: true, json: async () => [] });

    await render(<SoloSessionHistory />);

    expect(await screen.findByTestId('suelta-history-empty')).toBeTruthy();
    expect(screen.getByText(/todavía no hay sesiones sueltas/i)).toBeTruthy();
  });

  it('muestra un aviso cuando el API no responde y permite reintentar', async () => {
    fetchMock.mockRejectedValueOnce(new Error('Sin conexión'));
    fetchMock.mockResolvedValue({ ok: true, json: async () => [ENTRY] });

    await render(<SoloSessionHistory />);

    expect(await screen.findByTestId('suelta-history-error')).toBeTruthy();
    expect(screen.getByText('Sin conexión')).toBeTruthy();

    await userEvent.setup().press(screen.getByTestId('suelta-history-retry'));

    expect(await screen.findByTestId('suelta-history-item-suelta-1')).toBeTruthy();
    expect(screen.queryByTestId('suelta-history-error')).toBeNull();
  });
});
