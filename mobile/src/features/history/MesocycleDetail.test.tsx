import { render, screen, userEvent } from '@testing-library/react-native';

import { MesocycleDetail } from './MesocycleDetail';

/** Plan de ejemplo que sirve el API para un mesociclo pasado. */
const PLAN = {
  skillId: 'planche',
  trainingDays: 3,
  skillStage: {
    order: 1,
    name: 'Planche inclinada',
    exerciseId: 'planche-lean',
    criterion: { metric: 'seconds', target: 20, sets: 3 },
    notes: '',
  },
  microcycles: [
    {
      number: 1,
      sessions: [
        {
          day: 1,
          items: [
            {
              exerciseId: 'planche-lean',
              exerciseName: 'Planche inclinada',
              role: 'skill',
              pattern: null,
              sets: 3,
              repsMin: null,
              repsMax: null,
              holdSecondsMin: 20,
              holdSecondsMax: 20,
            },
          ],
        },
      ],
    },
  ],
};

/**
 * MesocycleDetail (#27): abre un mesociclo del historial vía `GET /plan/history/{id}` y pinta su
 * plan con la misma vista que el plan actual (PlanView).
 */
describe('MesocycleDetail', () => {
  beforeEach(() => {
    jest.restoreAllMocks();
  });

  it('abre el mesociclo y muestra su plan con la etapa del skill', async () => {
    const fetchMock = jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue({ ok: true, json: async () => PLAN } as Response);

    await render(<MesocycleDetail mesocycleId="meso-1" />);

    expect(await screen.findByText('Etapa 1 · Planche inclinada')).toBeOnTheScreen();
    expect(screen.getByText('Supera 20 s × 3 series')).toBeOnTheScreen();
    expect(screen.getByText('Planche inclinada')).toBeOnTheScreen();

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/plan/history/meso-1'),
      expect.objectContaining({ signal: expect.anything() }),
    );
  });

  it('muestra un aviso cuando el API no responde y permite reintentar', async () => {
    const fetchMock = jest.spyOn(globalThis, 'fetch');
    fetchMock
      .mockRejectedValueOnce(new Error('Sin conexión'))
      .mockResolvedValue({ ok: true, json: async () => PLAN } as Response);

    await render(<MesocycleDetail mesocycleId="meso-1" />);

    expect(await screen.findByTestId('mesocycle-detail-error')).toBeTruthy();
    expect(screen.getByText('Sin conexión')).toBeTruthy();

    await userEvent.setup().press(screen.getByTestId('mesocycle-detail-retry'));

    expect(await screen.findByText('Etapa 1 · Planche inclinada')).toBeOnTheScreen();
    expect(screen.queryByTestId('mesocycle-detail-error')).toBeNull();
  });
});
