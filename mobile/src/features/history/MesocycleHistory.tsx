import { useCallback, useEffect, useState } from 'react';
import { router } from 'expo-router';

import { fetchMesocycleHistory, type MesocycleSummary } from '@/api/mesocycleHistory';
import { messageOf } from '@/api/messageOf';
import { Button } from '@/design-system/Button';
import { Banner, EmptyState, Loading } from '@/design-system/Feedback';
import { ChevronRight } from '@/design-system/Icon';
import { Stack } from '@/design-system/layout';
import { ListRow } from '@/design-system/ListRow';
import { Text } from '@/design-system/Text';

/** Fecha del historial en formato corto español, p. ej. `1 nov`. */
function formatDate(value: string): string {
  return new Date(value).toLocaleDateString('es-ES', { day: 'numeric', month: 'short' });
}

/** Título de la fila de un mesociclo pasado, p. ej. `Planche · 3 días`. */
function summarize(entry: MesocycleSummary): string {
  return `${entry.skillName} · ${entry.trainingDays} días`;
}

type HistoryState =
  | { status: 'loading' }
  | { status: 'ready'; entries: MesocycleSummary[] }
  | { status: 'error'; message: string };

/**
 * Historial de mesociclos (#27): lista los mesociclos pasados (cerrados) vía `GET /plan/history`,
 * del más reciente al más antiguo. Al pulsar uno se abre su detalle (el plan tal y como se guardó)
 * en su propia pantalla. Es puramente presentacional: la lista llega ya cargada del API.
 */
export function MesocycleHistory() {
  const [state, setState] = useState<HistoryState>({ status: 'loading' });

  const load = useCallback((signal?: AbortSignal) => {
    fetchMesocycleHistory(signal)
      .then((entries) => setState({ status: 'ready', entries }))
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
    <Stack gap="sm" testID="mesocycle-history">
      <Stack gap="xs">
        <Text variant="labelTechnical" className="text-text-muted">
          MESOCICLOS ANTERIORES
        </Text>
        <Text variant="bodySm" className="text-text-muted">
          Los meses que ya cerraste, para revisar cómo entrenaste.
        </Text>
      </Stack>

      {state.status === 'loading' ? (
        <Loading label="Cargando historial…" size="small" testID="mesocycle-history-loading" />
      ) : null}

      {state.status === 'error' ? (
        <Stack gap="sm">
          <Banner role="error" message={state.message} testID="mesocycle-history-error" />
          <Button onPress={retry} testID="mesocycle-history-retry">
            Reintentar
          </Button>
        </Stack>
      ) : null}

      {state.status === 'ready' && state.entries.length === 0 ? (
        <EmptyState
          title="Todavía no hay mesociclos pasados"
          description="Cuando cierres un mes, quedará guardado aquí."
          testID="mesocycle-history-empty"
        />
      ) : null}

      {state.status === 'ready' && state.entries.length > 0 ? (
        <Stack gap="sm">
          {state.entries.map((entry, index) => (
            <ListRow
              key={entry.id}
              title={summarize(entry)}
              subtitle={`Cerrado el ${formatDate(entry.closedAtUtc)}`}
              trailing={<ChevronRight size={20} color="var(--color-text-muted)" />}
              onPress={() => router.push(`/plan-historial/${entry.id}`)}
              last={index === state.entries.length - 1}
              testID={`mesocycle-history-item-${entry.id}`}
            />
          ))}
        </Stack>
      ) : null}
    </Stack>
  );
}
