import { apiError, getApiBaseUrl } from '@/api/client';

/** Skill que se puede elegir como objetivo del mesociclo. */
export type ObjectiveSkill = {
  id: string;
  name: string;
};

/**
 * Los cuatro skills del catálogo, espejo de `knowledge/skills.json` (ids en inglés, nombres en
 * español). El selector de objetivo los ofrece en este orden.
 */
export const OBJECTIVE_SKILLS: readonly ObjectiveSkill[] = [
  { id: 'handstand', name: 'Pino' },
  { id: 'front-lever', name: 'Front lever' },
  { id: 'planche', name: 'Planche' },
  { id: 'pistol-squat', name: 'Pistol squat' },
];

/** Objetivo del mesociclo: el slug del skill elegido. */
export type Objective = {
  skillId: string;
};

/**
 * Lee el objetivo guardado (`GET /profile/objective`). Devuelve `null` si todavía no hay
 * objetivo (el API responde `404`), que es el estado inicial.
 */
export async function fetchObjective(signal?: AbortSignal): Promise<Objective | null> {
  const response = await fetch(`${getApiBaseUrl()}/profile/objective`, { signal });

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw await apiError(response);
  }

  const body = (await response.json()) as Partial<Objective> | null;
  if (!body || typeof body.skillId !== 'string') {
    return null;
  }

  return { skillId: body.skillId };
}

/** Fija el objetivo (`PUT /profile/objective`). Devuelve lo que quedó persistido. */
export async function saveObjective(skillId: string): Promise<Objective> {
  const response = await fetch(`${getApiBaseUrl()}/profile/objective`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ skillId }),
  });

  if (!response.ok) {
    throw await apiError(response);
  }

  return (await response.json()) as Objective;
}
