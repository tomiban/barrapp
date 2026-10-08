import { fireEvent, render, screen, within } from '@testing-library/react-native';

import type { Plan } from '@/api/plan';
import type { SessionLog } from '@/api/sessionLogs';
import {
  SessionLoggingView,
  type ExerciseSetsPayload,
  type SaveFeedback,
  type SessionLogSetPayload,
} from './SessionLoggingView';

/** Plan mínimo de 3 días con un bloque de skill, dos de fuerza y uno de core en el día 1. */
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
            },
            {
              exerciseId: 'pistol-box',
              exerciseName: 'Pistol al cajón',
              role: 'skill',
              pattern: null,
              sets: 2,
              repsMin: 5,
              repsMax: 8,
              holdSecondsMin: null,
              holdSecondsMax: null,
            },
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

/** Plan solo con fuerza, para los tests de guardado de reps. */
const strengthPlan: Plan = {
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
          ],
        },
      ],
    },
  ],
};

async function renderView(options?: {
  logs?: SessionLog[];
  onSave?: (
    session: { day: number; microcycleNumber: number; date: string | null },
    exercises: ExerciseSetsPayload[],
  ) => void;
  onUpdate?: (log: SessionLog, sets: SessionLogSetPayload[]) => void;
  onDelete?: (log: SessionLog) => void;
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
      onUpdate={options?.onUpdate ?? jest.fn()}
      onDelete={options?.onDelete ?? jest.fn()}
    />,
  );
  return { onSave };
}

