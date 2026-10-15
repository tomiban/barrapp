import { fireEvent, render, screen } from '@testing-library/react-native';

import type { Plan, PlanSession, PlanSessionItem } from '@/api/plan';
import type { SessionLog } from '@/api/sessionLogs';
import { PlanView } from './PlanView';

/** Ítem de sesión con los valores por defecto del plan; solo se pisa lo relevante del test. */
function item(
  partial: Pick<PlanSessionItem, 'exerciseId' | 'exerciseName' | 'role' | 'sets'> &
    Partial<PlanSessionItem>,
): PlanSessionItem {
  return {
    pattern: null,
    repsMin: null,
    repsMax: null,
    holdSecondsMin: null,
    holdSecondsMax: null,
    ...partial,
  };
}

/** Sesión con su día, día de la semana y fecha. */
function session(
  day: number,
  weekday: PlanSession['weekday'],
  date: string,
  items: PlanSessionItem[],
): PlanSession {
  return { day, weekday, date, items };
}

const SKILL = item({
  exerciseId: 'front-lever-tuck',
  exerciseName: 'Front lever tuck',
  role: 'skill',
  sets: 3,
  holdSecondsMin: 10,
  holdSecondsMax: 10,
});
const PULL = item({
  exerciseId: 'pull-up',
  exerciseName: 'Dominadas',
  role: 'strength',
  pattern: 'pull',
  sets: 4,
  repsMin: 6,
  repsMax: 8,
});
const PUSH = item({
  exerciseId: 'push-up',
  exerciseName: 'Flexiones',
  role: 'strength',
  pattern: 'push',
  sets: 3,
  repsMin: 8,
  repsMax: 12,
});
const LEG = item({
  exerciseId: 'pistol',
  exerciseName: 'Pistol squat',
  role: 'strength',
  pattern: 'leg',
  sets: 3,
  repsMin: 5,
  repsMax: 8,
});

/** Plan de 4 semanas; la semana 2 (hoy, lunes 9) es la semana en curso. */
const plan: Plan = {
  skillId: 'front-lever',
  trainingDays: 4,
  mesocycleId: 'meso-1',
  startDate: '2026-03-02',
  skillStage: {
    order: 2,
    name: 'Front lever tuck',
    exerciseId: 'front-lever-tuck',
    criterion: { metric: 'seconds', target: 10, sets: 3 },
    notes: '',
  },
  microcycles: [
    { number: 1, sessions: [session(1, 'monday', '2026-03-02', [SKILL, PULL, PUSH])] },
    {
      number: 2,
      sessions: [
        session(1, 'monday', '2026-03-09', [SKILL, PULL, PUSH]),
        session(2, 'tuesday', '2026-03-10', [SKILL, LEG]),
      ],
    },
    { number: 3, sessions: [session(1, 'monday', '2026-03-16', [SKILL, PULL])] },
    { number: 4, sessions: [session(1, 'monday', '2026-03-23', [SKILL, LEG])] },
  ],
};

const TODAY = '2026-03-09';

/** Registro de la sesión del lunes de la semana 1: la marca como hecha. */
const doneLog: SessionLog = {
  id: 'log-1',
  exerciseId: 'pull-up',
  exerciseName: 'Dominadas',
  metric: 'reps',
  mesocycleId: 'meso-1',
  microcycleNumber: 1,
  sessionDay: 1,
  recordedAtUtc: '2026-03-02T18:00:00Z',
  sets: [],
};

/**
 * PlanView (ticket #84): la hoja *Plan · Periodización 4S* del rediseño. Cabecera con semana y
 * reparto, selector de semanas S1–S4, volumen semanal prescrito y sesiones de la semana.
 */
