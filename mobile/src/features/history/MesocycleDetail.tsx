import { useCallback, useEffect, useState } from 'react';

import { fetchMesocycleDetail } from '@/api/mesocycleHistory';
import { messageOf } from '@/api/messageOf';
import type { Plan } from '@/api/plan';
import { Button } from '@/design-system/Button';
import { Banner, Loading } from '@/design-system/Feedback';
import { Stack } from '@/design-system/layout';
import { PlanView } from '@/features/plan/PlanView';

type DetailState =
  { status: 'loading' } | { status: 'ready'; plan: Plan } | { status: 'error'; message: string };

/**
 * Detalle de un mesociclo del historial (#27): abre el mesociclo pasado vía
 * `GET /plan/history/{id}` y pinta su plan tal y como se guardó, con la misma vista que el plan
 * actual. Es puramente presentacional; el id llega de la ruta.
 */
export function MesocycleDetail({ mesocycleId }: { mesocycleId: string }) {
  const [state, setState] = useState<DetailState>({ status: 'loading' });

  const load = useCallback(
    (signal?: AbortSignal) => {
      fetchMesocycleDetail(mesocycleId, signal)
        .then((plan) => setState({ status: 'ready', plan }))
        .catch((error: unknown) => {
          if (error instanceof Error && error.name === 'AbortError') {
            return;
          }
          setState({ status: 'error', message: messageOf(error) });
        });
    },
    [mesocycleId],
  );

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
    <Stack gap="sm" testID="mesocycle-detail">
      {state.status === 'loading' ? (
        <Loading label="Abriendo el mesociclo…" testID="mesocycle-detail-loading" />
      ) : null}

      {state.status === 'error' ? (
        <Stack gap="sm">
          <Banner role="error" message={state.message} testID="mesocycle-detail-error" />
          <Button onPress={retry} testID="mesocycle-detail-retry">
            Reintentar
          </Button>
        </Stack>
      ) : null}

      {state.status === 'ready' ? <PlanView plan={state.plan} /> : null}
    </Stack>
  );
}
