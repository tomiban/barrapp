import { apiError, getApiBaseUrl } from '@/api/client';

/** Patrón de movimiento de un ejercicio básico. */
export type ExercisePattern = 'push' | 'pull' | 'legs';

/** Ejercicio básico del perfil: código estable, nombre para la UI y patrón. */
export type BasicExercise = {
  code: string;
  name: string;
  pattern: ExercisePattern;
};

/**
 * Los tres ejercicios básicos del MVP, espejo del catálogo del dominio (códigos en
 * inglés, nombres y patrones en español). El catálogo completo llega más adelante.
 */
export const BASIC_EXERCISES: readonly BasicExercise[] = [
  { code: 'push_up', name: 'Flexión', pattern: 'push' },
  { code: 'pull_up', name: 'Dominada', pattern: 'pull' },
  { code: 'squat', name: 'Sentadilla', pattern: 'legs' },
];

/** Patrones en el orden en que se agrupan los máximos en la UI. */
export const EXERCISE_PATTERNS: readonly ExercisePattern[] = ['push', 'pull', 'legs'];

/** Nombre para la UI de cada patrón. */
export const EXERCISE_PATTERN_LABELS: Record<ExercisePattern, string> = {
  push: 'Empuje',
  pull: 'Tirón',
  legs: 'Pierna',
};

/** Máximo del atleta en un ejercicio básico; 0 indica regresión. */
export type Maximum = {
  exerciseCode: string;
  repetitions: number;
};

/** Código estable de un día de la semana, como lo intercambia el API. */
export type TrainingWeekdayCode =
  'monday' | 'tuesday' | 'wednesday' | 'thursday' | 'friday' | 'saturday' | 'sunday';

/** Los siete días de la semana, de lunes a domingo. */
export const TRAINING_WEEKDAYS: readonly TrainingWeekdayCode[] = [
  'monday',
  'tuesday',
  'wednesday',
  'thursday',
  'friday',
  'saturday',
  'sunday',
];

/** Nombre para la UI de cada día de la semana. */
export const WEEKDAY_LABELS: Record<TrainingWeekdayCode, string> = {
  monday: 'Lunes',
  tuesday: 'Martes',
  wednesday: 'Miércoles',
  thursday: 'Jueves',
  friday: 'Viernes',
  saturday: 'Sábado',
  sunday: 'Domingo',
};

/**
 * Días por defecto de cada frecuencia (3–5): los que usa el API cuando el atleta todavía no ha
 * elegido cuáles, para que «hoy» sea siempre un día real de entrenamiento.
 */
export const DEFAULT_TRAINING_WEEKDAYS: Readonly<Record<number, readonly TrainingWeekdayCode[]>> = {
  3: ['monday', 'wednesday', 'friday'],
  4: ['monday', 'tuesday', 'thursday', 'friday'],
  5: ['monday', 'tuesday', 'wednesday', 'thursday', 'friday'],
};

/** Perfil del atleta: peso y medidas en cm, días de entrenamiento y máximos. */
export type AthleteProfile = {
  weightKilograms: number;
  heightCentimeters: number;
  armSpanCentimeters: number;
  inseamCentimeters: number;
  trainingDays: number;
  /**
   * Días de la semana que entrena. Si no se envían, el API usa los días por defecto de la
   * frecuencia (ver `DEFAULT_TRAINING_WEEKDAYS`).
   */
  trainingWeekdays?: TrainingWeekdayCode[];
  maximums: Maximum[];
};

/**
 * Límites de peso y medidas que valida el API (spec 0001): 30–200 kg, 120–220 cm,
 * envergadura 100–250 cm y entrepierna 50–130 cm. Los días de entrenamiento viven aparte, en
 * `TRAINING_DAYS_LIMITS`. El cliente los replica para avisar antes de enviar; el API sigue
 * siendo la fuente de verdad.
 */
export const BODY_MEASUREMENT_LIMITS = {
  weightKilograms: { min: 30, max: 200 },
  heightCentimeters: { min: 120, max: 220 },
  armSpanCentimeters: { min: 100, max: 250 },
  inseamCentimeters: { min: 50, max: 130 },
} as const;

/** Días de entrenamiento admitidos por semana (3–5). */
export const TRAINING_DAYS_LIMITS = { min: 3, max: 5 } as const;

/** Mensajes de error por campo del formulario de perfil. */
export type AthleteProfileFieldErrors = {
  weight?: string;
  height?: string;
  armSpan?: string;
  inseam?: string;
  trainingDays?: string;
};

/** Borrador del formulario de perfil: medidas, días de entrenamiento y errores por campo. */
export type AthleteProfileDraft = {
  weightKilograms: number | null;
  heightCentimeters: number | null;
  armSpanCentimeters: number | null;
  inseamCentimeters: number | null;
  trainingDays: number | null;
  errors: AthleteProfileFieldErrors;
};

/** Borrador de un máximo: código del ejercicio y texto introducido. */
export type MaximumDraft = {
  exerciseCode: string;
  value: string;
};

/** Mensajes de error de los máximos, por código de ejercicio. */
export type MaximumFieldErrors = Record<string, string>;

/** Resultado de validar los borradores de los máximos. */
export type MaximumsValidation = {
  maximums: Maximum[] | null;
  errors: MaximumFieldErrors;
};

/** Convierte el texto de una medida en número; `null` si no es un número válido. */
function parseMeasurement(value: string): number | null {
  const normalized = value.trim().replace(',', '.');
  if (!/^\d+(\.\d+)?$/.test(normalized)) {
    return null;
  }

  return Number(normalized);
}

