import type { AthleteProfile } from '@/api/athleteProfile';
import type { Objective } from '@/api/objective';

/** Decisión de arranque: dónde abre la app en el primer render. */
export type StartupDecision = 'onboarding' | 'tabs';

/** Estado del atleta leído al arrancar, más si decidió omitir el onboarding. */
export type StartupInput = {
  /** Perfil guardado, o `null` si todavía no existe. */
  profile: AthleteProfile | null;
  /** Objetivo guardado, o `null` si todavía no existe. */
  objective: Objective | null;
  /** `true` si el atleta pulsó «Omitir por ahora» en esta sesión. */
  skipped: boolean;
};

/**
 * Decide dónde abre la app (spec 0003, ticket #90): en el **onboarding** si no
 * hay perfil u objetivo, y en las **pestañas** si los hay o si el atleta omitió
 * el onboarding. Es una función pura para poder cubrir la regla de la costura
 * sin montar el árbol de navegación.
 */
export function resolveStartup({ profile, objective, skipped }: StartupInput): StartupDecision {
  if (skipped) {
    return 'tabs';
  }

  return profile !== null && objective !== null ? 'tabs' : 'onboarding';
}
