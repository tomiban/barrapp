import { fireEvent, render, screen, within } from '@testing-library/react-native';

import type { Plan } from '@/api/plan';
import type { SessionLog } from '@/api/sessionLogs';

import {
  SessionLoggingView,
  type ExerciseSetsPayload,
  type SaveFeedback,
} from './SessionLoggingView';

/** Hold isométrico de skill (3 series) y ejercicio de fuerza en reps (2 series). */
const plan: Plan = {
  skillId: 'planche',
  trainingDays: 3,
  startDate: '2026-03-02',
  skillStage: {
    order: 1,
    name: 'Planche inclinada',
    exerciseId: 'planche-lean',
    criterion: { metric: 'seconds', target: 20, sets: 3 },
    notes: 'Inclinación con los hombros por delante de las manos.',
  },
  microcycles: [
    {
      number: 1,
      sessions: [
        {
          day: 1,
          weekday: null,
          date: null,
          items: [
            {
              exerciseId: 'handstand-wall-support',
              exerciseName: 'Pino en pared',
              role: 'skill',
              pattern: null,
              sets: 3,
              repsMin: null,
              repsMax: null,
              holdSecondsMin: 20,
              holdSecondsMax: 30,
              restSeconds: 90,
            },
            {
              exerciseId: 'push_up',
              exerciseName: 'Flexiones',
              role: 'strength',
              pattern: 'push',
              sets: 2,
              repsMin: 8,
              repsMax: 12,
              holdSecondsMin: null,
              holdSecondsMax: null,
              restSeconds: 60,
            },
          ],
        },
      ],
    },
  ],
};

/** Registro previo de la primera serie del hold. */
const savedLog: SessionLog = {
  id: 'log-1',
  exerciseId: 'handstand-wall-support',
  exerciseName: 'Pino en pared',
  metric: 'seconds',
  mesocycleId: null,
  sessionDay: 1,
  recordedAtUtc: '2026-10-05T18:30:00Z',
  sets: [{ setNumber: 1, value: 25, effort: 1 }],
};

async function renderView(options?: {
  plan?: Plan;
  logs?: SessionLog[];
  saving?: boolean;
  feedback?: SaveFeedback | null;
  onSave?: (
    session: { day: number; microcycleNumber: number; date: string | null },
    exercises: ExerciseSetsPayload[],
  ) => void;
}) {
  const onSave = options?.onSave ?? jest.fn();
  await render(
    <SessionLoggingView
      plan={options?.plan ?? plan}
      logs={options?.logs ?? []}
      saving={options?.saving ?? false}
      feedback={options?.feedback ?? null}
      onSave={onSave}
    />,
  );
  return { onSave };
}

/** Los pares `exerciseId`+`sets` del payload de guardado, para comparar sin el `item`. */
function savedExercises(onSave: jest.Mock) {
  return (onSave.mock.calls[0][1] as ExerciseSetsPayload[]).map(({ exerciseId, sets }) => ({
    exerciseId,
    sets,
  }));
}

