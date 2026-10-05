import { getApiBaseUrl } from '@/api/client';

/** Perfil del atleta: peso en kg y altura en cm. */
export type AthleteProfile = {
  weightKilograms: number;
  heightCentimeters: number;
};

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
    throw new Error(`El API respondió ${response.status} ${response.statusText}`.trim());
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
    throw new Error(`El API respondió ${response.status} ${response.statusText}`.trim());
  }

  return (await response.json()) as AthleteProfile;
}
