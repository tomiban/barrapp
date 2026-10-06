import { apiError, getApiBaseUrl } from '@/api/client';

/** Etapa actual del atleta en un skill. */
export type SkillProgress = {
  skillId: string;
  stageOrder: number;
};

/** Comprueba que un valor tiene la forma de una entrada de progreso. */
function isSkillProgress(value: unknown): value is SkillProgress {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const candidate = value as Partial<SkillProgress>;
  return typeof candidate.skillId === 'string' && typeof candidate.stageOrder === 'number';
}

/**
 * Lee la etapa actual del atleta en cada skill (`GET /catalog/progress`). El API devuelve una
 * entrada por skill del catálogo; un skill sin progreso guardado llega en la etapa 1.
 */
export async function fetchSkillProgress(signal?: AbortSignal): Promise<SkillProgress[]> {
  const response = await fetch(`${getApiBaseUrl()}/catalog/progress`, { signal });

  if (!response.ok) {
    throw await apiError(response);
  }

  const body = (await response.json()) as unknown;
  return Array.isArray(body) ? body.filter(isSkillProgress) : [];
}