describe('SessionLoggingView', () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.clearAllTimers();
    jest.useRealTimers();
  });

  it('pinta la sesión en curso: modo, fase, serie actual y unidad del ejercicio', async () => {
    await renderView();

    expect(screen.getByTestId('session-log-view')).toBeOnTheScreen();
    expect(screen.getByTestId('session-log-mode')).toHaveTextContent(
      'MODO EJECUCIÓN // ISOMÉTRICO',
    );
    expect(screen.getByText('FASE SKILL · ISOMETRÍA PURA')).toBeOnTheScreen();
    expect(screen.getByText('01 Pino en pared')).toBeOnTheScreen();
    expect(screen.getByTestId('session-log-current-set')).toHaveTextContent('SET 01 / 03');
    expect(screen.getByText('OBJETIVO: 20–30s SOSTÉN')).toBeOnTheScreen();
  });

  it('cronometra el hold activo con pausar y reiniciar', async () => {
    await renderView();

    expect(screen.getByTestId('session-log-chrono')).toBeOnTheScreen();
    expect(screen.getByTestId('session-log-chrono-state')).toHaveTextContent('CRONO PAUSADO');

    await fireEvent.press(screen.getByTestId('session-log-chrono-toggle'));
    expect(screen.getByTestId('session-log-chrono-state')).toHaveTextContent('CRONO ACTIVO');
    expect(screen.getByText('Pausar tiempo')).toBeOnTheScreen();
    expect(screen.getByTestId('session-log-chrono-reset')).toBeOnTheScreen();
  });

  it('navega entre ejercicios: el de reps no lleva cronómetro', async () => {
    await renderView();

    await fireEvent.press(screen.getByTestId('session-log-next'));

    expect(screen.getByText('02 Flexiones')).toBeOnTheScreen();
    expect(screen.getByTestId('session-log-mode')).toHaveTextContent('MODO EJECUCIÓN // DINÁMICO');
    expect(screen.queryByTestId('session-log-chrono')).toBeNull();
    expect(screen.getByText('OBJETIVO: 8–12 REPS')).toBeOnTheScreen();

    await fireEvent.press(screen.getByTestId('session-log-previous'));
    expect(screen.getByText('01 Pino en pared')).toBeOnTheScreen();
  });

  it('registra serie a serie: avanza la serie en curso y entra en descanso', async () => {
    await renderView();

    await fireEvent.press(screen.getByTestId('session-log-register'));

    expect(screen.getByTestId('session-log-current-set')).toHaveTextContent('SET 02 / 03');
    expect(screen.getByTestId('session-log-rest-timer')).toBeOnTheScreen();
    expect(
      within(screen.getByTestId('session-log-set-handstand-wall-support-1')).getByText('Superado'),
    ).toBeOnTheScreen();
  });

  it('deja saltar el descanso programado', async () => {
    await renderView();

    await fireEvent.press(screen.getByTestId('session-log-register'));
    expect(screen.getByTestId('session-log-rest-timer')).toBeOnTheScreen();

    await fireEvent.press(screen.getByTestId('session-log-rest-timer'));
    expect(screen.queryByTestId('session-log-rest-timer')).toBeNull();
  });

  it('guarda el resumen con las series registradas, de una en una', async () => {
    const onSave = jest.fn();
    await renderView({ onSave });

    await fireEvent.press(screen.getByTestId('session-log-register'));
    await fireEvent.press(screen.getByTestId('session-log-register'));
    await fireEvent.press(screen.getByTestId('session-log-register'));
    await fireEvent.press(screen.getByTestId('session-log-save'));

    expect(onSave).toHaveBeenCalledTimes(1);
    expect(onSave.mock.calls[0][0]).toEqual({ day: 1, microcycleNumber: 1, date: null });
    expect(savedExercises(onSave)).toEqual([
      {
        exerciseId: 'handstand-wall-support',
        sets: [
          { setNumber: 1, value: 30, effort: null, loadKg: null },
          { setNumber: 2, value: 30, effort: null, loadKg: null },
          { setNumber: 3, value: 30, effort: null, loadKg: null },
        ],
      },
    ]);
  });

  it('marca el ejercicio como terminado cuando están todas las series', async () => {
    await renderView();

    await fireEvent.press(screen.getByTestId('session-log-register'));
    await fireEvent.press(screen.getByTestId('session-log-register'));
    await fireEvent.press(screen.getByTestId('session-log-register'));

    expect(screen.getByText('Todas las series registradas')).toBeOnTheScreen();
  });

  it('avisa si se intenta cerrar sin ninguna serie registrada', async () => {
    const onSave = jest.fn();
    await renderView({ onSave });

    await fireEvent.press(screen.getByTestId('session-log-save'));

    expect(onSave).not.toHaveBeenCalled();
    expect(screen.getByText('No hay series registradas para guardar.')).toBeOnTheScreen();
  });

  it('anota el RIR real por serie y lo incluye en el guardado', async () => {
    const onSave = jest.fn();
    await renderView({ onSave });

    await fireEvent.press(screen.getByTestId('session-log-rir-2'));
    await fireEvent.press(screen.getByTestId('session-log-register'));
    await fireEvent.press(screen.getByTestId('session-log-save'));

    expect(savedExercises(onSave)).toEqual([
      {
        exerciseId: 'handstand-wall-support',
        sets: [{ setNumber: 1, value: 30, effort: 2, loadKg: null }],
      },
    ]);
  });

  it('el RIR real es opcional: se puede quitar la selección', async () => {
    const onSave = jest.fn();
    await renderView({ onSave });

    await fireEvent.press(screen.getByTestId('session-log-rir-1'));
    await fireEvent.press(screen.getByTestId('session-log-rir-1'));
    await fireEvent.press(screen.getByTestId('session-log-register'));
    await fireEvent.press(sessionScreenSave());
    expect(savedExercises(onSave)[0].sets[0].effort).toBeNull();
  });

  it('cicla el lastre y lo incluye en el guardado', async () => {
    const onSave = jest.fn();
    await renderView({ onSave });

    await fireEvent.press(screen.getByTestId('session-log-lastre'));
    await fireEvent.press(screen.getByTestId('session-log-register'));
    await fireEvent.press(screen.getByTestId('session-log-save'));

    expect(savedExercises(onSave)[0].sets[0].loadKg).toBe(2.5);
  });

  it('parte del registro guardado: precarga las series hechas y sigue por la pendiente', async () => {
    await renderView({ logs: [savedLog] });

    expect(screen.getByTestId('session-log-current-set')).toHaveTextContent('SET 02 / 03');
    expect(
      within(screen.getByTestId('session-log-set-handstand-wall-support-1')).getByText(
        '25s SOSTÉN · RIR 1 · +0kg',
      ),
    ).toBeOnTheScreen();
  });

  it('deja elegir el valor con los presets y el stepper', async () => {
    const onSave = jest.fn();
    await renderView({ onSave });

    // Preset 20 del hold (20–30 s).
    await fireEvent.press(screen.getByTestId('session-log-preset-20'));
    expect(screen.getByTestId('session-log-value-value')).toHaveTextContent('20');

    // El stepper sube el valor de la serie.
    await fireEvent.press(screen.getByTestId('session-log-value-increase'));
    expect(screen.getByTestId('session-log-value-value')).toHaveTextContent('21');

    await fireEvent.press(screen.getByTestId('session-log-register'));
    await fireEvent.press(screen.getByTestId('session-log-save'));
    expect(savedExercises(onSave)[0].sets[0].value).toBe(21);
  });

  it('muestra el aviso del servidor tras guardar', async () => {
    await renderView({
      feedback: { role: 'confirmed', message: 'Registro de la sesión guardado.' },
    });

    expect(screen.getByTestId('session-log-feedback')).toBeOnTheScreen();
    expect(screen.getByText('Registro de la sesión guardado.')).toBeOnTheScreen();
  });

  it('avisa cuando la sesión no tiene ejercicios que registrar', async () => {
    const emptyPlan: Plan = {
      ...plan,
      microcycles: [
        {
          number: 1,
          sessions: [{ day: 1, weekday: null, date: null, items: [] }],
        },
      ],
    };
    await renderView({ plan: emptyPlan });

    expect(screen.getByTestId('session-log-no-items')).toBeOnTheScreen();
    expect(screen.getByText('Esta sesión no tiene ejercicios que registrar.')).toBeOnTheScreen();
  });
});

/** Atajo para el botón de cierre, que cambia de etiqueta según el estado. */
function sessionScreenSave() {
  return screen.getByTestId('session-log-save');
}
