import { useCallback, useEffect, useState } from 'react';

import { messageOf } from '@/api/messageOf';
import { fetchPlan, type Plan, type PlanSessionItem } from '@/api/plan';
import {
  deleteSessionLog,
  fetchSessionLogs,
  registerSessionLog,
  updateSessionLog,
  type SessionLog,
  type SessionLogMetric,
} from '@/api/sessionLogs';
import { Button } from '@/design-system/Button';
import { Banner, Loading } from '@/design-system/Feedback';
import { CloudOff } from '@/design-system/Icon';
import { Stack } from '@/design-system/layout';
import { SectionHeader } from '@/design-system/ListRow';
import { AppHeader, Screen } from '@/design-system/Navigation';
import {
  SessionLoggingView,
  type ExerciseSetsPayload,
  type SaveFeedback,
  type SessionLogSetPayload,
} from '@/features/sessionLog/SessionLoggingView';
import { SoloSessionView } from '@/features/suelta/SoloSessionView';
import { SoloSessionHistory } from '@/features/suelta/SoloSessionHistory';
import { openPlanStore } from '@/offline/planStore';
import { readPlan } from '@/offline/readPlan';
import {
  mergeSessionLogs,
  openSessionLogOutbox,
  type SessionLogOutbox,
} from '@/offline/sessionLogOutbox';
import { syncPendingSessionLogs } from '@/offline/syncSessionLogs';

type LoadState =
  | { status: 'loading' }
  | { status: 'ready'; plan: Plan; logs: SessionLog[]; offline: boolean }
  | { status: 'error'; message: string };

/** La cola de sincronización se abre una sola vez y se reutiliza en cargas y guardados. */
let outboxPromise: Promise<SessionLogOutbox> | null = null;
function getSessionLogOutbox(): Promise<SessionLogOutbox> {
  outboxPromise ??= openSessionLogOutbox();
  return outboxPromise;
}

/** Un `fetch` abortado se propaga tal cual para que el llamador (la pantalla) lo ignore. */
function isAbortError(error: unknown): boolean {
  return error instanceof Error && error.name === 'AbortError';
}

/** Lee los registros del servidor; sin conexión devuelve la lista vacía (se completan con la cola local). */
async function fetchLogsOrEmpty(signal?: AbortSignal): Promise<SessionLog[]> {
  try {
    return await fetchSessionLogs(signal);
  } catch (error) {
    if (isAbortError(error)) {
      throw error;
    }
    return [];
  }
}

/** La unidad del valor se deriva del ejercicio: holds en segundos, lo demás en reps. */
function metricOf(item: PlanSessionItem): SessionLogMetric {
  return item.holdSecondsMin !== null || item.holdSecondsMax !== null ? 'seconds' : 'reps';
}

/** Busca el ítem del plan de un ejercicio en una sesión concreta (para dar nombre y unidad a la cola). */
function findPlanItem(plan: Plan, day: number, exerciseId: string): PlanSessionItem | undefined {
  for (const microcycle of plan.microcycles) {
    for (const session of microcycle.sessions) {
      if (session.day !== day) {
        continue;
      }
      return session.items.find((item) => item.exerciseId === exerciseId);
    }
  }
  return undefined;
}

/**
 * Pantalla Entreno (`/entreno`, antes la pestaña «Entrenar»): lee `GET /plan` (el motor lo genera a partir del perfil y el objetivo) y
 * `GET /session-logs`, y delega el registro set a set en `SessionLoggingView` (spec 0001, US-34).
 * Sin conexión sirve el plan de la caché local (#25) y, en vez de fallar, encola los registros en
 * la outbox local (#26): siguen apareciendo en la sesión y se suben «al recuperar la conexión» (al
 * cargar o al siguiente guardado), sin duplicados. Debajo queda el generador de sesión suelta
 * (#28), que no depende del plan.
 */
