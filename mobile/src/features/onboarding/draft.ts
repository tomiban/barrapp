import {
  BASIC_EXERCISES,
  DEFAULT_TRAINING_WEEKDAYS,
  TRAINING_DAYS_LIMITS,
  type AthleteProfile,
  type TrainingWeekdayCode,
} from '@/api/athleteProfile';

/** Campo de antropometría editable en el paso 1. */
export type MeasurementField =
  'weightKilograms' | 'heightCentimeters' | 'armSpanCentimeters' | 'inseamCentimeters';

/**
 * Borrador del onboarding (spec 0003, ticket #90): lo que el atleta va
 * completando a lo largo de los tres pasos antes de enviarlo al API.
 */
export type OnboardingDraft = {
  /** Peso en kg (paso 1). */
  weightKilograms: number;
  /** Altura en cm (paso 1). */
  heightCentimeters: number;
  /** Envergadura en cm (paso 1). */
  armSpanCentimeters: number;
  /** Entrepierna en cm (paso 1). */
  inseamCentimeters: number;
  /** Máximo en repeticiones por código de ejercicio básico (paso 1). */
  maximums: Record<string, number>;
  /** Skill objetivo del catálogo (paso 2). */
  skillId: string;
  /** Días de la semana que entrena (paso 2). */
  weekdays: TrainingWeekdayCode[];
};

/**
 * Valores de partida de los steppers. Se arranca dentro de los rangos válidos
 * de 0001 en valores neutros en vez de en el mínimo: bajar desde 30 kg o subir
 * desde 50 cm a base de pulsaciones sería peor que partir de un punto medio y
 * ajustar. El atleta confirma el valor al continuar.
 */
export const ONBOARDING_DEFAULTS: OnboardingDraft = {
  weightKilograms: 70,
  heightCentimeters: 175,
  armSpanCentimeters: 175,
  inseamCentimeters: 80,
  maximums: Object.fromEntries(BASIC_EXERCISES.map((exercise) => [exercise.code, 0])),
  skillId: '',
  weekdays: [...DEFAULT_TRAINING_WEEKDAYS[TRAINING_DAYS_LIMITS.min]],
};

/** Sección del encabezado `BARRAS / <SECCIÓN>` de cada paso. */
export type OnboardingStep = {
  /** Número de paso, 1-based. */
  id: number;
  /** Sección del encabezado (`PASO 0n / 03 · <SECCIÓN>`). */
  section: string;
  /** Título grande del paso. */
  title: string;
};

/** Los tres pasos del onboarding, alineados con el mockup «Calistenia Progresiva». */
export const ONBOARDING_STEPS: readonly OnboardingStep[] = [
  { id: 1, section: 'PERFIL', title: 'CONFIGURA TU BASE' },
  { id: 2, section: 'OBJETIVO', title: 'ELIGE TU OBJETIVO' },
  { id: 3, section: 'PLAN', title: 'TU PLAN ESTÁ LISTO' },
];

/** Etiqueta de cabecera de un paso: `PASO 0n / 03 · <SECCIÓN>`. */
export function stepKicker(step: OnboardingStep): string {
  return `PASO 0${step.id} / 03 · ${step.section}`;
}

/** Construye el cuerpo de `PUT /profile` a partir del borrador (perfil del atleta). */
export function buildProfilePayload(draft: OnboardingDraft): AthleteProfile {
  return {
    weightKilograms: draft.weightKilograms,
    heightCentimeters: draft.heightCentimeters,
    armSpanCentimeters: draft.armSpanCentimeters,
    inseamCentimeters: draft.inseamCentimeters,
    trainingDays: draft.weekdays.length,
    trainingWeekdays: draft.weekdays,
    maximums: BASIC_EXERCISES.map((exercise) => ({
      exerciseCode: exercise.code,
      repetitions: draft.maximums[exercise.code] ?? 0,
    })),
  };
}
