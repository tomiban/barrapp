import type { Plan, PlanSession, PlanSessionItem } from '@/api/plan';
import type { SessionLog } from '@/api/sessionLogs';
import {
  currentWeekNumber,
  doneSessionKeys,
  sessionExercises,
  sessionTitle,
  weekStateLabel,
  weeklyVolume,
} from './planSheet';

/** Ítem de sesión con los valores por defecto; solo se pisa lo relevante del test. */
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
function session(day: number, date: string | null, items: PlanSessionItem[]): PlanSession {
  return { day, weekday: date ? 'monday' : null, date, items };
}

function makePlan(microcycles: Plan['microcycles']): Plan {
  return {
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
    microcycles,
  };
}

describe('weeklyVolume', () => {
  it('sums the prescribed sets per pattern in the canonical order', () => {
    const sessions = [
      session(1, null, [
        item({ exerciseId: 'a', exerciseName: 'A', role: 'strength', pattern: 'pull', sets: 4 }),
        item({ exerciseId: 'b', exerciseName: 'B', role: 'skill', sets: 3 }),
        item({ exerciseId: 'c', exerciseName: 'C', role: 'strength', pattern: 'push', sets: 3 }),
      ]),
      session(2, null, [
        item({ exerciseId: 'd', exerciseName: 'D', role: 'strength', pattern: 'pull', sets: 2 }),
        item({ exerciseId: 'e', exerciseName: 'E', role: 'strength', pattern: 'leg', sets: 5 }),
      ]),
    ];

    expect(weeklyVolume(sessions)).toEqual([
      { pattern: 'push', label: 'Empuje', sets: 3 },
      { pattern: 'pull', label: 'Tirón', sets: 6 },
      { pattern: 'leg', label: 'Pierna', sets: 5 },
    ]);
  });

  it('ignores items without a pattern (skill and core)', () => {
    const sessions = [
      session(1, null, [item({ exerciseId: 'a', exerciseName: 'A', role: 'skill', sets: 3 })]),
    ];

    expect(weeklyVolume(sessions)).toEqual([]);
  });
});

describe('sessionTitle', () => {
  it('composes the strength patterns with the skill', () => {
    const value = session(1, null, [
      item({ exerciseId: 'a', exerciseName: 'Front lever tuck', role: 'skill', sets: 3 }),
      item({
        exerciseId: 'b',
        exerciseName: 'Dominadas',
        role: 'strength',
        pattern: 'pull',
        sets: 4,
      }),
      item({
        exerciseId: 'c',
        exerciseName: 'Flexiones',
        role: 'strength',
        pattern: 'push',
        sets: 3,
      }),
    ]);

    expect(sessionTitle(value)).toBe('Empuje y Tirón + Front lever tuck');
  });

  it('falls back to the session number when there is neither skill nor pattern', () => {
    const value = session(2, null, [
      item({ exerciseId: 'a', exerciseName: 'Plancha', role: 'core', sets: 3 }),
    ]);

    expect(sessionTitle(value)).toBe('Sesión 02');
  });
});

describe('sessionExercises', () => {
  it('joins the exercise names with commas and a final «y»', () => {
    const value = session(1, null, [
      item({ exerciseId: 'a', exerciseName: 'Front lever tuck', role: 'skill', sets: 3 }),
      item({
        exerciseId: 'b',
        exerciseName: 'Dominadas',
        role: 'strength',
        pattern: 'pull',
        sets: 4,
      }),
      item({
        exerciseId: 'c',
        exerciseName: 'Flexiones',
        role: 'strength',
        pattern: 'push',
        sets: 3,
      }),
    ]);

    expect(sessionExercises(value)).toBe('Front lever tuck, Dominadas y Flexiones');
  });
});

describe('currentWeekNumber', () => {
  const weeks: Plan['microcycles'] = [
    { number: 1, sessions: [session(1, '2026-03-02', [])] },
    { number: 2, sessions: [session(1, '2026-03-09', [])] },
    { number: 3, sessions: [session(1, '2026-03-16', [])] },
    { number: 4, sessions: [session(1, '2026-03-23', [])] },
  ];

  it('returns the week that contains today', () => {
    expect(currentWeekNumber(makePlan(weeks), '2026-03-11')).toBe(2);
  });

  it('returns the last week that already passed when today is a rest day between weeks', () => {
    expect(currentWeekNumber(makePlan(weeks), '2026-03-13')).toBe(2);
  });

  it('returns the first week when the plan has no calendar dates', () => {
    const withoutDates: Plan['microcycles'] = [
      { number: 1, sessions: [session(1, null, [])] },
      { number: 2, sessions: [session(1, null, [])] },
    ];

    expect(currentWeekNumber(makePlan(withoutDates), '2026-03-11')).toBe(1);
  });
});

describe('doneSessionKeys', () => {
  const log = (
    microcycleNumber: number | null,
    sessionDay: number,
    mesocycleId: string | null,
  ): SessionLog => ({
    id: `log-${microcycleNumber}-${sessionDay}`,
    exerciseId: 'a',
    exerciseName: 'A',
    metric: 'reps',
    mesocycleId,
    microcycleNumber,
    sessionDay,
    recordedAtUtc: '2026-03-02T18:00:00Z',
    sets: [],
  });

  it('keys the sessions of the active mesocycle with a record', () => {
    const keys = doneSessionKeys('meso-1', [
      log(1, 1, 'meso-1'),
      log(2, 1, 'meso-1'),
      log(1, 1, 'otro-mesociclo'),
      log(null, 2, 'meso-1'),
    ]);

    expect(keys.has('meso-1:1:1')).toBe(true);
    expect(keys.has('meso-1:2:1')).toBe(true);
    expect(keys.size).toBe(2);
  });
});

describe('weekStateLabel', () => {
  it('names the state and falls back to the week intensity for pending weeks', () => {
    expect(weekStateLabel(1, 'done')).toBe('Completada');
    expect(weekStateLabel(2, 'current')).toBe('En curso');
    expect(weekStateLabel(3, 'upcoming')).toBe('RIR 1');
    expect(weekStateLabel(4, 'upcoming')).toBe('Descarga');
    expect(weekStateLabel(1, 'upcoming')).toBe('Pendiente');
  });
});