describe('PlanView', () => {
  it('shows the current week, the split, the volume and the sessions from the plan', async () => {
    await render(<PlanView plan={plan} today={TODAY} />);

    // Cabecera: semana en curso, reparto de patrones, título y objetivo.
    expect(screen.getByText('Semana 2 de 4')).toBeOnTheScreen();
    expect(screen.getByText('Empuje / Tirón / Pierna')).toBeOnTheScreen();
    expect(screen.getByText('PLAN 01 · HIPERTROFIA')).toBeOnTheScreen();
    expect(screen.getByText('4 sesiones semanales · Objetivo RIR 2')).toBeOnTheScreen();

    // Volumen semanal prescrito, series por patrón.
    expect(screen.getByTestId('plan-volume-push')).toHaveTextContent(/3 Empuje/);
    expect(screen.getByTestId('plan-volume-pull')).toHaveTextContent(/4 Tirón/);
    expect(screen.getByTestId('plan-volume-leg')).toHaveTextContent(/3 Pierna/);

    // Sesiones de la semana: la de hoy con su CTA y la programada con «Pendiente».
    expect(screen.getByText('Hoy · Lunes · S01')).toBeOnTheScreen();
    expect(screen.getByTestId('plan-session-2-1')).toHaveTextContent(/10 series · ~45 min/);
    expect(screen.getByTestId('plan-session-2-1-start')).toBeOnTheScreen();

    expect(screen.getByTestId('plan-session-2-2')).toHaveTextContent(/Martes · S02/);
    expect(screen.getByTestId('plan-session-2-2')).toHaveTextContent(/Pendiente/);

    expect(screen.getByText('0 / 2 completadas')).toBeOnTheScreen();
  });

  it('paints the week states from the calendar and the records', async () => {
    await render(<PlanView plan={plan} today={TODAY} logs={[doneLog]} />);

    expect(screen.getByTestId('plan-week-1')).toHaveTextContent(/Completada/);
    expect(screen.getByTestId('plan-week-2')).toHaveTextContent(/En curso/);
    expect(screen.getByTestId('plan-week-3')).toHaveTextContent(/RIR 1/);
    expect(screen.getByTestId('plan-week-4')).toHaveTextContent(/Descarga/);

    // La semana en curso es la seleccionada por defecto.
    expect(screen.getByTestId('plan-week-2').props.accessibilityState).toMatchObject({
      selected: true,
    });
    expect(screen.getByTestId('plan-week-1').props.accessibilityState).toMatchObject({
      selected: false,
    });
  });

  it('switches the week and shows its sessions, volume and completion', async () => {
    await render(<PlanView plan={plan} today={TODAY} logs={[doneLog]} />);

    await fireEvent.press(screen.getByTestId('plan-week-1'));

    expect(screen.getByText('Semana 1 de 4')).toBeOnTheScreen();
    expect(screen.getByTestId('plan-volume-push')).toHaveTextContent(/3 Empuje/);
    expect(screen.getByTestId('plan-volume-pull')).toHaveTextContent(/4 Tirón/);
    expect(screen.queryByTestId('plan-volume-leg')).not.toBeOnTheScreen();
    expect(screen.getByText('1 / 1 completadas')).toBeOnTheScreen();
  });

  it('marks a session as done from its record and opens the session on press', async () => {
    const onStartSession = jest.fn();
    await render(
      <PlanView plan={plan} today={TODAY} logs={[doneLog]} onStartSession={onStartSession} />,
    );

    await fireEvent.press(screen.getByTestId('plan-week-1'));

    expect(screen.getByTestId('plan-session-1-1')).toHaveTextContent(/Hecho/);
    expect(screen.getByTestId('plan-session-1-1')).toHaveTextContent(/10 series finalizadas/);

    await fireEvent.press(screen.getByTestId('plan-session-1-1'));
    expect(onStartSession).toHaveBeenCalledTimes(1);
  });

  it('starts the session of today from its button', async () => {
    const onStartSession = jest.fn();
    await render(<PlanView plan={plan} today={TODAY} onStartSession={onStartSession} />);

    await fireEvent.press(screen.getByTestId('plan-session-2-1-start'));
    expect(onStartSession).toHaveBeenCalledTimes(1);
  });

  it('shows the empty state when the mesocycle has no weeks', async () => {
    await render(<PlanView plan={{ ...plan, microcycles: [] }} />);

    expect(screen.getByTestId('plan-empty')).toBeOnTheScreen();
  });
});
