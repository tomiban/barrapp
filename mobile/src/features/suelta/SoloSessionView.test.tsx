import { render, screen, userEvent } from '@testing-library/react-native';

import type { SoloSession } from '@/api/soloSession';
import { SoloSessionView } from './SoloSessionView';

/** Sesión suelta de ejemplo: empuje, 30 min, energía media, con core. */
const SESSION: SoloSession = {
  focus: 'patron',
  pattern: 'push',
  skillId: null,
  skillName: null,
  timeMinutes: 30,
  energy: 'media',
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
    {
      exerciseId: 'hollow-body-hold',
      exerciseName: 'Hollow body',
      role: 'core',
      pattern: null,
      sets: 3,
      repsMin: null,
      repsMax: null,
      holdSecondsMin: 20,
      holdSecondsMax: 30,
      note: null,
    },
  ],
};

/**
 * SoloSessionView: formulario del generador de sesión suelta. Elige tiempo, energía y foco, pide
 * la sesión al API y pinta lo que devuelve el motor. «Sorpréndeme» no deja elegir patrón; la
 * respuesta revela qué ha tocado.
 */
describe('SoloSessionView', () => {
  const fetchMock = jest.fn();

  beforeEach(() => {
    fetchMock.mockReset();
    global.fetch = fetchMock as unknown as typeof fetch;
  });

  it('muestra los selectores de tiempo, energía y foco con el patrón por defecto', async () => {
    await render(<SoloSessionView />);

    expect(screen.getByTestId('suelta-time')).toHaveProp('accessibilityRole', 'radiogroup');
    expect(screen.getByTestId('suelta-energy')).toHaveProp('accessibilityRole', 'radiogroup');
    expect(screen.getByTestId('suelta-focus')).toHaveProp('accessibilityRole', 'radiogroup');
    expect(screen.getByTestId('suelta-pattern')).toHaveProp('accessibilityRole', 'radiogroup');
    expect(screen.getByTestId('suelta-generate')).toBeTruthy();
  });

  it('oculta el patrón cuando el foco no es de patrón', async () => {
    await render(<SoloSessionView />);

    await userEvent.setup().press(screen.getByRole('radio', { name: 'Sorpréndeme' }));

    expect(screen.queryByTestId('suelta-pattern')).toBeNull();
  });

  it('genera la sesión y pinta sus filas', async () => {
    fetchMock.mockResolvedValue({ ok: true, json: async () => SESSION });

    await render(<SoloSessionView />);
    await userEvent.setup().press(screen.getByRole('radio', { name: '15 min' }));
    await userEvent.setup().press(screen.getByTestId('suelta-generate'));

    expect(await screen.findByTestId('suelta-result')).toBeTruthy();
    expect(screen.getByTestId('suelta-summary')).toHaveTextContent('Empuje');
    expect(screen.getByTestId('suelta-item-push_up')).toBeTruthy();
    expect(screen.getByTestId('suelta-item-hollow-body-hold')).toBeTruthy();

    // El patrón solo viaja con el foco de patrón, y el tiempo elegido es el que se manda.
    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/sessions/suelta'),
      expect.objectContaining({
        method: 'POST',
        body: expect.stringContaining('"timeMinutes":15'),
      }),
    );
    expect(JSON.parse(fetchMock.mock.calls[0][1].body)).toEqual({
      timeMinutes: 15,
      energy: 'media',
      focus: 'patron',
      pattern: 'push',
    });
  });

  it('muestra un aviso cuando el API no responde', async () => {
    fetchMock.mockRejectedValue(new Error('Sin conexión'));

    await render(<SoloSessionView />);
    await userEvent.setup().press(screen.getByTestId('suelta-generate'));

    expect(await screen.findByTestId('suelta-error')).toBeTruthy();
    expect(screen.getByText('Sin conexión')).toBeTruthy();
  });
});
