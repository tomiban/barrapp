import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { getApiBaseUrl } from '@/api/client';
import { fetchPing, type PingResponse } from '@/api/ping';
import { borders, colors, radius, spacing, typography } from '@/theme/tokens';

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
    <SafeAreaView style={styles.screen}>
      <View style={styles.content}>
        <Text style={styles.eyebrow}>Barrapp · esqueleto caminante</Text>
        <Text style={styles.title}>Conexión con el API</Text>

        {state.status === 'loading' && (
          <View style={styles.statusRow}>
            <ActivityIndicator color={colors.primary} />
            <Text style={styles.body}>Consultando {getApiBaseUrl()}/ping…</Text>
          </View>
        )}

        {state.status === 'success' && (
          <View style={styles.card}>
            <Text style={styles.answer}>{state.data.message}</Text>
            <Text style={styles.meta}>Servidor: {state.data.serverTimeUtc}</Text>
          </View>
        )}

        {state.status === 'error' && (
          <View style={[styles.card, styles.cardError]}>
            <Text style={styles.answer}>Sin conexión</Text>
            <Text style={styles.meta}>{state.message}</Text>
          </View>
        )}

        <Text style={styles.meta}>API: {getApiBaseUrl()}</Text>

        <Pressable
          accessibilityRole="button"
          onPress={retry}
          style={({ pressed }) => [styles.button, pressed && styles.buttonPressed]}
        >
          <Text style={styles.buttonLabel}>Reintentar</Text>
        </Pressable>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    backgroundColor: colors.canvas,
  },
  content: {
    flex: 1,
    justifyContent: 'center',
    gap: spacing.md,
    paddingHorizontal: spacing.lg,
  },
  eyebrow: {
    ...typography.eyebrow,
    color: colors.textMuted,
  },
  title: {
    ...typography.title,
    color: colors.text,
    marginBottom: spacing.sm,
  },
  statusRow: {
    alignItems: 'center',
    flexDirection: 'row',
    gap: spacing.sm,
  },
  card: {
    backgroundColor: colors.surface,
    borderColor: colors.border,
    borderWidth: borders.hairline,
    borderRadius: radius.md,
    gap: spacing.sm,
    padding: spacing.md,
  },
  cardError: {
    borderColor: colors.error,
  },
  answer: {
    ...typography.metric,
    color: colors.text,
  },
  body: {
    ...typography.body,
    color: colors.text,
  },
  meta: {
    ...typography.meta,
    color: colors.textMuted,
  },
  button: {
    alignItems: 'center',
    backgroundColor: colors.primary,
    borderRadius: radius.base,
    marginTop: spacing.md,
    paddingVertical: spacing.md,
  },
  buttonPressed: {
    opacity: 0.8,
  },
  buttonLabel: {
    ...typography.bodyBold,
    color: colors.onPrimary,
  },
});