/** Valida una medida: devuelve el número o el mensaje de error que le corresponde. */
function validateMeasurement(
  value: string,
  limits: { readonly min: number; readonly max: number },
  missingMessage: string,
  rangeMessage: string,
): { value: number | null; error?: string } {
  const parsed = parseMeasurement(value);
  if (parsed === null) {
    return { value: null, error: missingMessage };
  }

  if (parsed < limits.min || parsed > limits.max) {
    return { value: null, error: rangeMessage };
  }

  return { value: parsed };
}

/** Valida los días de entrenamiento: devuelve el número o el mensaje que le corresponde. */
function validateTrainingDays(trainingDays: number): { value: number | null; error?: string } {
  if (
    !Number.isInteger(trainingDays) ||
    trainingDays < TRAINING_DAYS_LIMITS.min ||
    trainingDays > TRAINING_DAYS_LIMITS.max
  ) {
    return {
      value: null,
      error: `Los días de entrenamiento deben estar entre ${TRAINING_DAYS_LIMITS.min} y ${TRAINING_DAYS_LIMITS.max}.`,
    };
  }

  return { value: trainingDays };
}

/**
 * Valida el borrador del formulario contra los límites del perfil. Devuelve las
 * medidas numéricas cuando son válidas (admite coma decimal) y un mensaje claro
 * por campo cuando no: distingue un valor ausente o no numérico de uno fuera de
 * rango. Los días de entrenamiento se validan contra 3–5.
 */
export function validateAthleteProfileDraft(
  weight: string,
  height: string,
  armSpan: string,
  inseam: string,
  trainingDays: number,
): AthleteProfileDraft {
  const {
    weightKilograms: weightLimits,
    heightCentimeters: heightLimits,
    armSpanCentimeters: armSpanLimits,
    inseamCentimeters: inseamLimits,
  } = BODY_MEASUREMENT_LIMITS;

  const weightResult = validateMeasurement(
    weight,
    weightLimits,
    'Introduce el peso en kg.',
    `El peso debe estar entre ${weightLimits.min} y ${weightLimits.max} kg.`,
  );
  const heightResult = validateMeasurement(
    height,
    heightLimits,
    'Introduce la altura en cm.',
    `La altura debe estar entre ${heightLimits.min} y ${heightLimits.max} cm.`,
  );
  const armSpanResult = validateMeasurement(
    armSpan,
    armSpanLimits,
    'Introduce la envergadura en cm.',
    `La envergadura debe estar entre ${armSpanLimits.min} y ${armSpanLimits.max} cm.`,
  );
  const inseamResult = validateMeasurement(
    inseam,
    inseamLimits,
    'Introduce la entrepierna en cm.',
    `La entrepierna debe estar entre ${inseamLimits.min} y ${inseamLimits.max} cm.`,
  );
  const trainingDaysResult = validateTrainingDays(trainingDays);

  const errors: AthleteProfileFieldErrors = {};
  if (weightResult.error) {
    errors.weight = weightResult.error;
  }
  if (heightResult.error) {
    errors.height = heightResult.error;
  }
  if (armSpanResult.error) {
    errors.armSpan = armSpanResult.error;
  }
  if (inseamResult.error) {
    errors.inseam = inseamResult.error;
  }
  if (trainingDaysResult.error) {
    errors.trainingDays = trainingDaysResult.error;
  }

  return {
    weightKilograms: weightResult.value,
    heightCentimeters: heightResult.value,
    armSpanCentimeters: armSpanResult.value,
    inseamCentimeters: inseamResult.value,
    trainingDays: trainingDaysResult.value,
    errors,
  };
}

/**
 * Valida los borradores de los máximos. Son repeticiones enteras (nada de decimales ni
 * comas): 0 es válido e indica regresión. Exige un borrador por cada ejercicio básico y
 * devuelve los máximos numéricos, o `null` junto con un mensaje claro por ejercicio.
 */
export function validateMaximumDrafts(drafts: readonly MaximumDraft[]): MaximumsValidation {
  const errors: MaximumFieldErrors = {};
  const maximums: Maximum[] = [];

  for (const exercise of BASIC_EXERCISES) {
    const draft = drafts.find((candidate) => candidate.exerciseCode === exercise.code);
    const value = draft?.value.trim() ?? '';

    if (value === '') {
      errors[exercise.code] = 'Introduce las repeticiones.';
      continue;
    }

    if (!/^-?\d+$/.test(value)) {
      errors[exercise.code] = 'Introduce un número entero de repeticiones.';
      continue;
    }

    const repetitions = Number(value);
    if (repetitions < 0) {
      errors[exercise.code] = 'El máximo no puede ser negativo.';
      continue;
    }

    maximums.push({ exerciseCode: exercise.code, repetitions });
  }

  return {
    maximums: Object.keys(errors).length === 0 ? maximums : null,
    errors,
  };
}

/**
 * Lee el perfil guardado (`GET /profile`). Devuelve `null` si todavía no hay perfil
 * (el API responde `404`), que es el estado inicial del onboarding.
 */
export async function fetchAthleteProfile(signal?: AbortSignal): Promise<AthleteProfile | null> {
  const response = await fetch(`${getApiBaseUrl()}/profile`, { signal });

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as AthleteProfile;
}

/** Crea o actualiza el perfil (`PUT /profile`). Devuelve lo que quedó persistido. */
export async function saveAthleteProfile(profile: AthleteProfile): Promise<AthleteProfile> {
  const response = await fetch(`${getApiBaseUrl()}/profile`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(profile),
  });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as AthleteProfile;
}
