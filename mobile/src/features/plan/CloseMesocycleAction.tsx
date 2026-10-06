import { useState } from 'react';

import { Button } from '@/design-system/Button';
import { Banner } from '@/design-system/Feedback';
import { BottomSheet } from '@/design-system/Modal';
import { Stack } from '@/design-system/layout';
import { Text } from '@/design-system/Text';

/** Estado del cierre del mesociclo mostrado por la acción. */
export type CloseMesocycleState =
  | { status: 'idle' }
  | { status: 'running' }
  | { status: 'success' }
  | { status: 'error'; message: string };

/**
 * Acción de cierre del mesociclo (spec 0001, US-24; ticket #24): pide confirmación en un
 * BottomSheet («cerrar» es irreversible: pasa el mesociclo al historial y ajusta los máximos) y
 * luego dispara con `onClose`. Presentacional: recibe el estado del flujo y dispara la llamada;
 * la red y la recarga del plan viven fuera.
 */
export function CloseMesocycleAction({
  state,
  onClose,
  className,
  testID,
}: {
  state: CloseMesocycleState;
  onClose: () => void;
  className?: string;
  testID?: string;
}) {
  const [confirming, setConfirming] = useState(false);
  const busy = state.status === 'running';

  return (
    <Stack gap="sm" className={className} testID={testID}>
      <Button
        onPress={() => setConfirming(true)}
        disabled={busy}
        variant="secondary"
        testID={testID ? `${testID}-button` : undefined}
      >
        {busy ? 'Cerrando…' : 'Cerrar mesociclo'}
      </Button>

      <BottomSheet
        visible={confirming && !busy}
        onClose={() => setConfirming(false)}
        title="Cerrar el mesociclo"
        testID={testID ? `${testID}-confirm` : undefined}
      >
        <Stack gap="md">
          <Text variant="bodyMd">
            Se cerrará el mesociclo, se ajustarán tus máximos con las sesiones registradas y pasará
            al historial.
          </Text>

          <Stack direction="row" gap="sm">
            <Button
              onPress={() => setConfirming(false)}
              variant="tertiary"
              className="flex-1"
              testID={testID ? `${testID}-confirm-cancel` : undefined}
            >
              Cancelar
            </Button>
            <Button
              onPress={() => {
                setConfirming(false);
                onClose();
              }}
              className="flex-1"
              testID={testID ? `${testID}-confirm-accept` : undefined}
            >
              Cerrar mesociclo
            </Button>
          </Stack>
        </Stack>
      </BottomSheet>

      {state.status === 'success' ? (
        <Banner
          role="confirmed"
          message="Mesociclo cerrado: los máximos quedan ajustados para el próximo mes."
          testID={testID ? `${testID}-success` : undefined}
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
