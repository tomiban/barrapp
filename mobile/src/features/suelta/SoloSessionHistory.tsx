import { useCallback, useEffect, useState } from 'react';

import { EXERCISE_GROUP_LABELS } from '@/api/catalog/exercises';
import { messageOf } from '@/api/messageOf';
import { fetchSueltaHistory, type SueltaHistoryEntry } from '@/api/soloSessionHistory';
import { Button } from '@/design-system/Button';
import { Banner, EmptyState, Loading } from '@/design-system/Feedback';
import { Stack } from '@/design-system/layout';
import { ListRow } from '@/design-system/ListRow';
import { StatusBadge } from '@/design-system/StatusBadge';
import { Text } from '@/design-system/Text';

const ENERGY_LABELS: Record<SueltaHistoryEntry['energy'], string> = {
  baja: 'Baja',
  media: 'Media',
  alta: 'Alta',
};

/** Fecha del historial en formato corto español, p. ej. `5 oct`. */
function formatDate(value: string): string {
  return new Date(value).toLocaleDateString('es-ES', { day: 'numeric', month: 'short' });
}

/** Línea resumen de una suelta: foco resuelto, tiempo y energía. */
function summarize(entry: SueltaHistoryEntry): string {
  const focus =
    entry.skillName !== null
      ? `Skill · ${entry.skillName}`
      : EXERCISE_GROUP_LABELS[entry.pattern ?? 'push'];
  return `${focus} · ${entry.timeMinutes} min · energía ${ENERGY_LABELS[entry.energy]}`;
}

type HistoryState =
  | { status: 'loading' }
  | { status: 'ready'; entries: SueltaHistoryEntry[] }
  | { status: 'error'; message: string };

/**
 * Historial de sesiones sueltas (#29): muestra las sueltas guardadas vía `GET /sessions/suelta`,
 * etiquetadas como tales. Es puramente presentacional y deja claro que una suelta no cambia el
 * plan ni los máximos.
 */
export function SoloSessionHistory() {
  const [state, setState] = useState<HistoryState>({ status: 'loading' });

  const load = useCallback((signal?: AbortSignal) => {
    fetchSueltaHistory(signal)
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
    <Stack gap="sm" testID="suelta-history">
      <Stack gap="xs">
        <Text variant="labelTechnical" className="text-text-muted">
          HISTORIAL DE SESIONES SUELTAS
        </Text>
        <Text variant="bodySm" className="text-text-muted">
          Las sueltas se quedan aquí, etiquetadas como tales: no cambian tu plan ni tus máximos.
        </Text>
      </Stack>

      {state.status === 'loading' ? (
        <Loading label="Cargando historial…" size="small" testID="suelta-history-loading" />
      ) : null}

      {state.status === 'error' ? (
        <Stack gap="sm">
          <Banner role="error" message={state.message} testID="suelta-history-error" />
          <Button onPress={retry} testID="suelta-history-retry">
            Reintentar
          </Button>
        </Stack>
      ) : null}

      {state.status === 'ready' && state.entries.length === 0 ? (
        <EmptyState
          title="Todavía no hay sesiones sueltas"
          description="Genera una desde arriba y quedará guardada aquí."
          testID="suelta-history-empty"
        />
      ) : null}

      {state.status === 'ready' && state.entries.length > 0 ? (
        <Stack gap="sm">
          {state.entries.map((entry, index) => (
            <ListRow
              key={entry.id}
              title={summarize(entry)}
              subtitle={`${formatDate(entry.createdAtUtc)} · suelta`}
              trailing={
                <StatusBadge
                  role={entry.status === 'registrada' ? 'confirmed' : 'active'}
                  label={entry.status === 'registrada' ? 'Registrada' : 'Generada'}
                  showDot={false}
                  testID={`suelta-history-status-${entry.id}`}
                />
              }
              last={index === state.entries.length - 1}
              testID={`suelta-history-item-${entry.id}`}
            />
          ))}
        </Stack>
      ) : null}
    </Stack>
  );
}