export default function TrainScreen() {
  const [state, setState] = useState<LoadState>({ status: 'loading' });
  const [saving, setSaving] = useState(false);
  const [feedback, setFeedback] = useState<SaveFeedback | null>(null);

  const load = useCallback((signal?: AbortSignal) => {
    void (async () => {
      try {
        const planStore = await openPlanStore();
        const planResult = await readPlan(fetchPlan, planStore, signal);
        if (planResult.source === 'failure') {
          setState({ status: 'error', message: planResult.message });
          return;
        }

        const serverLogs = await fetchLogsOrEmpty(signal);
        const outbox = await getSessionLogOutbox();
        const pending = await outbox.listPending();
        const offline = planResult.source === 'cache';
        setState({
          status: 'ready',
          plan: planResult.plan,
          logs: mergeSessionLogs(serverLogs, pending),
          offline,
        });

        // «Al recuperar la conexión» (heurística práctica): al cargar, si la cola no está vacía, se sube.
        if (pending.length > 0) {
          const result = await syncPendingSessionLogs(outbox, registerSessionLog);
          if (result.synced > 0) {
            const freshLogs = await fetchLogsOrEmpty(signal);
            const freshPending = await outbox.listPending();
            setState((current) =>
              current.status === 'ready'
                ? { ...current, logs: mergeSessionLogs(freshLogs, freshPending), offline: false }
                : current,
            );
          }
        }
      } catch (error) {
        if (isAbortError(error)) {
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
    setFeedback(null);
    load();
  }, [load]);

  /** Vuelve a leer los registros (servidor + cola local) y refresca el estado de la sesión. */
  const refreshLogs = useCallback(async () => {
    const [serverLogs, pending] = await Promise.all([
      fetchLogsOrEmpty(),
      getSessionLogOutbox().then((outbox) => outbox.listPending()),
    ]);
    setState((current) =>
      current.status === 'ready'
        ? {
            ...current,
            logs: mergeSessionLogs(serverLogs, pending),
            offline: pending.length > 0,
          }
        : current,
    );
  }, []);

  const handleSave = useCallback(
    async (day: number, exercises: ExerciseSetsPayload[]) => {
      setSaving(true);
      setFeedback(null);
      try {
        const plan = state.status === 'ready' ? state.plan : null;
        const outbox = await getSessionLogOutbox();
        let queuedCount = 0;

        for (const exercise of exercises) {
          try {
            await registerSessionLog({
              exerciseId: exercise.exerciseId,
              sessionDay: day,
              sets: exercise.sets,
            });
          } catch {
            // Sin conexión (o el guardado no llegó): se encola para subirlo al recuperar la red.
            queuedCount += 1;
            const item = plan ? findPlanItem(plan, day, exercise.exerciseId) : undefined;
            await outbox.enqueue({
              exerciseId: exercise.exerciseId,
              exerciseName: item?.exerciseName ?? exercise.exerciseId,
              metric: item ? metricOf(item) : null,
              sessionDay: day,
              sets: exercise.sets.map((set) => ({
                setNumber: set.setNumber,
                value: set.value,
                effort: set.effort ?? null,
              })),
            });
          }
        }

        // El propio guardado aprovecha para subir la cola pendiente: sin duplicados.
        await syncPendingSessionLogs(outbox, registerSessionLog);
        await refreshLogs();
        setFeedback({
          role: 'confirmed',
          message:
            queuedCount > 0
              ? 'Guardado sin conexión. Se subirá al recuperar la red.'
              : 'Registro de la sesión guardado.',
        });
      } catch (error) {
        setFeedback({ role: 'error', message: messageOf(error) });
      } finally {
        setSaving(false);
      }
    },
    [refreshLogs, state],
  );

  /** Edita un registro confirmado vía `PUT /session-logs/{id}` y recarga los registros. */
  const handleUpdate = useCallback(
    async (logId: string, sets: SessionLogSetPayload[]) => {
      setSaving(true);
      setFeedback(null);
      try {
        await updateSessionLog(logId, { sets });
        await refreshLogs();
        setFeedback({ role: 'confirmed', message: 'Registro actualizado.' });
      } catch (error) {
        setFeedback({ role: 'error', message: messageOf(error) });
      } finally {
        setSaving(false);
      }
    },
    [refreshLogs],
  );

  /** Elimina un registro confirmado vía `DELETE /session-logs/{id}` y recarga los registros. */
  const handleDelete = useCallback(
    async (logId: string) => {
      setSaving(true);
      setFeedback(null);
      try {
        await deleteSessionLog(logId);
        await refreshLogs();
        setFeedback({ role: 'confirmed', message: 'Registro borrado.' });
      } catch (error) {
        setFeedback({ role: 'error', message: messageOf(error) });
      } finally {
        setSaving(false);
      }
    },
    [refreshLogs],
  );

  return (
    <Screen header={<AppHeader section="Entreno" />} testID="train-screen">
      {state.status === 'loading' ? (
        <Loading label="Preparando la sesión…" testID="train-loading" />
      ) : null}

      {state.status === 'error' ? (
        <Stack gap="sm">
          <Banner role="error" message={state.message} testID="train-error" />
          <Button onPress={retry} testID="train-retry">
            Reintentar
          </Button>
        </Stack>
      ) : null}

      {state.status === 'ready' ? (
        <Stack gap="md">
          {state.offline ? (
            <Banner
              role="inactive"
              icon={CloudOff}
              message="Sin conexión: los registros se guardarán en la cola."
              testID="train-offline-banner"
            />
          ) : null}
          <SessionLoggingView
            plan={state.plan}
            logs={state.logs}
            saving={saving}
            feedback={feedback}
            onSave={handleSave}
            onUpdate={handleUpdate}
            onDelete={handleDelete}
          />
        </Stack>
      ) : null}

      <Stack gap="sm">
        <SectionHeader label="Sesión suelta" testID="train-suelta-header" />
        <SoloSessionView />
        <SoloSessionHistory />
      </Stack>
    </Screen>
  );
}
