import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';

import { fetchAthleteProfile } from '@/api/athleteProfile';
import { fetchObjective } from '@/api/objective';

import { resolveStartup, type StartupDecision } from './startup';

/**
 * Valor del contexto de arranque: dónde debe abrir la app y cómo dejar que el
 * onboarding termine u omita.
 */
export type StartupContextValue = {
  /** `loading` mientras se leen perfil y objetivo; `ready` cuando ya hay decisión. */
  status: 'loading' | 'ready';
  /** Dónde abre la app: `onboarding` o `tabs`. */
  decision: StartupDecision;
  /** Marca que el onboarding terminó (perfil y objetivo guardados) → pestañas. */
  completeOnboarding: () => void;
  /** Marca un «Omitir por ahora» → pestañas sin bloquear. */
  skipOnboarding: () => void;
};

const StartupContext = createContext<StartupContextValue | null>(null);

/**
 * Lee el perfil y el objetivo una única vez al arrancar y decide si la app abre
 * en el onboarding o en las pestañas (spec 0003, ticket #90).
 *
 * Es tolerante a fallos: si el API no responde al arrancar, deja entrar por el
 * onboarding (donde «Omitir por ahora» no bloquea) en vez de quedarse colgada.
 */
export function StartupProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<'loading' | 'ready'>('loading');
  const [loadedDecision, setLoadedDecision] = useState<StartupDecision>('onboarding');
  const [skipped, setSkipped] = useState(false);
  const [completed, setCompleted] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    let active = true;

    void (async () => {
      try {
        const [profile, objective] = await Promise.all([
          fetchAthleteProfile(controller.signal),
          fetchObjective(controller.signal),
        ]);
        if (!active) {
          return;
        }
        setLoadedDecision(resolveStartup({ profile, objective, skipped: false }));
      } catch {
        // Sin respuesta del API, el onboarding es la entrada segura.
        if (active) {
          setLoadedDecision('onboarding');
        }
      } finally {
        if (active) {
          setStatus('ready');
        }
      }
    })();

    return () => {
      active = false;
      controller.abort();
    };
  }, []);

  const completeOnboarding = useCallback(() => {
    setCompleted(true);
  }, []);

  const skipOnboarding = useCallback(() => {
    setSkipped(true);
  }, []);

  const value = useMemo<StartupContextValue>(
    () => ({
      status,
      decision: skipped || completed ? 'tabs' : loadedDecision,
      completeOnboarding,
      skipOnboarding,
    }),
    [status, skipped, completed, loadedDecision, completeOnboarding, skipOnboarding],
  );

  return <StartupContext.Provider value={value}>{children}</StartupContext.Provider>;
}

/** Acceso al contexto de arranque; falla si no hay `StartupProvider` arriba. */
export function useStartup(): StartupContextValue {
  const value = useContext(StartupContext);
  if (!value) {
    throw new Error('useStartup debe usarse dentro de <StartupProvider>');
  }
  return value;
}
