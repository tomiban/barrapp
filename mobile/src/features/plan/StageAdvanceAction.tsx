import { Button } from '@/design-system/Button';
import { Banner } from '@/design-system/Feedback';
import { Stack } from '@/design-system/layout';

/** Estado del avance de etapa mostrado por la acción. */
export type StageAdvanceState =
  | { status: 'idle' }
  | { status: 'running' }
  | { status: 'success'; stageOrder: number }
  | { status: 'kept' }
  | { status: 'error'; message: string };

/**
 * Acción de avance de etapa del skill objetivo (spec 0001, US-19; ticket #23): evalúa el criterio
 * con los registros y sube de etapa cuando se cumple en dos sesiones consecutivas. Presentacional:
 * recibe el estado y dispara con `onAdvance`; la llamada de red y la recarga del plan viven fuera.
 */
export function StageAdvanceAction({
  state,
  onAdvance,
  className,
  testID,
}: {
  state: StageAdvanceState;
  onAdvance: () => void;
  className?: string;
  testID?: string;
}) {
  const busy = state.status === 'running';

  return (
    <Stack gap="sm" className={className} testID={testID}>
      <Button
        onPress={onAdvance}
        disabled={busy}
        variant="secondary"
        testID={testID ? `${testID}-button` : undefined}
      >
        {busy ? 'Evaluando…' : 'Avanzar etapa'}
      </Button>

      {state.status === 'success' ? (
        <Banner
          role="confirmed"
          message={`¡Subiste a la etapa ${state.stageOrder}!`}
          testID={testID ? `${testID}-success` : undefined}
        />
      ) : null}

      {state.status === 'kept' ? (
        <Banner
          role="inactive"
          message="El criterio aún no se cumple en dos sesiones consecutivas."
          testID={testID ? `${testID}-kept` : undefined}
        />
      ) : null}

      {state.status === 'error' ? (
        <Banner
          role="error"
          message={state.message}
          testID={testID ? `${testID}-error` : undefined}
        />
      ) : null}
    </Stack>
  );
}
