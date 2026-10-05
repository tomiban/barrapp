import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { getApiBaseUrl } from '@/api/client';
import { fetchPing, type PingResponse } from '@/api/ping';
import { DesignSystemPreview } from '@/components/DesignSystemPreview';

type PingState =
  | { status: 'loading' }
  | { status: 'success'; data: PingResponse }
  | { status: 'error'; message: string };

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : 'Error desconocido';
}

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
    <SafeAreaView edges={['top', 'left', 'right']} className="flex-1 bg-canvas">
      <ScrollView contentContainerClassName="grow justify-center gap-md px-lg">
        <Text className="font-mono-semibold text-label-technical text-text-muted">
          BARRAPP · ESQUELETO CAMINANTE
        </Text>
        <Text className="mb-sm font-display text-headline-lg text-text">Conexión con el API</Text>

        {state.status === 'loading' && (
          <View className="flex-row items-center gap-sm">
            <ActivityIndicator colorClassName="accent-primary" />
            <Text className="font-body text-body-md text-text">
              Consultando {getApiBaseUrl()}/ping…
            </Text>
          </View>
        )}

        {state.status === 'success' && (
          <View className="gap-sm rounded-md border border-border bg-surface p-md">
            <Text className="font-mono-bold text-headline-md text-text">{state.data.message}</Text>
            <Text className="font-body text-body-sm text-text-muted">
              Servidor: {state.data.serverTimeUtc}
            </Text>
          </View>
        )}

        {state.status === 'error' && (
          <View className="gap-sm rounded-md border border-error bg-surface p-md">
            <Text className="font-mono-bold text-headline-md text-text">Sin conexión</Text>
            <Text className="font-body text-body-sm text-text-muted">{state.message}</Text>
          </View>
        )}

        <Text className="font-body text-body-sm text-text-muted">API: {getApiBaseUrl()}</Text>

        <Pressable
          accessibilityRole="button"
          onPress={retry}
          className="mt-md items-center rounded-base bg-primary py-md active:opacity-80"
        >
          <Text className="font-display-semibold text-body-md text-on-primary">Reintentar</Text>
        </Pressable>

        <DesignSystemPreview />
      </ScrollView>
    </SafeAreaView>
  );
}