describe('SessionLoggingView', () => {
  it('pinta los ejercicios loggeables del día elegido con un campo por serie', async () => {
    await renderView();

    expect(screen.getByTestId('session-log-view')).toBeOnTheScreen();
    expect(screen.getByText('Pino en pared')).toBeOnTheScreen();
    expect(screen.getByText('Flexiones')).toBeOnTheScreen();
    expect(screen.getByText('Dominadas')).toBeOnTheScreen();
    expect(screen.getByText('Cuerpo hueco')).toBeOnTheScreen();

    // El skill en segundos y el core (hold) prescriben segundos; la unidad sale del ejercicio.
    expect(
      within(screen.getByTestId('session-log-item-handstand-wall-support')).getByText(
        '3 × 20–30 s',
      ),
    ).toBeOnTheScreen();
    expect(screen.getByText('2 × 5–8 reps')).toBeOnTheScreen();

    // Un campo por serie, con su etiqueta y la prescripción del plan.
    expect(screen.getByTestId('session-log-set-push_up-1')).toBeOnTheScreen();
    expect(screen.getByTestId('session-log-set-push_up-3')).toBeOnTheScreen();
    expect(
      within(screen.getByTestId('session-log-item-push_up')).getByText('Serie 1'),
    ).toBeOnTheScreen();
    expect(screen.getByText('3 × 8–12 reps')).toBeOnTheScreen();
  });

  it('guarda las reps introducidas, serie a serie, para cada ejercicio', async () => {
    const onSave = jest.fn();
    await render(
      <SessionLoggingView
        plan={strengthPlan}
        logs={[]}
        saving={false}
        feedback={null}
        onSave={onSave}
        onUpdate={jest.fn()}
        onDelete={jest.fn()}
      />,
    );

    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-1'), '10');
    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-2'), '11');
    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-3'), '12');
    await fireEvent.changeText(screen.getByTestId('session-log-set-pull_up-1'), '5');
    await fireEvent.changeText(screen.getByTestId('session-log-set-pull_up-2'), '6');
    await fireEvent.press(screen.getByTestId('session-log-save'));

    expect(onSave).toHaveBeenCalledTimes(1);
    expect(onSave.mock.calls[0][0]).toEqual({ day: 1, microcycleNumber: 1, date: null });
    expect(
      (onSave.mock.calls[0][1] as ExerciseSetsPayload[]).map(({ exerciseId, sets }) => ({
        exerciseId,
        sets,
      })),
    ).toEqual([
      {
        exerciseId: 'push_up',
        sets: [
          { setNumber: 1, value: 10, effort: null },
          { setNumber: 2, value: 11, effort: null },
          { setNumber: 3, value: 12, effort: null },
        ],
      },
      {
        exerciseId: 'pull_up',
        sets: [
          { setNumber: 1, value: 5, effort: null },
          { setNumber: 2, value: 6, effort: null },
        ],
      },
    ]);
  });

  it('avisa cuando falta completar alguna serie antes de guardar', async () => {
    const onSave = jest.fn();
    await render(
      <SessionLoggingView
        plan={strengthPlan}
        logs={[]}
        saving={false}
        feedback={null}
        onSave={onSave}
        onUpdate={jest.fn()}
        onDelete={jest.fn()}
      />,
    );

    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-1'), '10');
    await fireEvent.press(screen.getByTestId('session-log-save'));

    expect(onSave).not.toHaveBeenCalled();
    expect(
      screen.getByText('Completa los valores de todas las series antes de guardar.'),
    ).toBeOnTheScreen();
  });

  it('guarda los segundos aguantados por serie en los holds de skill y core', async () => {
    const holdsPlan: Plan = {
      ...plan,
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
                  sets: 2,
                  repsMin: null,
                  repsMax: null,
                  holdSecondsMin: 20,
                  holdSecondsMax: 30,
                },
                {
                  exerciseId: 'hollow-body-hold',
                  exerciseName: 'Cuerpo hueco',
                  role: 'core',
                  pattern: null,
                  sets: 2,
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

    const onSave = jest.fn();
    await render(
      <SessionLoggingView
        plan={holdsPlan}
        logs={[]}
        saving={false}
        feedback={null}
        onSave={onSave}
        onUpdate={jest.fn()}
        onDelete={jest.fn()}
      />,
    );

    await fireEvent.changeText(
      screen.getByTestId('session-log-set-handstand-wall-support-1'),
      '25',
    );
    await fireEvent.changeText(
      screen.getByTestId('session-log-set-handstand-wall-support-2'),
      '30',
    );
    await fireEvent.changeText(screen.getByTestId('session-log-set-hollow-body-hold-1'), '20');
    await fireEvent.changeText(screen.getByTestId('session-log-set-hollow-body-hold-2'), '22');
    await fireEvent.press(screen.getByTestId('session-log-save'));

    expect(onSave.mock.calls[0][0]).toEqual({ day: 1, microcycleNumber: 1, date: null });
    expect(
      (onSave.mock.calls[0][1] as ExerciseSetsPayload[]).map(({ exerciseId, sets }) => ({
        exerciseId,
        sets,
      })),
    ).toEqual([
      {
        exerciseId: 'handstand-wall-support',
        sets: [
          { setNumber: 1, value: 25, effort: null },
          { setNumber: 2, value: 30, effort: null },
        ],
      },
      {
        exerciseId: 'hollow-body-hold',
        sets: [
          { setNumber: 1, value: 20, effort: null },
          { setNumber: 2, value: 22, effort: null },
        ],
      },
    ]);
  });

  it('registra en reps un skill de reps, con la unidad en su prescripción', async () => {
    const repsSkillPlan: Plan = {
      ...plan,
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
                  exerciseId: 'pistol-box',
                  exerciseName: 'Pistol al cajón',
                  role: 'skill',
                  pattern: null,
                  sets: 2,
                  repsMin: 5,
                  repsMax: 8,
                  holdSecondsMin: null,
                  holdSecondsMax: null,
                },
              ],
            },
          ],
        },
      ],
    };

    const onSave = jest.fn();
    await render(
      <SessionLoggingView
        plan={repsSkillPlan}
        logs={[]}
        saving={false}
        feedback={null}
        onSave={onSave}
        onUpdate={jest.fn()}
        onDelete={jest.fn()}
      />,
    );

    expect(screen.getByText('2 × 5–8 reps')).toBeOnTheScreen();
    await fireEvent.changeText(screen.getByTestId('session-log-set-pistol-box-1'), '6');
    await fireEvent.changeText(screen.getByTestId('session-log-set-pistol-box-2'), '5');
    await fireEvent.press(screen.getByTestId('session-log-save'));

    expect(onSave.mock.calls[0][0]).toEqual({ day: 1, microcycleNumber: 1, date: null });
    expect(
      (onSave.mock.calls[0][1] as ExerciseSetsPayload[]).map(({ exerciseId, sets }) => ({
        exerciseId,
        sets,
      })),
    ).toEqual([
      {
        exerciseId: 'pistol-box',
        sets: [
          { setNumber: 1, value: 6, effort: null },
          { setNumber: 2, value: 5, effort: null },
        ],
      },
    ]);
  });

  it('muestra un ejercicio ya registrado como confirmado y lo excluye del guardado', async () => {
    const onSave = jest.fn();
    await render(
      <SessionLoggingView
        plan={strengthPlan}
        logs={[savedLog]}
        saving={false}
        feedback={null}
        onSave={onSave}
        onUpdate={jest.fn()}
        onDelete={jest.fn()}
      />,
    );

    // El registro existente precarga los campos y los deshabilita.
    expect(screen.getByTestId('session-log-set-push_up-1')).toHaveProp('value', '10');
    expect(screen.getByTestId('session-log-set-push_up-1')).toHaveProp('editable', false);
    expect(screen.getByTestId('session-log-item-push_up-status')).toBeOnTheScreen();

    // Solo la dominada pendiente entra en el guardado.
    await fireEvent.changeText(screen.getByTestId('session-log-set-pull_up-1'), '5');
    await fireEvent.changeText(screen.getByTestId('session-log-set-pull_up-2'), '6');
    await fireEvent.press(screen.getByTestId('session-log-save'));

    expect(onSave.mock.calls[0][0]).toEqual({ day: 1, microcycleNumber: 1, date: null });
    expect(
      (onSave.mock.calls[0][1] as ExerciseSetsPayload[]).map(({ exerciseId, sets }) => ({
        exerciseId,
        sets,
      })),
    ).toEqual([
      {
        exerciseId: 'pull_up',
        sets: [
          { setNumber: 1, value: 5, effort: null },
          { setNumber: 2, value: 6, effort: null },
        ],
      },
    ]);
  });

  it('muestra el registro guardado de la sesión con sus valores reales', async () => {
    await renderView({ logs: [savedLog] });

    expect(screen.getByTestId('session-log-saved-push_up')).toBeOnTheScreen();
    expect(screen.getByText('10 · 11 · 12 reps')).toBeOnTheScreen();
  });

  it('muestra un hold guardado en segundos con la unidad del ejercicio', async () => {
    const secondsLog: SessionLog = {
      id: 'log-2',
      exerciseId: 'hollow-body-hold',
      exerciseName: 'Cuerpo hueco',
      metric: 'seconds',
      mesocycleId: null,
      sessionDay: 1,
      recordedAtUtc: '2026-10-05T18:35:00Z',
      sets: [
        { setNumber: 1, value: 25, effort: null },
        { setNumber: 2, value: 30, effort: null },
        { setNumber: 3, value: 28, effort: null },
      ],
    };

    await renderView({ logs: [secondsLog] });

    expect(screen.getByTestId('session-log-saved-hollow-body-hold')).toBeOnTheScreen();
    expect(screen.getByText('25 · 30 · 28 s')).toBeOnTheScreen();
  });

  it('ofrece un campo opcional de RIR/RPE por serie, vacío por defecto', async () => {
    await renderView();

    expect(screen.getByTestId('session-log-effort-push_up-1')).toBeOnTheScreen();
    expect(screen.getByTestId('session-log-effort-push_up-1')).toHaveProp('value', '');
    expect(screen.getByTestId('session-log-effort-push_up-3')).toBeOnTheScreen();
    expect(screen.getByTestId('session-log-effort-hollow-body-hold-1')).toBeOnTheScreen();
    expect(screen.getAllByText('RIR/RPE').length).toBeGreaterThan(0);
  });

  it('guarda el RIR/RPE anotado por serie junto al valor real', async () => {
    const onSave = jest.fn();
    await render(
      <SessionLoggingView
        plan={strengthPlan}
        logs={[]}
        saving={false}
        feedback={null}
        onSave={onSave}
        onUpdate={jest.fn()}
        onDelete={jest.fn()}
      />,
    );

    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-1'), '10');
    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-2'), '11');
    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-3'), '12');
    await fireEvent.changeText(screen.getByTestId('session-log-effort-push_up-1'), '2');
    await fireEvent.changeText(screen.getByTestId('session-log-effort-push_up-3'), '3');
    await fireEvent.changeText(screen.getByTestId('session-log-set-pull_up-1'), '5');
    await fireEvent.changeText(screen.getByTestId('session-log-set-pull_up-2'), '6');
    await fireEvent.press(screen.getByTestId('session-log-save'));

    expect(onSave.mock.calls[0][0]).toEqual({ day: 1, microcycleNumber: 1, date: null });
    expect(
      (onSave.mock.calls[0][1] as ExerciseSetsPayload[]).map(({ exerciseId, sets }) => ({
        exerciseId,
        sets,
      })),
    ).toEqual([
      {
        exerciseId: 'push_up',
        sets: [
          { setNumber: 1, value: 10, effort: 2 },
          { setNumber: 2, value: 11, effort: null },
          { setNumber: 3, value: 12, effort: 3 },
        ],
      },
      {
        exerciseId: 'pull_up',
        sets: [
          { setNumber: 1, value: 5, effort: null },
          { setNumber: 2, value: 6, effort: null },
        ],
      },
    ]);
  });

  it('avisa cuando el RIR/RPE escapa del rango 0 a 10', async () => {
    const onSave = jest.fn();
    await render(
      <SessionLoggingView
        plan={strengthPlan}
        logs={[]}
        saving={false}
        feedback={null}
        onSave={onSave}
        onUpdate={jest.fn()}
        onDelete={jest.fn()}
      />,
    );

    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-1'), '10');
    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-2'), '11');
    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-3'), '12');
    await fireEvent.changeText(screen.getByTestId('session-log-effort-push_up-1'), '11');
    await fireEvent.press(screen.getByTestId('session-log-save'));

    expect(onSave).not.toHaveBeenCalled();
    expect(screen.getByText('El RIR/RPE debe ser un número entre 0 y 10.')).toBeOnTheScreen();
  });

  it('no ofrece el campo de RIR/RPE en las series ya registradas', async () => {
    await render(
      <SessionLoggingView
        plan={strengthPlan}
        logs={[savedLog]}
        saving={false}
        feedback={null}
        onSave={jest.fn()}
        onUpdate={jest.fn()}
        onDelete={jest.fn()}
      />,
    );

    expect(screen.queryByTestId('session-log-effort-push_up-1')).toBeNull();
    expect(screen.getByTestId('session-log-effort-pull_up-1')).toBeOnTheScreen();
  });

  it('muestra el RIR/RPE real junto a los valores en el registro guardado', async () => {
    const logWithEffort: SessionLog = {
      ...savedLog,
      sets: [
        { setNumber: 1, value: 10, effort: 2 },
        { setNumber: 2, value: 11, effort: 2 },
        { setNumber: 3, value: 12, effort: null },
      ],
    };
    await renderView({ logs: [logWithEffort] });

    expect(screen.getByTestId('session-log-saved-push_up')).toBeOnTheScreen();
    expect(screen.getByText('10 · 11 · 12 reps · RIR 2')).toBeOnTheScreen();
  });

  it('muestra el aviso del servidor tras guardar', async () => {
    await renderView({
      feedback: { role: 'confirmed', message: 'Registro de la sesión guardado.' },
    });

    expect(screen.getByTestId('session-log-feedback')).toBeOnTheScreen();
    expect(screen.getByText('Registro de la sesión guardado.')).toBeOnTheScreen();
  });

  it('avisa cuando la sesión no tiene ejercicios que registrar', async () => {
    const emptySessionPlan: Plan = {
      ...plan,
      microcycles: [
        {
          number: 1,
          sessions: [
            {
              day: 1,
              weekday: null,
              date: null,
              items: [],
            },
          ],
        },
      ],
    };

    await render(
      <SessionLoggingView
        plan={emptySessionPlan}
        logs={[]}
        saving={false}
        feedback={null}
        onSave={jest.fn()}
        onUpdate={jest.fn()}
        onDelete={jest.fn()}
      />,
    );

    expect(screen.getByTestId('session-log-no-items')).toBeOnTheScreen();
    expect(screen.getByText('Esta sesión no tiene ejercicios que registrar.')).toBeOnTheScreen();
  });

  it('ofrece editar y borrar un registro confirmado', async () => {
    await renderView({ logs: [savedLog] });

    expect(screen.getByTestId('session-log-edit-push_up')).toBeOnTheScreen();
    expect(screen.getByTestId('session-log-delete-push_up')).toBeOnTheScreen();
  });

  it('no ofrece editar ni borrar un registro que sigue pendiente en la cola local', async () => {
    await renderView({ logs: [{ ...savedLog, id: 'client-1', pending: true }] });

    expect(screen.queryByTestId('session-log-edit-push_up')).toBeNull();
    expect(screen.queryByTestId('session-log-delete-push_up')).toBeNull();
  });

  it('edita un registro: re-habilita los campos con sus valores y guarda los cambios', async () => {
    const onSave = jest.fn();
    const onUpdate = jest.fn();
    await render(
      <SessionLoggingView
        plan={strengthPlan}
        logs={[savedLog]}
        saving={false}
        feedback={null}
        onSave={onSave}
        onUpdate={onUpdate}
        onDelete={jest.fn()}
      />,
    );

    expect(screen.getByTestId('session-log-set-push_up-1')).toHaveProp('editable', false);
    expect(screen.queryByLabelText('Editando')).toBeNull();

    await fireEvent.press(screen.getByTestId('session-log-edit-push_up'));

    expect(screen.getByTestId('session-log-set-push_up-1')).toHaveProp('editable', true);
    expect(screen.getByLabelText('Editando')).toBeOnTheScreen();
    expect(screen.getByTestId('session-log-effort-push_up-1')).toBeOnTheScreen();

    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-1'), '15');
    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-3'), '16');
    await fireEvent.changeText(screen.getByTestId('session-log-effort-push_up-2'), '2');
    await fireEvent.press(screen.getByTestId('session-log-update-push_up'));

    expect(onUpdate).toHaveBeenCalledTimes(1);
    expect(onUpdate.mock.calls[0][0]).toEqual(savedLog);
    expect(onUpdate.mock.calls[0][1]).toEqual([
      { setNumber: 1, value: 15, effort: null },
      { setNumber: 2, value: 11, effort: 2 },
      { setNumber: 3, value: 16, effort: null },
    ]);
  });

  it('cancela la edición sin guardar cambios', async () => {
    const onUpdate = jest.fn();
    await render(
      <SessionLoggingView
        plan={strengthPlan}
        logs={[savedLog]}
        saving={false}
        feedback={null}
        onSave={jest.fn()}
        onUpdate={onUpdate}
        onDelete={jest.fn()}
      />,
    );

    await fireEvent.press(screen.getByTestId('session-log-edit-push_up'));
    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-1'), '99');
    await fireEvent.press(screen.getByTestId('session-log-cancel-push_up'));

    expect(onUpdate).not.toHaveBeenCalled();
    expect(screen.getByTestId('session-log-set-push_up-1')).toHaveProp('editable', false);
    expect(screen.getByTestId('session-log-set-push_up-1')).toHaveProp('value', '10');
  });

  it('avisa si la edición deja alguna serie vacía', async () => {
    const onUpdate = jest.fn();
    await render(
      <SessionLoggingView
        plan={strengthPlan}
        logs={[savedLog]}
        saving={false}
        feedback={null}
        onSave={jest.fn()}
        onUpdate={onUpdate}
        onDelete={jest.fn()}
      />,
    );

    await fireEvent.press(screen.getByTestId('session-log-edit-push_up'));
    await fireEvent.changeText(screen.getByTestId('session-log-set-push_up-1'), '');
    await fireEvent.press(screen.getByTestId('session-log-update-push_up'));

    expect(onUpdate).not.toHaveBeenCalled();
    expect(
      screen.getByText('Completa los valores de todas las series antes de guardar.'),
    ).toBeOnTheScreen();
  });

  it('borra un registro guardado con su acción', async () => {
    const onSave = jest.fn();
    const onDelete = jest.fn();
    await render(
      <SessionLoggingView
        plan={strengthPlan}
        logs={[savedLog]}
        saving={false}
        feedback={null}
        onSave={onSave}
        onUpdate={jest.fn()}
        onDelete={onDelete}
      />,
    );

    await fireEvent.press(screen.getByTestId('session-log-delete-push_up'));

    expect(onDelete).toHaveBeenCalledTimes(1);
    expect(onDelete).toHaveBeenCalledWith(savedLog);
  });
});
