import { useCallback, useEffect, useState } from 'react';

import { messageOf } from '@/api/messageOf';
import { fetchPlan, type Plan, type PlanSessionItem } from '@/api/plan';
import {
  deleteSessionLogItem,
  fetchSessionLogs,
  registerSessionLogItem,
  updateSessionLogItem,
  type RegisterSessionLogItemInput,
  type SessionLog,
  type SessionLogMetric,
} from '@/api/sessionLogs';
import { Button } from '@/design-system/Button';
import { Banner, Loading } from '@/design-system/Feedback';
import { CloudOff } from '@/design-system/Icon';
import { Stack } from '@/design-system/layout';
import { SectionHeader } from '@/design-system/ListRow';
import { Screen } from '@/design-system/Navigation';
import { AppHeader } from '@/features/navigation';
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

/** Rebuilds legacy queue entries only when their original mesocycle is still active. */
function legacyRequestFor(
  plan: Plan,
  entry: {
    mesocycleId: string | null;
    sessionDay: number;
    exerciseId: string;
    sets: SessionLogSetPayload[];
    clientId: string;
  },
): RegisterSessionLogItemInput | null {
  if (!plan.mesocycleId || entry.mesocycleId !== plan.mesocycleId) {
    return null;
  }

  const candidates: RegisterSessionLogItemInput[] = [];
  for (const microcycle of plan.microcycles) {
    for (const session of microcycle.sessions) {
      if (session.day !== entry.sessionDay) {
        continue;
      }
      const item = session.items.find((candidate) => candidate.exerciseId === entry.exerciseId);
      if (!item || !session.date) {
        continue;
      }
      const request = registrationFor(
        plan,
        { day: entry.sessionDay, microcycleNumber: microcycle.number, date: session.date },
        { exerciseId: entry.exerciseId, item, sets: entry.sets },
        entry.clientId,
      );
      if (request) {
        candidates.push(request);
      }
    }
  }

  return candidates.length === 1 ? candidates[0] : null;
}

function registrationFor(
  plan: Plan,
  session: { day: number; microcycleNumber: number; date: string | null },
  exercise: ExerciseSetsPayload,
  clientId?: string,
): RegisterSessionLogItemInput | null {
  if (!plan.mesocycleId || !session.date) {
    return null;
  }
  return {
    session: {
      kind: 'mesocycle',
      date: session.date,
      mesocycleId: plan.mesocycleId,
      microcycleNumber: session.microcycleNumber,
      sessionDay: session.day,
    },
    item: {
      exerciseId: exercise.item.exerciseId,
      role: exercise.item.role,
      pattern: exercise.item.pattern,
      prescribedSets: exercise.item.sets,
      repsMin: exercise.item.repsMin,
      repsMax: exercise.item.repsMax,
      holdSecondsMin: exercise.item.holdSecondsMin,
      holdSecondsMax: exercise.item.holdSecondsMax,
      note: exercise.item.note ?? null,
      sets: exercise.sets.map((set) => ({
        setNumber: set.setNumber,
        value: set.value,
        actualRir: set.effort,
        loadKg: null,
      })),
    },
    clientId,
  };
}

/**
 * Pantalla Entrenar: lee `GET /plan` (el motor lo genera a partir del perfil y el objetivo) y
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
          const result = await syncPendingSessionLogs(outbox, registerSessionLogItem, (entry) =>
            legacyRequestFor(planResult.plan, {
              ...entry,
              sets: entry.sets.map((set) => ({ ...set, effort: set.effort })),
            }),
          );
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
    async (
      session: { day: number; microcycleNumber: number; date: string | null },
      exercises: ExerciseSetsPayload[],
    ) => {
      setSaving(true);
      setFeedback(null);
      try {
        const plan = state.status === 'ready' ? state.plan : null;
        if (!plan) {
          throw new Error('No hay un plan disponible para registrar la sesión.');
        }
        const outbox = await getSessionLogOutbox();

        for (const exercise of exercises) {
          const request = registrationFor(plan, session, exercise);
          await outbox.enqueue({
            exerciseId: exercise.exerciseId,
            exerciseName: exercise.item.exerciseName,
            metric: metricOf(exercise.item),
            mesocycleId: plan.mesocycleId,
            microcycleNumber: session.microcycleNumber,
            sessionDay: session.day,
            sets: exercise.sets.map((set) => ({
              setNumber: set.setNumber,
              value: set.value,
              effort: set.effort ?? null,
            })),
            ...(request ? { request } : {}),
          });
        }

        const result = await syncPendingSessionLogs(outbox, registerSessionLogItem, (entry) =>
          legacyRequestFor(plan, {
            ...entry,
            sets: entry.sets.map((set) => ({ ...set, effort: set.effort })),
          }),
        );
        await refreshLogs();
        setFeedback({
          role: 'confirmed',
          message:
            result.remaining > 0
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

  const handleUpdate = useCallback(
    async (log: SessionLog, sets: SessionLogSetPayload[]) => {
      setSaving(true);
      setFeedback(null);
      try {
        if (!log.sessionLogId) {
          throw new Error('No se pudo identificar la sesión del registro.');
        }
        await updateSessionLogItem(
          log.sessionLogId,
          log.id,
          sets.map((set) => ({
            setNumber: set.setNumber,
            value: set.value,
            actualRir: set.effort,
            loadKg: null,
          })),
        );
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

  /** Elimina un ítem confirmado y recarga los registros. */
  const handleDelete = useCallback(
    async (log: SessionLog) => {
      setSaving(true);
      setFeedback(null);
      try {
        if (!log.sessionLogId) {
          throw new Error('No se pudo identificar la sesión del registro.');
        }
        await deleteSessionLogItem(log.sessionLogId, log.id);
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
