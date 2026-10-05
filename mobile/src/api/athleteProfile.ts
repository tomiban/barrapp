import { getApiBaseUrl } from '@/api/client';

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

/** Perfil del atleta: peso en kg, altura en cm, días de entrenamiento y máximos. */
export type AthleteProfile = {
  weightKilograms: number;
  heightCentimeters: number;
  trainingDays: number;
  maximums: Maximum[];
};

/**
 * Límites que valida el API (spec 0001): peso 30–200 kg, altura 120–220 cm y
 * días de entrenamiento 3–5. El cliente los replica para avisar antes de
 * enviar; el API sigue siendo la fuente de verdad.
 */
export const ATHLETE_PROFILE_LIMITS = {
  weightKilograms: { min: 30, max: 200 },
  heightCentimeters: { min: 120, max: 220 },
} as const;

/** Días de entrenamiento admitidos por semana (3–5). */
export const TRAINING_DAYS_LIMITS = { min: 3, max: 5 } as const;

/** Mensajes de error por campo del formulario de perfil. */
export type AthleteProfileFieldErrors = {
  weight?: string;
  height?: string;
  trainingDays?: string;
};

/** Resultado de validar el borrador del formulario de perfil. */
export type AthleteProfileMeasurements = {
  weightKilograms: number | null;
  heightCentimeters: number | null;
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
export function validateAthleteProfileMeasurements(
  weight: string,
  height: string,
  trainingDays: number,
): AthleteProfileMeasurements {
  const { weightKilograms: weightLimits, heightCentimeters: heightLimits } = ATHLETE_PROFILE_LIMITS;

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
  const trainingDaysResult = validateTrainingDays(trainingDays);

  const errors: AthleteProfileFieldErrors = {};
  if (weightResult.error) {
    errors.weight = weightResult.error;
  }
  if (heightResult.error) {
    errors.height = heightResult.error;
  }
  if (trainingDaysResult.error) {
    errors.trainingDays = trainingDaysResult.error;
  }

  return {
    weightKilograms: weightResult.value,
    heightCentimeters: heightResult.value,
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
 * Error uniforme cuando el API responde algo distinto de un 2xx. Si el cuerpo es
 * un Problem Details con `detail`, se propaga ese mensaje (el API ya lo redacta
 * para la persona); si no, se cae a un mensaje genérico.
 */
async function apiError(response: Response): Promise<Error> {
  try {
    const problem = (await response.json()) as { detail?: unknown } | null;
    if (problem && typeof problem.detail === 'string' && problem.detail.length > 0) {
      return new Error(problem.detail);
    }
  } catch {
    // El cuerpo no era JSON: se usa el mensaje genérico.
  }

  return new Error(`El API respondió ${response.status} ${response.statusText}`.trim());
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
