import { getApiBaseUrl } from '@/api/client';

/** Perfil del atleta: peso en kg y altura en cm. */
export type AthleteProfile = {
  weightKilograms: number;
  heightCentimeters: number;
};

/**
 * Límites que valida el API (spec 0001): peso 30–200 kg y altura 120–220 cm.
 * El cliente los replica para avisar antes de enviar; el API sigue siendo la
 * fuente de verdad.
 */
export const ATHLETE_PROFILE_LIMITS = {
  weightKilograms: { min: 30, max: 200 },
  heightCentimeters: { min: 120, max: 220 },
} as const;

/** Mensajes de error por campo del formulario de perfil. */
export type AthleteProfileFieldErrors = {
  weight?: string;
  height?: string;
};

/** Resultado de validar el borrador del formulario de perfil. */
export type AthleteProfileMeasurements = {
  weightKilograms: number | null;
  heightCentimeters: number | null;
  errors: AthleteProfileFieldErrors;
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

/**
 * Valida el borrador del formulario contra los límites del perfil. Devuelve las
 * medidas numéricas cuando son válidas (admite coma decimal) y un mensaje claro
 * por campo cuando no: distingue un valor ausente o no numérico de uno fuera de
 * rango.
 */
export function validateAthleteProfileMeasurements(
  weight: string,
  height: string,
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

  const errors: AthleteProfileFieldErrors = {};
  if (weightResult.error) {
    errors.weight = weightResult.error;
  }
  if (heightResult.error) {
    errors.height = heightResult.error;
  }

  return {
    weightKilograms: weightResult.value,
    heightCentimeters: heightResult.value,
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
