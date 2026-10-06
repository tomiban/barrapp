import { useCallback, useEffect, useState } from 'react';

import { messageOf } from '@/api/messageOf';
import { advanceSkillStage } from '@/api/catalog/progress';
import { closeMesocycle, fetchPlan, type Plan } from '@/api/plan';
import { Button } from '@/design-system/Button';
import { Banner, Loading } from '@/design-system/Feedback';
import { CloudOff } from '@/design-system/Icon';
import { Stack } from '@/design-system/layout';
import { SectionHeader } from '@/design-system/ListRow';
import { Header, Screen } from '@/design-system/Navigation';
import { StageAdvanceAction, type StageAdvanceState } from '@/features/plan/StageAdvanceAction';
import {
  CloseMesocycleAction,
  type CloseMesocycleState,
} from '@/features/plan/CloseMesocycleAction';
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
  const [advanceState, setAdvanceState] = useState<StageAdvanceState>({ status: 'idle' });
  const [closeState, setCloseState] = useState<CloseMesocycleState>({ status: 'idle' });

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

  const advance = useCallback(async () => {
    if (state.status !== 'ready' || state.offline) {
      return;
    }

    setAdvanceState({ status: 'running' });
    try {
      const result = await advanceSkillStage(state.plan.skillId);
      setAdvanceState(
        result.advanced ? { status: 'success', stageOrder: result.stageOrder } : { status: 'kept' },
      );

      // Si subió de etapa, el plan deja de reflejar la etapa actual: se recarga.
      if (result.advanced) {
        load();
      }
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        return;
      }
      setAdvanceState({ status: 'error', message: messageOf(error) });
    }
  }, [state, load]);

  const close = useCallback(async () => {
    if (state.status !== 'ready' || state.offline) {
      return;
    }

    setCloseState({ status: 'running' });
    try {
      await closeMesocycle();
      setCloseState({ status: 'success' });

      // Sin mesociclo activo, el próximo GET /plan regenera con los máximos ajustados.
      load();
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        return;
      }
      setCloseState({ status: 'error', message: messageOf(error) });
    }
  }, [state, load]);

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

          {!state.offline ? (
            <StageAdvanceAction state={advanceState} onAdvance={advance} testID="plan-advance" />
          ) : null}

          {!state.offline ? (
            <CloseMesocycleAction state={closeState} onClose={close} testID="plan-close" />
          ) : null}

          <SectionHeader label="Historial de mesociclos" testID="plan-history-header" />
          <MesocycleHistory />
        </Stack>
      ) : null}
    </Screen>
  );
}
