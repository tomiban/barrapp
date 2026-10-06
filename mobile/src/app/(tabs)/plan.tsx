import { useCallback, useEffect, useState } from 'react';

import { messageOf } from '@/api/messageOf';
import { fetchPlan, type Plan } from '@/api/plan';
import { Button } from '@/design-system/Button';
import { Banner, Loading } from '@/design-system/Feedback';
import { Stack } from '@/design-system/layout';
import { Header, Screen } from '@/design-system/Navigation';
import { PlanView } from '@/features/plan/PlanView';

type LoadState =
  { status: 'loading' } | { status: 'ready'; plan: Plan } | { status: 'error'; message: string };

/**
 * Pantalla de planificación: lee `GET /plan` (el motor lo genera en el servidor a partir del
 * perfil y el objetivo guardados) y delega el pintado semana a semana en `PlanView`.
 */
export default function PlanScreen() {
  const [state, setState] = useState<LoadState>({ status: 'loading' });

  const load = useCallback((signal?: AbortSignal) => {
    fetchPlan(signal)
      .then((plan) => setState({ status: 'ready', plan }))
      .catch((error: unknown) => {
        if (error instanceof Error && error.name === 'AbortError') {
          return;
        }
        setState({ status: 'error', message: messageOf(error) });
      });
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    load(controller.signal);
    return () => controller.abort();
  }, [load]);

  const retry = useCallback(() => {
    setState({ status: 'loading' });
    load();
  }, [load]);

  return (
    <Screen testID="plan-screen" header={<Header title="Plan" />}>
      {state.status === 'loading' ? (
        <Loading label="Generando el plan…" testID="plan-loading" />
      ) : null}

      {state.status === 'error' ? (
        <Stack gap="sm">
          <Banner role="error" message={state.message} testID="plan-error" />
          <Button onPress={retry} testID="plan-retry">
            Reintentar
          </Button>
        </Stack>
      ) : null}

      {state.status === 'ready' ? <PlanView plan={state.plan} /> : null}
    </Screen>
  );
}
