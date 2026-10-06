import { fireEvent, render, screen, within } from '@testing-library/react-native';

import type { Plan } from '@/api/plan';
import type { SessionLog } from '@/api/sessionLogs';
import {
  SessionLoggingView,
  type ExerciseSetsPayload,
  type SaveFeedback,
} from './SessionLoggingView';

/** Plan mínimo de 3 días con dos bloques de fuerza y uno de core en el día 1. */
const plan: Plan = {
  skillId: 'planche',
  trainingDays: 3,
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
          items: [
            {
              exerciseId: 'push_up',
              exerciseName: 'Flexiones',
              role: 'strength',
              pattern: 'push',
              sets: 3,
              repsMin: 8,
              repsMax: 12,
              holdSecondsMin: null,
              holdSecondsMax: null,
            },
            {
              exerciseId: 'pull_up',
              exerciseName: 'Dominadas',
              role: 'strength',
              pattern: 'pull',
              sets: 2,
              repsMin: 4,
              repsMax: 6,
              holdSecondsMin: null,
              holdSecondsMax: null,
            },
            {
              exerciseId: 'hollow-body-hold',
              exerciseName: 'Cuerpo hueco',
              role: 'core',
              pattern: null,
              sets: 3,
              repsMin: null,
              repsMax: null,
              holdSecondsMin: 20,
              holdSecondsMax: 30,
            },
          ],
        },
      ],
    },
  ],
};

const savedLog: SessionLog = {
  id: 'log-1',
  exerciseId: 'push_up',
  exerciseName: 'Flexiones',
  metric: 'reps',
  mesocycleId: null,
  sessionDay: 1,
  recordedAtUtc: '2026-10-05T18:30:00Z',
  sets: [
    { setNumber: 1, value: 10, effort: null },
    { setNumber: 2, value: 11, effort: null },
    { setNumber: 3, value: 12, effort: null },
  ],
};

async function renderView(options?: {
  logs?: SessionLog[];
  onSave?: (day: number, exercises: ExerciseSetsPayload[]) => void;
  feedback?: SaveFeedback | null;
  saving?: boolean;
}) {
  const onSave = options?.onSave ?? jest.fn();
  await render(
    <SessionLoggingView
      plan={plan}
      logs={options?.logs ?? []}
      saving={options?.saving ?? false}
      feedback={options?.feedback ?? null}
      onSave={onSave}
    />,
  );
  return { onSave };
}

describe('SessionLoggingView', () => {
  it('pinta los ejercicios de fuerza del día elegido con un campo por serie', async () => {
    await renderView();

    expect(screen.getByTestId('session-log-view')).toBeOnTheScreen();
    expect(screen.getByText('Flexiones')).toBeOnTheScreen();
    expect(screen.getByText('Dominadas')).toBeOnTheScreen();

    // El core no se registra en #19: solo fuerza.
    expect(screen.queryByText('Cuerpo hueco')).toBeNull();

    // Un campo por serie, con su etiqueta y la prescripción del plan.
    expect(screen.getByTestId('session-log-set-push_up-1')).toBeOnTheScreen();
    expect(screen.getByTestId('session-log-set-push_up-3')).toBeOnTheScreen();
    expect(
      within(screen.getByTestId('session-log-item-push_up')).getByText('Serie 1'),
    ).toBeOnTheScreen();
    expect(screen.getByText('3 × 8–12 reps')).toBeOnTheScreen();
  });

  it('guarda las reps introducidas, serie a serie, para cada ejercicio', async () => {
    const { onSave } = await renderView();

    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-1'), '10');
    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-2'), '11');
    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-3'), '12');
    await fireEvent.changeText(screen.getByTestId('session-log-set-pull_up-1'), '5');
    await fireEvent.changeText(screen.getByTestId('session-log-set-pull_up-2'), '6');
    await fireEvent.press(screen.getByTestId('session-log-save'));

    expect(onSave).toHaveBeenCalledTimes(1);
    expect(onSave).toHaveBeenCalledWith(1, [
      {
        exerciseId: 'push_up',
        sets: [
          { setNumber: 1, value: 10 },
          { setNumber: 2, value: 11 },
          { setNumber: 3, value: 12 },
        ],
      },
      {
        exerciseId: 'pull_up',
        sets: [
          { setNumber: 1, value: 5 },
          { setNumber: 2, value: 6 },
        ],
      },
    ]);
  });

  it('avisa cuando falta completar alguna serie antes de guardar', async () => {
    const { onSave } = await renderView();

    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-1'), '10');
    await fireEvent.press(screen.getByTestId('session-log-save'));

    expect(onSave).not.toHaveBeenCalled();
    expect(
      screen.getByText('Completa las reps de todas las series antes de guardar.'),
    ).toBeOnTheScreen();
  });

  it('muestra un ejercicio ya registrado como confirmado y lo excluye del guardado', async () => {
    const { onSave } = await renderView({ logs: [savedLog] });

    // El registro existente precarga los campos y los deshabilita.
    expect(screen.getByTestId('session-log-set-push_up-1')).toHaveProp('value', '10');
    expect(screen.getByTestId('session-log-set-push_up-1')).toHaveProp('editable', false);
    expect(screen.getByTestId('session-log-item-push_up-status')).toBeOnTheScreen();

    // Solo la dominada pendiente entra en el guardado.
    await fireEvent.changeText(screen.getByTestId('session-log-set-pull_up-1'), '5');
    await fireEvent.changeText(screen.getByTestId('session-log-set-pull_up-2'), '6');
    await fireEvent.press(screen.getByTestId('session-log-save'));

    expect(onSave).toHaveBeenCalledWith(1, [
      {
        exerciseId: 'pull_up',
        sets: [
          { setNumber: 1, value: 5 },
          { setNumber: 2, value: 6 },
        ],
      },
    ]);
  });

  it('muestra el registro guardado de la sesión con sus valores reales', async () => {
    await renderView({ logs: [savedLog] });

    expect(screen.getByTestId('session-log-saved-push_up')).toBeOnTheScreen();
    expect(screen.getByText('10 · 11 · 12 reps')).toBeOnTheScreen();
  });

  it('muestra el aviso del servidor tras guardar', async () => {
    await renderView({
      feedback: { role: 'confirmed', message: 'Registro de la sesión guardado.' },
    });

    expect(screen.getByTestId('session-log-feedback')).toBeOnTheScreen();
    expect(screen.getByText('Registro de la sesión guardado.')).toBeOnTheScreen();
  });

  it('avisa cuando la sesión no tiene ejercicios de fuerza', async () => {
    const coreOnly: Plan = {
      ...plan,
      microcycles: [
        {
          number: 1,
          sessions: [
            {
              day: 1,
              items: [
                {
                  exerciseId: 'hollow-body-hold',
                  exerciseName: 'Cuerpo hueco',
                  role: 'core',
                  pattern: null,
                  sets: 3,
                  repsMin: null,
                  repsMax: null,
                  holdSecondsMin: 20,
                  holdSecondsMax: 30,
                },
              ],
            },
          ],
        },
      ],
    };

    await render(
      <SessionLoggingView
        plan={coreOnly}
        logs={[]}
        saving={false}
        feedback={null}
        onSave={jest.fn()}
      />,
    );

    expect(screen.getByTestId('session-log-no-strength')).toBeOnTheScreen();
    expect(
      screen.getByText('Esta sesión no tiene ejercicios de fuerza que registrar.'),
    ).toBeOnTheScreen();
  });
});
