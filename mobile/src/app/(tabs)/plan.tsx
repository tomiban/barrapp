import { useCallback, useEffect, useState } from 'react';

import { messageOf } from '@/api/messageOf';
import { fetchPlan, type Plan } from '@/api/plan';
import { Button } from '@/design-system/Button';
import { Banner, Loading } from '@/design-system/Feedback';
import { CloudOff } from '@/design-system/Icon';
import { Stack } from '@/design-system/layout';
import { SectionHeader } from '@/design-system/ListRow';
import { Header, Screen } from '@/design-system/Navigation';
import { MesocycleHistory } from '@/features/history/MesocycleHistory';
import { PlanView } from '@/features/plan/PlanView';
import { openPlanStore } from '@/offline/planStore';
import { readPlan, type PlanReadResult } from '@/offline/readPlan';

type LoadState =
  | { status: 'loading' }
  | { status: 'ready'; plan: Plan; offline: boolean }
  | { status: 'error'; message: string };

/** Traduce el resultado de `readPlan` al estado de la pantalla. */
function toLoadState(result: PlanReadResult): LoadState {
  if (result.source === 'failure') {
    return { status: 'error', message: result.message };
  }
  return { status: 'ready', plan: result.plan, offline: result.source === 'cache' };
}

/**
 * Pantalla de planificación: lee `GET /plan` (el motor lo genera en el servidor a partir del
 * perfil y el objetivo guardados) y, si no hay conexión, sirve la copia guardada en el almacén
 * local. Cada plan fresco se guarda en la caché para la próxima lectura offline.
 */
export default function PlanScreen() {
  const [state, setState] = useState<LoadState>({ status: 'loading' });

  const load = useCallback((signal?: AbortSignal) => {
    void (async () => {
      try {
        const store = await openPlanStore();
        const result = await readPlan(fetchPlan, store, signal);
        setState(toLoadState(result));
      } catch (error) {
        if (error instanceof Error && error.name === 'AbortError') {
          return;
        }
        setState({ status: 'error', message: messageOf(error) });
      }
    })();
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

      {state.status === 'ready' ? (
        <Stack gap="sm">
          {state.offline ? (
            <Banner
              role="inactive"
              icon={CloudOff}
              message="Sin conexión: mostrando el plan guardado."
              testID="plan-offline-banner"
            />
          ) : null}
          <PlanView plan={state.plan} />

          <SectionHeader label="Historial de mesociclos" testID="plan-history-header" />
          <MesocycleHistory />
        </Stack>
      ) : null}
    </Screen>
  );
}
