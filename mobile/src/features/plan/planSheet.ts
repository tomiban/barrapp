import { EXERCISE_GROUP_LABELS, type ExerciseGroupCode } from '@/api/catalog/exercises';
import { WEEKDAY_LABELS } from '@/api/athleteProfile';
import type { Plan, PlanSession } from '@/api/plan';
import type { SessionLog } from '@/api/sessionLogs';

/*
 * Marcadores del mockup «Plan · Periodización 4S» que todavía no tienen dato real: el DTO de
 * `GET /plan` solo expone el número de semana y sus sesiones, así que el nombre del mesociclo, el
 * periodo y la intensidad (RIR) por semana no llegan del motor. `docs/specs/0003-pantallas.md`
 * recoge RIR 1 (semana 3) y descarga (semana 4) como estados del selector.
 * shortcut: contenido estático del mockup; sustituir por los datos reales del motor cuando se
 * expongan. Ver #117.
 */
export const MESOCYCLE_CODE = 'PLAN 01';
export const PERIODIZATION_LABEL = 'HIPERTROFIA';
export const TARGET_RIR = 2;
/** Duración estimada de una sesión; el DTO del plan no expone una estimación por sesión. Ver #119. */
export const SESSION_DURATION_MINUTES = 45;
export const WEEK_INTENSITY_LABEL: Readonly<Record<number, string>> = {
  3: 'RIR 1',
  4: 'Descarga',
};

/** Orden a dos dígitos, p. ej. `02`. */
export function twoDigits(value: number): string {
  return String(Math.max(Math.trunc(value), 0)).padStart(2, '0');
}

/** Une una lista con comas y «y» final, p. ej. `Flexiones, Fondos y Dominadas`. */
function joinSpanish(items: readonly string[]): string {
  if (items.length <= 1) {
    return items[0] ?? '';
  }

  return `${items.slice(0, -1).join(', ')} y ${items[items.length - 1]}`;
}

/** Clave estable de una sesión para cruzar el plan con los registros. */
export function sessionKey(
  mesocycleId: string | null,
  microcycleNumber: number,
  day: number,
): string {
  return `${mesocycleId ?? '-'}:${microcycleNumber}:${day}`;
}

/**
 * Claves de las sesiones del mesociclo activo que ya tienen algún registro.
 *
 * El listado de registros (`GET /session-logs`) no expone el flag `completed` de la sesión, así que
 * «tiene algún registro» es el proxy de «hecha» por ahora.
 * shortcut: proxy por presencia de registros; usar el flag `completed` del `SessionLog` cuando se
 * exponga. Ver #118.
 */
export function doneSessionKeys(
  mesocycleId: string | null,
  logs: readonly SessionLog[],
): ReadonlySet<string> {
  const keys = new Set<string>();

  for (const log of logs) {
    if (
      log.mesocycleId !== mesocycleId ||
      log.microcycleNumber === null ||
      log.microcycleNumber === undefined
    ) {
      continue;
    }
    keys.add(sessionKey(mesocycleId, log.microcycleNumber, log.sessionDay));
  }

  return keys;
}

/** Series prescritas de una sesión. */
export function sessionSets(session: PlanSession): number {
  return session.items.reduce((total, item) => total + item.sets, 0);
}

/** Orden canónico de los patrones en la fila de volumen. */
const PATTERN_ORDER: readonly ExerciseGroupCode[] = ['push', 'pull', 'leg', 'core', 'cardio'];

/** Volumen semanal prescrito: series por patrón, en el orden canónico. */
export type PatternVolume = { pattern: ExerciseGroupCode; label: string; sets: number };

/** Suma las series prescritas de la semana por patrón (los patrones sin series no aparecen). */
export function weeklyVolume(sessions: readonly PlanSession[]): PatternVolume[] {
  const totals = new Map<ExerciseGroupCode, number>();

  for (const session of sessions) {
    for (const item of session.items) {
      if (!item.pattern) {
        continue;
      }
      totals.set(item.pattern, (totals.get(item.pattern) ?? 0) + item.sets);
    }
  }

  return PATTERN_ORDER.filter((pattern) => totals.has(pattern)).map((pattern) => ({
    pattern,
    label: EXERCISE_GROUP_LABELS[pattern],
    sets: totals.get(pattern) ?? 0,
  }));
}

/** Título de la sesión a partir de sus ítems: patrones de fuerza + skill, p. ej. `Empuje y Tirón + Front lever`. */
export function sessionTitle(session: PlanSession): string {
  const patterns = weeklyVolume([session]).map((entry) => entry.label);
  const skill = session.items.find((item) => item.role === 'skill')?.exerciseName;

  if (patterns.length > 0 && skill) {
    return `${patterns.join(' y ')} + ${skill}`;
  }
  if (skill) {
    return skill;
  }
  if (patterns.length > 0) {
    return patterns.join(' y ');
  }

  return `Sesión ${twoDigits(session.day)}`;
}

/** Ejercicios de la sesión, p. ej. `Flexiones, Fondos y Dominadas`. */
export function sessionExercises(session: PlanSession): string {
  return joinSpanish(session.items.map((item) => item.exerciseName));
}

/** Etiqueta del día de la sesión, p. ej. `Lunes` o `Día 2` si el plan no trae calendario. */
export function sessionDayLabel(session: PlanSession): string {
  return session.weekday ? WEEKDAY_LABELS[session.weekday] : `Día ${session.day}`;
}

/** ¿Es la sesión de hoy? Compara la fecha de la sesión con hoy (`AAAA-MM-DD`). */
export function isSessionToday(session: PlanSession, today: string): boolean {
  return session.date !== null && session.date === today;
}

/**
 * Número de la semana en curso según las fechas del plan: la semana que contiene hoy o, si hoy cae
 * en un descanso, la última que ya pasó. Un plan sin fechas (sin calendario, #94) empieza en la 1.
 */
export function currentWeekNumber(plan: Plan, today: string): number {
  const weeks = plan.microcycles
    .map((microcycle) => ({
      number: microcycle.number,
      dates: microcycle.sessions
        .map((session) => session.date)
        .filter((date): date is string => date !== null),
    }))
    .filter((week) => week.dates.length > 0);

  if (weeks.length === 0) {
    return plan.microcycles[0]?.number ?? 1;
  }

  const containing = weeks.find((week) => week.dates.some((date) => date === today));
  if (containing) {
    return containing.number;
  }

  const past = weeks.filter((week) => week.dates.every((date) => date < today));
  if (past.length > 0) {
    return past[past.length - 1].number;
  }

  return weeks[0].number;
}

/** Estado visual de una semana en el selector. */
export type WeekState = 'done' | 'current' | 'upcoming';

/** Estado de la semana: la actual gana; si no, «hecha» si todas sus sesiones tienen registro. */
export function weekState(number: number, currentWeek: number, done: boolean): WeekState {
  if (number === currentWeek) {
    return 'current';
  }

  return done ? 'done' : 'upcoming';
}

/** Etiqueta del selector de semana, p. ej. `Completada`, `En curso` o `DESCARGA`. */
export function weekStateLabel(number: number, state: WeekState): string {
  if (state === 'done') {
    return 'Completada';
  }
  if (state === 'current') {
    return 'En curso';
  }

  return WEEK_INTENSITY_LABEL[number] ?? 'Pendiente';
}
