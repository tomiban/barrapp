import { useCallback, useEffect, useState } from 'react';

import { getApiBaseUrl } from '@/api/client';
import { fetchPing, type PingResponse } from '@/api/ping';
import { Button } from '@/design-system/Button';
import { Banner, Loading } from '@/design-system/Feedback';
import { Box, Stack } from '@/design-system/layout';
import { Header, Screen } from '@/design-system/Navigation';
import { StatusBadge } from '@/design-system/StatusBadge';
import { Text } from '@/design-system/Text';

type PingState =
  | { status: 'loading' }
  | { status: 'success'; data: PingResponse }
  | { status: 'error'; message: string };

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : 'Error desconocido';
}

/**
 * Pantalla de inicio del esqueleto caminante: comprueba la conexión con el API
 * (`GET /ping`) usando los componentes del design system (`Screen`/`Header`,
 * `Loading`, `StatusBadge`, `Banner`, `Button` y `Text`), sin estilos ad hoc.
 */
export default function HomeScreen() {
  const [state, setState] = useState<PingState>({ status: 'loading' });

  const showSuccess = useCallback((data: PingResponse) => {
    setState({ status: 'success', data });
  }, []);

  const showError = useCallback((error: unknown) => {
    if (error instanceof Error && error.name === 'AbortError') {
      return;
    }
    setState({ status: 'error', message: messageOf(error) });
  }, []);

  const load = useCallback(
    (signal?: AbortSignal) => {
      fetchPing(signal).then(showSuccess).catch(showError);
    },
    [showSuccess, showError],
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
    <Screen header={<Header title="Conexión con el API" />} testID="home-screen">
      <Stack gap="md">
        <Text variant="labelTechnical" className="text-text-muted">
          BARRAPP · ESQUELETO CAMINANTE
        </Text>

        {state.status === 'loading' ? (
          <Loading label={`Consultando ${getApiBaseUrl()}/ping…`} testID="home-loading" />
        ) : null}

        {state.status === 'success' ? (
          <Box className="gap-sm rounded-md border border-border bg-surface p-md">
            <StatusBadge role="confirmed" label="Conectado" testID="home-status" />
            <Text variant="headlineMd">{state.data.message}</Text>
            <Text variant="bodySm" className="text-text-muted">
              Servidor: {state.data.serverTimeUtc}
            </Text>
          </Box>
        ) : null}

        {state.status === 'error' ? (
          <Stack gap="sm">
            <StatusBadge role="error" label="Sin conexión" testID="home-status" />
            <Banner role="error" message={state.message} testID="home-error" />
          </Stack>
        ) : null}

        <Text variant="bodySm" className="text-text-muted">
          API: {getApiBaseUrl()}
        </Text>

        <Button onPress={retry} testID="home-retry">
          Reintentar
        </Button>
      </Stack>
    </Screen>
  );
}
